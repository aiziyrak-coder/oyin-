import json, os

OUT = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\spec\wardrobe.json"
E = []


def add(id, kind, box, **kw):
    e = {"id": id, "kind": kind, "box": [round(v, 1) for v in box]}
    e.update(kw)
    E.append(e)


R = True  # remove from background

# ---------------- NAV ----------------
add("nav.brand_icon", "icon", [17.3, 10.6, 32.3, 26.9], icon="lynxos-logo", color="#FFFFFF", fill="#3F72EBFF", radius=3.5,
    anchor="tl", group="nav", remove=R, pad=2,
    notes="Blue rounded-square app icon (fill #3F72EB, lighter #628BFE at centre, darker #2E5FD8 edges, soft blue glow) with a white "
          "stylised '@'/lynx-swirl glyph (glyph ink ~[20.1,13.7,28.3,23.2], #D6F0FF).")
add("nav.brand_text", "text", [37.3, 14.6, 74.2, 24.0], text="Lynxos", fontPx=10.1, weight="bold", color="#FFFFFF",
    anchor="tl", group="nav", remove=R, pad=2,
    notes="Box = cap top of 'L' to bottom of 'y' descender; baseline ~21.9 (cap height 7.3).")
tabs = [("nav.tab_home", [93.1, 15.8, 129.6, 21.9], "Bosh sahifa", "no descenders; baseline 21.9"),
        ("nav.tab_world", [150.0, 16.0, 170.9, 23.3], "Dunyo", "includes 'y' descender; baseline ~22.0"),
        ("nav.tab_friends", [191.2, 15.9, 215.7, 22.0], "Do'stlar", "no descenders; baseline 22.0"),
        ("nav.tab_top", [235.7, 15.9, 259.2, 23.2], "Top-lar", "includes 'p' descender; baseline ~22.0"),
        ("nav.tab_settings", [279.1, 16.0, 313.8, 22.1], "Sozlamalar", "no descenders; baseline 22.1")]
for id, b, t, n in tabs:
    add(id, "text", b, text=t, fontPx=8.5, weight="medium", color="#D6DAE3", anchor="tc", group="nav", state="normal",
        remove=R, pad=2,
        notes="Ink box (cap top to " + n + "). Cap height ~6.1. On this page NO nav tab is highlighted (Garderob is reached from the "
              "profile/home, it is not a top-nav tab): no selected pill, no underline. Glyphs are AI-blurred at 1x; sampled core "
              "~#C5C9D2, intended light grey-white ~#D6DAE3 (white at ~85%).")
add("nav.lang_globe", "icon", [371.7, 13.7, 383.0, 25.3], icon="globe", color="#E2EAF6", anchor="tr", group="nav", remove=R, pad=2,
    notes="Outline globe (meridian + 2 parallels), stroke ~1.2px.")
add("nav.lang_text", "text", [387.2, 16.5, 396.0, 22.1], text="UZ", fontPx=7.8, weight="medium", color="#D0DAEA", anchor="tr",
    group="nav", remove=R, pad=2, notes="Cap top to baseline.")
add("nav.lang_chevron", "icon", [399.4, 18.0, 403.2, 21.1], icon="chevron-down", color="#B4C0D2", anchor="tr", group="nav",
    remove=R, pad=2, notes="Small thin chevron.")
add("nav.bell", "icon", [414.8, 13.8, 424.6, 25.4], icon="bell", color="#E4ECF8", anchor="tr", group="nav", remove=R, pad=2,
    notes="Outline bell, stroke ~1.2px; clapper at bottom centre.")
add("nav.bell_dot", "dot", [421.7, 13.4, 425.9, 17.5], color="#E0435E", anchor="tr", group="nav", remove=R, pad=2,
    notes="Red notification dot over the bell's top-right (brightest 1x px #BF4057; intended saturated red/pink ~#E0435E).")
add("nav.profile_pill", "pill", [432.8, 9.6, 502.6, 29.6], fill="#303D54E6", stroke="#FFFFFF14", radius=10.0, anchor="tr",
    group="nav", remove=R, pad=2,
    notes="Dark glass capsule (sampled #303C53 top, #32435B bottom; background around ~#242E42). Very faint lighter rim.")
add("nav.profile_avatar", "avatar", [436.5, 11.3, 453.3, 28.2], stroke="#4A4E5AFF", radius=8.4, anchor="tr", group="nav",
    remove=R, pad=1,
    notes="Circular photo avatar of the same young man (black hair, white hoodie) with a ~1.5px greyish ring (#444857). "
          "Photo area ~[438.7,11.6,451.3,27.8].")
add("nav.profile_name", "text", [456.0, 16.9, 486.8, 22.8], text="Lynxos_user", fontPx=7.5, weight="medium", color="#C8D0DC",
    anchor="tr", group="nav", remove=R, pad=2,
    notes="AI-garbled at 1x (reads like 'Lynxos_user'); box = top of 'L' to underscore/baseline.")
add("nav.profile_chevron", "icon", [491.9, 18.2, 496.8, 21.2], icon="chevron-down", color="#C3D0E8", anchor="tr", group="nav",
    remove=R, pad=2)

# ---------------- HEADER ----------------
add("header.title", "text", [18.3, 45.9, 95.0, 59.1], text="Garderob", fontPx=18.3, weight="bold", color="#FFFFFF", anchor="tl",
    group="header", remove=R, pad=2, notes="Cap top to baseline (no descenders). Cap height 13.2.")
add("header.subtitle", "text", [18.0, 66.7, 144.5, 76.1], text="O'zingizning uslubingizni yarating", fontPx=8.4,
    weight="regular", color="#B4BAC6", anchor="tl", group="header", remove=R, pad=2,
    notes="Box includes descenders (g, y); cap 'O' 67.2..73.3 (cap height ~6.1), baseline ~73.3. The last glyphs ('ng') run "
          "over the bright lit door frame at x>=140, so the right edge (~144.5) is approximate. Muted grey (white ~70%).")

# ---------------- SIDE LIST ----------------
side = [  # (row box, icon box, icon name, label box, text, label note)
    ([16.8, 91.6, 122.4, 113.6], [27.0, 97.8, 38.0, 107.6], "shirt", [48.3, 99.4, 75.5, 107.0], "Kiyimlar",
     "cap top to 'y' descender; baseline ~106.3"),
    ([16.8, 114.6, 122.4, 140.6], [27.0, 124.1, 38.2, 133.0], "shoe", [48.6, 125.4, 87.3, 133.0], "Poyabzallar",
     "cap top to 'y' descender; baseline ~131.8"),
    ([16.8, 141.6, 122.4, 167.8], [27.1, 150.2, 38.0, 160.2], "cap", [48.3, 152.6, 91.6, 159.0], "Aksessuarlar",
     "no descenders; baseline 159.0"),
    ([16.8, 169.0, 122.4, 195.5], [27.5, 177.1, 37.9, 188.0], "hair", [48.5, 179.4, 92.8, 187.3], "Soch turmagi",
     "cap top to 'g' descender; baseline ~185.7"),
    ([16.8, 196.6, 122.4, 222.6], [27.3, 204.9, 37.8, 215.2], "outfit-card", [48.3, 206.9, 76.9, 213.1], "Obrazlar",
     "no descenders; baseline 213.1"),
    ([16.8, 223.6, 122.4, 249.6], [27.8, 231.7, 37.8, 243.0], "bag", [48.7, 234.0, 92.5, 242.0], "Saqlanganlar",
     "cap top to 'q'/'g' descenders; baseline ~240.3"),
]
icon_notes = {
    "shirt": "Outline T-shirt icon (white, stroke ~1.3px).",
    "shoe": "Outline sneaker/boot icon (side view, sole curving right).",
    "cap": "Outline baseball-cap / hat icon (dome with brim).",
    "hair": "Outline hairstyle icon (head with arched hair).",
    "outfit-card": "Outline 'look/persona' icon: rounded card/mannequin with a small face/lines (obraz = look).",
    "bag": "Outline shopping-bag icon with handle.",
}
for i, (rb, ib, ic, lb, txt, ln) in enumerate(side):
    sel = i == 0
    add(f"side.item_{i}", "button", rb, fill=("#1F5CFDFF" if sel else "#1E2533D9"),
        stroke=("#3D78FFFF" if sel else "#2C354BFF"), radius=4.0, anchor="tl", group="side",
        state=("selected" if sel else "normal"), remove=R, pad=2,
        notes=("Selected row: solid royal blue (#1F5CFD, very slightly lighter top), soft blue glow. NOTE: drawn 22.0 px tall while "
               "unselected rows are ~26 px; builder may equalise to 26 (row pitch ~27.2)." if sel else
               "Dark translucent glass row (sampled #222A38 over a #060E1B left background), 1px lighter border #2A3349. "
               "Rows stack with ~1px gap, pitch ~27.2."))
    add(f"side.item_{i}_icon", "icon", ib, icon=ic, color=("#FFFFFF" if sel else "#EEF4FF"), anchor="tl", group="side",
        state=("selected" if sel else "normal"), remove=R, pad=2, notes=icon_notes[ic])
    add(f"side.item_{i}_label", "text", lb, text=txt, fontPx=8.8, weight=("semibold" if sel else "medium"),
        color=("#FFFFFF" if sel else "#E2E6EE"), anchor="tl", group="side", state=("selected" if sel else "normal"),
        remove=R, pad=2, notes="Ink box: " + ln + ". Cap height ~6.2.")

# ---------------- STYLE TABS ----------------
add("wardrobe.tabs_bar", "panel", [277.8, 55.5, 501.0, 77.6], fill="#26324AD9", stroke="#FFFFFF10", radius=4.5, anchor="tr",
    group="tabs", remove=R, pad=2,
    notes="Segmented control: dark glass bar (sampled #303C52..#344156). Segment cells: [277.8-322.8] Hammasi (selected), "
          "[322.8-372.3] Kostyumlar, [372.3-416.3] Sportcha, [416.3-459.5] Kundalik, [459.5-501.0] Maxsus.")
add("wardrobe.tab_selected", "pill", [277.8, 55.3, 322.8, 78.0], fill="#438DFDFF", radius=4.5, anchor="tr", group="tabs",
    state="selected", remove=R, pad=2,
    notes="Selected segment: bright azure blue #438DFD (lighter than the side-list blue #1F5CFD), faint bright bottom edge, glow.")
stabs = [([286.0, 63.7, 316.0, 69.8], "Hammasi", "no descenders", True),
         ([331.0, 63.9, 366.2, 71.1], "Kostyumlar", "includes 'y' descender; baseline ~69.9", False),
         ([380.5, 64.0, 409.0, 70.9], "Sportcha", "includes 'p' descender; baseline ~69.9", False),
         ([424.2, 63.9, 451.3, 69.9], "Kundalik", "no descenders", False),
         ([468.3, 64.1, 493.0, 69.9], "Maxsus", "no descenders", False)]
for i, (b, t, n, sel) in enumerate(stabs):
    add(f"wardrobe.tab_{i}", "text", b, text=t, fontPx=8.4, weight=("semibold" if sel else "medium"),
        color=("#FFFFFF" if sel else "#D0D7E4"), anchor="tr", group="tabs", state=("selected" if sel else "normal"),
        remove=R, pad=2, notes="Label ink box (" + n + "); cap height ~6.0; centred in its segment cell.")
for i, x in enumerate([372.3, 416.3, 459.5]):
    add(f"wardrobe.tab_sep_{i}", "line", [x - 0.5, 56.5, x + 0.5, 77.0], color="#3A475C", anchor="tr", group="tabs",
        remove=R, pad=1, notes="1px vertical divider between unselected segments (slightly lighter than bar).")

# ---------------- ITEM GRID ----------------
cols = [(279.3, 330.9), (336.8, 388.0), (393.9, 445.0), (450.6, 501.8)]
rows = [(88.9, 141.9), (148.9, 201.8), (207.9, 258.6)]
badges = [[322.1, 93.7, 327.1, 98.7], [379.0, 93.7, 383.9, 98.7], [436.0, 94.0, 441.2, 98.9], [493.0, 93.9, 497.4, 98.5],
          [322.4, 153.7, 326.9, 158.1], [379.2, 153.7, 383.9, 158.0], [436.0, 153.5, 440.7, 158.1], [493.2, 153.7, 498.0, 158.0],
          [322.8, 212.9, 327.2, 217.3], [379.4, 212.9, 383.7, 217.2], [436.0, 212.7, 440.4, 217.4], [493.1, 213.1, 497.6, 217.5]]
items = [
    ([289, 97, 323, 137], "Charcoal-grey zip bomber/track jacket, bright white centre zipper stripe and white inner collar, small white chest logo."),
    ([346, 97, 381, 139], "Black leather-look bomber jacket with rust-brown fur/shearling collar, light zip line, small chest logo; blue jeans visible under the hem."),
    ([403, 97, 437, 136], "Black track jacket, stand collar, dark zipper, tiny chest logo (almost merges with tile)."),
    ([459, 97, 493, 135], "White / light-grey full-zip track jacket with stand collar."),
    ([289, 156, 323, 194], "Taupe / warm-grey zip jacket with white zipper stripe and small cyan chest logo."),
    ([345, 156, 381, 195], "Camel / tan (mustard-brown) bomber jacket, white shirt collar showing, ribbed hem and cuffs, chest patch."),
    ([403, 157, 436, 195], "Black high-collar / hooded jacket, plain."),
    ([459, 157, 493, 196], "Black hooded zip jacket with small white chest logo."),
    ([289, 216, 323, 253], "Navy-blue zip jacket with white zipper stripe and small cyan chest logo."),
    ([347, 217, 379, 252], "White short-sleeve polo / zip shirt with small chest logo."),
    ([403, 217, 436, 253], "Black zip jacket with grey zipper and white chest logo."),
    ([459, 217, 493, 253], "Olive / khaki-green zip jacket with light zipper and tan collar lining."),
]
for r, (y0, y1) in enumerate(rows):
    for c, (x0, x1) in enumerate(cols):
        n = r * 4 + c
        add(f"wardrobe.tile_{n}", "tile", [x0, y0, x1, y1], fill="#141F2EFF", stroke="#5F6D82FF", radius=4.5, anchor="cr",
            group="grid", state="normal", remove=R, pad=2,
            notes=f"Grid row {r} col {c}. Dark navy card (#131D2B top -> #1A2638 bottom), 1px light slate border (#5F6D82). "
                  f"Item: {items[n][1]}")
        add(f"wardrobe.tile_{n}_image", "image", items[n][0], anchor="cr", group="grid", remove=R, pad=0,
            notes="Approx. bounds of the item render inside the tile (centred horizontally, bottom-aligned). " + items[n][1])
        bb = badges[n]
        add(f"wardrobe.tile_{n}_badge", "badge", bb, icon="diamond", color="#D6F0FF", anchor="cr", group="grid", remove=R, pad=1,
            notes="Small glowing light-cyan diamond/gem badge in the tile's top-right corner (centre ~tile.x1-6.3, tile.y0+7.3)."
                  + (" This one is drawn malformed (heart/split shape) - use the same diamond." if n == 11 else ""))

# ---------------- COLOR SWATCHES + SAVE ----------------
add("wardrobe.swatch_bar", "panel", [278.2, 265.6, 385.5, 292.7], fill="#3A3B46E6", stroke="#FFFFFF10", radius=5.5, anchor="br",
    group="colors", remove=R, pad=2, notes="Neutral dark-grey glass bar (sampled #40404B) holding 5 colour swatches.")
sw = [([283.2, 270.2, 297.0, 284.2], "#F09812", "#F6C27A", "orange"),
      ([303.6, 270.1, 317.2, 284.2], "#A2AC0C", "#C8CC80", "lime / yellow-green"),
      ([324.1, 269.9, 338.2, 284.0], "#0E1418", "#5C606C", "black"),
      ([344.7, 270.2, 358.2, 284.1], "#8393DF", "#B4C0E8", "periwinkle / lavender blue"),
      ([364.9, 270.0, 379.2, 284.2], "#823D77", "#B58BB6", "plum / magenta-purple")]
for i, (b, c, ring, nm) in enumerate(sw):
    add(f"wardrobe.swatch_{i}", "swatch", b, color=c, stroke=ring + "FF", radius=7.0, anchor="br", group="colors",
        state="normal", remove=R, pad=1.5,
        notes=f"{nm} circle, d~14 incl. a ~1px lighter ring and a thin dark outer shadow. Centres pitch ~20.5 px at y~277.1. "
              "No swatch is visibly marked selected.")
add("wardrobe.save_button", "button", [395.9, 265.8, 501.8, 294.0], fill="#2160FCFF", radius=6.0, anchor="br", group="colors",
    remove=R, pad=3, notes="Solid royal-blue CTA (#215CFC..#2160FF) with a soft blue glow/shadow below.")
add("wardrobe.save_label", "text", [432.6, 276.9, 463.4, 285.7], text="Saqlash", fontPx=9.2, weight="semibold", color="#FFFFFF",
    anchor="br", group="colors", remove=R, pad=2,
    notes="Cap top to 'q' descender; baseline ~283.5 (cap height ~6.6). Label visually centred in the button, arrow right.")
add("wardrobe.save_arrow", "icon", [482.1, 276.5, 490.3, 284.0], icon="arrow-right", color="#FFFFFF", anchor="br",
    group="colors", remove=R, pad=2, notes="Thin right arrow, right-aligned ~11.5 px from the button's right edge.")

# ---------------- CHARACTER ----------------
body = [[197, 46.2], [203, 46.4], [207.5, 47.6], [210.8, 50.5], [212.8, 55], [213.2, 61], [211.8, 66.5], [210.5, 71.5], [208, 76],
        [211, 79.5], [215.5, 83.5], [221, 88.5], [227.5, 93], [231.5, 100], [233.5, 109], [235.5, 119], [237, 130], [238.5, 144],
        [239.6, 157], [240.2, 169], [238.8, 178], [236.8, 186], [233.5, 192.5], [229, 192.5], [228.5, 197], [229.5, 205],
        [229.8, 215], [228.8, 223], [227.3, 233], [226.3, 245], [226.6, 258], [227.2, 268], [226.5, 274], [228.5, 282],
        [230.6, 289], [231.2, 295], [231, 300], [229, 305.5], [208.5, 305.5], [207, 300], [209.5, 293], [209.5, 280], [206, 275],
        [203.5, 268], [201.5, 258], [201.3, 245], [200.3, 232], [199.3, 218], [197.8, 206], [195.3, 199.5], [192.8, 205],
        [191.2, 218], [189.3, 232], [188.6, 245], [188.8, 258], [186.5, 266], [182, 273], [178.5, 278], [177.8, 292],
        [179.5, 300], [178.5, 305.5], [157, 305.5], [156.8, 298], [158, 290], [160.5, 283], [162.8, 277], [161.8, 268],
        [161.5, 255], [162.5, 240], [163.5, 225], [164.5, 210], [165.3, 198], [165.5, 193], [161, 192.8], [157, 188.5],
        [155.3, 181], [153, 173], [152, 165], [152.8, 150], [154.8, 135], [156.8, 121], [159.3, 109], [161.8, 99.5], [166, 93.5],
        [172, 89.5], [179.5, 85], [182, 81], [187.5, 77.5], [184.5, 71], [181.8, 64], [181.8, 57], [184.5, 51.5], [188.8, 47.8],
        [193, 46.4]]
strap = [[221.5, 220], [226.5, 220], [226.5, 229], [230, 233.5], [245, 235.5], [258, 236.5], [263, 238.5], [263, 244], [250, 243],
         [238, 241.8], [229, 239.8], [223.5, 234.5], [221.5, 227]]
shadow = [[177, 287], [196, 285.5], [212, 285], [213, 293], [205, 295.5], [185, 296], [177, 296.5]]

spec = {
    "page": "wardrobe",
    "size": [519, 309],
    "elements": E,
    "character": {
        "centerX": 197.0, "headTopY": 46.3, "feetY": 296.0, "horizonY": 195.0,
        "notes": "Young man, full body, facing camera: black hair, white hoodie under a black bomber jacket with white side stripes "
                 "on the sleeves, black crossbody bag strap diagonally from right shoulder to left hip (bag at left hip), dark "
                 "charcoal cargo pants with side pockets, white sneakers. Shoulders x 152-240, feet: left shoe x 158-179, right shoe "
                 "x 207-231, soles at y~295.5-296.4; faint floor reflections to y~305 and a soft cast shadow between the shoes "
                 "(y 285-296). centerX = body midline (head centre ~200, feet midpoint ~195). horizonY ~195 (+-15, low camera at about the avatar's hip/crotch height, crotch y~199 - same style as the home page): the plant-pot top rim (y~209) is seen almost edge-on while its base (y~235) curves as seen from above; the back-wall/floor junction is at y~232 and the 2.47x-distance estimate from pot size (0.45 m) gives (296-H)=2.47*(234-H) -> H~192. An eye-level camera (H~60) is unlikely (it would make the lit door frame only ~1.8 m tall). Ceiling light strips meet at (248,55) but that is a panel corner, not a vanishing point."
    },
    "removePolygons": [
        {"name": "character", "points": body, "pad": 2},
        {"name": "hanging_bag_strap", "points": strap, "pad": 2},
        {"name": "floor_shadow_between_shoes", "points": shadow, "pad": 2},
    ],
    "assets": [
        {"name": "wardrobe_bg", "box": [0, 0, 519, 308],
         "notes": "Full-bleed room interior (modern boutique/dressing room: lit vertical LED door frame at x~140-150, glass partition, "
                  "potted palm at x~245-280, glossy grey tile floor). Remove every element with remove=true plus the three "
                  "removePolygons (character, dangling bag strap on the floor, shadow between shoes). Keep the dark wall region at "
                  "x<83 (it has a vertical edge at x~83 from y~30 to ~260; looks like architecture, not UI - verify after inpaint). "
                  "Row y=308 and column x=518 are the black composite border - crop/extend them."},
        {"name": "character_cutout", "box": [150, 44, 242, 307],
         "notes": "The standing avatar (reference for the 3D character's pose/scale): head top 46.3, soles 296. Nothing overlaps "
                  "it except the floor behind; the dangling strap to the right (x 222-263, y 220-244) belongs to it."},
        {"name": "avatar_photo", "box": [436.5, 11.3, 453.3, 28.2], "notes": "Profile avatar image in the nav pill (circle)."},
    ] + [
        {"name": f"tile_{n}_item", "box": items[n][0],
         "notes": items[n][1] + " Crop from the tile; mask out the diamond badge (wardrobe.tile_%d_badge) if it overlaps." % n}
        for n in range(12)
    ],
}
os.makedirs(os.path.dirname(OUT), exist_ok=True)
json.dump(spec, open(OUT, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print("elements", len(E))
