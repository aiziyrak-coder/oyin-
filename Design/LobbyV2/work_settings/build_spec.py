import json, os

OUT = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\spec\settings.json"

E = []


def el(id, kind, box, **kw):
    d = {"id": id, "kind": kind, "box": [round(v, 1) for v in box]}
    d.update(kw)
    E.append(d)


NAV_TAB = "#C8D2E3"
# ---------------- NAV ----------------
el("nav.brand_icon", "icon", [13.8, 11.3, 29.2, 26.9], icon="brand-swirl", color="#D9FEFF", fill="#3D78EAFF", radius=3.5,
   anchor="tl", group="nav",
   notes="Blue rounded-square app icon. Fill gradient top-left #4D88F8 -> bottom-right #2A6EEF. White/cyan swirl glyph (like '@'/'e' spiral), glyph ink box ~[17.1,14.2,25.5,24.6].")
el("nav.brand_text", "text", [34.6, 15.2, 71.4, 24.8], text="Lynxos", fontPx=10.8, weight="bold", color="#FBFFFF", anchor="tl",
   group="nav", notes="Box includes 'y' descender (to 24.8). Cap top 15.2, baseline 23.0.")
tabs = [("nav.tab_home", "Bosh sahifa", [89.5, 16.7, 125.3, 23.0], "cap top to baseline (no descenders)", "#BAC3D2"),
        ("nav.tab_world", "Dunyo", [144.1, 16.7, 165.2, 24.2], "includes 'y' descender; baseline 23.0", "#C2CBDB"),
        ("nav.tab_friends", "Do'stlar", [184.6, 16.7, 209.2, 23.0], "cap top to baseline (no descenders)", "#D1DDED"),
        ("nav.tab_top", "Top-lar", [228.4, 16.8, 251.6, 24.2], "includes 'p' descender; baseline 23.0", "#CEDBF1"),
        ("nav.tab_settings", "Sozlamalar", [270.0, 17.0, 305.3, 23.0], "cap top to baseline (no descenders)", "#CAD7EA")]
for tid, txt, box, n, samp in tabs:
    extra = {}
    note = n + ". Sampled core %s; all tabs share one color (~#C8D2E3, ~80%% white)." % samp
    if tid == "nav.tab_settings":
        extra["state"] = "selected"
        note += " Current page, BUT the concept draws NO highlight pill and NO underline here - tab looks identical to the others."
    else:
        extra["state"] = "normal"
    el(tid, "tab", box, text=txt, fontPx=8.75, weight="medium", color=NAV_TAB, anchor="tl", group="nav", notes=note, **extra)
el("nav.lang_globe", "icon", [334.6, 14.0, 347.3, 27.0], icon="globe", color="#E4ECF8", anchor="tr", group="nav",
   notes="Outline globe (meridian + 2 parallels), ~1.3px stroke.")
el("nav.lang_text", "text", [352.9, 17.6, 360.9, 23.4], text="UZ", fontPx=8.1, weight="semibold", color="#DEE9FF", anchor="tr",
   group="nav", notes="Cap top to baseline.")
el("nav.lang_chevron", "icon", [366.5, 19.3, 370.9, 22.3], icon="chevron-down", color="#C0CFE0", anchor="tr", group="nav",
   notes="Small thin chevron.")
el("nav.bell", "icon", [386.0, 13.7, 396.9, 26.9], icon="bell", color="#E4F0FC", anchor="tr", group="nav",
   notes="Outline bell ~1.3px stroke, clapper at bottom.")
el("nav.bell_dot", "dot", [394.1, 14.2, 397.8, 17.9], color="#E0475C", anchor="tr", group="nav",
   notes="Red notification dot at bell top-right, ~3.7px diameter. Brightest sampled pixel #CF4151 (tiny + blurred), true color brighter red.")
el("nav.profile_pill", "pill", [407.0, 10.4, 482.0, 30.8], fill="#A0B4D233", radius=10.2, anchor="tr", group="nav",
   notes="Translucent glass pill; composite color over bg ~#2F415D (bg #14263C). No visible stroke.")
el("nav.profile_avatar", "avatar", [408.3, 11.0, 426.6, 29.6], stroke="#9AA6B8FF", anchor="tr", group="nav",
   notes="Circular avatar photo (same young man as settings.avatar), ~18.5px diameter, thin light grey rim (brighter on the left side). Sits at the left end of the pill.")
el("nav.profile_name", "text", [431.0, 18.3, 464.8, 24.2], text="Lyrxos_user", intended="Lynxos_user", fontPx=6.5, weight="medium",
   color="#BCC9DE", anchor="tr", group="nav", notes="Tiny text; box includes 'y' descender/underscore; cap top 18.3, baseline 23.0.")
el("nav.profile_chevron", "icon", [470.2, 19.0, 474.9, 22.3], icon="chevron-down", color="#CEE1FD", anchor="tr", group="nav")

# ---------------- HEADER ----------------
el("header.title", "text", [14.7, 44.8, 95.6, 56.8], text="Sozlamalar", fontPx=16.6, weight="bold", color="#FBFFFF", anchor="tl",
   group="header", notes="Cap top 44.8 to baseline 56.9, no descenders.")
el("header.subtitle", "text", [14.2, 64.5, 155.2, 73.2], text="Hisobingiz va tajnbangizmi boshqaring",
   intended="Hisobingiz va tajribangizni boshqaring", fontPx=9.0, weight="regular", color="#9AA4B8", anchor="tl", group="header",
   notes="Garbled AI text. Box includes descenders (g, j, q); cap top 64.5, baseline 71.0. Muted grey-blue (~60% white).")

# ---------------- SIDE LIST ----------------
side = [
    ("Profil", None, "user", [85.0, 105.7], [24.1, 90.1, 33.9, 100.6], [46.0, 91.8, 63.4, 98.4], "cap top to baseline"),
    ("Xavfaizlik", "Xavfsizlik", "shield-clock", [106.8, 127.9], [24.3, 111.6, 33.9, 123.0], [46.0, 114.1, 77.4, 120.3], "cap top to baseline"),
    ("Bildirishnomalar", None, "bell", [128.7, 150.2], [24.3, 134.0, 33.9, 145.4], [46.0, 136.1, 100.5, 142.5], "cap top to baseline"),
    ("Ovoz va grafika", None, "volume", [151.0, 172.6], [23.8, 157.4, 34.7, 166.5], [45.7, 158.6, 98.6, 166.5], "includes 'g' descender; baseline 165.0"),
    ("Boshqaruv", None, "gamepad", [173.4, 195.6], [24.0, 179.4, 34.6, 190.0], [46.0, 181.2, 82.6, 189.2], "includes 'q' descender; baseline 188.0"),
    ("Marfiylik", "Maxfiylik", "lock", [196.4, 218.3], [24.1, 201.7, 34.0, 213.5], [46.0, 204.2, 75.4, 211.9], "includes 'y' descender; baseline 210.6"),
    ("Til", None, "globe", [219.1, 241.5], [23.6, 225.3, 34.5, 236.0], [45.7, 227.4, 53.6, 233.4], "cap top to baseline"),
    ("Yordam", None, "help-circle", [242.3, 264.7], [23.7, 248.4, 34.3, 259.3], [46.0, 250.5, 71.5, 256.8], "cap top to baseline"),
]
icon_notes = {
    "user": "Filled person silhouette (head circle + shoulders), white.",
    "shield-clock": "Outline shield with a clock/timer inside (security).",
    "bell": "Outline bell (notifications).",
    "volume": "Outline speaker with sound waves (sound & graphics).",
    "gamepad": "Outline game controller / joystick-like blob icon (controls); AI-drawn shape is unclear, use a gamepad icon.",
    "lock": "Outline padlock (privacy).",
    "globe": "Outline globe (language).",
    "help-circle": "Circle with '?' (help).",
}
for n, (lab, intended, icon, (y0, y1), ibox, lbox, lnote) in enumerate(side):
    sel = n == 0
    tile = dict(fill="#1F60FBFF" if sel else "#FFFFFF0E", radius=4.0, anchor="tl", group="side",
                state="selected" if sel else "normal")
    if sel:
        tile["notes"] = ("Selected row: solid bright blue, very slight vertical gradient (#2462E9 top edge, #1E61FC middle, #2063F8 bottom), "
                         "faint blue glow just below. No stroke.")
    else:
        tile["stroke"] = "#FFFFFF1A"
        tile["notes"] = ("Separate dark glass tile (composite ~#1A293B over page bg ~#0D1C30): white ~5.5% fill, 1px faint lighter "
                         "border (~10% white). Tiles are stacked with ~1px gap, pitch ~22.7px, height ~21.5px.")
    el("side.item_%d" % n, "button", [12.6, y0, 125.3, y1], **tile)
    el("side.item_%d_icon" % n, "icon", ibox, icon=icon, color="#FFFFFF" if sel else "#E8F0FA", anchor="tl", group="side",
       state="selected" if sel else "normal", notes=icon_notes[icon] + " Stroke ~1.3px, icon center x~29.1.")
    lab_d = dict(text=lab, fontPx=8.9, weight="semibold" if sel else "medium", color="#FFFFFF" if sel else "#D8E0EE",
                 anchor="tl", group="side", state="selected" if sel else "normal",
                 notes=lnote + (". Sampled #CCF4FF (white text blurred over blue)" if sel else ". Sampled core ~#D4DCEC."))
    if intended:
        lab_d["intended"] = intended
        lab_d["notes"] += " Garbled AI text."
    el("side.item_%d_label" % n, "text", lbox, **lab_d)

# ---------------- CONTENT PANEL ----------------
el("settings.panel", "panel", [183.5, 52.6, 483.9, 264.8], fill="#5A78A54D", stroke="#FFFFFF1F", radius=6.0, anchor="tr",
   group="content",
   notes="Large glass card, composite ~#26364A..#2A3C56 (lighter toward right/bottom following the background); 1px lighter border "
         "(~12% white). Backdrop blur. Everything below is laid out inside it.")
el("settings.avatar", "avatar", [196.6, 63.7, 244.4, 111.1], stroke="#8892A8FF", anchor="tl", group="content",
   notes="Circular portrait photo (young man, dark hair, dark suit, white shirt, light grey studio bg), diameter ~47.5, "
         "1.5px light steel ring #8892A8. Center (220.5, 87.4).")
el("settings.name", "text", [254.1, 77.2, 315.7, 87.3], text="Lynxos_user", fontPx=10.8, weight="semibold", color="#F7FDFF",
   anchor="tl", group="content", notes="Box includes 'y' descender and underscore; cap top 77.2, baseline 85.0.")
el("settings.id", "text", [254.1, 93.0, 294.0, 99.0], text="ID: #842193", fontPx=8.3, weight="regular", color="#98A7C0",
   anchor="tl", group="content",
   notes="Cap top to baseline. '#' is drawn blurry (looks like a slashed 0/ø). Muted grey-blue.")
el("settings.edit_button", "button", [419.7, 77.7, 470.9, 98.0], fill="#A9BCDD24", stroke="#FFFFFF14", radius=4.5, anchor="tr",
   group="content", notes="Secondary glass button, composite ~#3C4E6A over panel; faint lighter border.")
el("settings.edit_button_label", "text", [429.3, 85.1, 461.9, 90.9], text="Tahrirlash", fontPx=8.1, weight="medium",
   color="#D6DFFA", anchor="tr", group="content", notes="Centered in the button. Cap top to baseline.")

seps = [119.3, 147.1, 175.1, 203.0, 231.0]
for n, y in enumerate(seps):
    el("settings.sep_%d" % n, "line", [200.0, y - 0.5, 467.0, y + 0.5], color="#FFFFFF14", fill="#FFFFFF14", anchor="tl",
       group="content",
       notes="1px hairline divider (~8% white over panel). Fades out at both ends: full strength ~x216..456, alpha ramps to 0 by x~204 "
       "and x~468." + (" This first divider separates the profile header from the rows." if n == 0 else ""))

rows = [("Ism", "Lynxos_user", [201.0, 130.4, 213.8, 136.8], [269.0, 130.4, 315.5, 138.5], [462.0, 130.3, 466.3, 136.9],
         "cap top to baseline", "includes 'y' descender + underscore; baseline 137.0"),
        ("Email", "user@lynxos.uz", [201.0, 158.1, 220.9, 164.9], [269.0, 158.5, 325.0, 166.8], [462.0, 158.4, 466.4, 164.9],
         "cap top to baseline", "includes 'y' descender and '@'; baseline 165.0"),
        ("Mamlakat", "O'zbekiston", [201.0, 186.3, 237.6, 192.9], [268.7, 186.3, 310.5, 193.0], [462.0, 186.2, 466.3, 192.9],
         "cap top to baseline", "cap top to baseline (no descenders)"),
        ("Til", "O'zbekcha", [200.7, 214.0, 209.0, 220.6], [269.0, 214.2, 306.0, 220.9], [462.1, 214.2, 466.3, 220.8],
         "cap top to baseline", "cap top to baseline (no descenders)"),
        ("Uslub", "Kundalik", [201.0, 242.3, 222.0, 248.9], [269.0, 242.4, 298.9, 249.0], [462.1, 242.2, 466.3, 248.8],
         "cap top to baseline", "cap top to baseline (no descenders)")]
row_bounds = [(119.8, 146.6), (147.6, 174.6), (175.6, 202.5), (203.5, 230.5), (231.5, 259.0)]
val_intended = {"O'zbekiston": "O'zbekiston"}
for n, (lab, val, lb, vb, cb, ln, vn) in enumerate(rows):
    y0, y1 = row_bounds[n]
    el("settings.row_%d" % n, "panel", [200.0, y0, 467.0, y1], fill="#00000000", anchor="tl", group="content",
       notes="Logical row / hit area (nothing drawn). Row pitch 27.9px between dividers; label, value and chevron are vertically centered.")
    el("settings.row_%d_label" % n, "text", lb, text=lab, fontPx=9.2, weight="medium", color="#E2EAF8", anchor="tl",
       group="content", notes=ln + ". Left column x=201.")
    vd = dict(text=val, fontPx=9.2, weight="regular", color="#D6DFF0", anchor="tl", group="content",
              notes=vn + ". Value column x=269 (slightly dimmer than label).")
    if val == "O'zbekiston":
        vd["notes"] += " The 'o' in '-ston' is drawn with a stray accent mark ('stón'); intended plain O'zbekiston."
    el("settings.row_%d_value" % n, "text", vb, **vd)
    el("settings.row_%d_chevron" % n, "icon", cb, icon="chevron-right", color="#DCE8FC", anchor="tr", group="content",
       notes="Bold-ish right chevron, ~4.3x6.6px, right-aligned (right edge x~466.3, 17.6px from panel right edge).")

spec = {
    "page": "settings",
    "size": [499, 285],
    "notes": ("Settings page. Page background is a dark blurred navy gradient (left ~#071525, center ~#152230, right ~#20324C, "
              "slight bokeh light blotches behind the panel); no art to clean, recreate procedurally. No nav bar background. "
              "The bright 1px line along y~1 and the rounded outer corners are the concept screen frame, not UI. "
              "Anchors of content-panel children describe their alignment inside settings.panel (tl = left column, tr = right-aligned)."),
    "elements": E,
    "removePolygons": [],
    "assets": [
        {"name": "settings.avatar_photo", "box": [196.6, 63.7, 244.4, 111.1],
         "notes": "Circular portrait photo of the player (young man, dark hair, suit). Crop and apply circular mask; the 1.5px light ring (#8892A8) is UI and should be drawn separately."},
        {"name": "nav.profile_avatar_photo", "box": [408.3, 11.0, 426.6, 29.6],
         "notes": "Same portrait at tiny size - prefer reusing settings.avatar_photo scaled down."},
        {"name": "nav.brand_icon", "box": [13.8, 11.3, 29.2, 26.9],
         "notes": "Lynxos app icon (blue rounded square + white swirl); shared across all pages."},
    ],
}
os.makedirs(os.path.dirname(OUT), exist_ok=True)
with open(OUT, "w", encoding="utf-8") as f:
    json.dump(spec, f, ensure_ascii=False, indent=1)
print(len(E), "elements")
