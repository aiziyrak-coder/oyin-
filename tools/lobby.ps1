param([switch]$Build)
$ErrorActionPreference = 'Stop'
$projectDir = Split-Path -Parent $PSScriptRoot
$gameFile = Join-Path $projectDir 'Builds/LobbyV2/CraDev.exe'
if ($Build -or -not (Test-Path -LiteralPath $gameFile)) {
    $versionLine = Get-Content (Join-Path $projectDir 'ProjectSettings/ProjectVersion.txt') | Where-Object { $_ -match '^m_EditorVersion:' }
    $version = ($versionLine -split ':',2)[1].Trim()
    $unityFile = Join-Path $env:ProgramFiles "Unity/Hub/Editor/$version/Editor/Unity.exe"
    if (-not (Test-Path -LiteralPath $unityFile)) { throw "Unity $version topilmadi." }
    $logFile = Join-Path $projectDir 'Logs/lobby-v2-build.log'
    $arguments = "-batchmode -quit -projectPath `"$projectDir`" -executeMethod CraDev.EditorTools.CraDevBatch.BuildLobby -logFile `"$logFile`""
    $job = Start-Process -FilePath $unityFile -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
    if ($job.ExitCode -ne 0) { throw "Build tugamadi. Log: $logFile" }
}
$ready = $false
try { $null = Invoke-RestMethod 'http://localhost:8080/api/stats' -TimeoutSec 2; $ready=$true } catch {}
if (-not $ready) {
    if (Get-NetTCPConnection -LocalPort 8080 -State Listen -ErrorAction SilentlyContinue) {
        throw "8080 portda boshqa yoki eski server ochiq. Avval uni tekshiring."
    }
    $nodeFile = (Get-Command node -ErrorAction Stop).Source
    Start-Process -FilePath $nodeFile -ArgumentList '--disable-warning=ExperimentalWarning src/server.js' -WorkingDirectory (Join-Path $projectDir 'Server') -WindowStyle Hidden
    for ($i=0; $i -lt 20; $i++) {
        try { $null=Invoke-RestMethod 'http://localhost:8080/api/stats' -TimeoutSec 1; $ready=$true; break } catch { Start-Sleep -Milliseconds 250 }
    }
    if (-not $ready) { throw "O'yin serveri ishga tushmadi." }
}
Start-Process -FilePath $gameFile -WorkingDirectory (Split-Path -Parent $gameFile)
