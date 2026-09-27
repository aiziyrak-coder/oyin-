#!/usr/bin/env bash
# UI ikonkalari va 9-slice spritelarni qayta yaratib, Unity loyihasiga joylaydi.
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
art="$here/../../Assets/CraDev/UI/Art"
mkdir -p "$art"
node "$here/render_icons.js" "$art"
python3 "$here/make_sprites.py" "$art"
echo "Tayyor: $art"
