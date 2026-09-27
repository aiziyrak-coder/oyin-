#!/usr/bin/env bash
# CDCGroup logo qatlamlari va ovozini qayta yaratib, Unity loyihasiga joylaydi.
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
art="$here/../../Assets/CraDev/CDCGroup/Art"
audio="$here/../../Assets/CraDev/CDCGroup/Audio"
out="$here/.out"
mkdir -p "$out" "$art" "$audio"
node "$here/../tools/render_layers.js" "$here/compose.html" "$out" cdc line group
python3 "$here/make_textures.py" "$out" "$art" "$here/layout.json"
python3 "$here/make_sound.py" "$audio/CDC_Sound.wav"
python3 -c "from PIL import Image; Image.open('$out/_preview_2x.png').convert('RGB').resize((1920, 1080), Image.LANCZOS).save('$here/preview.png', optimize=True)"
echo "Tayyor: $art, $audio"
