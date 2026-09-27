#!/usr/bin/env bash
# CraDev logo qatlamlari, teksturalar va intro ovozini qayta yaratib, Unity loyihasiga joylaydi.
# Talablar: Node.js + playwright (Chromium bilan), Python 3 + numpy + pillow.
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
art="$here/../../Assets/CraDev/Intro/Art"
audio="$here/../../Assets/CraDev/Intro/Audio"
out="$here/.out"
mkdir -p "$out" "$art" "$audio"
node "$here/../tools/render_layers.js" "$here/compose.html" "$out" tile glyph wordmark tagline
python3 "$here/make_textures.py" "$out" "$art" "$here/layout.json"
python3 "$here/make_sound.py" "$audio/CraDev_Sound.wav"
python3 -c "from PIL import Image; Image.open('$out/_preview_2x.png').convert('RGB').resize((1920, 1080), Image.LANCZOS).save('$here/preview.png', optimize=True)"
echo "Tayyor: $art, $audio"
