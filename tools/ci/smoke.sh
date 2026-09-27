#!/usr/bin/env bash
# CI tekshiruvi: o'yinning Linux build'ini virtual ekranda (Xvfb) server bilan birga ishga tushiradi va
# o'yinchi kabi o'tadi: intro'lar va Loading'ni kutadi, nickname yozadi, avatar tanlaydi, qahramonni
# aylantiradi va "Create character" ni bosadi. Ekrandagi yozuvlar OCR (tesseract) bilan tekshiriladi.
#
# Ishlatish: tools/ci/smoke.sh <build papkasi> [natija papkasi]
# Natija: video, skrinshotlar, OCR matnlari, Player.log, server.log va summary.md.
# Biror tekshiruv o'tmasa chiqish kodi 1.
#
# Kerakli paketlar: xvfb xdotool ffmpeg imagemagick tesseract-ocr curl, Node.js 22.13+.
# GAME_CMD o'zgaruvchisi o'yin o'rniga boshqa buyruqni ishga tushirish uchun (skriptni sinash uchun).

set -uo pipefail

GAME_DIR=${1:-game}
OUT=$(mkdir -p "${2:-smoke}" && cd "${2:-smoke}" && pwd)
ROOT=$(cd "$(dirname "$0")/../.." && pwd)
NICK="Tester$((RANDOM % 9000 + 1000))"
PORT=8080
export DISPLAY=:99

mkdir -p "$OUT/frames"
: > "$OUT/checks.txt"
PIDS=()

log() { echo "[smoke] $*"; }

cleanup() {
    for pid in "${PIDS[@]}"; do kill "$pid" 2>/dev/null; done
}
trap cleanup EXIT

# ---- Natijalar: har bir tekshiruv PASS yoki FAIL ----
check() { # $1 = nomi, $2 = 0 (o'tdi) yoki boshqa
    if [ "$2" -eq 0 ]; then echo "PASS|$1" >> "$OUT/checks.txt"; log "PASS  $1"
    else echo "FAIL|$1" >> "$OUT/checks.txt"; log "FAIL  $1"; fi
}

# ---- Ekran: skrinshot va OCR ----
# OCR matni bo'shliqlarsiz va kichik harflarda saqlanadi: "L O A D I N G" -> "loading".
# Ikki o'tish: qora fondagi och yozuvlar (22%) va rangli tugmalardagi oq yozuvlar (75%).
ocr() { # $1 = rasm, natija stdout'ga
    local level
    for level in 22 75; do
        convert "$1" -colorspace Gray -threshold "$level%" -negate "$OUT/.ocr.png" 2>/dev/null &&
            tesseract "$OUT/.ocr.png" - 2>/dev/null
    done | tr -d ' \n\r\t' | tr '[:upper:]' '[:lower:]'
}

shot() { # $1 = nomi
    import -window root "$OUT/$1.png" 2>/dev/null
    ocr "$OUT/$1.png" > "$OUT/$1.txt"
}

seen() { # $1 = nomi, $2 = regex (kichik harflarda, bo'shliqsiz)
    grep -qE "$2" "$OUT/$1.txt"
}

game_alive() { kill -0 "$GAME_PID" 2>/dev/null; }

wait_for() { # $1 = nomi, $2 = regex, $3 = kutish (s)
    local deadline=$((SECONDS + $3))
    while [ $SECONDS -lt "$deadline" ]; do
        shot "$1"
        seen "$1" "$2" && return 0
        game_alive || { log "o'yin yopilib qoldi"; return 1; }
        sleep 0.5
    done
    return 1
}

click() { xdotool mousemove "$1" "$2" sleep 0.15 click 1; }

# ---- 1. Server ----
DB_PATH="$OUT/cradev.db" PORT=$PORT node --disable-warning=ExperimentalWarning "$ROOT/Server/src/server.js" > "$OUT/server.log" 2>&1 &
PIDS+=($!)
for _ in $(seq 1 40); do curl -sf "http://localhost:$PORT/health" > /dev/null && break; sleep 0.25; done
curl -sf "http://localhost:$PORT/health" > /dev/null
check "server ishga tushdi" $?

# ---- 2. Virtual ekran va video ----
Xvfb :99 -screen 0 1920x1080x24 -nolisten tcp > /dev/null 2>&1 &
PIDS+=($!)
sleep 1
ffmpeg -loglevel error -y -f x11grab -video_size 1920x1080 -framerate 15 -i :99 \
    -c:v libx264 -preset veryfast -crf 28 -pix_fmt yuv420p "$OUT/gameplay.mp4" < /dev/null &
FFMPEG_PID=$!
VIDEO_START=$SECONDS

# ---- 3. O'yin ----
if [ -n "${GAME_CMD:-}" ]; then
    bash -c "$GAME_CMD" > "$OUT/Player.log" 2>&1 &
else
    GAME=""
    for name in CraDev CraDev.x86_64; do
        [ -f "$GAME_DIR/$name" ] && GAME="$GAME_DIR/$name" && break
    done
    if [ -z "$GAME" ]; then log "o'yin fayli topilmadi: $GAME_DIR"; ls -la "$GAME_DIR"; exit 1; fi
    chmod +x "$GAME"
    # GPU yo'q: Mesa'ning dasturiy OpenGL'i (llvmpipe)
    LIBGL_ALWAYS_SOFTWARE=1 "$GAME" -force-glcore -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 \
        -logFile "$OUT/Player.log" &
fi
GAME_PID=$!
PIDS+=("$GAME_PID")
log "o'yin ishga tushdi (pid $GAME_PID), nickname: $NICK"

# ---- 4. Avatar yaratish ekrani (intro'lar va Loading'dan keyin) ----
wait_for 04-character-creation 'createyour|nickname' 120
check "avatar yaratish ekrani ochildi" $?
CREATION_AT=$((SECONDS - VIDEO_START))

# Oyna menejeri yo'q: klaviatura o'yin oynasiga borishi uchun fokusni o'zimiz beramiz
WINDOW=$(xdotool search --onlyvisible --name 'CraDev' 2>/dev/null | head -1)
[ -n "$WINDOW" ] && xdotool windowfocus "$WINDOW" 2>/dev/null

# Joylashuv 1920x1080 da (CraDevSceneBuilder.BuildCharacterCreation): forma chapda x=120, y=115 dan boshlanadi
click 360 446                       # nickname maydoni
sleep 0.3
xdotool type --delay 90 "$NICK"
wait_for 05-nickname-available 'nicknameisavailable' 15
check "nickname server'da tekshirildi (available)" $?

click 262 628                       # 2-avatar kartasi
sleep 0.8
shot 06-avatar-2

# Qahramonni sichqoncha bilan aylantirish (3D qahramon ekranning o'ng qismida, x ~ 1286)
xdotool mousemove 1290 500 mousedown 1
for _ in $(seq 1 16); do xdotool mousemove_relative -- 14 0; sleep 0.03; done
shot 07-rotating
xdotool mouseup 1
sleep 1.5
shot 08-rotated
# Faqat qahramon turgan joy solishtiriladi (kursor va boshqa elementlar hisobga olinmaydi)
stage='[800x820+890+120]'
diff=$(compare -metric AE -fuzz 8% "$OUT/06-avatar-2.png$stage" "$OUT/08-rotated.png$stage" null: 2>&1 | cut -d' ' -f1)
log "aylanishdan keyin o'zgargan piksellar: $diff"
rotated=1
[ "${diff%.*}" -gt 5000 ] 2>/dev/null && rotated=0
check "qahramon aylandi (rasm o'zgardi)" "$rotated"

click 1378 984                      # "Back" tugmasi
sleep 1.5
shot 09-back
click 1194 984                      # "Front" tugmasi
sleep 1.5
shot 10-front

click 360 897                       # "Create character"
wait_for 11-created 'welcome' 10
check "profil yaratildi (Welcome, ...)" $?

wait_for 12-main-menu 'welcomeback|customize' 40
check "bosh menyu ochildi (Welcome back, Play, Customize)" $?

# Esc: chiqishni tasdiqlash oynasi chiqadi, "Cancel" bilan yopiladi
xdotool key Escape
wait_for 13-quit-dialog 'quitgame' 10
check "Esc bosilganda 'Quit game?' so'raladi" $?
xdotool key Escape

# Server nickname'ni band qilgan bo'lishi kerak
AVAIL=$(curl -sf "http://localhost:$PORT/api/nicknames/availability?name=$NICK")
echo "$AVAIL" > "$OUT/availability.json"
echo "$AVAIL" | grep -q '"available":false'
check "server'da $NICK band qilindi" $?

sleep 2
kill -INT "$FFMPEG_PID" 2>/dev/null
wait "$FFMPEG_PID" 2>/dev/null
kill "$GAME_PID" 2>/dev/null
wait "$GAME_PID" 2>/dev/null

# ---- 5. Intro'lar videodan: avatar ekranigacha bo'lgan kadrlar OCR qilinadi ----
ffmpeg -loglevel error -y -t "$CREATION_AT" -i "$OUT/gameplay.mp4" -vf fps=3 "$OUT/frames/f_%03d.png" < /dev/null
: > "$OUT/frames/ocr.txt"
for f in "$OUT"/frames/f_*.png; do
    [ -f "$f" ] || continue
    echo "$(basename "$f") $(ocr "$f")" >> "$OUT/frames/ocr.txt"
done
grep -qE 'neweraof|cradev' "$OUT/frames/ocr.txt"
check "CraDev intro ko'rindi" $?
grep -qE 'cdc|group' "$OUT/frames/ocr.txt"
check "CDCGroup ko'rindi" $?
grep -qE 'loading|[0-9]+%' "$OUT/frames/ocr.txt"
check "Loading ko'rindi" $?

# ---- 6. Player.log: istisnolar bo'lmasligi kerak ----
if [ -f "$OUT/Player.log" ]; then
    grep -nE 'Exception|error CS[0-9]+' "$OUT/Player.log" | grep -v 'ExperimentalWarning' > "$OUT/player-errors.txt"
    [ ! -s "$OUT/player-errors.txt" ]
    check "Player.log da xato yo'q" $?
fi

# ---- 7. Umumiy rasm va hisobot ----
montage "$OUT"/0[4-9]-*.png "$OUT"/1[0-2]-*.png -tile 3x -geometry 640x360+6+6 -background '#111' "$OUT/overview.jpg" 2>/dev/null

{
    echo "## CraDev smoke test"
    echo
    echo "| Tekshiruv | Natija |"
    echo "|---|---|"
    while IFS='|' read -r status name; do
        [ "$status" = PASS ] && echo "| $name | ✅ |" || echo "| $name | ❌ |"
    done < "$OUT/checks.txt"
    echo
    echo "Nickname: \`$NICK\`. Video, skrinshotlar va loglar: **CraDev-smoke** artifact."
} > "$OUT/summary.md"
[ -n "${GITHUB_STEP_SUMMARY:-}" ] && cat "$OUT/summary.md" >> "$GITHUB_STEP_SUMMARY"

echo
echo "===== OCR (asosiy ekranlar) ====="
for t in "$OUT"/[01][0-9]-*.txt; do echo "$(basename "$t" .txt): $(head -c 300 "$t")"; done
echo
echo "===== Player.log: [CraDev] va xatolar ====="
[ -f "$OUT/Player.log" ] && grep -nE '\[CraDev\]|Exception|error' "$OUT/Player.log" | head -60
echo
echo "===== Natija ====="
cat "$OUT/checks.txt"

! grep -q '^FAIL' "$OUT/checks.txt"
