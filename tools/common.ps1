# CraDev: tools\unity.ps1 va tools\lobby.ps1 uchun umumiy funksiyalar. Ular shu faylni dot-source qiladi:
#   . (Join-Path $PSScriptRoot 'common.ps1')
# Windows PowerShell 5.1 bilan mos. Fayl faqat ASCII belgilarda (5.1 BOM'siz faylni ANSI deb o'qiydi).

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$LogDir = Join-Path $ProjectRoot 'Logs'
$BatchClass = 'CraDev.EditorTools.CraDevBatch'

# Yagona o'yin papkasi: Play-LobbyV2.cmd, tools\lobby.ps1 va tools\unity.cmd build/run/lobby/play shu exe'ni
# yig'adi va ochadi. build-stamp.txt build qachon va qaysi commit'dan yig'ilganini saqlaydi.
$GameBuildPath = 'Builds/LobbyV2/CraDev.exe'
$GameExe = Join-Path $ProjectRoot 'Builds\LobbyV2\CraDev.exe'
$BuildStampFile = Join-Path $ProjectRoot 'Builds\LobbyV2\build-stamp.txt'

# O'yin serveri (Server/, Node.js). Launcher yoqqan server holati: Logs\server-state.json
$ServerDir = Join-Path $ProjectRoot 'Server'
$ServerPort = 8080
$ServerStateFile = Join-Path $LogDir 'server-state.json'

function Write-Step([string]$Text) { Write-Host "[CraDev] $Text" -ForegroundColor Cyan }
function Write-Warn([string]$Text) { Write-Host "[CraDev] $Text" -ForegroundColor Yellow }
function Write-Fail([string]$Text) { Write-Host "[CraDev] $Text" -ForegroundColor Red }

# Oyna orqali odam ishlatyaptimi (Play-LobbyV2.cmd) yoki skript avtomatik chaqirilganmi (stdin yo'naltirilgan)
function Test-Interactive {
    try { return ([Environment]::UserInteractive -and -not [Console]::IsInputRedirected) } catch { return $false }
}

# ---------------------------------------------------------------- Unity

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

# Unity.exe yo'li yoki $null (sababi ekranga yoziladi). $Override: -Unity parametri yoki UNITY_EXE.
function Find-Unity([string]$Override) {
    if ($Override) {
        if (Test-Path -LiteralPath $Override) { return (Resolve-Path -LiteralPath $Override).Path }
        Write-Fail "Unity.exe topilmadi: $Override (UNITY_EXE yoki -Unity)"
        return $null
    }

    $wanted = Get-ProjectVersion
    $editors = @(Get-InstalledEditors)
    if ($editors.Count -eq 0) {
        Write-Fail "Unity topilmadi. Unity Hub orqali Unity 6 ni o'rnating yoki UNITY_EXE ga Unity.exe yo'lini yozing."
        return $null
    }

    $exact = $editors | Where-Object { $_.Version -eq $wanted } | Select-Object -First 1
    if ($exact) { return $exact.Path }

    # Aynan shu versiya yo'q: eng yangi Unity 6 (6000.x), u ham bo'lmasa eng yangisi
    $sorted = $editors | Sort-Object { ConvertTo-VersionKey $_.Version } -Descending
    $best = $sorted | Where-Object { $_.Version -like '6000.*' } | Select-Object -First 1
    if (-not $best) { $best = $sorted | Select-Object -First 1 }
    Write-Warn "Loyiha versiyasi $wanted o'rnatilmagan, $($best.Version) ishlatiladi (Unity loyihani shu versiyaga moslaydi)."
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

# Unity'ni batchmode'da ishga tushiradi. Qaytaradi: 0 - muvaffaqiyat, 1 - Unity xatosi, 2 - Unity topilmadi yoki loyiha ochiq.
# Log: Logs\<LogName>.log (standart: batch-<Name>.log).
function Invoke-UnityBatch([string]$Name, [string[]]$Extra, [string]$UnityOverride, [string]$LogName) {
    $exe = Find-Unity $UnityOverride
    if (-not $exe) { return 2 }
    if (Test-ProjectOpen) {
        Write-Fail "Loyiha Unity tahrirlovchisida ochiq. Batchmode ishlashi uchun Unity oynasini yoping."
        return 2
    }

    New-Item -ItemType Directory -Force $LogDir | Out-Null
    if (-not $LogName) { $LogName = "batch-$Name" }
    $log = Join-Path $LogDir "$LogName.log"
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
        return 0
    }

    Write-Fail "$Name XATO bilan tugadi (kod $code, $elapsed). To'liq log: $log"
    if ($seen.Count -eq 0 -and (Test-Path $log)) {
        Write-Host "--- logning oxiri ---"
        Get-Content $log -Tail 40 | ForEach-Object { Write-Host $_ }
    }
    return 1
}

# ---------------------------------------------------------------- Build va uning eskirganligi

# Joriy commit (git bo'lmasa yoki repo emas bo'lsa bo'sh). Faqat ma'lumot uchun: eskirganlik fayl vaqtlaridan aniqlanadi.
function Get-GitCommit {
    $git = Get-Command git -ErrorAction SilentlyContinue
    if (-not $git) { return '' }
    $commit = ''
    try {
        $sha = & $git.Source -C $ProjectRoot rev-parse --short HEAD 2>$null
        if ($LASTEXITCODE -eq 0 -and $sha) { $commit = ([string]$sha).Trim() }
    }
    catch { }
    if (-not $commit) { return '' }
    try {
        $dirty = & $git.Source -C $ProjectRoot status --porcelain -- Assets Packages ProjectSettings 2>$null
        if ($dirty) { $commit += ' (+commit qilinmagan o''zgarishlar)' }
    }
    catch { }
    return $commit
}

# Assets, Packages va ProjectSettings ichida $Since (UTC) dan keyin o'zgargan fayl va papkalar: soni va eng yangisi.
# O'chirilgan fayl o'z papkasining vaqtini yangilaydi, git pull esa o'zgargan fayllar vaqtini yangilaydi.
function Get-SourceChanges([DateTime]$Since) {
    $newest = $null
    $count = 0
    foreach ($name in 'Assets', 'Packages', 'ProjectSettings') {
        $root = Join-Path $ProjectRoot $name
        if (-not (Test-Path -LiteralPath $root)) { continue }
        $dir = New-Object System.IO.DirectoryInfo($root)
        $items = @($dir) + @($dir.EnumerateFileSystemInfos('*', [System.IO.SearchOption]::AllDirectories))
        foreach ($item in $items) {
            if ($item.LastWriteTimeUtc -le $Since) { continue }
            $count++
            if (-not $newest -or $item.LastWriteTimeUtc -gt $newest.LastWriteTimeUtc) { $newest = $item }
        }
    }
    return [pscustomobject]@{ Count = $count; Newest = $newest }
}

function Write-BuildStamp([string]$Method) {
    # Kelajak sanali fayl (soat noto'g'ri bo'lgan) har safar qayta yig'ishga olib kelmasligi uchun
    $builtAt = [DateTime]::UtcNow
    $future = Get-SourceChanges $builtAt
    if ($future.Newest) { $builtAt = $future.Newest.LastWriteTimeUtc }
    $lines = @(
        "built_utc_ticks=$($builtAt.Ticks)",
        "built=$((Get-Date).ToString('yyyy-MM-dd HH:mm'))",
        "method=$Method",
        "commit=$(Get-GitCommit)"
    )
    Set-Content -LiteralPath $BuildStampFile -Value $lines -Encoding ASCII
}

function Read-BuildStamp {
    if (-not (Test-Path -LiteralPath $BuildStampFile)) { return $null }
    $stamp = @{}
    foreach ($line in Get-Content -LiteralPath $BuildStampFile) {
        $i = $line.IndexOf('=')
        if ($i -gt 0) { $stamp[$line.Substring(0, $i)] = $line.Substring($i + 1) }
    }
    return $stamp
}

# Build bormi va eskirganmi: Assets, Packages yoki ProjectSettings ichida build'dan keyin o'zgargan narsa bo'lsa, eskirgan
function Get-BuildStatus {
    $status = [pscustomobject]@{ Exists = $false; Stale = $false; Reason = ''; Info = '' }
    if (-not (Test-Path -LiteralPath $GameExe)) {
        $status.Reason = "o'yin hali yig'ilmagan ($GameBuildPath yo'q)"
        return $status
    }
    $status.Exists = $true

    $stamp = Read-BuildStamp
    $ticks = 0L
    if (-not $stamp -or -not [long]::TryParse([string]$stamp['built_utc_ticks'], [ref]$ticks)) {
        $status.Stale = $true
        $status.Reason = "build qachon va qaysi koddan yig'ilgani noma'lum (build-stamp.txt yo'q)"
        return $status
    }
    $status.Info = "$($stamp['built']), $($stamp['method'])"
    if ($stamp['commit']) { $status.Info += ", commit $($stamp['commit'])" }

    $changes = Get-SourceChanges (New-Object DateTime($ticks, [DateTimeKind]::Utc))
    if ($changes.Count -gt 0) {
        $relative = $changes.Newest.FullName.Substring($ProjectRoot.Length).TrimStart('\', '/')
        $status.Stale = $true
        $status.Reason = "build'dan keyin $($changes.Count) ta fayl yoki papka o'zgargan (eng yangisi: $relative)"
    }
    return $status
}

# Builds\LobbyV2 ga yig'adi. Standart: faqat lobby (MainMenu + WorldSandbox, avatar xaritalari qayta pishirilmaydi).
# -Full: barcha 6 sahna noldan (CraDevBatch.BuildGame; grafik karta bo'lsa avatar kartalari va xaritalari qayta chiziladi).
function Invoke-GameBuild([switch]$Full, [string]$UnityOverride) {
    if ($Full) {
        $method = 'BuildGame'
        $code = Invoke-UnityBatch 'build' @('-executeMethod', "$BatchClass.BuildGame", '-customBuildPath', $GameBuildPath) $UnityOverride
    }
    else {
        $method = 'BuildLobby'
        $code = Invoke-UnityBatch 'lobby' @('-executeMethod', "$BatchClass.BuildLobby", '-customBuildPath', $GameBuildPath) $UnityOverride 'lobby-v2-build'
    }
    if ($code -eq 0) {
        if (Test-Path -LiteralPath $GameExe) {
            Write-BuildStamp $method
            Write-Step "O'yin yig'ildi: $GameExe"
        }
        else {
            Write-Fail "Unity xatosiz tugadi, lekin $GameExe paydo bo'lmadi."
            $code = 1
        }
    }
    return $code
}

# ---------------------------------------------------------------- O'yin serveri

function Get-ServerFiles {
    $files = @()
    $src = Join-Path $ServerDir 'src'
    if (Test-Path -LiteralPath $src) { $files += @(Get-ChildItem -LiteralPath $src -Recurse -File) }
    $package = Join-Path $ServerDir 'package.json'
    if (Test-Path -LiteralPath $package) { $files += Get-Item -LiteralPath $package }
    return $files
}

# Server kodining barmoq izi (Server/src va package.json mazmuni): o'zgarsa, launcher yoqqan server qayta yoqiladi
function Get-ServerFingerprint {
    $lines = foreach ($file in (@(Get-ServerFiles) | Sort-Object FullName)) {
        $relative = $file.FullName.Substring($ServerDir.Length)
        "$relative=$((Get-FileHash -Algorithm SHA256 -LiteralPath $file.FullName).Hash)"
    }
    $bytes = [System.Text.Encoding]::UTF8.GetBytes((@($lines) -join "`n"))
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return (($sha.ComputeHash($bytes) | ForEach-Object { $_.ToString('x2') }) -join '') }
    finally { $sha.Dispose() }
}

# Server kodidagi eng oxirgi o'zgarish vaqti (UTC)
function Get-ServerSourceTime {
    $newest = $null
    foreach ($file in @(Get-ServerFiles)) {
        if (-not $newest -or $file.LastWriteTimeUtc -gt $newest) { $newest = $file.LastWriteTimeUtc }
    }
    return $newest
}

# Node.js 22.13+ (server node:sqlite ishlatadi). Qaytaradi: Path (yoki $null) va Problem.
function Find-Node {
    $command = Get-Command node -ErrorAction SilentlyContinue
    if (-not $command) { return [pscustomobject]@{ Path = $null; Problem = 'Node.js topilmadi' } }
    $text = ''
    try { $text = ([string](& $command.Source --version 2>$null)).Trim() } catch { }
    $version = $null
    if ($text -match '^v?(\d+)\.(\d+)\.(\d+)') { $version = [version]"$($Matches[1]).$($Matches[2]).$($Matches[3])" }
    if (-not $version -or $version -lt [version]'22.13.0') {
        return [pscustomobject]@{ Path = $null; Problem = "Node.js $text eski yoki ishlamayapti: server uchun 22.13 yoki yangisi kerak" }
    }
    return [pscustomobject]@{ Path = $command.Source; Problem = $null }
}

# Node.js bo'lmasa: GitHub CI yig'gan CraDevServer.exe (tools/ci/server-exe.sh), agar shu kompyuterda bo'lsa
function Find-ServerExe {
    foreach ($candidate in @('Server\dist\CraDevServer.exe', 'dist\CraDevServer.exe', 'server-exe\CraDevServer.exe')) {
        $path = Join-Path $ProjectRoot $candidate
        if (Test-Path -LiteralPath $path) { return $path }
    }
    return $null
}

# Portni tinglayotgan jarayon ID'si yoki $null
function Get-PortOwner([int]$Port) {
    if (Get-Command Get-NetTCPConnection -ErrorAction SilentlyContinue) {
        $connection = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue) | Select-Object -First 1
        if ($connection) { return [int]$connection.OwningProcess }
        return $null
    }
    # Eski Windows: netstat (holat nomi tarjima qilinadi, shuning uchun bo'sh masofaviy manzil bo'yicha)
    foreach ($line in @(netstat -ano -p TCP)) {
        if ($line -match "^\s*TCP\s+\S+:$Port\s+(0\.0\.0\.0:0|\[::\]:0)\s+\S+\s+(\d+)\s*$") { return [int]$Matches[2] }
    }
    return $null
}

function Get-ProcessCommandLine([int]$ProcessId) {
    try { return [string](Get-CimInstance Win32_Process -Filter "ProcessId = $ProcessId" -ErrorAction Stop).CommandLine } catch { }
    try { return [string](Get-WmiObject Win32_Process -Filter "ProcessId = $ProcessId" -ErrorAction Stop).CommandLine } catch { }
    return ''
}

# Jarayon boshlangan vaqt (UTC ticks, matn): PID qayta ishlatilganini ajratish uchun
function Get-ProcessStartTicks($Process) {
    try { return [string]$Process.StartTime.ToUniversalTime().Ticks } catch { return '' }
}

function Test-ServerResponds {
    try { $null = Invoke-RestMethod -Uri "http://localhost:$ServerPort/health" -TimeoutSec 2; return $true }
    catch [System.Net.WebException] { return ($null -ne $_.Exception.Response) }
    catch { return $false }
}

# 8080 dagi server aynan CraDev serverimi (/api/stats javobida "online" bor)
function Test-CraDevServer {
    try {
        $stats = Invoke-RestMethod -Uri "http://localhost:$ServerPort/api/stats" -TimeoutSec 2
        return ($null -ne $stats -and @($stats.PSObject.Properties.Name) -contains 'online')
    }
    catch { return $false }
}

function Read-ServerState {
    if (-not (Test-Path -LiteralPath $ServerStateFile)) { return $null }
    try { return (Get-Content -LiteralPath $ServerStateFile -Raw | ConvertFrom-Json) } catch { return $null }
}

function Save-ServerState($Process, [string]$Fingerprint) {
    New-Item -ItemType Directory -Force $LogDir | Out-Null
    $state = [ordered]@{ pid = $Process.Id; started = (Get-ProcessStartTicks $Process); fingerprint = $Fingerprint; port = $ServerPort }
    ($state | ConvertTo-Json) | Set-Content -LiteralPath $ServerStateFile -Encoding ASCII
}

function Clear-ServerState([int]$ProcessId) {
    $state = Read-ServerState
    if ($state -and [int]$state.pid -eq $ProcessId) { Remove-Item -LiteralPath $ServerStateFile -Force -ErrorAction SilentlyContinue }
}

function Stop-ServerProcess([int]$ProcessId) {
    try { Stop-Process -Id $ProcessId -Force -ErrorAction Stop } catch { }
    try { Wait-Process -Id $ProcessId -Timeout 10 -ErrorAction SilentlyContinue } catch { }
    for ($i = 0; $i -lt 20 -and (Get-PortOwner $ServerPort) -eq $ProcessId; $i++) { Start-Sleep -Milliseconds 250 }
}

# Launcher yoqqan server hali ishlayotgan bo'lsa (PID va boshlanish vaqti mos), uni to'xtatadi
function Stop-ManagedServer {
    $state = Read-ServerState
    if (-not $state) { Write-Step "Launcher yoqqan server yo'q."; return }
    $process = Get-Process -Id ([int]$state.pid) -ErrorAction SilentlyContinue
    if ($process -and (Get-ProcessStartTicks $process) -eq [string]$state.started) {
        Stop-ServerProcess $process.Id
        Write-Step "Server to'xtatildi (PID $($process.Id))."
    }
    else { Write-Step "Launcher yoqqan server allaqachon to'xtagan." }
    Remove-Item -LiteralPath $ServerStateFile -Force -ErrorAction SilentlyContinue
}

# O'yin serverini tayyorlaydi: 8080 da yangi CraDev serveri ishlasa, o'shani ishlatadi; eski kod bilan ishlayotgan
# (Server/src o'zgargan) yoki javob bermayotgan launcher serverini qayta yoqadi; bo'lmasa yangisini yoqadi.
# Qaytaradi: Ok (o'yinni ochish mumkin), Started (shu chaqiruv yoqdi), Pid, StartTicks, Offline (serversiz).
# WindowStyle: Hidden (loglar Logs\server.log ga) yoki Minimized (server oynasi vazifalar panelida).
function Start-GameServer([string]$WindowStyle = 'Hidden') {
    $result = [pscustomobject]@{ Ok = $true; Started = $false; Pid = 0; StartTicks = ''; Offline = $false }
    $node = Find-Node
    $serverExe = $null
    if (-not $node.Path) { $serverExe = Find-ServerExe }
    $fingerprint = ''
    if ($node.Path) { $fingerprint = Get-ServerFingerprint }
    elseif ($serverExe) { $fingerprint = 'exe:' + (Get-FileHash -Algorithm SHA256 -LiteralPath $serverExe).Hash }

    $owner = Get-PortOwner $ServerPort
    if ($owner) {
        $process = Get-Process -Id $owner -ErrorAction SilentlyContinue
        $name = '?'
        if ($process) { $name = $process.ProcessName }
        $state = Read-ServerState
        $managed = $process -and $state -and [int]$state.pid -eq $owner -and [string]$state.started -eq (Get-ProcessStartTicks $process)
        $known = $managed -or $name -eq 'CraDevServer'
        if (-not $known -and $name -eq 'node') {
            $commandLine = Get-ProcessCommandLine $owner
            $known = ((-not $commandLine) -or $commandLine -match 'server\.js') -and (Test-CraDevServer)
        }
        if (-not $known) {
            Write-Fail "$ServerPort portni boshqa dastur band qilgan: $name (PID $owner). Uni yoping (eski CraDev serveri bo'lsa, Vazifalar dispetcherida shu PID'ni to'xtating)."
            $result.Ok = $false
            return $result
        }

        $stale = $null
        if ($managed) {
            if ($fingerprint -and [string]$state.fingerprint -ne $fingerprint) { $stale = "server kodi (Server/src) u yoqilgandan keyin o'zgargan" }
        }
        elseif ($name -eq 'node') {
            $sourceTime = Get-ServerSourceTime
            $startedAt = $null
            try { $startedAt = $process.StartTime } catch { }
            if ($sourceTime -and $startedAt -and $startedAt.ToUniversalTime() -lt $sourceTime) {
                $stale = "server $($startedAt.ToString('yyyy-MM-dd HH:mm')) da yoqilgan, Server/src undan keyin o'zgargan"
            }
        }
        if (-not $stale -and -not (Test-ServerResponds)) { $stale = 'server javob bermayapti' }
        if (-not $stale) {
            Write-Step "Server ishlayapti: http://localhost:$ServerPort (PID $owner)"
            $result.Pid = $owner
            return $result
        }
        if (-not $node.Path -and -not $serverExe) {
            Write-Warn "Server eski ($stale), lekin uni qayta yoqib bo'lmaydi: $($node.Problem). Eski server ishlatiladi."
            $result.Pid = $owner
            return $result
        }
        Write-Warn "Eski server qayta yoqiladi: $stale (PID $owner)."
        Stop-ServerProcess $owner
        if (Get-PortOwner $ServerPort) {
            Write-Fail "Eski serverni to'xtatib bo'lmadi (PID $owner). Vazifalar dispetcherida uni yoping."
            $result.Ok = $false
            return $result
        }
        Clear-ServerState $owner
    }

    if (-not $node.Path -and -not $serverExe) {
        Write-Warn "$($node.Problem). Server yoqilmadi: o'yin 'offline' rejimda ochiladi. Node.js 22.13+ ni o'rnating: https://nodejs.org"
        $result.Offline = $true
        return $result
    }

    New-Item -ItemType Directory -Force $LogDir | Out-Null
    $outLog = Join-Path $LogDir 'server.log'
    $errLog = Join-Path $LogDir 'server-error.log'
    $start = @{ WorkingDirectory = $ServerDir; WindowStyle = $WindowStyle; PassThru = $true }
    if ($node.Path) {
        $start.FilePath = $node.Path
        $start.ArgumentList = @('--disable-warning=ExperimentalWarning', 'src/server.js')
    }
    else {
        $start.FilePath = $serverExe
        Write-Warn "Node.js yo'q ($($node.Problem)): $serverExe ishlatiladi. U yig'ilgan paytdagi server kodi bilan ishlaydi."
    }
    $process = $null
    if ($WindowStyle -eq 'Hidden') {
        try { $process = Start-Process @start -RedirectStandardOutput $outLog -RedirectStandardError $errLog }
        catch { $process = $null }
    }
    if (-not $process) { $process = Start-Process @start }

    for ($i = 0; $i -lt 40; $i++) {
        if ((Test-ServerResponds) -or $process.HasExited) { break }
        Start-Sleep -Milliseconds 250
    }
    if ($process.HasExited -or -not (Test-ServerResponds)) {
        Write-Fail "O'yin serveri ishga tushmadi ($($start.FilePath))."
        if ($WindowStyle -eq 'Hidden') {
            Write-Host "         Log: $outLog, $errLog"
            try { Get-Content -LiteralPath $errLog -Tail 15 -ErrorAction Stop | ForEach-Object { Write-Host "  $_" } } catch { }
        }
        if (-not $process.HasExited) { Stop-ServerProcess $process.Id }
        $result.Ok = $false
        return $result
    }

    Save-ServerState $process $fingerprint
    $result.Started = $true
    $result.Pid = $process.Id
    $result.StartTicks = Get-ProcessStartTicks $process
    if ($WindowStyle -eq 'Hidden') { Write-Step "Server yoqildi: http://localhost:$ServerPort (PID $($process.Id), log: Logs\server.log)" }
    else { Write-Step "Server yoqildi: http://localhost:$ServerPort (oynasi vazifalar panelida)" }
    return $result
}

# Shu skript yoqqan serverni to'xtatadi (PID va boshlanish vaqti hali mos bo'lsa)
function Stop-GameServer($Server) {
    if (-not ($Server -and $Server.Started)) { return }
    $process = Get-Process -Id $Server.Pid -ErrorAction SilentlyContinue
    if ($process -and (Get-ProcessStartTicks $process) -eq $Server.StartTicks) {
        Stop-ServerProcess $Server.Pid
        Write-Step "Server to'xtatildi."
    }
    Clear-ServerState $Server.Pid
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

# O'yinni ochadi. -Wait: o'yin va boshqa CraDev.exe nusxalari yopilguncha kutib, $Server ni (shu skript yoqqan
# bo'lsa) to'xtatadi. Kutmasa, server keyingi ishga tushirishgacha ishlab turadi (launcher uni qayta ishlatadi).
function Start-Game($Server, [switch]$Wait) {
    $game = Start-Process -FilePath $GameExe -WorkingDirectory (Split-Path -Parent $GameExe) -PassThru
    Write-Step "O'yin ochildi. Log: $(Get-PlayerLogPath)"
    if (-not ($Wait -and $Server -and $Server.Started)) { return }

    Write-Host "         O'yin yopilgach server ham to'xtatiladi. Bu oynani yopmang (yopilsa, server keyingi safar qayta ishlatiladi)."
    $game.WaitForExit()
    while (@(Get-Process -Name CraDev -ErrorAction SilentlyContinue).Count -gt 0) { Start-Sleep -Seconds 2 }
    Stop-GameServer $Server
}
