#!/usr/bin/env bash
# O'yin serverini Node.js o'rnatilmagan kompyuterda ham ishlaydigan bitta faylga yig'adi
# (Node.js "single executable application"). Server kodi o'zgarmaydi: Server/src/server.js.
#
# Ishlatish: tools/ci/server-exe.sh <natija papkasi>
# Natija: CraDevServer.exe (Windows x64) va cradev-server (Linux x64, CI'da tekshirish uchun).
# Kerak: Node.js 22.13+ va internet (esbuild, postject va Windows uchun node.exe yuklanadi).

set -euo pipefail

OUT=$(mkdir -p "$1" && cd "$1" && pwd)
ROOT=$(cd "$(dirname "$0")/../.." && pwd)
WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT
NODE_VERSION=$(node -v)

# 1. Server bitta CommonJS faylga. SQLite'ning "experimental" ogohlantirishi o'yinchiga ko'rsatilmaydi.
npx -y esbuild@0.25 "$ROOT/Server/src/server.js" --bundle --platform=node --format=cjs --target=node22 \
    --banner:js="process.removeAllListeners('warning');" --outfile="$WORK/server.cjs" --log-level=warning

# 2. Node ichiga joylanadigan blob
cat > "$WORK/sea-config.json" <<EOF
{ "main": "$WORK/server.cjs", "output": "$WORK/sea.blob", "disableExperimentalSEAWarning": true }
EOF
node --experimental-sea-config "$WORK/sea-config.json"

inject() {
    npx -y postject@1.0.0-alpha.6 "$1" NODE_SEA_BLOB "$WORK/sea.blob" \
        --sentinel-fuse NODE_SEA_FUSE_fce680ab2cc467b6e072b8b5df1996b2 2>&1 | grep -v "string offset" || true
}

# 3. Linux: shu kompyuterdagi node
cp "$(command -v node)" "$OUT/cradev-server"
chmod u+w "$OUT/cradev-server"
inject "$OUT/cradev-server"

# 4. Windows: xuddi shu versiyadagi rasmiy node.exe (blob versiyasi mos bo'lishi shart)
curl -fsSL -o "$OUT/CraDevServer.exe" "https://nodejs.org/dist/$NODE_VERSION/win-x64/node.exe"
inject "$OUT/CraDevServer.exe"

ls -la "$OUT"
