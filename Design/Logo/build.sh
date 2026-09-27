#!/usr/bin/env bash
# Logo qatlamlari, teksturalar va intro ovozini qayta yaratib, Unity loyihasiga joylaydi.
# Talablar: Node.js + playwright (Chromium bilan), Python 3 + numpy + pillow.
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
art="$here/../../Assets/CraDev/Intro/Art"
audio="$here/../../Assets/CraDev/Intro/Audio"
out="$here/.out"
mkdir -p "$out" "$art" "$audio"
node "$here/render.js" "$out"
python3 "$here/make_textures.py" "$out" "$art" "$here/layout.json"
python3 "$here/make_sound.py" "$audio/CraDev_IntroSound.wav"
cp "$out/_preview_2x.png" "$here/preview.png"
echo "Tayyor: $art, $audio"
