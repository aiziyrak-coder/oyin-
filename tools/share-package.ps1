# Safe, self-contained friend package creation. Compatible with Windows PowerShell 5.1.
# This file defines functions only; dot-sourcing it never starts a server or tunnel.

function Get-SharePackageUrl {
    param([Parameter(Mandatory = $true)][string]$Url, [switch]$LocalOnly)
    $value = $Url.Trim()
    $uri = $null
    if (-not [Uri]::TryCreate($value, [UriKind]::Absolute, [ref]$uri) -or
        $uri.Scheme -notin @('https', 'http') -or -not $uri.Host -or
        $uri.UserInfo -or $uri.Query -or $uri.Fragment -or $uri.AbsolutePath -ne '/' -or
        $value -match '[\s"''%&|<>^!`]' -or $value -match '[^\x21-\x7E]' -or $uri.Host.StartsWith('-')) {
        throw 'Server manzili faqat http(s)://host[:port] shaklida bo''lishi kerak.'
    }
    if ($uri.Scheme -eq 'http') {
        $address = $null
        $loopback = $uri.DnsSafeHost -eq 'localhost'
        if ([Net.IPAddress]::TryParse($uri.DnsSafeHost.Trim('[', ']'), [ref]$address)) {
            $loopback = [Net.IPAddress]::IsLoopback($address)
        }
        if (-not $LocalOnly -or -not $loopback) {
            throw 'Internet orqali ulashish uchun HTTPS kerak. HTTP faqat -LocalOnly va shu kompyuter manzilida mumkin.'
        }
    }
    return $uri.GetLeftPart([UriPartial]::Authority).TrimEnd('/')
}

function Assert-SharePackagePlainPath {
    param([Parameter(Mandatory = $true)][string]$Path)
    # Check ancestors as well as the selected object: a junction above it also changes the target.
    $current = [IO.Path]::GetFullPath($Path)
    while ($current) {
        if (Test-Path -LiteralPath $current) {
            $item = Get-Item -LiteralPath $current -Force -ErrorAction Stop
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw 'Havola yoki junction orqali ZIP yaratish mumkin emas.'
            }
        }
        $parent = [IO.Directory]::GetParent($current)
        if ($null -eq $parent) { break }
        $current = $parent.FullName
    }
}

function Test-SharePackageExcluded {
    param([Parameter(Mandatory = $true)][IO.FileSystemInfo]$Item)
    $name = $Item.Name
    if ($name -like '*DoNotShip*' -or $name -like '.env*' -or
        $name -in @('.git', '.svn', '.hg', '.npmrc', '.netrc', 'node_modules', 'Logs', 'Server',
            'server.txt', 'build-stamp.txt', 'credentials.json', 'credentials.xml', 'secrets.json',
            'tokens.json', 'id_rsa', 'id_ed25519')) { return $true }
    if ($name -match '\.(db|sqlite|sqlite3)(-(wal|shm|journal))?$' -or
        $Item.Extension -in @('.log', '.pdb', '.mdb', '.dmp', '.pem', '.key', '.pfx', '.p12')) { return $true }
    return $false
}

function Get-SharePackageFiles {
    param([Parameter(Mandatory = $true)][string]$GameDirectory)
    $files = New-Object 'System.Collections.Generic.List[System.IO.FileInfo]'
    $queue = New-Object 'System.Collections.Generic.Queue[System.IO.DirectoryInfo]'
    $rootFiles = @('CraDev.exe', 'UnityPlayer.dll', 'UnityCrashHandler64.exe', 'DirectML.dll', 'GameAssembly.dll', 'baselib.dll', 'ASSET-CREDITS.txt')
    $rootDirectories = @('CraDev_Data', 'MonoBleedingEdge', 'D3D12')
    foreach ($item in Get-ChildItem -LiteralPath $GameDirectory -Force -ErrorAction Stop) {
        if ($item.Name -notin ($rootFiles + $rootDirectories)) { continue }
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "ZIPga kiritiladigan fayl havola/junction bo'lishi mumkin emas: $($item.Name)"
        }
        if ($item.Name -in $rootDirectories) {
            if (-not $item.PSIsContainer) { throw "O'yin papkasi noto'g'ri: $($item.Name)" }
            $queue.Enqueue($item)
        }
        else {
            if ($item.PSIsContainer) { throw "O'yin fayli noto'g'ri: $($item.Name)" }
            $files.Add($item)
        }
    }
    while ($queue.Count -gt 0) {
        $directory = $queue.Dequeue()
        foreach ($item in Get-ChildItem -LiteralPath $directory.FullName -Force -ErrorAction Stop) {
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "O'yin ichida havola/junction topildi: $($item.Name)"
            }
            if (Test-SharePackageExcluded $item) { continue }
            if ($item.PSIsContainer) { $queue.Enqueue($item) }
            else { $files.Add($item) }
        }
    }
    return $files.ToArray()
}

function New-SharePackage {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$GameDirectory,
        [Parameter(Mandatory = $true)][string]$OutputDirectory,
        [Parameter(Mandatory = $true)][string]$Url,
        [switch]$LocalOnly
    )
    $ErrorActionPreference = 'Stop'
    $endpoint = Get-SharePackageUrl -Url $Url -LocalOnly:$LocalOnly
    if (-not [IO.Path]::IsPathRooted($GameDirectory) -or -not [IO.Path]::IsPathRooted($OutputDirectory)) {
        throw 'Oyin va ZIP papkalari toliq manzil bilan berilishi kerak.'
    }
    $source = [IO.Path]::GetFullPath($GameDirectory).TrimEnd('\', '/')
    $destination = [IO.Path]::GetFullPath($OutputDirectory).TrimEnd('\', '/')
    if (-not (Test-Path -LiteralPath $source -PathType Container)) { throw 'Oyin papkasi topilmadi.' }
    $sourcePrefix = $source + [IO.Path]::DirectorySeparatorChar
    $destinationPrefix = $destination + [IO.Path]::DirectorySeparatorChar
    if ($source.Equals($destination, [StringComparison]::OrdinalIgnoreCase) -or
        $destinationPrefix.StartsWith($sourcePrefix, [StringComparison]::OrdinalIgnoreCase) -or
        $sourcePrefix.StartsWith($destinationPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'ZIP papkasi asl oyin papkasidan alohida bolishi kerak.'
    }
    Assert-SharePackagePlainPath $source
    Assert-SharePackagePlainPath $destination
    $parent = [IO.Directory]::GetParent($destination)
    if ($null -eq $parent -or -not (Test-Path -LiteralPath $parent.FullName -PathType Container)) {
        throw 'ZIP uchun mavjud ota papka va yangi alohida papka kerak.'
    }
    foreach ($required in @('CraDev.exe', 'UnityPlayer.dll')) {
        $path = Join-Path $source $required
        if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or (Get-Item -LiteralPath $path).Length -eq 0) {
            throw "Oyin fayli topilmadi yoki bosh: $required"
        }
    }
    if (-not (Test-Path -LiteralPath (Join-Path $source 'CraDev_Data') -PathType Container)) {
        throw 'CraDev_Data papkasi topilmadi.'
    }
    $zip = $destination + '.zip'
    $partial = $destination + '.partial.zip'
    foreach ($path in @($destination, $zip, $partial)) {
        if (Test-Path -LiteralPath $path) { throw 'Bu ZIP yoki papka allaqachon mavjud. Yangi nom tanlang; eski nusxa ozgartirilmadi.' }
    }
    # Enumerate and validate the entire selected tree BEFORE creating output. Never follow reparse points.
    $files = @(Get-SharePackageFiles -GameDirectory $source)
    $sourceBytes = ($files | Measure-Object -Property Length -Sum).Sum
    Write-Host ("[NewWorld] ZIP uchun {0} ta oyin fayli tayyorlanmoqda ({1:N0} MB)..." -f $files.Count, ($sourceBytes / 1MB))
    New-Item -ItemType Directory -Path $destination -ErrorAction Stop | Out-Null
    foreach ($file in $files) {
        # Re-check in case the source was replaced while copying a large build.
        Assert-SharePackagePlainPath $file.FullName
        $relative = $file.FullName.Substring($sourcePrefix.Length)
        $target = Join-Path $destination $relative
        $targetParent = Split-Path -Parent $target
        if (-not (Test-Path -LiteralPath $targetParent)) { New-Item -ItemType Directory -Path $targetParent -Force | Out-Null }
        Copy-Item -LiteralPath $file.FullName -Destination $target -ErrorAction Stop
    }
    # Keep essential Unity directories even if a small fixture/optional runtime directory was empty.
    foreach ($directoryName in @('CraDev_Data', 'MonoBleedingEdge', 'D3D12')) {
        if (Test-Path -LiteralPath (Join-Path $source $directoryName) -PathType Container) {
            $target = Join-Path $destination $directoryName
            if (-not (Test-Path -LiteralPath $target)) { New-Item -ItemType Directory -Path $target | Out-Null }
        }
    }
    [IO.File]::WriteAllText((Join-Path $destination 'server.txt'), $endpoint + "`r`n", [Text.Encoding]::ASCII)
    $launcher = @'
@echo off
setlocal
cd /d "%~dp0"
if not exist "%~dp0CraDev_Data\" (
  echo Avval ZIPni toliq oching: Extract All. Keyin Start-NewWorld.cmd ni bosing.
  pause
  exit /b 1
)
start "" "%~dp0CraDev.exe" -server "__SERVER_URL__"
endlocal
'@
    $launcher = $launcher.Replace('__SERVER_URL__', $endpoint).Replace("`r`n", "`n").Replace("`n", "`r`n") + "`r`n"
    [IO.File]::WriteAllText((Join-Path $destination 'Start-NewWorld.cmd'), $launcher, [Text.Encoding]::ASCII)
    $instructions = @'
NEWWORLD - DOSTLAR BILAN SINOV

1. ZIP ustiga ong tugma bosing va Extract All orqali HAMMASINI oching.
2. Ochilgan papkada Start-NewWorld.cmd ni ikki marta bosing.
3. Birinchi kirishda profilingizni yarating. Avvalgi profil odatdagidek ochiladi.
4. Mezbon bilan bir serverdasiz. NewWorldga kirish tugmasi orqali olamga kiring.

Mezbon kompyuteri, interneti va ulashish xizmati yoqilgan tursin.
Share-Test oynasini yopish mumkin. Stop-Share-Test ulashish xizmatini toxtatadi.
Faol Share-Test qayta ochilsa, ayni manzil saqlanadi. Tunnel manzili vaqtinchalik.
Ulashish toxtatilib qayta yoqilsa, mezbon yangi ZIP yuborishi kerak.
Aloqa bolmasa, mezbon bilan boglaning. Eski ZIP eski manzilga ulanadi.
Bu sinov versiyasi: internet kechikishi va grafik talablar kompyuterga bogliq.
Node.js yoki Unity dostning kompyuteriga ornatilishi shart emas (Windows x64).

BOSHQARUV
W A S D - yurish; sichqoncha - qarash; Shift - yugurish; Space - sakrash.
E - yaqin shaxmat doskasi; M - mikrofon; N - eshitish; Esc - sozlamalar.
Ovoz uchun mikrofon ruxsati va quloqchin tavsiya etiladi.

Papkadagi CraDev_Data va boshqa oyin fayllarini alohida ajratmang.
Bu ZIPda mezbonning bazasi, parollari yoki mahalliy profili bolmaydi.
'@
    if ($LocalOnly) {
        $instructions = "FAQAT SHU KOMPYUTER UCHUN TEKSHIRUV. BU ZIP INTERNET ORQALI DOSTGA ULANMAYDI.`r`n`r`n" + $instructions
    }
    [IO.File]::WriteAllText((Join-Path $destination 'READ-ME.txt'), $instructions.Replace("`r`n", "`n").Replace("`n", "`r`n") + "`r`n", [Text.Encoding]::ASCII)
    $notices = @'
NewWorld test build - third-party asset information

City material sources (CC0): Poly Haven, https://polyhaven.com/license
Asphalt 02 / Rob Tuytel: https://polyhaven.com/a/asphalt_02
Marble 01 / Rob Tuytel: https://polyhaven.com/a/marble_01
Black Walnut Veneer 02 / Jenelle van Heerden: https://polyhaven.com/a/black_walnut_veneer_02

The Unity player runtime and other bundled game assets remain subject to their
respective licenses. This test package is not a source asset redistribution license.
'@
    [IO.File]::WriteAllText((Join-Path $destination 'THIRD-PARTY-NOTICES.txt'), $notices.Replace("`r`n", "`n").Replace("`n", "`r`n") + "`r`n", [Text.Encoding]::ASCII)
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    Write-Host '[NewWorld] ZIP siqilmoqda. Katta oyin uchun bir necha daqiqa kerak bolishi mumkin...'
    $archive = [IO.Compression.ZipFile]::Open($partial, [IO.Compression.ZipArchiveMode]::Create)
    try {
        $archiveFiles = @(Get-ChildItem -LiteralPath $destination -Recurse -File -Force | Sort-Object FullName)
        $lastPercent = -10
        for ($i = 0; $i -lt $archiveFiles.Count; $i++) {
            $file = $archiveFiles[$i]
            $entryName = $file.FullName.Substring($destinationPrefix.Length).Replace('\', '/')
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $entryName, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
            $percent = [int](100 * ($i + 1) / $archiveFiles.Count)
            if ($percent -ge $lastPercent + 10 -or $i -eq $archiveFiles.Count - 1) {
                Write-Host "[NewWorld] ZIP: $percent%"
                $lastPercent = $percent
            }
        }
        foreach ($directoryName in @('CraDev_Data', 'MonoBleedingEdge', 'D3D12')) {
            $target = Join-Path $destination $directoryName
            if ((Test-Path -LiteralPath $target -PathType Container) -and @(Get-ChildItem -LiteralPath $target -Force).Count -eq 0) {
                $archive.CreateEntry($directoryName + '/') | Out-Null
            }
        }
    }
    finally { $archive.Dispose() }
    # File.Move does not overwrite an existing ZIP, including a competing run's result.
    [IO.File]::Move($partial, $zip)
    $size = (Get-Item -LiteralPath $zip).Length
    Write-Host ("[NewWorld] ZIP tayyor: {0:N1} MB" -f ($size / 1MB))
    return [pscustomobject]@{ Directory = $destination; Zip = $zip; Bytes = [long]$size }
}
