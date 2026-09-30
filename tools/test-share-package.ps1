# Offline regression test. Creates only unique synthetic fixtures under Logs; never starts game/server/tunnel.
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'share-package.ps1')
$projectRoot = Split-Path -Parent $PSScriptRoot
$testRoot = Join-Path $projectRoot ('Logs\share-package-tests-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot | Out-Null
$script:passed = 0

function Assert-PackageTest([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "FAIL: $Message" }
    $script:passed++
    Write-Host "PASS: $Message"
}

function Add-PackageFixtureFile([string]$Root, [string]$Relative, [string]$Content = 'synthetic fixture') {
    $path = Join-Path $Root $Relative
    $directory = Split-Path -Parent $path
    if (-not (Test-Path -LiteralPath $directory)) { New-Item -ItemType Directory -Path $directory -Force | Out-Null }
    [IO.File]::WriteAllText($path, $Content, [Text.Encoding]::ASCII)
}

function New-PackageFixture([string]$Name) {
    $root = Join-Path $testRoot $Name
    New-Item -ItemType Directory -Path $root | Out-Null
    foreach ($name in @('CraDev.exe', 'UnityPlayer.dll', 'UnityCrashHandler64.exe', 'DirectML.dll',
        'CraDev_Data\globalgamemanagers', 'CraDev_Data\Managed\Assembly-CSharp.dll',
        'CraDev_Data\Plugins\x86_64\plugin.dll', 'CraDev_Data\Resources\unity_builtin_extra',
        'MonoBleedingEdge\EmbedRuntime\mono-2.0-bdwgc.dll', 'D3D12\D3D12Core.dll')) {
        Add-PackageFixtureFile $root $name ('safe fixture ' + $name)
    }
    return $root
}

function Assert-PackageThrows([scriptblock]$Action, [string]$Message) {
    $threw = $false
    try { & $Action | Out-Null } catch { $threw = $true }
    Assert-PackageTest $threw $Message
}

$game = New-PackageFixture 'game'
foreach ($path in @('server.txt', 'build-stamp.txt', 'real-user.db', 'Unexpected.dll', 'notes.txt',
    'Server\secret.json', 'CraDev_BurstDebugInformation_DoNotShip\symbols.txt',
    'CraDev_Data\debug.pdb', 'CraDev_Data\debug.mdb', 'CraDev_Data\Player.log',
    'CraDev_Data\.env', 'CraDev_Data\.env.production', 'CraDev_Data\private.pem',
    'CraDev_Data\secrets.json', 'CraDev_Data\tokens.json', 'CraDev_Data\credentials.json',
    'CraDev_Data\nested\world.db', 'CraDev_Data\nested\world.db-wal',
    'CraDev_Data\nested\world.sqlite3-shm', 'CraDev_Data\nested\private.pfx',
    'CraDev_Data\Logs\session.txt', 'CraDev_Data\Hidden_DoNotShip\internal.dll',
    'MonoBleedingEdge\do-not-ship.pdb', 'D3D12\gpu.log')) {
    Add-PackageFixtureFile $game $path 'PRIVATE_FIXTURE_DO_NOT_SHIP'
}
$originalExe = Join-Path $game 'CraDev.exe'
[IO.File]::SetAttributes($originalExe, [IO.FileAttributes]::ReadOnly)
$before = @{}
foreach ($file in Get-ChildItem -LiteralPath $game -Recurse -File -Force) {
    $before[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
}
$output = Join-Path $testRoot 'friend'
$results = @(New-SharePackage -GameDirectory $game -OutputDirectory $output -Url 'https://friends.example.test/')
Assert-PackageTest ($results.Count -eq 1) 'helper emits one result object only'
$result = $results[0]
Assert-PackageTest (($result.PSObject.Properties.Name -join ',') -eq 'Directory,Zip,Bytes') 'result API contains Directory, Zip, Bytes'
Assert-PackageTest ($result.Directory -eq $output -and $result.Zip -eq ($output + '.zip')) 'output paths match unique request'
Assert-PackageTest ($result.Bytes -gt 0 -and $result.Bytes -eq (Get-Item -LiteralPath $result.Zip).Length) 'finished ZIP exists and reports byte size'
Assert-PackageTest (-not (Test-Path -LiteralPath ($output + '.partial.zip'))) 'partial ZIP is published only after completion'
$after = @(Get-ChildItem -LiteralPath $game -Recurse -File -Force)
Assert-PackageTest ($after.Count -eq $before.Count) 'original tree file count is unchanged'
foreach ($file in $after) {
    Assert-PackageTest ((Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash -eq $before[$file.FullName]) ('original content unchanged: ' + $file.Name)
}
Assert-PackageTest (((Get-Item -LiteralPath $originalExe).Attributes -band [IO.FileAttributes]::ReadOnly) -ne 0) 'readonly original remains readonly'
$archive = [IO.Compression.ZipFile]::OpenRead($result.Zip)
try {
    $entryNames = @($archive.Entries | ForEach-Object { $_.FullName })
    $expected = @('CraDev.exe', 'UnityPlayer.dll', 'UnityCrashHandler64.exe', 'DirectML.dll',
        'CraDev_Data/globalgamemanagers', 'CraDev_Data/Managed/Assembly-CSharp.dll',
        'CraDev_Data/Plugins/x86_64/plugin.dll', 'CraDev_Data/Resources/unity_builtin_extra',
        'MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll', 'D3D12/D3D12Core.dll',
        'server.txt', 'Start-NewWorld.cmd', 'READ-ME.txt', 'THIRD-PARTY-NOTICES.txt')
    Assert-PackageTest ($entryNames.Count -eq $expected.Count) 'ZIP has exactly runtime and generated delivery files'
    foreach ($name in $expected) { Assert-PackageTest ($entryNames -contains $name) ('ZIP includes ' + $name) }
    foreach ($entry in $archive.Entries) {
        Assert-PackageTest (-not [IO.Path]::IsPathRooted($entry.FullName) -and $entry.FullName -notmatch '(^|/)\.\.(/|$)') ('relative safe archive entry: ' + $entry.FullName)
        $reader = New-Object IO.StreamReader($entry.Open())
        try { $content = $reader.ReadToEnd() } finally { $reader.Dispose() }
        Assert-PackageTest (-not $content.Contains('PRIVATE_FIXTURE_DO_NOT_SHIP')) ('private fixture excluded: ' + $entry.FullName)
        Assert-PackageTest (-not $content.Contains($testRoot) -and -not $content.Contains($env:USERPROFILE)) ('no host path: ' + $entry.FullName)
        if ($entry.FullName -eq 'server.txt') { Assert-PackageTest ($content.Trim() -eq 'https://friends.example.test') 'server.txt contains canonical endpoint' }
        if ($entry.FullName -eq 'Start-NewWorld.cmd') {
            Assert-PackageTest ($content.Contains('-server "https://friends.example.test"')) 'launcher explicitly selects friend endpoint, overriding stale environment'
            Assert-PackageTest ($content.Contains('%~dp0CraDev.exe') -and $content.Contains('Extract All')) 'launcher is portable and explains extraction'
        }
        if ($entry.FullName -eq 'READ-ME.txt') {
            Assert-PackageTest ($content.Contains('Share-Test oynasi') -and $content.Contains('vaqtinchalik')) 'friend instructions explain host uptime and temporary tunnel'
            Assert-PackageTest ($content.Contains('oynasini yopish mumkin') -and $content.Contains('ayni manzil saqlanadi')) 'instructions match persistent sharing and active URL reuse'
        }
    }
} finally { $archive.Dispose() }

$zipHash = (Get-FileHash -LiteralPath $result.Zip -Algorithm SHA256).Hash
Assert-PackageThrows { New-SharePackage -GameDirectory $game -OutputDirectory $output -Url 'https://friends.example.test' } 'existing output is never overwritten'
Assert-PackageTest ((Get-FileHash -LiteralPath $result.Zip -Algorithm SHA256).Hash -eq $zipHash) 'existing ZIP bytes stay identical'
Assert-PackageThrows { New-SharePackage -GameDirectory $game -OutputDirectory (Join-Path $game 'bad-output') -Url 'https://friends.example.test' } 'nested output source rejected'
Assert-PackageThrows { New-SharePackage -GameDirectory $game -OutputDirectory $testRoot -Url 'https://friends.example.test' } 'source inside output rejected'
Assert-PackageThrows { New-SharePackage -GameDirectory 'relative-game' -OutputDirectory (Join-Path $testRoot 'relative') -Url 'https://friends.example.test' } 'relative source rejected'
$missing = Join-Path $testRoot 'missing-game'
New-Item -ItemType Directory -Path $missing | Out-Null
Assert-PackageThrows { New-SharePackage -GameDirectory $missing -OutputDirectory (Join-Path $testRoot 'missing-result') -Url 'https://friends.example.test' } 'missing Unity runtime rejected'
Assert-PackageTest (-not (Test-Path -LiteralPath (Join-Path $testRoot 'missing-result'))) 'invalid source leaves no package'

$invalidUrls = @('http://friends.example.test', 'http://127.0.0.1:8080', 'ftp://friends.example.test',
    'https://user:password@friends.example.test', 'https://friends.example.test/api',
    'https://friends.example.test?token=bad', 'https://friends.example.test#bad',
    'https://friends.example.test/%22', 'https://friends.example.test/ & calc', 'not-an-url')
$index = 0
foreach ($badUrl in $invalidUrls) {
    $index++
    $badOutput = Join-Path $testRoot ('invalid-url-' + $index)
    Assert-PackageThrows { New-SharePackage -GameDirectory $game -OutputDirectory $badOutput -Url $badUrl } ('invalid/unsafe URL rejected: case ' + $index)
    Assert-PackageTest (-not (Test-Path -LiteralPath $badOutput)) ('URL validation creates no output: case ' + $index)
}
Assert-PackageThrows { Get-SharePackageUrl -Url 'http://192.168.0.1:8080' -LocalOnly } 'LocalOnly does not permit LAN/internet HTTP'
$ipv6 = [Uri](Get-SharePackageUrl -Url 'http://[::1]:8080' -LocalOnly)
Assert-PackageTest ($ipv6.IsLoopback -and $ipv6.Port -eq 8080 -and $ipv6.Scheme -eq 'http') 'IPv6 loopback is allowed explicitly'
$local = New-SharePackage -GameDirectory $game -OutputDirectory (Join-Path $testRoot 'local-friend') -Url 'http://127.0.0.1:8088' -LocalOnly
Assert-PackageTest ((Get-Content -LiteralPath (Join-Path $local.Directory 'READ-ME.txt') -Raw).StartsWith('FAQAT SHU KOMPYUTER')) 'local-only package cannot be mistaken for internet sharing'

$junctionGame = New-PackageFixture 'junction-game'
$outside = Join-Path $testRoot 'outside'
New-Item -ItemType Directory -Path $outside | Out-Null
Add-PackageFixtureFile $outside 'secret.txt' 'PRIVATE_FIXTURE_DO_NOT_SHIP'
$junction = Join-Path $junctionGame 'CraDev_Data\linked-folder'
$junctionReady = $false
try { New-Item -ItemType Junction -Path $junction -Target $outside -ErrorAction Stop | Out-Null; $junctionReady = $true }
catch { Write-Host ('SKIP: junction fixture creation unavailable: ' + $_.Exception.Message) }
if ($junctionReady) {
    $junctionOutput = Join-Path $testRoot 'junction-result'
    Assert-PackageThrows { New-SharePackage -GameDirectory $junctionGame -OutputDirectory $junctionOutput -Url 'https://friends.example.test' } 'nested junction is rejected before copying'
    Assert-PackageTest (-not (Test-Path -LiteralPath $junctionOutput)) 'junction rejection leaves no package'
    Assert-PackageThrows { New-SharePackage -GameDirectory $game -OutputDirectory (Join-Path $junction 'bad') -Url 'https://friends.example.test' } 'junction output ancestor is rejected'
}
Write-Host ("SHARE PACKAGE TESTS: {0} passed. Synthetic fixtures: {1}" -f $script:passed, $testRoot)
