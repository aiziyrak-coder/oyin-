# NewWorld test sharing. Own processes only; existing servers and server.txt are never overwritten.
param([switch]$Build, [switch]$LocalOnly, [switch]$SkipPackage, [switch]$NoWait)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
$gameDir = Join-Path $ProjectRoot 'Builds\LobbyV2'
$gameFile = Join-Path $gameDir 'CraDev.exe'
$server = $null; $tunnel = $null
$savedPort = $env:PORT; $savedTrust = $env:TRUST_PROXY; $savedHost = $env:HOST
$runId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,6)
$logDir = Join-Path $ProjectRoot 'Logs'
New-Item -ItemType Directory -Force $logDir | Out-Null
try {
    if (-not $SkipPackage) {
        $status = Get-BuildStatus
        if ($Build -or $status.Stale -or -not (Test-Path -LiteralPath $gameFile)) {
            if ((Invoke-GameBuild) -ne 0) { throw 'Unity build failed. See Logs/lobby-v2-build.log.' }
        }
    }
    # Do not kill the process on 8080: it may be an unrelated program or another player's server.
    $port = $null
    foreach ($candidate in @(8080) + (8090..8110)) {
        $probe = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, $candidate)
        try { $probe.Start(); $port = $candidate; break } catch { } finally { $probe.Stop() }
    }
    if (-not $port) { throw 'No free local test port (8080, 8090..8110).' }
    $env:PORT = "$port"; $env:HOST = '127.0.0.1'; $env:TRUST_PROXY = $(if ($LocalOnly) { '0' } else { '1' })
    $nodeFile = (Get-Command node -ErrorAction Stop).Source
    if (-not (Ensure-ServerDependencies $nodeFile)) { throw 'Server dependencies are unavailable.' }
    $server = Start-Process -FilePath $nodeFile -ArgumentList '--disable-warning=ExperimentalWarning src/server.js' `
        -WorkingDirectory (Join-Path $ProjectRoot 'Server') -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $logDir "share-server-$runId.log") -RedirectStandardError (Join-Path $logDir "share-server-$runId.err.log")
    $localUrl = "http://127.0.0.1:$port"
    $ready = $false
    for ($i = 0; $i -lt 40; $i++) {
        if ($server.HasExited) { break }
        try { $r = Invoke-RestMethod "$localUrl/health" -TimeoutSec 1; if ($r.ok) { $ready = $true; break } } catch { }
        Start-Sleep -Milliseconds 250
    }
    if (-not $ready) { throw 'Test server did not become healthy. See Logs/share-server-*.' }
    $url = $localUrl
    if (-not $LocalOnly) {
        $binDir = Join-Path $PSScriptRoot 'bin'
        $cf = Join-Path $binDir 'cloudflared.exe'
        if (-not (Test-Path -LiteralPath $cf)) {
            New-Item -ItemType Directory -Force $binDir | Out-Null
            Invoke-WebRequest 'https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe' -OutFile $cf -UseBasicParsing
        }
        $cfLog = Join-Path $logDir "share-tunnel-$runId.log"
        $tunnel = Start-Process -FilePath $cf -ArgumentList "tunnel --no-autoupdate --url $localUrl --logfile `"$cfLog`"" -WindowStyle Hidden -PassThru
        $url = $null
        for ($i = 0; $i -lt 120 -and -not $url; $i++) {
            if ($tunnel.HasExited) { break }
            Start-Sleep -Milliseconds 500
            if (Test-Path -LiteralPath $cfLog) {
                $match = Select-String -LiteralPath $cfLog -Pattern 'https://[a-z0-9-]+\.trycloudflare\.com' | Select-Object -First 1
                if ($match) { $url = $match.Matches[0].Value }
            }
        }
        if (-not $url) { throw 'Tunnel did not provide an HTTPS address.' }
        $reachable = $false
        for ($i = 0; $i -lt 15; $i++) {
            try { $r = Invoke-RestMethod "$url/health" -TimeoutSec 3; if ($r.ok) { $reachable = $true; break } } catch { }
            Start-Sleep -Seconds 1
        }
        if (-not $reachable) { throw 'Public tunnel health check failed; no broken ZIP will be produced.' }
    }
    Write-Host "[NewWorld] Test server: $url (PID $($server.Id))"
    if (-not $SkipPackage) {
        # Unique output paths preserve previous packages. No recursive delete or local server.txt mutation.
        $shareDir = Join-Path $ProjectRoot "Builds\NewWorld-test-$runId"
        $zip = "$shareDir.zip"
        New-Item -ItemType Directory -Path $shareDir | Out-Null
        foreach ($item in Get-ChildItem -LiteralPath $gameDir) {
            if ($item.Name -like '*DoNotShip*' -or $item.Name -eq 'server.txt' -or $item.Extension -in '.log','.pdb') { continue }
            Copy-Item -LiteralPath $item.FullName -Destination $shareDir -Recurse
        }
        Set-Content -LiteralPath (Join-Path $shareDir 'server.txt') -Value $url -Encoding ASCII
        Compress-Archive -Path (Join-Path $shareDir '*') -DestinationPath $zip
        Write-Host "[NewWorld] ZIP: $zip"
    }
    if (-not $NoWait) {
        if (-not $SkipPackage) { Start-Process -FilePath $gameFile -ArgumentList "-server $url" -WorkingDirectory $gameDir }
        Write-Host '[NewWorld] Keep this window open while testing. Press Enter to stop this test server.'
        [void](Read-Host)
    }
} finally {
    foreach ($owned in @($tunnel, $server)) {
        if ($owned -and -not $owned.HasExited) { $owned.Kill(); $owned.WaitForExit(5000) | Out-Null }
    }
    $env:PORT = $savedPort; $env:TRUST_PROXY = $savedTrust; $env:HOST = $savedHost
}
