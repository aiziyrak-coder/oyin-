<#
.SYNOPSIS
  CraDev loyihasini Unity oynasini ochmasdan (batchmode) boshqarish.

.DESCRIPTION
  Buyruqlar:
    check      loyihani ochadi, skriptlarni kompilyatsiya qiladi va chiqadi (xatolarni ko'rsatadi)
    scenes     barcha sahnalarni qayta yaratadi ("CraDev > Sahnalarni yaratish" bilan bir xil)
    build      barcha sahnalarni yaratadi va o'yinni Builds\LobbyV2\ ga yig'adi (grafik karta bo'lsa
               avatar kartalari va yuz/kiyim xaritalari ham qayta chiziladi)
    lobby      faqat lobbyni (MainMenu + WorldSandbox) qayta yaratib Builds\LobbyV2\ ga yig'adi
    run        build + serverni (kerak bo'lsa) yoqib o'yinni alohida oynada ishga tushiradi
    play       Play-LobbyV2.cmd bilan bir xil: build eskirgan bo'lsa lobbyni yig'adi, server bilan ochadi
    playerlog  oxirgi ishga tushirilgan o'yinning logini (Player.log) ko'rsatadi
    open       loyihani Unity tahrirlovchisida ochadi
    where      qaysi Unity.exe ishlatilishini ko'rsatadi

  Hamma buyruqlar bitta o'yin papkasini ishlatadi: Builds\LobbyV2\CraDev.exe (build-stamp.txt bilan).
  Unity.exe Unity Hub o'rnatgan papkalardan qidiriladi. Boshqa joyda bo'lsa:
  UNITY_EXE muhit o'zgaruvchisi yoki -Unity parametri.

  Batchmode loyiha Unity'da ochiq bo'lmaganda ishlaydi. Loglar: Logs\batch-<buyruq>.log
  (lobby: Logs\lobby-v2-build.log).
  Chiqish kodi: 0 - muvaffaqiyat, 1 - Unity xatosi, 2 - loyiha ochiq yoki Unity topilmadi.

.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File tools\unity.ps1 scenes
.EXAMPLE
  tools\unity.cmd run
#>
param(
    [Parameter(Position = 0)]
    [ValidateSet('check', 'scenes', 'build', 'lobby', 'run', 'play', 'playerlog', 'open', 'where')]
    [string]$Command = 'check',

    [string]$Unity = $env:UNITY_EXE
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')

function Exit-OnError([int]$Code) { if ($Code -ne 0) { exit $Code } }

switch ($Command) {
    'where' {
        Write-Host "Loyiha:        $ProjectRoot"
        Write-Host "Loyiha Unity:  $(Get-ProjectVersion)"
        Write-Host "Ishlatiladi:   $(Find-Unity $Unity)"
        foreach ($e in Get-InstalledEditors) { Write-Host "  o'rnatilgan: $($e.Version)  $($e.Path)" }
        Write-Host "O'yin:         $GameExe"
    }
    'check' {
        Exit-OnError (Invoke-UnityBatch 'check' @() $Unity)
    }
    'scenes' {
        Exit-OnError (Invoke-UnityBatch 'scenes' @('-executeMethod', "$BatchClass.CreateScenes") $Unity)
    }
    'build' {
        Exit-OnError (Invoke-GameBuild -Full -UnityOverride $Unity)
    }
    'lobby' {
        Exit-OnError (Invoke-GameBuild -UnityOverride $Unity)
    }
    'run' {
        Exit-OnError (Invoke-GameBuild -Full -UnityOverride $Unity)
        $server = Start-GameServer -WindowStyle Minimized
        if (-not $server.Ok) { exit 1 }
        Write-Step "O'yin ishga tushirilmoqda. Yopish: Alt+F4."
        Start-Game $server
    }
    'play' {
        & (Join-Path $PSScriptRoot 'lobby.ps1') -Unity $Unity
        exit $LASTEXITCODE
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
        $exe = Find-Unity $Unity
        if (-not $exe) { exit 2 }
        Write-Step "Unity ochilmoqda: $exe"
        Start-Process -FilePath $exe -ArgumentList @('-projectPath', "`"$ProjectRoot`"")
    }
}
