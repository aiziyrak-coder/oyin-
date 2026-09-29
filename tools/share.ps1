# CraDev: boshqa odamlar bilan internet orqali sinash.
# 1) serverni shu kompyuterda yoqadi, 2) Cloudflare bepul tunneli orqali unga internet manzil (https) oladi,
# 3) o'yin nusxasiga server.txt yozib, do'stlarga yuboriladigan ZIP yaratadi: Builds\CraDev-test.zip
# Oyna ochiq turguncha server va tunnel ishlaydi. Tunnel manzili har ishga tushirishda yangi bo'ladi -> ZIP ham yangilanadi.
param([switch]$Build)
$ErrorActionPreference = 'Stop'
$projectDir = Split-Path -Parent $PSScriptRoot
$gameDir = Join-Path $projectDir 'Builds\LobbyV2'
$gameFile = Join-Path $gameDir 'CraDev.exe'
$logDir = Join-Path $projectDir 'Logs'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

if ($Build -or -not (Test-Path -LiteralPath $gameFile)) {
    Write-Host "[CraDev] O'yin yig'ilmoqda..."
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'lobby.ps1') -Build -NoWait
    if (-not (Test-Path -LiteralPath $gameFile)) { throw "Build topilmadi: $gameFile" }
    Get-Process CraDev -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
}

# --- Server (8080 bandmi - eski serverni to'xtatamiz, proksi rejimida qayta yoqamiz)
$old = Get-NetTCPConnection -LocalPort 8080 -State Listen -ErrorAction SilentlyContinue
foreach ($c in $old) { Stop-Process -Id $c.OwningProcess -Force -ErrorAction SilentlyContinue }
Start-Sleep -Milliseconds 500
# Eski serverni yopib bo'lmasa (masalan, Administrator oynasidan yoqilgan) bo'sh portni tanlaymiz
$port = 8080
foreach ($candidate in @(8080) + (8090..8099)) {
    if (-not (Get-NetTCPConnection -LocalPort $candidate -State Listen -ErrorAction SilentlyContinue)) { $port = $candidate; break }
}
if ($port -ne 8080) { Write-Host "[CraDev] 8080 port band (eski server yopilmadi): server $port portda yoqiladi." }
$env:PORT = "$port"
$nodeFile = (Get-Command node -ErrorAction Stop).Source
$env:TRUST_PROXY = '1'
$server = Start-Process -FilePath $nodeFile -ArgumentList '--disable-warning=ExperimentalWarning src/server.js' `
    -WorkingDirectory (Join-Path $projectDir 'Server') -WindowStyle Hidden -PassThru `
    -RedirectStandardOutput (Join-Path $logDir 'share-server.log') -RedirectStandardError (Join-Path $logDir 'share-server.err.log')
$ready = $false
for ($i = 0; $i -lt 40; $i++) {
    try { $null = Invoke-RestMethod 'http://localhost:$port/api/stats' -TimeoutSec 1; $ready = $true; break } catch { Start-Sleep -Milliseconds 250 }
}
if (-not $ready) { throw "Server ishga tushmadi. Log: Logs\share-server.err.log" }
Write-Host "[CraDev] Server yoqildi (http://localhost:$port)."

# --- Tunnel (cloudflared bir marta yuklab olinadi)
$binDir = Join-Path $PSScriptRoot 'bin'
$cf = Join-Path $binDir 'cloudflared.exe'
if (-not (Test-Path -LiteralPath $cf)) {
    New-Item -ItemType Directory -Force -Path $binDir | Out-Null
    Write-Host "[CraDev] cloudflared yuklab olinmoqda (bir marta)..."
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest 'https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe' -OutFile $cf -UseBasicParsing
}
$cfLog = Join-Path $logDir 'share-tunnel.log'
Remove-Item -LiteralPath $cfLog -ErrorAction SilentlyContinue
$tunnel = Start-Process -FilePath $cf -ArgumentList "tunnel --no-autoupdate --url http://localhost:$port --logfile `"$cfLog`"" -WindowStyle Hidden -PassThru
$url = $null
for ($i = 0; $i -lt 120 -and -not $url; $i++) {
    Start-Sleep -Milliseconds 500
    if (Test-Path -LiteralPath $cfLog) {
        $m = Select-String -LiteralPath $cfLog -Pattern 'https://[a-z0-9-]+\.trycloudflare\.com' | Select-Object -First 1
        if ($m) { $url = $m.Matches[0].Value }
    }
}
if (-not $url) { throw "Tunnel manzili olinmadi. Log: Logs\share-tunnel.log" }
for ($i = 0; $i -lt 60; $i++) {
    try { $null = Invoke-RestMethod "$url/api/stats" -TimeoutSec 3; break } catch { Start-Sleep -Seconds 1 }
}

# --- Do'stlar uchun ZIP (o'yin + server.txt)
$shareDir = Join-Path $projectDir 'Builds\CraDev-test'
$zip = Join-Path $projectDir 'Builds\CraDev-test.zip'
if (Test-Path -LiteralPath $shareDir) { Remove-Item -LiteralPath $shareDir -Recurse -Force }
Copy-Item -LiteralPath $gameDir -Destination $shareDir -Recurse
Get-ChildItem -LiteralPath $shareDir -Filter '*_BurstDebugInformation_DoNotShip' -Directory | Remove-Item -Recurse -Force
Set-Content -LiteralPath (Join-Path $shareDir 'server.txt') -Value $url -Encoding ASCII
Set-Content -LiteralPath (Join-Path $gameDir 'server.txt') -Value $url -Encoding ASCII
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
Compress-Archive -Path (Join-Path $shareDir '*') -DestinationPath $zip

Write-Host ""
Write-Host "==================================================================="
Write-Host " Server internetda: $url"
Write-Host " Do'stlarga yuboring: $zip"
Write-Host "   (ular ZIP'ni ochib CraDev.exe ni ishga tushiradi)"
Write-Host " Siz ham o'ynashingiz mumkin: $gameFile"
Write-Host " Bu oynani YOPMANG. Tugatish uchun Enter bosing."
Write-Host "==================================================================="
Start-Process -FilePath $gameFile -WorkingDirectory $gameDir
[void](Read-Host)

Stop-Process -Id $tunnel.Id -Force -ErrorAction SilentlyContinue
Stop-Process -Id $server.Id -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $gameDir 'server.txt') -ErrorAction SilentlyContinue
Write-Host "[CraDev] Server va tunnel to'xtatildi."
