<#
.SYNOPSIS
  CraDev loyihasini Unity oynasini ochmasdan (batchmode) boshqarish.

.DESCRIPTION
  Buyruqlar:
    check      loyihani ochadi, skriptlarni kompilyatsiya qiladi va chiqadi (xatolarni ko'rsatadi)
    scenes     barcha sahnalarni qayta yaratadi ("CraDev > Sahnalarni yaratish" bilan bir xil)
    build      sahnalarni yaratadi va o'yinni Builds\ papkasiga yig'adi
    run        build + o'yinni alohida oynada ishga tushiradi
    playerlog  oxirgi ishga tushirilgan o'yinning logini (Player.log) ko'rsatadi
    open       loyihani Unity tahrirlovchisida ochadi
    where      qaysi Unity.exe ishlatilishini ko'rsatadi

  Unity.exe Unity Hub o'rnatgan papkalardan qidiriladi. Boshqa joyda bo'lsa:
  UNITY_EXE muhit o'zgaruvchisi yoki -Unity parametri.

  Batchmode loyiha Unity'da ochiq bo'lmaganda ishlaydi. Loglar: Logs\batch-<buyruq>.log.
  Chiqish kodi: 0 - muvaffaqiyat, 1 - Unity xatosi, 2 - loyiha ochiq yoki Unity topilmadi.

.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File tools\unity.ps1 scenes
.EXAMPLE
  tools\unity.cmd run
#>
param(
    [Parameter(Position = 0)]
    [ValidateSet('check', 'scenes', 'build', 'run', 'playerlog', 'open', 'where')]
    [string]$Command = 'check',

    [string]$Unity = $env:UNITY_EXE
)

$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$LogDir = Join-Path $ProjectRoot 'Logs'
$BatchClass = 'CraDev.EditorTools.CraDevBatch'
$GameExe = Join-Path $ProjectRoot 'Builds\StandaloneWindows64\CraDev.exe'

function Write-Step([string]$Text) { Write-Host "[CraDev] $Text" -ForegroundColor Cyan }
function Write-Fail([string]$Text) { Write-Host "[CraDev] $Text" -ForegroundColor Red }

function Get-ProjectVersion {
    $file = Join-Path $ProjectRoot 'ProjectSettings\ProjectVersion.txt'
    foreach ($line in Get-Content $file) {
        if ($line -match '^m_EditorVersion:\s*(\S+)') { return $Matches[1] }
    }
    return $null
}

# "6000.0.23f1" -> [version]6000.0.23 (tartiblash uchun)
function ConvertTo-VersionKey([string]$Name) {
    $numeric = $Name -replace '[a-zA-Z].*$', ''
    $key = $null
    if ([version]::TryParse($numeric, [ref]$key)) { return $key }
    return [version]'0.0'
}

function Get-InstalledEditors {
    $roots = New-Object System.Collections.Generic.List[string]
    $roots.Add((Join-Path $env:ProgramFiles 'Unity\Hub\Editor'))

    # Unity Hub'da o'rnatish papkasi o'zgartirilgan bo'lsa
    $hubConfig = Join-Path $env:APPDATA 'UnityHub\secondaryInstallPath.json'
    if (Test-Path $hubConfig) {
        $custom = $null
        try { $custom = Get-Content $hubConfig -Raw | ConvertFrom-Json } catch { }
        if ($custom -is [string] -and $custom.Trim()) { $roots.Add($custom.Trim()) }
    }

    $editors = @()
    foreach ($root in $roots) {
        if (-not (Test-Path $root)) { continue }
        foreach ($dir in Get-ChildItem $root -Directory) {
            $exe = Join-Path $dir.FullName 'Editor\Unity.exe'
            if (Test-Path $exe) {
                $editors += [pscustomobject]@{ Version = $dir.Name; Path = $exe }
            }
        }
    }

    # Hub'siz o'rnatilgan Unity
    $legacy = Join-Path $env:ProgramFiles 'Unity\Editor\Unity.exe'
    if (Test-Path $legacy) {
        $editors += [pscustomobject]@{ Version = (Get-Item $legacy).VersionInfo.ProductVersion; Path = $legacy }
    }
    return $editors
}

function Find-Unity {
    if ($Unity) {
        if (Test-Path $Unity) { return (Resolve-Path $Unity).Path }
        Write-Fail "Unity.exe topilmadi: $Unity (UNITY_EXE yoki -Unity)"
        exit 2
    }

    $wanted = Get-ProjectVersion
    $editors = @(Get-InstalledEditors)
    if ($editors.Count -eq 0) {
        Write-Fail "Unity topilmadi. Unity Hub orqali Unity 6 ni o'rnating yoki UNITY_EXE ga Unity.exe yo'lini yozing."
        exit 2
    }

    $exact = $editors | Where-Object { $_.Version -eq $wanted } | Select-Object -First 1
    if ($exact) { return $exact.Path }

    # Aynan shu versiya yo'q: eng yangi Unity 6 (6000.x), u ham bo'lmasa eng yangisi
    $sorted = $editors | Sort-Object { ConvertTo-VersionKey $_.Version } -Descending
    $best = $sorted | Where-Object { $_.Version -like '6000.*' } | Select-Object -First 1
    if (-not $best) { $best = $sorted | Select-Object -First 1 }
    Write-Host "[CraDev] Loyiha versiyasi $wanted o'rnatilmagan, $($best.Version) ishlatiladi (Unity loyihani shu versiyaga moslaydi)." -ForegroundColor Yellow
    return $best.Path
}

# Unity loyihani ochganda Temp\UnityLockfile ni band qilib turadi
function Test-ProjectOpen {
    $lock = Join-Path $ProjectRoot 'Temp\UnityLockfile'
    if (-not (Test-Path $lock)) { return $false }
    try {
        $stream = [System.IO.File]::Open($lock, 'Open', 'ReadWrite', 'None')
        $stream.Close()
        return $false
    }
    catch { return $true }
}

# Muhim qatorlar: CraDev xabarlari, kompilyatsiya xatolari, istisnolar, build natijasi
$Interesting = '\[CraDev\]|error CS\d+|Exception|Scripts have compiler errors|Build Finished|Build completed with a result|Aborting batchmode|executeMethod (class|method)'

# Logning yangi qismini o'qib, muhim qatorlarni chiqaradi. Keyingi o'qish joyini qaytaradi.
function Show-LogProgress([string]$Log, [long]$Position, $Seen) {
    if (-not (Test-Path $Log)) { return $Position }
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    $fs = [System.IO.File]::Open($Log, 'Open', 'Read', 'ReadWrite')
    try {
        if ($fs.Length -lt $Position) { $Position = 0 }
        [void]$fs.Seek($Position, 'Begin')
        $text = (New-Object System.IO.StreamReader($fs, $utf8)).ReadToEnd()
    }
    finally { $fs.Dispose() }

    # Oxirgi to'liq qatorgacha: yarim yozilgan qator keyingi safar o'qiladi
    $end = $text.LastIndexOf("`n")
    if ($end -lt 0) { return $Position }
    $complete = $text.Substring(0, $end + 1)

    foreach ($line in $complete -split "`r?`n") {
        $line = $line.TrimEnd()
        if (-not $line -or $line -notmatch $Interesting -or -not $Seen.Add($line)) { continue }
        if ($line -match 'error|Exception|XATO|Aborting') { Write-Host $line -ForegroundColor Red }
        else { Write-Host $line }
    }
    return $Position + $utf8.GetByteCount($complete)
}

function Invoke-UnityBatch([string]$Name, [string[]]$Extra) {
    $exe = Find-Unity
    if (Test-ProjectOpen) {
        Write-Fail "Loyiha Unity tahrirlovchisida ochiq. Batchmode ishlashi uchun Unity oynasini yoping."
        exit 2
    }

    New-Item -ItemType Directory -Force $LogDir | Out-Null
    $log = Join-Path $LogDir "batch-$Name.log"
    if (Test-Path $log) { Remove-Item $log -Force }

    $argList = @('-batchmode', '-quit', '-projectPath', "`"$ProjectRoot`"", '-logFile', "`"$log`"")
    if ($Extra) { $argList += $Extra }
    Write-Step "$Name boshlandi ($exe)"
    Write-Host "         Birinchi ochilishda Unity Library papkasini yaratadi, bu bir necha daqiqa davom etadi."

    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    $process = Start-Process -FilePath $exe -ArgumentList $argList -PassThru -NoNewWindow
    $null = $process.Handle  # ExitCode keyin ham o'qilishi uchun

    $seen = New-Object 'System.Collections.Generic.HashSet[string]'
    $position = 0L
    while (-not $process.HasExited) {
        Start-Sleep -Milliseconds 1000
        $position = Show-LogProgress $log $position $seen
    }
    $process.WaitForExit()
    $position = Show-LogProgress $log $position $seen

    $elapsed = $timer.Elapsed.ToString('mm\:ss')
    $code = $process.ExitCode
    if ($code -eq 0) {
        Write-Step "$Name tugadi ($elapsed). Log: $log"
        return
    }

    Write-Fail "$Name XATO bilan tugadi (kod $code, $elapsed). To'liq log: $log"
    if ($seen.Count -eq 0 -and (Test-Path $log)) {
        Write-Host "--- logning oxiri ---"
        Get-Content $log -Tail 40 | ForEach-Object { Write-Host $_ }
    }
    exit 1
}

function Get-PlayerLogPath {
    # O'yin logi: %USERPROFILE%\AppData\LocalLow\<Company>\<Product>\Player.log
    $settings = Join-Path $ProjectRoot 'ProjectSettings\ProjectSettings.asset'
    $company = 'CraDev'
    $product = 'CraDev'
    if (Test-Path $settings) {
        foreach ($line in Get-Content $settings) {
            if ($line -match '^\s+companyName:\s*(.+)$') { $company = $Matches[1].Trim() }
            elseif ($line -match '^\s+productName:\s*(.+)$') { $product = $Matches[1].Trim() }
        }
    }
    return Join-Path $env:USERPROFILE "AppData\LocalLow\$company\$product\Player.log"
}

switch ($Command) {
    'where' {
        Write-Host "Loyiha:        $ProjectRoot"
        Write-Host "Loyiha Unity:  $(Get-ProjectVersion)"
        Write-Host "Ishlatiladi:   $(Find-Unity)"
        foreach ($e in Get-InstalledEditors) { Write-Host "  o'rnatilgan: $($e.Version)  $($e.Path)" }
    }
    'check' {
        Invoke-UnityBatch 'check' @()
    }
    'scenes' {
        Invoke-UnityBatch 'scenes' @('-executeMethod', "$BatchClass.CreateScenes")
    }
    'build' {
        Invoke-UnityBatch 'build' @('-executeMethod', "$BatchClass.BuildGame")
        Write-Step "O'yin: $GameExe"
    }
    'run' {
        Invoke-UnityBatch 'build' @('-executeMethod', "$BatchClass.BuildGame")
        Write-Step "O'yin ishga tushirilmoqda. Yopish: Alt+F4. Log: $(Get-PlayerLogPath)"
        Start-Process -FilePath $GameExe -WorkingDirectory (Split-Path -Parent $GameExe)
    }
    'playerlog' {
        $playerLog = Get-PlayerLogPath
        if (-not (Test-Path $playerLog)) {
            Write-Fail "Player.log topilmadi: $playerLog (o'yin hali ishga tushirilmagan)"
            exit 1
        }
        Write-Step $playerLog
        Get-Content $playerLog -Tail 80 | ForEach-Object { Write-Host $_ }
    }
    'open' {
        $exe = Find-Unity
        Write-Step "Unity ochilmoqda: $exe"
        Start-Process -FilePath $exe -ArgumentList @('-projectPath', "`"$ProjectRoot`"")
    }
}
