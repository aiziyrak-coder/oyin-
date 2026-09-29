#!/usr/bin/env bash
# Unity'siz TAXMINIY C# kompilyatsiya tekshiruvi (batafsil: README.md).
# Runtime (Assets/CraDev, Editor'siz) va Editor (Assets/CraDev/Editor) kodini Unity 2021 modullari (NuGet),
# eski UnityEditor.dll (2018.1, NuGet) va qo'lda yozilgan stublar bilan alohida kompilyatsiya qiladi.
# Editor uchun edbase.txt dagi bazaviy xatolar (eski UnityEditor.dll da yo'q API) e'tiborga olinmaydi.
#
# Ishlatish: tools/ci/cs-check/check.sh [--update-baseline] [repo_ildizi]
#   --update-baseline  joriy Editor xatolarini edbase.txt ga yozadi (faqat Unity 6 API'si ekanini tekshirgandan keyin!)
# Kerak: .NET SDK 8 (dotnet), curl, unzip yoki python3; birinchi ishga tushirishda internet.
# Muhit: CS_CHECK_WORK (vaqtinchalik build papkasi), CS_CHECK_CACHE (UnityEditor.dll keshi).
# Natija: 0 - toza ("OK: ..."), 1 - kompilyatsiya xatolari, 2 - muhit xatosi (dotnet yo'q, yuklab bo'lmadi).

set -uo pipefail

UPDATE_BASELINE=0
if [ "${1:-}" = "--update-baseline" ]; then UPDATE_BASELINE=1; shift; fi

HERE=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
ROOT=$(cd "${1:-$HERE/../../..}" && pwd) || { echo "XATO: repo papkasi topilmadi: ${1:-}"; exit 2; }
[ -d "$ROOT/Assets/CraDev" ] || { echo "XATO: $ROOT ichida Assets/CraDev yo'q"; exit 2; }
command -v dotnet >/dev/null 2>&1 || { echo "XATO: dotnet topilmadi (.NET SDK 8 kerak)"; exit 2; }

TMP=${TMPDIR:-/tmp}
KEY=$(printf '%s' "$ROOT" | cksum | cut -d' ' -f1)
WORK=${CS_CHECK_WORK:-$TMP/cradev-cs-check-$KEY}
CACHE=${CS_CHECK_CACHE:-$TMP/cradev-cs-check-cache}

# 1. UnityEditor.dll: repo'da saqlanmaydi, nuget.org dan yuklanadi va SHA-256 bilan tekshiriladi
EDITOR_VERSION=2018.1.6-f1
EDITOR_SHA256=8eafaf26f5d9f4820321bb1c47888fc78cfbb6d02b922414e8b2ea3bb6c045d5
EDITOR_URL=https://api.nuget.org/v3-flatcontainer/unity3d.unityeditor/$EDITOR_VERSION/unity3d.unityeditor.$EDITOR_VERSION.nupkg
EDITOR_DLL=$CACHE/unityeditor-$EDITOR_VERSION/UnityEditor.dll

sha256() { if command -v sha256sum >/dev/null 2>&1; then sha256sum "$1"; else shasum -a 256 "$1"; fi | cut -d' ' -f1; }

if [ ! -s "$EDITOR_DLL" ] || [ "$(sha256 "$EDITOR_DLL")" != "$EDITOR_SHA256" ]; then
    mkdir -p "$(dirname "$EDITOR_DLL")"
    pkg=$(mktemp "$CACHE/unityeditor.XXXXXX")
    if ! curl -fsSL --retry 3 -o "$pkg" "$EDITOR_URL"; then
        rm -f "$pkg"; echo "XATO: UnityEditor.dll yuklanmadi: $EDITOR_URL"; exit 2
    fi
    if command -v unzip >/dev/null 2>&1; then
        unzip -p "$pkg" lib/UnityEditor.dll > "$EDITOR_DLL.part"
    else
        python3 -c 'import sys,zipfile; open(sys.argv[2],"wb").write(zipfile.ZipFile(sys.argv[1]).read("lib/UnityEditor.dll"))' "$pkg" "$EDITOR_DLL.part"
    fi
    rm -f "$pkg"
    if [ ! -s "$EDITOR_DLL.part" ] || [ "$(sha256 "$EDITOR_DLL.part")" != "$EDITOR_SHA256" ]; then
        rm -f "$EDITOR_DLL.part"; echo "XATO: UnityEditor.dll kutilgan fayl emas (SHA-256 mos kelmadi)"; exit 2
    fi
    mv -f "$EDITOR_DLL.part" "$EDITOR_DLL"
fi

# 2. Loyihalar vaqtinchalik papkada yig'iladi (repo ichida bin/ va obj/ qolmaydi)
mkdir -p "$WORK/ed"
cp "$HERE/check.csproj" "$WORK/check.csproj"
cp "$HERE/ed/ed.csproj" "$WORK/ed/ed.csproj"
PROPS=(-nologo -clp:NoSummary "-p:RepoRoot=$ROOT/" "-p:HarnessDir=$HERE/" "-p:UnityEditorDll=$EDITOR_DLL")

# "xato" qatorlari: repo yo'lisiz, oxiridagi [loyiha.csproj] siz, takrorlarsiz
errors() { grep -E ' error [A-Z]+[0-9]+' | sed -e "s#^$ROOT/##" -e 's# \[[^]]*\]$##' | sort -u; }
strip_pos() { sed -E 's/\([0-9]+,[0-9]+\)//'; }

OUT=$(dotnet build "$WORK/check.csproj" "${PROPS[@]}" 2>&1); CODE=$?
R=$(printf '%s\n' "$OUT" | errors)
if [ -n "$R" ]; then echo "RUNTIME XATOLAR:"; echo "$R"; exit 1; fi
if [ $CODE -ne 0 ]; then echo "RUNTIME: dotnet build muvaffaqiyatsiz (kod $CODE):"; printf '%s\n' "$OUT" | tail -30; exit 2; fi

OUT=$(dotnet build "$WORK/ed/ed.csproj" "${PROPS[@]}" 2>&1); CODE=$?
E=$(printf '%s\n' "$OUT" | errors)
if [ $CODE -ne 0 ] && [ -z "$E" ]; then echo "EDITOR: dotnet build muvaffaqiyatsiz (kod $CODE):"; printf '%s\n' "$OUT" | tail -30; exit 2; fi

if [ $UPDATE_BASELINE -eq 1 ]; then
    if [ -n "$E" ]; then printf '%s\n' "$E" > "$HERE/edbase.txt"; else : > "$HERE/edbase.txt"; fi
    echo "edbase.txt yangilandi: $(printf '%s' "$E" | grep -c . || true) ta bazaviy Editor xatosi."
    exit 0
fi

BASE=$(strip_pos < "$HERE/edbase.txt" | grep . | sort -u)
NEW=$(comm -23 <(printf '%s\n' "$E" | strip_pos | grep . | sort -u) <(printf '%s\n' "$BASE"))
if [ -n "$NEW" ]; then
    echo "EDITOR YANGI XATOLAR (edbase.txt da yo'q):"
    printf '%s\n' "$E" | awk 'NR==FNR { s[$0] = 1; next } { k = $0; gsub(/\([0-9]+,[0-9]+\)/, "", k); if (k in s) print }' <(printf '%s\n' "$NEW") -
    exit 1
fi

GONE=$(comm -13 <(printf '%s\n' "$E" | strip_pos | grep . | sort -u) <(printf '%s\n' "$BASE") | grep -c . || true)
[ "$GONE" -gt 0 ] && echo "Eslatma: edbase.txt dagi $GONE ta bazaviy xato endi uchramaydi (--update-baseline bilan tozalash mumkin)."
echo "OK: runtime va editor kompilyatsiyasi toza (bazaga nisbatan)"
