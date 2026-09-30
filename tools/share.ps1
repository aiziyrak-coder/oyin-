# NewWorld friend test. Background sharing survives packaging/console closure.
# Stop explicitly with Stop-Share-Test.cmd. Never stop a borrowed lobby server.
#Requires -Version 5.1
param([switch]$Build, [switch]$LocalOnly, [switch]$SkipPackage, [switch]$NoWait,
      [switch]$Stop, [switch]$Status)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
. (Join-Path $PSScriptRoot 'common.ps1')
. (Join-Path $PSScriptRoot 'share-package.ps1')
$gameDir = Join-Path $ProjectRoot 'Builds\LobbyV2'
$shareStateFile = Join-Path $LogDir 'share-state.json'
$runId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,6)
New-Item -ItemType Directory -Force $LogDir | Out-Null

function Get-ShareProcessInfo($Process) {
    return [pscustomobject]@{ Pid = $Process.Id; Started = (Get-ProcessStartTicks $Process); Name = $Process.ProcessName }
}
function Test-ShareProcess($Info) {
    if (-not $Info -or $Info.Name -notin @('node','CraDevServer','cloudflared')) { return $false }
    $p = Get-Process -Id ([int]$Info.Pid) -ErrorAction SilentlyContinue
    return ($p -and $p.ProcessName -eq $Info.Name -and (Get-ProcessStartTicks $p) -eq [string]$Info.Started)
}
function Read-ShareState {
    if (-not (Test-Path -LiteralPath $shareStateFile)) { return $null }
    try {
        $value = Get-Content -LiteralPath $shareStateFile -Raw | ConvertFrom-Json
        # Migrate PowerShell 5.1's decorated Get-Content string from an earlier test run.
        if ($value.BuildStamp -and $value.BuildStamp -isnot [string] -and $value.BuildStamp.PSObject.Properties['value']) {
            $value.BuildStamp = [string]$value.BuildStamp.value
        }
        return $value
    } catch { return $null }
}
function Save-ShareState($Value) {
    $temporary = Join-Path $LogDir "share-state-$runId.tmp"
    $Value | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $temporary -Encoding UTF8
    Move-Item -LiteralPath $temporary -Destination $shareStateFile -Force
}
function Stop-ShareProcesses($Value) {
    if (-not $Value) { return }
    $owned = @($Value.Tunnel)
    if ($Value.OwnsServer) { $owned += $Value.Server }
    foreach ($info in $owned) {
        if (Test-ShareProcess $info) {
            Stop-Process -Id ([int]$info.Pid) -ErrorAction Stop
            Write-Host "[NewWorld] Ulashish jarayoni to'xtatildi: $($info.Name)."
        }
    }
}
function Test-ShareHealth([string]$Url, [int]$Seconds = 5) {
    # Do not disable HTTPS certificate validation. Old Windows PowerShell also needs TLS 1.2.
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
    try {
        $reply = Invoke-RestMethod -Uri "$Url/health" -TimeoutSec $Seconds
        if ($reply.ok -eq $true) { return $true }
        $script:lastHealthError = 'Server /health javobi kutilgan shaklda emas.'
    } catch { $script:lastHealthError = $_.Exception.Message }
    return $false
}
function Test-ShareAlive($Value) {
    if (-not $Value -or $Value.Status -ne 'Ready' -or -not (Test-ShareProcess $Value.Server)) { return $false }
    if (-not $Value.LocalOnly -and -not (Test-ShareProcess $Value.Tunnel)) { return $false }
    return (Test-ShareHealth $Value.LocalUrl)
}
function Show-ShareReady($Value) {
    Write-Host ''
    Write-Host "[NewWorld] Server: $($Value.Url)"
    if ($Value.Zip) {
        Write-Host "[NewWorld] DOSTINGIZGA YUBORING: $($Value.Zip)" -ForegroundColor Green
        Write-Host "[NewWorld] ZIPni to'liq ochib, Start-NewWorld.cmd ni ishga tushirsin."
    }
    if ($Value.LocalOnly) { Write-Warn 'Bu FAQAT shu kompyuter sinovi. Ushbu ZIP internetdagi dost uchun ishlamaydi.' }
    else { Write-Host "[NewWorld] Kompyuter va internet yoqilgan tursin. Bu oynani yopish mumkin."
        Write-Host "[NewWorld] Ulashishni tugatish: Stop-Share-Test.cmd. Keyin bu ZIP manzili ishlamaydi." }
    if (-not $Value.OwnsServer) { Write-Host '[NewWorld] Mavjud lobby serveri ishlatilmoqda; uni ham ochiq qoldiring.' }
}

$lock = $null; $transcript = $false; $newServer = $null; $newTunnel = $null; $keep = $false
$savedPort = $env:PORT; $savedTrust = $env:TRUST_PROXY; $savedHost = $env:HOST
try {
    # Prevent simultaneous double-clicks from creating different URLs or corrupting state.
    try { $lock = [IO.File]::Open((Join-Path $LogDir 'share.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None) }
    catch { throw 'Share-Test allaqachon ishlayapti. Oldingi oynadagi tayyor ZIPni kuting.' }
    Start-Transcript -LiteralPath (Join-Path $LogDir "share-session-$runId.log") | Out-Null
    $transcript = $true
    $previous = Read-ShareState
    if ($Stop) {
        Stop-ShareProcesses $previous
        if ($previous) { $previous.Status = 'Stopped'; Save-ShareState $previous }
        Write-Host "[NewWorld] Ulashish yopildi. Oddiy lobby serveri o'chirilmaydi."
        exit 0
    }
    if ($Status) {
        if ((Test-ShareAlive $previous) -and (Test-ShareHealth $previous.Url)) { Show-ShareReady $previous; exit 0 }
        throw 'Faol ulashish topilmadi yoki internet manzili javob bermadi. Share-Test.cmd ni oching.'
    }
    if (-not $SkipPackage) {
        $buildState = Get-BuildStatus
        if ($Build -or $buildState.Stale -or -not $buildState.Exists) {
            if ((Invoke-GameBuild) -ne 0) { throw "O'yin yig'ilmadi. Logs/lobby-v2-build.log ni ko'ring." }
        }
    }
    $fingerprint = Get-ServerFingerprint
    # ReadAllText returns an undecorated CLR string on BOTH PowerShell versions.
    # PS5 Get-Content metadata otherwise serializes as an object and forces ZIP recreation.
    $stamp = if (Test-Path -LiteralPath $BuildStampFile) { [IO.File]::ReadAllText($BuildStampFile) } else { '' }
    $live = Test-ShareAlive $previous
    if ($live -and [bool]$previous.LocalOnly -ne [bool]$LocalOnly) {
        throw "Boshqa turdagi ulashish faol. Avval Stop-Share-Test.cmd orqali uni yoping."
    }
    if ($live -and $previous.Fingerprint -ne $fingerprint) {
        throw "Server kodi yangilangan. Avval Stop-Share-Test.cmd, keyin Share-Test.cmd ni oching."
    }
    if ($live) {
        Write-Host '[NewWorld] Oldingi faol manzil saqlanmoqda; ZIPdagi manzil buzilmaydi.'
        $state = $previous
    } else {
        # Only clean a previous recorded owned process whose PID AND start time still match.
        Stop-ShareProcesses $previous
        $state = [pscustomobject]@{ Status = 'Starting'; RunId = $runId; LocalOnly = [bool]$LocalOnly;
            Server = $null; OwnsServer = $false; Tunnel = $null; LocalUrl = ''; Url = '';
            Zip = ''; Directory = ''; BuildStamp = ''; Fingerprint = $fingerprint; Created = (Get-Date).ToString('o') }
        # Reuse only the positively identified, current launcher server: host and friend share world memory.
        $managed = Read-ServerState
        $managedProcess = if ($managed) { Get-Process -Id ([int]$managed.pid) -ErrorAction SilentlyContinue } else { $null }
        if ($managedProcess -and $managedProcess.ProcessName -in @('node','CraDevServer') -and
            (Get-ProcessStartTicks $managedProcess) -eq [string]$managed.started -and
            $managed.fingerprint -eq $fingerprint -and (Get-PortOwner ([int]$managed.port)) -eq [int]$managed.pid -and
            (Test-ShareHealth "http://127.0.0.1:$($managed.port)")) {
            $state.Server = Get-ShareProcessInfo $managedProcess
            $state.LocalUrl = "http://127.0.0.1:$($managed.port)"
            Write-Host '[NewWorld] Siz va dostingiz bir xil ishlayotgan lobby/olam serveriga ulanasiz.'
        } else {
            $node = Find-Node
            if (-not $node.Path) { throw "Node.js 22.13+ kerak. $($node.Problem)" }
            if (-not (Ensure-ServerDependencies $node.Path)) { throw "Server kutubxonalarini tayyorlab bo'lmadi." }
            $port = $null
            foreach ($candidate in @(8080) + (8090..8110)) {
                $probe = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, $candidate)
                try { $probe.Start(); $port = $candidate; break } catch { } finally { $probe.Stop() }
            }
            if (-not $port) { throw "8080, 8090..8110 orasida bo'sh port topilmadi." }
            $env:PORT = "$port"; $env:HOST = '127.0.0.1'; $env:TRUST_PROXY = $(if ($LocalOnly) { '0' } else { '1' })
            $newServer = Start-Process -FilePath $node.Path -ArgumentList '--disable-warning=ExperimentalWarning src/server.js' `
                -WorkingDirectory $ServerDir -WindowStyle Hidden -PassThru `
                -RedirectStandardOutput (Join-Path $LogDir "share-server-$runId.log") -RedirectStandardError (Join-Path $LogDir "share-server-$runId.err.log")
            $state.Server = Get-ShareProcessInfo $newServer; $state.OwnsServer = $true
            $state.LocalUrl = "http://127.0.0.1:$port"
            $ready = $false
            for ($i = 0; $i -lt 30; $i++) {
                if ($newServer.HasExited) { break }
                if (Test-ShareHealth $state.LocalUrl 1) { $ready = $true; break }
                Start-Sleep -Milliseconds 300
            }
            if (-not $ready) { throw "Mahalliy server ochilmadi. Logs/share-server-$runId.err.log" }
        }
        $state.Url = $state.LocalUrl
        if (-not $LocalOnly) {
            $cf = Join-Path $PSScriptRoot 'bin\cloudflared.exe'
            if (-not (Test-Path -LiteralPath $cf)) {
                New-Item -ItemType Directory -Force (Split-Path -Parent $cf) | Out-Null
                $download = "$cf.download"
                [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
                Invoke-WebRequest 'https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe' -OutFile $download -UseBasicParsing
                Move-Item -LiteralPath $download -Destination $cf
            }
            $cfLog = Join-Path $LogDir "share-tunnel-$runId.log"
            Write-Host '[NewWorld] Cloudflare orqali vaqtinchalik HTTPS manzil ochilmoqda...'
            $newTunnel = Start-Process -FilePath $cf -ArgumentList "tunnel --no-autoupdate --url $($state.LocalUrl) --logfile `"$cfLog`"" `
                -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $LogDir "share-tunnel-$runId.out.log") `
                -RedirectStandardError (Join-Path $LogDir "share-tunnel-$runId.err.log")
            $state.Tunnel = Get-ShareProcessInfo $newTunnel; $state.Url = ''
            for ($i = 0; $i -lt 120 -and -not $state.Url; $i++) {
                if ($newTunnel.HasExited) { break }
                Start-Sleep -Milliseconds 500
                if (Test-Path -LiteralPath $cfLog) {
                    $entry = Select-String -LiteralPath $cfLog -Pattern 'https://[a-z0-9-]+\.trycloudflare\.com' | Select-Object -First 1
                    if ($entry) { $state.Url = $entry.Matches[0].Value }
                }
            }
            if (-not $state.Url) { throw "Cloudflare manzil bermadi. Tafsilot: $cfLog" }
        }
    }
    # A freshly-issued quick-tunnel DNS record may not be reachable immediately.
    $deadline = [DateTime]::UtcNow.AddSeconds(120); $reachable = $false; $attempt = 0
    do {
        $attempt++
        if (Test-ShareHealth $state.Url 5) { $reachable = $true; break }
        if (-not (Test-ShareProcess $state.Server) -or (-not $LocalOnly -and -not (Test-ShareProcess $state.Tunnel))) { break }
        if ($attempt -eq 1 -or $attempt % 5 -eq 0) { Write-Host "[NewWorld] Manzil tayyor bo'lishi kutilmoqda: $script:lastHealthError" }
        Start-Sleep -Seconds 2
    } while ([DateTime]::UtcNow -lt $deadline)
    if (-not $reachable) { throw "Internet manzili tekshiruvdan o'tmadi: $script:lastHealthError. Ishlamaydigan ZIP yaratilmaydi." }
    if (-not $SkipPackage) {
        if ($state.Zip -and (Test-Path -LiteralPath $state.Zip) -and $state.BuildStamp -eq $stamp -and -not $Build) {
            Write-Host '[NewWorld] Tayyor ZIP joriy build bilan mos, qayta siqish shart emas.'
        } else {
            $package = New-SharePackage -GameDirectory $gameDir -OutputDirectory (Join-Path $ProjectRoot "Builds\NewWorld-test-$runId") -Url $state.Url -LocalOnly:$LocalOnly
            $state.Zip = $package.Zip; $state.Directory = $package.Directory; $state.BuildStamp = $stamp
        }
    }
    if (-not (Test-ShareHealth $state.Url 8)) { throw "ZIPdan keyingi ulanish tekshiruvi o'tmadi: $script:lastHealthError" }
    $state.Status = 'Ready'; Save-ShareState $state; $keep = $true
    Show-ShareReady $state
    if (-not $NoWait -and -not $SkipPackage) {
        if (-not (Get-Process CraDev -ErrorAction SilentlyContinue)) {
            Start-Process -FilePath $GameExe -ArgumentList "-server $($state.LocalUrl)" -WorkingDirectory $gameDir
        } elseif ($state.OwnsServer) {
            Write-Warn "Oldingi oyin boshqa serverda bo'lishi mumkin. Yangi ZIP papkasidagi Start-NewWorld.cmd orqali kiring."
        }
    }
} catch {
    Write-Host "[NewWorld] XATO: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "[NewWorld] Tafsilot: Logs/share-session-$runId.log"
    exit 1
} finally {
    if (-not $keep) {
        foreach ($owned in @($newTunnel, $newServer)) {
            if ($owned -and -not $owned.HasExited) { $owned.Kill(); $owned.WaitForExit(5000) | Out-Null }
        }
    }
    $env:PORT = $savedPort; $env:TRUST_PROXY = $savedTrust; $env:HOST = $savedHost
    if ($transcript) { Stop-Transcript | Out-Null }
    if ($lock) { $lock.Dispose() }
}
