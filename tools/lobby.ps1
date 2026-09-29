<#
.SYNOPSIS
  CraDev o'yinini (Builds\LobbyV2\CraDev.exe) server bilan birga ochadi. Play-LobbyV2.cmd shu skriptni chaqiradi.

.DESCRIPTION
  1. Build: exe yo'q bo'lsa yoki Assets, Packages, ProjectSettings ichida build'dan keyin nimadir o'zgargan bo'lsa
     (masalan git pull), lobby avtomatik qayta yig'iladi (CraDevBatch.BuildLobby: MainMenu + WorldSandbox).
     Build vaqti va commit Builds\LobbyV2\build-stamp.txt da. Unity oynasi ochiq bo'lsa, yig'ib bo'lmaydi.
  2. Server: 8080 da yangi CraDev serveri ishlasa, o'shani ishlatadi. Launcher yoqqan server Server/src
     o'zgargandan keyin qayta yoqiladi; qo'lda yoqilgan (npm start) server kod o'zgargandan oldin yoqilgan bo'lsa ham.
     Yangi server yashirin ishlaydi (log: Logs\server.log) va o'yin yopilgach to'xtatiladi.
  3. O'yin ochiladi. Oynadan ishga tushirilganda, shu skript server yoqqan bo'lsa, o'yin yopilguncha kutadi.

  Parametrlar:
    -Build       lobbyni har holda qayta yig'adi
    -Full        barcha 6 sahnani noldan yaratib yig'adi (tools\unity.cmd build bilan bir xil;
                 grafik karta bo'lsa avatar kartalari va yuz/kiyim xaritalari ham qayta chiziladi)
    -NoBuild     build eskirgan bo'lsa ham mavjud exe'ni ochadi
    -NoWait      o'yinni ochib darhol chiqadi (server ishlab qoladi, keyingi safar qayta ishlatiladi)
    -StopServer  launcher yoqqan serverni to'xtatadi va chiqadi
    -Unity       Unity.exe yo'li (standart: UNITY_EXE yoki Unity Hub papkalari)

  Chiqish kodi: 0 - o'yin ochildi, 1 - xato, 2 - Unity topilmadi yoki loyiha Unity'da ochiq,
  3 - o'yin serversiz ('offline') ochildi.

.EXAMPLE
  Play-LobbyV2.cmd
.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File tools\lobby.ps1 -Build -NoWait
#>
param(
    [switch]$Build,
    [switch]$Full,
    [switch]$NoBuild,
    [switch]$NoWait,
    [switch]$StopServer,
    [string]$Unity = $env:UNITY_EXE
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')

if ($StopServer) {
    Stop-ManagedServer
    exit 0
}

$interactive = Test-Interactive
$status = Get-BuildStatus

$reason = $null
if ($Build -or $Full) { $reason = "so'ralgan" }
elseif (-not $status.Exists) { $reason = $status.Reason }
elseif ($status.Stale) {
    if ($NoBuild) { Write-Warn "Build eskirgan ($($status.Reason)), lekin -NoBuild: mavjud build ochiladi." }
    else { $reason = "build eskirgan: $($status.Reason)" }
}

if ($reason) {
    Write-Step "O'yin qayta yig'iladi ($reason)."
    if (-not $Full) {
        Write-Host "         Barcha sahnalar qayta yaratiladi."
    }
    $code = Invoke-GameBuild -Full:$Full -UnityOverride $Unity
    if ($code -ne 0) {
        # Avtomatik qayta yig'ish muvaffaqiyatsiz bo'lsa, eski buildni faqat o'yinchi rozi bo'lsa ochamiz
        if (-not $status.Exists -or $Build -or $Full -or -not $interactive) { exit $code }
        $answer = Read-Host "[CraDev] Eski build ochilsinmi? Unda oxirgi o'zgarishlar yo'q. (h - ha, Enter - yo'q)"
        if ($answer -notmatch '^\s*h') { exit $code }
        Write-Warn "ESKI build ochilmoqda ($($status.Reason))."
    }
    $status = Get-BuildStatus
}
if ($status.Info) { Write-Step "Build: $($status.Info)" }

$server = Start-GameServer -WindowStyle Hidden
if (-not $server.Ok) { exit 1 }

Start-Game $server -Wait:($interactive -and -not $NoWait)
if ($server.Offline) { exit 3 }
exit 0
