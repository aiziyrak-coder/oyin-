#!/usr/bin/env bash
# Loading ekrani teksturalarini qayta yaratib, Unity loyihasiga joylaydi.
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
art="$here/../../Assets/CraDev/Loading/Art"
mkdir -p "$art"
python3 "$here/make_textures.py" "$art"
echo "Tayyor: $art"
