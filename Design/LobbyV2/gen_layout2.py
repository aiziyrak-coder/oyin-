"""spec/<page>.verified.json (konsept sahifalari o'lchovlari) -> Assets/CraDev/Editor/LobbyLayout.cs

python gen_layout2.py <spec_dir> <out.cs>
"""
import json, os, sys

PAGES = ["home", "map", "wardrobe", "shops", "education", "business", "friends", "settings"]
spec_dir, out = sys.argv[1], sys.argv[2]


def f(v):
    return f"{float(v):.2f}f"


def s(v):
    v = (v or "").replace("\\", "\\\\").replace('"', '\\"').replace("\n", "\\n")
    return f'"{v}"'


def color(c):
    c = (c or "").strip()
    if not c.startswith("#"):
        return '""'
    return s(c[1:])


blocks = []
for page in PAGES:
    path = os.path.join(spec_dir, page + ".verified.json")
    if not os.path.exists(path):
        path = os.path.join(spec_dir, page + ".json")
    spec = json.load(open(path, encoding="utf-8-sig"))
    W, H = spec["size"]
    lines = []
    seen = set()
    for e in spec["elements"]:
        if e["id"] in seen or not e.get("box"):
            continue
        seen.add(e["id"])
        b = e["box"]
        text = e.get("intended") or e.get("text") or ""
        lines.append(
            f'                ["{e["id"]}"] = new Item({f(b[0])}, {f(b[1])}, {f(b[2])}, {f(b[3])}, {f(e.get("fontPx") or 0)}, '
            f'{color(e.get("color"))}, {color(e.get("fill"))}, {color(e.get("stroke"))}, {f(e.get("radius") or 0)}, '
            f'{s(e.get("anchor") or "tl")}, {s(e.get("weight") or "")}, {s(text)}),')
    ch = spec.get("character") or {}
    extra = ""
    if ch:
        extra = (f'\n            {{ CharacterX = {f(ch.get("centerX", 0))}, CharacterHeadY = {f(ch.get("headTopY", 0))}, '
                 f'CharacterFeetY = {f(ch.get("feetY", 0))}, HorizonY = {f(ch.get("horizonY", 0))} }}')
    name = page[0].upper() + page[1:]
    blocks.append(f'''        public static readonly Page {name} = new Page("{page}", {f(W)}, {f(H)},
            new Dictionary<string, Item>
            {{
{chr(10).join(lines)}
            }}){extra};''')

code = f'''// AVTOMATIK YARATILGAN: foydalanuvchi konsept sahifalarining o'lchovlari (scratchpad/v2/spec/*.verified.json).
// Qayta yaratish: gen_layout2.py. Qo'lda o'zgartirilmaydi - kerak bo'lsa builder'da tuzatiladi.
using System.Collections.Generic;
using UnityEngine;

namespace CraDev.EditorTools
{{
    /// <summary>
    /// Lobby sahifalarining joylashuvi: har bir elementning konsept rasmdagi o'rni (piksel, y pastga), shrift o'lchami,
    /// ranglari va qaysi ekran chetiga bog'langani. Builder bularni 1920x1080 ga o'tkazadi (LobbyUi.Place).
    /// </summary>
    static class LobbyLayout
    {{
        public readonly struct Item
        {{
            public readonly float X0, Y0, X1, Y1, FontPx, Radius;
            public readonly Color Color, Fill, Stroke;
            public readonly bool HasColor, HasFill, HasStroke;
            public readonly string Anchor, Weight, Text;

            public Item(float x0, float y0, float x1, float y1, float fontPx, string color, string fill, string stroke, float radius,
                string anchor, string weight, string text)
            {{
                X0 = x0; Y0 = y0; X1 = x1; Y1 = y1; FontPx = fontPx; Radius = radius; Anchor = anchor; Weight = weight; Text = text;
                HasColor = Parse(color, out Color);
                HasFill = Parse(fill, out Fill);
                HasStroke = Parse(stroke, out Stroke);
            }}

            static bool Parse(string hex, out Color c)
            {{
                c = Color.white;
                return !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString("#" + hex, out c);
            }}

            public float Width => X1 - X0;
            public float Height => Y1 - Y0;
            public Vector2 Center => new Vector2((X0 + X1) / 2f, (Y0 + Y1) / 2f);
        }}

        public sealed class Page
        {{
            public readonly string Name;
            public readonly float W, H;
            readonly Dictionary<string, Item> items;
            public float CharacterX, CharacterHeadY, CharacterFeetY, HorizonY;

            public Page(string name, float w, float h, Dictionary<string, Item> items) {{ Name = name; W = w; H = h; this.items = items; }}

            /// <summary>Rasm pikselidan UI birligiga (1920x1080 ichiga to'liq sig'adi).</summary>
            public float Scale => Mathf.Min(1920f / W, 1080f / H);
            public float ExtraX => 1920f - W * Scale;
            public float ExtraY => 1080f - H * Scale;
            public bool Has(string id) => items.ContainsKey(id);
            public Item this[string id] => items.TryGetValue(id, out var item) ? item : throw new KeyNotFoundException($"LobbyLayout.{{Name}}: {{id}}");
            public IEnumerable<string> Ids => items.Keys;
        }}

{chr(10).join(blocks)}
    }}
}}
'''
open(out, "w", encoding="utf-8", newline="\n").write(code)
print("ok ->", out)
