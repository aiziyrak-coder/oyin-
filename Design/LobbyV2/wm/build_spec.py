import json, os

E = []


def el(id, kind, box, **kw):
    d = {"id": id, "kind": kind, "box": [round(float(v), 1) for v in box]}
    d.update(kw)
    if "remove" not in d:
        d["remove"] = True
    E.append(d)


def txt(id, box, text, fontPx, weight, color, group, anchor, pad=2, notes="", **kw):
    el(id, "text", box, text=text, fontPx=fontPx, weight=weight, color=color, group=group, anchor=anchor, pad=pad,
       notes=notes, **kw)


# ---------------------------------------------------------------- NAV
el("nav.brand_icon", "icon", [19.4, 15.6, 38.6, 36.2], icon="lynxos-logo", fill="#2E66F5FF", color="#D5F9FF", radius=5,
   group="nav", anchor="tl", pad=3,
   notes="Rounded-square app icon, solid blue (#3166F4 top-right .. #2B5CF5 bottom), soft blue glow. White glyph = two "
         "concentric open 'C' rings (swirl/@ style) with ink box [23.0,19.2,34.4,33.3].")
txt("nav.brand_text", [46.3, 20.5, 96.6, 33.0], "Lynxos", 13.6, "extrabold", "#F4FAFF", "nav", "tl",
    notes="Box = cap top of 'L' to descender of 'y'; baseline y=30.3 (cap height 9.8). Rounded geometric sans (Nunito/Poppins-like).")
txt("nav.tab_home", [131.6, 21.1, 177.7, 28.8], "Bosh sahifa", 10.3, "medium", "#C4D0DE", "nav", "tl", state="normal",
    notes="No descenders; box bottom = baseline 28.8 (cap 7.4).")
txt("nav.tab_world", [204.0, 21.2, 230.9, 30.5], "Dunyo", 10.3, "semibold", "#D0E6FF", "nav", "tl", state="selected",
    notes="Selected tab. Box bottom = 'y' descender; baseline ~28.8. Text is near-white (#E6F2FF core).")
txt("nav.tab_friends", [257.2, 21.3, 287.7, 28.7], "Do'stlar", 10.3, "medium", "#CEDBF0", "nav", "tl", state="normal",
    notes="No descenders; baseline 28.7.")
txt("nav.tab_top", [314.5, 21.3, 344.3, 30.2], "Top-lar", 10.3, "medium", "#C9D8EB", "nav", "tl", state="normal",
    notes="Box bottom = 'p' descender; baseline ~28.8.")
txt("nav.tab_settings", [370.3, 21.3, 414.7, 28.8], "Sozlamalar", 10.3, "medium", "#CDDCF2", "nav", "tl", state="normal",
    notes="No descenders; baseline 28.8. Tab gaps: text-to-text spacing ~26-29 px.")
el("nav.tab_selected", "tab", [190.6, 12.0, 243.4, 40.5], fill="#1C54C5D9", radius=3, group="nav", anchor="tl",
   state="selected", pad=3,
   notes="Highlight behind 'Dunyo': vertical gradient, fully transparent at top (y~12) -> #13408C at y~30 -> #1C54C5 at "
         "y 36-37 (strongest), then the underline; faint blue glow fades below to y~43. Sides fairly hard (x 190.6 / 243.4).")
el("nav.tab_underline", "line", [192.2, 37.9, 241.7, 39.6], color="#9BC6FF", radius=1, group="nav", anchor="tl",
   state="selected", pad=3, notes="Bright 1.7px bar with rounded ends and blue glow (#3D7BFF) just under the selected tab.")
el("nav.lang_globe", "icon", [506.2, 18.6, 521.6, 34.5], icon="globe", color="#DEECFF", group="nav", anchor="tr", pad=2,
   notes="Outline globe (meridians + parallels), ~1.5px stroke.")
txt("nav.lang_text", [528.0, 23.2, 538.0, 29.9], "UZ", 9.3, "medium", "#C1D4F4", "nav", "tr",
    notes="Caps only; box bottom = baseline (cap 6.7).")
el("nav.lang_chevron", "icon", [543.7, 25.3, 548.7, 28.0], icon="chevron-down", color="#C0D5F7", group="nav", anchor="tr",
   pad=2, notes="Small thin chevron.")
el("nav.bell", "icon", [571.5, 16.8, 586.3, 34.8], icon="bell", color="#E8F6FF", group="nav", anchor="tr", pad=2,
   notes="Outline bell with clapper; ~1.6px stroke.")
el("nav.bell_dot", "dot", [582.8, 18.0, 587.6, 22.8], color="#D62B4A", group="nav", anchor="tr", pad=2,
   notes="Red notification dot (~4.8px) overlapping the bell's top-right; core ~#E8344F, soft red glow.")
el("nav.profile_pill", "pill", [605.3, 10.8, 729.6, 41.8], fill="#2E4262CC", stroke="#1E2E4480", radius=15.5,
   group="nav", anchor="tr", pad=3,
   notes="Dark glass capsule (composite over map #3C506E). Slightly darker rim on the left around the avatar.")
el("nav.profile_avatar", "avatar", [610.3, 13.5, 635.3, 38.8], radius=12.6, fill="#767F8EFF", group="nav", anchor="tr",
   pad=2, notes="Circular portrait of the default 3D avatar (brown hair, light shirt) on grey #767F8E background.")
txt("nav.profile_name", [642.0, 23.3, 698.7, 32.7], "Lynxos_user", 10.8, "medium", "#D9E6FF", "nav", "tr",
    notes="Box bottom = underscore/'y' descender; baseline 31.0 (cap 7.8).")
el("nav.profile_chevron", "icon", [710.6, 25.0, 718.5, 29.4], icon="chevron-down", color="#D6E5FE", group="nav",
   anchor="tr", pad=2)

# ---------------------------------------------------------------- HEADER
txt("header.title", [20.7, 63.0, 154.2, 82.9], "Dunyo xaritasi", 21.7, "bold", "#F5FBFF", "header", "tl",
    notes="Box bottom = 'y' descender; baseline 78.9 (cap 15.6). Slight dark text shadow.")
txt("header.subtitle", [20.0, 89.3, 126.7, 100.4], "Istalgan joyga boring", 11.5, "regular", "#9DA9BE", "header", "tl",
    notes="Box bottom = descenders (j,y,g); baseline 97.9 (cap 8.3).")

# ---------------------------------------------------------------- SIDE
el("side.panel", "panel", [0, 46, 152.8, 415], fill="#06142680", stroke="#FFFFFF14", group="side", anchor="tl", pad=0,
   notes="Full-height dark glass column behind header + category list; no visible top edge (fades into the dark top-left "
         "vignette), right edge = 1px faint hairline at x~152.8. Horizontal gradient: ~#001020 (alpha ~0.9) at x=0 -> "
         "~#0A2139 (alpha ~0.5) at the right edge. Extends to the bottom of the screen.")
rows = [(115.8, 145.5), (147.3, 179.5), (180.5, 212.5), (213.5, 245.5), (246.5, 278.5), (279.5, 311.5),
        (312.5, 345.5), (346.5, 378.6)]
icons = [
    ([30.8, 123.6, 45.0, 137.0], "apps", "#D2F5FF",
     "AI drew a house/shirt hybrid; intended 'all categories' icon (e.g. grid/apps or map)."),
    ([31.0, 156.3, 44.7, 171.0], "bag", "#E6EBF7", "Shopping bag outline."),
    ([30.3, 190.5, 45.6, 203.8], "graduation-cap", "#EEF6FE", "Mortarboard outline."),
    ([30.6, 223.0, 45.0, 235.8], "briefcase", "#EEF5FE",
     "Garbled 'A'-like glyph; intended a business icon (briefcase / office building)."),
    ([30.0, 256.4, 45.9, 269.0], "gamepad", "#EDF3FF", "Game controller outline."),
    ([31.0, 288.6, 44.9, 303.4], "home", "#EAF3FE",
     "Drawn as a bag with a pin inside; intended housing icon (home)."),
    ([31.0, 322.2, 45.0, 337.0], "calendar", "#E4EDF8", "Calendar outline with a dot."),
    ([30.0, 355.7, 45.4, 371.2], "gear", "#EEF6FF", "Gear/cog outline."),
]
labels = [
    ([58.4, 126.6, 93.4, 134.5], "Barchasi", "#C8EDFF", "No descenders; baseline 134.4 (cap 7.8). Sampled core on blue; design intent is probably pure white."),
    ([58.6, 160.0, 103.9, 169.6], "Magazinlar", "#D6E2EE", "Box bottom = 'g' descender; baseline 167.2."),
    ([58.6, 193.0, 129.0, 202.2], "O'quv markazlari", "#DCE5F4", "Box bottom = 'q' descender; baseline ~200.3."),
    ([58.7, 226.0, 120.3, 233.7], "Biznes markazi", "#D8E0ED", "No descenders; baseline 233.7."),
    ([59.0, 258.8, 133.6, 268.2], "Ko'ngilochar zona", "#DBE2F1", "Box bottom = 'g' descender; baseline ~266.4."),
    ([58.6, 292.0, 106.9, 301.5], "Turar joylar", "#D3DCEB", "Box bottom = 'j','y' descenders; baseline ~299.6."),
    ([58.7, 325.8, 96.8, 333.5], "Tadbirlar", "#D4DDEC", "No descenders; baseline 333.5."),
    ([58.8, 359.5, 98.3, 367.2], "Xizmatlar", "#D8E0EE", "No descenders; baseline 367.2."),
]
for i, (t, b) in enumerate(rows):
    sel = i == 0
    if sel:
        el("side.item_0", "button", [18.6, t, 152.8, b], fill="#215BFDFF", radius=4.5, group="side", anchor="tl",
           state="selected", pad=4,
           notes="Selected row: solid bright blue #215BFD (slightly lighter top #1D5CF8), 1px lighter top rim, blue outer "
                 "glow (~#1E5BFF 45%, ~3px) especially below.")
    else:
        el("side.item_%d" % i, "button", [18.6, t, 152.8, b], fill="#FFFFFF0D", stroke="#FFFFFF1F", radius=4,
           group="side", anchor="tl", state="normal", pad=2,
           notes="Dark glass row (composite ~#192C42 at right, ~#0E1D2F at left because the panel gradient is darker "
                 "there); 1px light border. Rows are ~32-33px tall with a ~1px gap (pitch ~33.1).")
    ib, ic, icol, inote = icons[i]
    el("side.item_%d_icon" % i, "icon", ib, icon=ic, color=icol, group="side", anchor="tl",
       state="selected" if sel else "normal", pad=2, notes=inote + " Outline style ~1.5px stroke.")
    lb, lt, lcol, lnote = labels[i]
    txt("side.item_%d_label" % i, lb, lt, 10.2, "semibold" if sel else "medium", lcol, "side", "tl",
        state="selected" if sel else "normal", notes=lnote + " Label x starts at 58.6 for all rows.")
el("side.item_0_chevron", "icon", [139.2, 129.4, 141.7, 133.4], icon="chevron-right", color="#3874FF", group="side",
   anchor="tl", state="selected", pad=2,
   notes="Barely visible lighter mark at the right of the selected row (maybe a tiny chevron); optional.")

# ---------------------------------------------------------------- SEARCH / ZOOM
el("map.search", "input", [531.8, 58.9, 729.6, 90.0], fill="#C8DCFF40", stroke="#FFFFFF26", radius=6, group="content",
   anchor="tr", pad=3,
   notes="Light frosted-glass search field (composite #748DB1 over the map). Brighter 1px top rim; faint darker shadow "
         "at the left edge.")
el("map.search_icon", "icon", [542.8, 68.1, 555.4, 80.7], icon="search", color="#D7EAFF", group="content", anchor="tr",
   pad=2, notes="Magnifier outline, ~1.6px stroke.")
txt("map.search_placeholder", [565.8, 70.9, 659.3, 80.2], "Joylashuvni qidirish...", 9.9, "regular", "#AEC4E3",
    "content", "tr", notes="Placeholder text. Box bottom = 'y','q' descenders; baseline 78.3 (cap 7.1).")
el("map.zoom_panel", "panel", [703.4, 126.4, 730.0, 173.9], fill="#1E3352E6", stroke="#0A152433", radius=5,
   group="content", anchor="tr", pad=3,
   notes="Vertical dark-glass capsule holding +/- (composite #263B57). Split into two 26x23.7 buttons at y=150.1.")
el("map.zoom_in", "button", [703.4, 126.4, 730.0, 150.1], group="content", anchor="tr", pad=1,
   notes="Upper half of zoom panel (hit area).")
el("map.zoom_in_icon", "icon", [712.5, 134.5, 721.3, 143.7], icon="plus", color="#E4EEFF", group="content", anchor="tr",
   pad=2, notes="Plus, ~1.8px stroke; sampled #D1D8EF.")
el("map.zoom_divider", "line", [705.0, 149.6, 728.5, 150.6], color="#FFFFFF14", group="content", anchor="tr", pad=1,
   notes="Very faint 1px separator between + and -.")
el("map.zoom_out", "button", [703.4, 150.1, 730.0, 173.9], group="content", anchor="tr", pad=1,
   notes="Lower half of zoom panel (hit area).")
el("map.zoom_out_icon", "icon", [712.6, 162.0, 721.0, 163.4], icon="minus", color="#DCEAFD", group="content",
   anchor="tr", pad=2)

# ---------------------------------------------------------------- BOTTOM-RIGHT CARD
el("map.card", "panel", [533.2, 355.4, 729.5, 405.0], fill="#0C1C30E6", stroke="#FFFFFF14", radius=6, group="cards",
   anchor="br", pad=3,
   notes="'Shahar markazi' location card: dark navy glass, slightly lighter at top-right (#213042) than bottom-left "
         "(#0D2035); soft shadow.")
el("map.card_image", "image", [540.8, 362.8, 580.0, 397.6], radius=4, group="cards", anchor="br", pad=1,
   notes="Thumbnail of the city centre (spire tower + avenue). See assets.map_card_thumb.")
txt("map.card_title", [590.0, 370.8, 653.0, 378.3], "Shahar markazi", 10.0, "bold", "#DFE9F7", "cards", "br",
    notes="No descenders; box bottom = baseline 378.2 (cap 7.2).")
txt("map.card_subtitle", [590.0, 385.3, 655.7, 393.3], "Hozir onlayn: 1,248", 8.3, "regular", "#99A9BA", "cards", "br",
    notes="Box bottom = 'y' descender; baseline 391.4 (cap 6.0). Number is dynamic (online count).")
el("map.card_arrow_btn", "button", [699.8, 369.6, 721.6, 392.0], fill="#2D3D58FF", stroke="#FFFFFF1A", radius=11,
   group="cards", anchor="br", pad=2, notes="Circular button (d~22) with faint light rim.")
el("map.card_arrow", "icon", [706.7, 376.5, 714.3, 385.2], icon="arrow-right", color="#CADBF3", group="cards",
   anchor="br", pad=1)

# ---------------------------------------------------------------- PINS
# (name, circle, bubble, stem or None, point, circleTip, icon box, icon, label box, text, fontPx, label color,
#  bubble fill, stroke, disk, ring)
pins = [
    ("Magazinlar", [234.0, 148.6, 272.8, 187.4], [253.4, 150.3, 336.0, 184.5], [286.5, 183.5, 292.0, 188.2],
     [289.3, 188.0], [253.3, 188.4], [246.1, 159.5, 260.7, 176.9], "bag", [275.0, 163.2, 325.7, 173.6], 11.4,
     "#F3F2FC", "#4B454CF0", "#B07080", "#B03252", "#3F1B33",
     "Label box bottom = 'g' descender; baseline 171.4 (cap 8.2). Bubble: dark warm-grey glass with rose/salmon 1.5px stroke."),
    ("O'quv markazlari", [462.5, 109.5, 500.0, 147.0], [481.3, 111.5, 582.6, 144.0], None,
     [522.6, 144.0], [481.3, 147.0], [472.4, 121.5, 489.4, 136.3], "graduation-cap", [502.0, 123.1, 572.9, 132.5], 10.6,
     "#E5F3FA", "#36585AF0", "#6CA6A0", "#3F9667", "#235844",
     "Label box bottom = 'q' descender; baseline ~131 (cap ~7.6). Bubble: dark teal-green glass, green stroke. No stem drawn: point = bubble bottom centre."),
    ("Biznes markazi", [300.8, 88.8, 337.5, 125.5], [318.6, 89.5, 411.3, 122.3], [356.5, 121.8, 361.5, 126.0],
     [359.0, 126.0], [318.6, 128.3], [312.1, 98.2, 326.7, 114.7], "building", [340.1, 101.4, 402.0, 109.2], 10.0,
     "#DCF2FF", "#1C4A7FF0", "#5CA1E1", "#2464C0", "#0D3F7E",
     "Label has no descenders; box bottom = baseline 109.1 (cap 7.2). Bubble: royal-blue glass with light-blue stroke + soft blue glow."),
    ("Ko'ngilochar zona", [519.3, 184.8, 559.3, 224.8], [539.3, 187.6, 646.5, 221.5], [575.5, 221.0, 581.5, 227.3],
     [578.7, 227.0], [537.5, 227.8], [529.2, 198.6, 547.0, 212.1], "gamepad", [560.0, 199.4, 636.5, 209.4], 10.1,
     "#E4F5FF", "#2A4878F0", "#7792C0", "#6B3CA7", "#1B1E5D",
     "Label box bottom = 'g' descender; baseline 207.4 (cap 7.3). Bubble: indigo-blue glass, periwinkle stroke; purple icon disk."),
    ("Turar joylar", [234.3, 233.6, 274.7, 274.0], [254.5, 236.5, 338.4, 270.3], [292.8, 269.8, 296.2, 276.5],
     [294.4, 276.5], [254.5, 274.5], [246.2, 246.0, 262.8, 262.0], "home", [275.5, 249.0, 328.5, 259.5], 11.1,
     "#F4EFF2", "#494340F0", "#CDA477", "#C0703A", "#3D2B1A",
     "Label box bottom = 'j','y' descenders; baseline 257 (cap 8.0). Bubble: dark warm-grey glass with gold stroke; orange disk; gold stem."),
    ("Tadbirlar", [382.4, 281.3, 422.4, 321.3], [402.4, 283.6, 480.3, 318.2], None,
     [431.4, 318.2], [402.4, 321.3], [395.0, 293.0, 411.0, 310.0], "calendar", [425.2, 296.4, 468.0, 305.0], 11.6,
     "#FBF3FF", "#3F285FF0", "#A168B9", "#663189", "#351454",
     "Label has no descenders; box bottom = baseline 304.8 (cap 8.4). Bubble: deep purple glass, violet stroke, magenta glow spilling ~3px below. "
     "The magenta-lit plaza just below (x~428-440, y~319-327) looks like map art, not UI -> kept; point = bubble bottom centre."),
    ("Xizmatlar", [546.6, 296.8, 584.8, 335.0], [565.7, 298.6, 644.8, 332.8], None,
     [595.7, 332.8], [565.7, 335.0], [558.0, 307.1, 575.9, 325.6], "gear", [589.2, 311.2, 633.9, 319.6], 11.4,
     "#EBFAFF", "#1B4774F0", "#4E8ABA", "#2E66B5", "#1D3F6D",
     "Label has no descenders; box bottom = baseline 319.5 (cap 8.2). Bubble: navy-blue glass with blue stroke; sits over sea. No stem drawn: point = bubble bottom centre."),
]
for n, (name, circ, bub, stem, point, ctip, ib, icon, lb, fpx, lcol, fill, stroke, disk, ring, note) in enumerate(pins):
    full = [min(circ[0], bub[0]), min(circ[1], bub[1]), max(circ[2], bub[2]), max(circ[3], bub[3])]
    el("map.pin_%d" % n, "pin", full, text=name, point=point, circleTip=ctip, fill=fill, stroke=stroke + "FF",
       radius=7, group="pins", anchor="c", pad=4,
       notes="Map pin for category '%s' (side.item_%d). Anchored in MAP space (moves with pan/zoom). Shape = icon circle "
             "on the left + rounded label bubble (radius ~7, 1.5px stroke) whose left end starts at the circle centre; "
             "bubble box %s. 'point' = tip of the bubble's bottom stem (or bubble bottom centre when no stem is drawn); "
             "'circleTip' = small teardrop tip under the icon circle (alternative anchor). %s"
             % (name, n + 1, [round(v, 1) for v in bub], note))
    el("map.pin_%d_circle" % n, "badge", circ, fill=disk + "FF", stroke=stroke + "FF", radius=round((circ[2] - circ[0]) / 2, 1),
       group="pins", anchor="c", pad=3,
       notes="Icon disk: outer %s stroke (~1.5px), then dark ring %s (~2.5px), then solid disk %s. Slightly taller than "
             "the bubble (overhangs ~1-3px top/bottom)." % (stroke, ring, disk))
    el("map.pin_%d_icon" % n, "icon", ib, icon=icon, color=["#FFF9F8","#F3FFF7","#E8F8FF","#FEF5FF","#FFF8ED","#FFF5FF","#F3FDFF"][n], group="pins", anchor="c", pad=1,
       notes="Solid white glyph centred in the disk.")
    txt("map.pin_%d_label" % n, lb, name, fpx, "semibold", lcol, "pins", "c", pad=1,
        notes=note.split(" Bubble:")[0] + " Pin labels vary 10.0-11.6px in the drawing; ~10.8px uniform is fine.")
    if stem:
        el("map.pin_%d_stem" % n, "line", stem, color=stroke, group="pins", anchor="c", pad=2,
           notes="Short pointer stem/tail under the bubble; its bottom = 'point' of map.pin_%d." % n)

# ---------------------------------------------------------------- SPEC
spec = {
    "page": "map",
    "size": [759, 415],
    "elements": E,
    "removePolygons": [],
    "assets": [
        {"name": "map_background", "box": [0, 0, 759, 415],
         "notes": "Full-bleed isometric island-city map at dusk (central spire tower, districts, sea). Remove every "
                  "element above with remove=true (nav, header, side panel + list, search, zoom, all 7 pins incl. "
                  "stems/glow, bottom-right card). Top-left is naturally dark (vignette/sea) - keep that."},
        {"name": "map_card_thumb", "box": [540.8, 362.8, 580.0, 397.6],
         "notes": "Thumbnail inside the 'Shahar markazi' card (city centre spire + avenue, daylight). Rounded corners r~4 "
                  "(crop square, mask in Unity). No overlay to remove."},
        {"name": "profile_avatar", "box": [610.3, 13.5, 635.3, 38.8],
         "notes": "Circular avatar portrait from the nav profile pill (circle mask). Optional; likely shared with other pages."},
    ],
    "notes": "Right-side column right edge is consistently x~729.6 (29px margin); left content margin x~18.6-20.7. "
             "No distinct nav bar background (nav sits directly on the dark top of the art)."
}
out = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\spec\map.json"
os.makedirs(os.path.dirname(out), exist_ok=True)
with open(out, "w", encoding="utf-8") as f:
    json.dump(spec, f, ensure_ascii=False, indent=1)
print(out, len(E))
