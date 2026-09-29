import json, os

OUT = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\spec\friends.json"

E = []


def el(id, kind, box, **kw):
    d = {"id": id, "kind": kind, "box": [round(float(v), 1) for v in box]}
    d.update(kw)
    E.append(d)


def fpx(cap):
    return round(cap / 0.72, 1)


# ---------------------------------------------------------------- NAV
el("nav.brand_icon", "icon", [13.7, 11.4, 28.8, 26.8], icon="logo", color="#D8FCFF", fill="#3676FCFF", radius=3.5,
   anchor="tl", group="nav", remove=True, pad=2,
   notes="Rounded-square app logo: blue fill #3676FC (slightly lighter top-left), white/ice-blue swirl 'C'/@-like glyph (#D8FCFF). ~15 px square.")
el("nav.brand_text", "text", [34.0, 15.1, 71.5, 24.7], text="Lynxos", fontPx=fpx(22.7 - 15.1), weight="bold", color="#FAFFFF",
   anchor="tl", group="nav", remove=True, pad=2,
   notes="Box = cap top (L) to descender of y. Cap height 7.6 px (15.1-22.7), baseline y=22.7.")
el("nav.tab_home", "text", [93.8, 16.1, 131.0, 22.7], text="Bosh sahifa", fontPx=fpx(6.6), weight="medium", color="#CBD4E3",
   anchor="tc", group="nav", state="normal", remove=True, pad=2, notes="Cap top to baseline (no descenders). Baseline y=22.7.")
el("nav.tab_selected", "panel", [141.3, 9.0, 182.9, 31.6], fill="#1448A9E6", radius=2, anchor="tc", group="nav", state="selected",
   remove=True, pad=2,
   notes="DRAWN ON 'Dunyo' BY MISTAKE - in game this highlight goes behind the active tab (Do'stlar). Vertical gradient: transparent/"
         "#13233B at top (y~9) -> #152D5E (y~17) -> #1448A9 at bottom (y~29); soft blue glow. Underline sits on its bottom edge.")
el("nav.tab_underline", "line", [141.8, 29.8, 182.8, 31.4], color="#90C4FF", radius=0.8, anchor="tc", group="nav", state="selected",
   remove=True, pad=2, notes="Bright glowing underline (core #96C9FF, 1.6 px thick) spanning the full width of nav.tab_selected; outer glow #3C68BC.")
el("nav.tab_world", "text", [151.5, 16.1, 173.5, 24.2], text="Dunyo", fontPx=fpx(22.8 - 16.1), weight="semibold", color="#DBF1FF",
   anchor="tc", group="nav", state="selected", remove=True, pad=2,
   notes="Drawn as the selected tab (mistake; on this page Do'stlar should be selected). Box to y descender; baseline y=22.8.")
el("nav.tab_friends", "text", [194.9, 16.2, 220.2, 22.8], text="Do'stlar", fontPx=fpx(6.6), weight="medium", color="#D7E3F5",
   anchor="tc", group="nav", state="normal", remove=True, pad=2,
   notes="In game this is the SELECTED tab on this page (use selected styling: #DBF1FF semibold + nav.tab_selected + nav.tab_underline).")
el("nav.tab_top", "text", [241.4, 16.3, 265.6, 24.0], text="Top-lar", fontPx=fpx(6.6), weight="medium", color="#D2DDF3",
   anchor="tc", group="nav", state="normal", remove=True, pad=2, notes="Box to p descender; baseline y~22.4. Same style as the other nav tabs (measured T cap 6.0 is blur-reduced).")
el("nav.tab_settings", "text", [285.6, 16.5, 321.9, 23.1], text="Sozlamalar", fontPx=fpx(6.6), weight="medium", color="#C9D2E3",
   anchor="tc", group="nav", state="normal", remove=True, pad=2, notes="Cap top to baseline.")
el("nav.lang_globe", "icon", [346.1, 14.4, 358.0, 26.1], icon="globe", color="#DCE6F2", anchor="tr", group="nav", remove=True, pad=2,
   notes="Outline globe (meridian + 2 latitude lines), ~1.3 px stroke.")
el("nav.lang_text", "text", [361.4, 18.0, 369.3, 23.1], text="UZ", fontPx=fpx(5.1), weight="medium", color="#A7B4C9", anchor="tr",
   group="nav", remove=True, pad=2)
el("nav.lang_chevron", "icon", [372.6, 19.3, 376.4, 22.1], icon="chevron-down", color="#A6B7CF", anchor="tr", group="nav",
   remove=True, pad=2)
el("nav.bell", "icon", [388.0, 14.1, 399.1, 26.8], icon="bell", color="#DADFEC", anchor="tr", group="nav", remove=True, pad=2,
   notes="Outline bell, ~1.3 px stroke.")
el("nav.bell_dot", "dot", [396.0, 14.4, 400.0, 17.5], color="#D04452", anchor="tr", group="nav", remove=True, pad=1.5,
   notes="Red notification dot on the bell's top-right (sampled core #B7414D, small/blurred; use a saturated red ~#E5484D).")
el("nav.profile_pill", "pill", [406.6, 10.3, 478.3, 30.8], fill="#283A54E6", stroke="#3A4A62B0", radius=10.2, anchor="tr",
   group="nav", remove=True, pad=2,
   notes="Fully rounded glass pill; fill observed #283A54-#2A3C54, faint lighter 1 px rim (top #32425A). Left end holds the avatar on a lighter grey disc (#4E586C, ~[407,10.5,428.6,30.8]).")
el("nav.profile_avatar", "avatar", [412.4, 12.5, 426.6, 29.2], anchor="tr", group="nav", remove=True, pad=1,
   notes="Round portrait photo (young man, dark hair) inside a ~2-3 px light grey ring/disc (#4E586C..#5E697E) that fills the pill's left cap. Circle centre ~(419.5,20.8), diameter ~16.")
el("nav.profile_name", "text", [430.1, 18.4, 462.8, 24.0], text="Lynxos_user", fontPx=fpx(23.2 - 18.4), weight="medium",
   color="#C3CDDE", anchor="tr", group="nav", remove=True, pad=2,
   notes="Drawn blurry/garbled but reads 'Lynxos_user'. Box includes underscore/descender; baseline ~23.2.")
el("nav.profile_chevron", "icon", [467.9, 19.0, 472.6, 22.1], icon="chevron-down", color="#BBCCE4", anchor="tr", group="nav",
   remove=True, pad=2)

# ---------------------------------------------------------------- HEADER
el("header.title", "text", [14.4, 45.0, 138.9, 58.6], text="Do'stlar va muloqot", fontPx=fpx(55.9 - 45.0), weight="bold",
   color="#FDFFFF", anchor="tl", group="header", remove=True, pad=3,
   notes="Box = cap top (D) to q descender; baseline y=55.9, cap height 10.9.")
el("header.subtitle", "text", [14.0, 64.4, 134.4, 73.3], text="Yangi odamlar bilan tanishing", fontPx=fpx(71.4 - 64.6),
   weight="regular", color="#ADBBD0", anchor="tl", group="header", remove=True, pad=3,
   notes="Box = cap/ascender top to g descender; baseline ~71.4. Muted blue-grey (anti-aliased average #8694A8).")

# ---------------------------------------------------------------- CONTENT TABS (segmented control)
el("friends.tabs_bar", "panel", [229.5, 48.6, 477.7, 70.4], fill="#1F344FF0", stroke="#2E435E99", radius=4, anchor="tr",
   group="tabs", remove=True, pad=2,
   notes="Segmented control container: darker navy glass (#1F344F) than the surrounding bg (#2C4563), faint lighter top rim. 4 equal-ish segments (~62 px each).")
el("friends.tab_selected", "button", [229.8, 48.1, 294.6, 70.8], fill="#3880FAFF", radius=4, anchor="tr", group="tabs",
   state="selected", remove=True, pad=2,
   notes="Selected segment fill: bright blue #3880FA (left/top #3A84FC, right part #3375F9), subtle soft glow. Occupies segment 0.")
el("friends.tab_0", "tab", [229.8, 48.6, 294.6, 70.4], anchor="tr", group="tabs", state="selected", remove=True, pad=1,
   notes="Segment 0 hit area (Do'stlar).")
el("friends.tab_0_label", "text", [248.8, 56.7, 276.4, 63.1], text="Do'stlar", fontPx=fpx(63.1 - 56.8), weight="semibold",
   color="#FFFFFF", anchor="tr", group="tabs", state="selected", remove=True, pad=2,
   notes="White on blue (sampled #D9F8FF, blurred). Cap top to baseline.")
el("friends.tab_1", "tab", [294.6, 48.6, 356.9, 70.4], anchor="tr", group="tabs", state="normal", remove=True, pad=1,
   notes="Segment 1 (Guruhlar).")
el("friends.tab_1_label", "text", [310.8, 56.5, 341.1, 63.1], text="Grunhlar", intended="Guruhlar", fontPx=fpx(6.5),
   weight="medium", color="#C6D5EB", anchor="tr", group="tabs", state="normal", remove=True, pad=2,
   notes="AI-garbled spelling 'Grunhlar' -> 'Guruhlar' (Groups).")
el("friends.tab_sep_0", "line", [355.9, 50.0, 357.9, 69.0], color="#263953", anchor="tr", group="tabs", remove=True, pad=1,
   notes="Very faint vertical divider between segments 1 and 2 (+5 lum over fill).")
el("friends.tab_2", "tab", [356.9, 48.6, 418.0, 70.4], anchor="tr", group="tabs", state="normal", remove=True, pad=1,
   notes="Segment 2 (Tadbirlar).")
el("friends.tab_2_label", "text", [372.9, 56.5, 403.2, 63.1], text="Tadbirlar", fontPx=fpx(6.5), weight="medium", color="#C6D3EA",
   anchor="tr", group="tabs", state="normal", remove=True, pad=2, notes="Events.")
el("friends.tab_sep_1", "line", [416.9, 50.0, 418.9, 69.0], color="#253A55", anchor="tr", group="tabs", remove=True, pad=1,
   notes="Very faint vertical divider between segments 2 and 3.")
el("friends.tab_3", "tab", [418.0, 48.6, 477.7, 70.4], anchor="tr", group="tabs", state="normal", remove=True, pad=1,
   notes="Segment 3 (Chat).")
el("friends.tab_3_label", "text", [440.2, 56.5, 456.6, 63.0], text="Chat", fontPx=fpx(6.5), weight="medium", color="#C6D2E8",
   anchor="tr", group="tabs", state="normal", remove=True, pad=2)

# ---------------------------------------------------------------- SEARCH
el("friends.search", "input", [185.6, 79.0, 478.8, 102.8], fill="#334968E6", stroke="#4D6385B0", radius=5, anchor="tr",
   group="search", remove=True, pad=2,
   notes="Glass search field spanning the right content column. Fill observed #334968 (top) -> #304766 (bottom) over bg #2A4464; 1 px lighter rim (#4D6385 top, #405673 left). A tiny faint speck at ~(467,90) inside is an artifact - ignore.")
el("friends.search_icon", "icon", [194.9, 85.9, 205.0, 95.9], icon="search", color="#C6D8F0", anchor="tr", group="search",
   remove=True, pad=2, notes="Magnifier, ~1.5 px stroke, handle to bottom-right.")
el("friends.search_placeholder", "text", [212.1, 87.6, 291.8, 94.9], text="Foydalanuvchini qidirish...",
   fontPx=fpx(93.1 - 87.7), weight="regular", color="#8194B1", anchor="tr", group="search", remove=True, pad=2,
   notes="Placeholder, drawn blurry but legible as 'Foydalanuvchini qidirish...' (Search user...). Box to y/q descenders; baseline ~93.1.")

# ---------------------------------------------------------------- HERO
el("friends.hero_image", "image", [186.0, 108.5, 479.0, 267.6], radius=5.5, stroke="#8C96B080", anchor="tr", group="hero",
   notes="Photo-style picture: group of young people sitting on a terrace, seen from behind, looking at a futuristic skyline at sunset (tree top-right). Thin light rim (~1 px, #8C96B0 on the left edge) and dark drop shadow below. NOT marked remove so the inpainted page keeps it for the asset crop; overlay text/button below are removed.")
el("friends.hero_title", "text", [203.7, 128.8, 347.2, 140.5], text="Birga dunyoni kashf eting", fontPx=fpx(137.8 - 129.0),
   weight="bold", color="#F5FFFF", anchor="tl", group="hero", remove=True, pad=3,
   notes="Positioned relative to hero_image top-left (+17.9, +20.4). White with soft dark text shadow. Box = cap top to g descender; baseline ~137.8.")
el("friends.hero_button", "button", [269.3, 229.0, 393.6, 256.7], fill="#1D58FBFF", radius=5, anchor="bc", group="hero",
   remove=True, pad=3,
   notes="Primary CTA, horizontally centred on the hero (hero centre x=332.4, button centre x=331.5), bottom 11 px above hero bottom. Fill #1D58FB (top #1854F8 -> bottom #225CFE), dark navy outline/shadow (#000C3D) around it.")
el("friends.hero_button_label", "text", [303.7, 239.9, 348.9, 248.0], text="Do'st topish", fontPx=fpx(246.9 - 240.0),
   weight="semibold", color="#FFFFFF", anchor="bc", group="hero", remove=True, pad=2,
   notes="White (sampled #D0F3FF, blurred). Box to p descender; baseline ~246.9.")
el("friends.hero_button_arrow", "icon", [374.1, 239.8, 382.0, 247.2], icon="arrow-right", color="#FFFFFF", anchor="bc", group="hero",
   remove=True, pad=2, notes="Thin right arrow, ~1.3 px stroke, right-aligned inside the button (11.6 px from its right edge).")

# ---------------------------------------------------------------- SIDE LIST (Tavsiyalar)
el("side.panel", "panel", [11.7, 85.4, 175.3, 260.0], fill="#253650E0", stroke="#34465E80", radius=5, anchor="tl", group="side",
   remove=True, pad=3,
   notes="Glass card: fill #24344C in header area -> #273850 in list area -> #25334C bottom; faint lighter rim (top row #273850 over bg). Dark soft shadow outside (#021020 at x=11).")
el("side.title", "text", [19.9, 94.7, 61.4, 103.8], text="Tavsiyalar", fontPx=fpx(101.6 - 94.7), weight="semibold",
   color="#F1FAFF", anchor="tl", group="side", remove=True, pad=2,
   notes="Panel heading 'Recommendations'. Box to y descender; baseline ~101.6.")
el("side.title_dot", "dot", [164.9, 97.0, 166.4, 98.9], color="#415169", anchor="tl", group="side", remove=True, pad=1.5,
   notes="Tiny faint dot at the right of the heading row - probably an AI artifact (or a hint of a 'more' menu). Optional.")

rows = [
    dict(fill=[12.5, 109.6, 174.9, 145.3], av=[19.6, 115.7, 44.2, 140.2], ring="#9397A9",
         name=[51.1, 119.9, 82.0, 126.9], ntext="Anvar_uz", ncol="#E1EEFF", ncap=125.5 - 119.9,
         st=[50.6, 130.6, 74.1, 137.9], stext="Onlayn", sint=None, scol="#398B76", scap=136.3 - 130.8,
         snote="Green 'online' status (sampled muted #398B76 on the highlighted row; rows 1 uses brighter #45A06F - suggest one online green ~#3DBE7A).",
         act=[152.9, 121.6, 165.9, 135.0], acol="#B7C5DC", state="selected"),
    dict(fill=[12.5, 145.3, 174.9, 181.0], av=[19.3, 151.9, 44.3, 176.0], ring="#D7D5D3",
         name=[50.9, 155.0, 80.1, 163.0], ntext="Sofia_9S", ncol="#D4DDE8", ncap=6.1,
         st=[50.7, 166.1, 75.3, 173.2], stext="Onlayn", sint=None, scol="#45A06F", scap=5.8,
         snote="Green 'online' status; drawn heavily garbled (blob) - intended 'Onlayn'.",
         act=[153.5, 158.2, 165.0, 169.8], acol="#EEFCFF", state="normal"),
    dict(fill=[12.5, 181.0, 174.9, 217.0], av=[19.2, 187.4, 44.6, 212.1], ring="#848F9E",
         name=[51.3, 191.0, 82.0, 197.1], ntext="TechMan", ncol="#E3ECFB", ncap=197.1 - 191.0,
         st=[50.7, 202.3, 74.2, 209.2], stext="O'ynda", sint="O'yinda", scol="#833D38", scap=208.2 - 202.2,
         snote="Red/coral 'in game' status (sampled dark #833D38 because of blur; suggest ~#E0605A). Garbled 'O'ynda' -> 'O'yinda'. Box to y descender.",
         act=[153.5, 194.0, 165.1, 205.6], acol="#F1FDFF", state="normal"),
    dict(fill=[12.5, 217.0, 174.9, 253.0], av=[19.1, 223.8, 44.9, 249.0], ring="#7A7D8B",
         name=[51.0, 227.3, 75.7, 233.4], ntext="Madina", ncol="#D4DFF0", ncap=233.4 - 227.4,
         st=[50.9, 238.8, 71.9, 244.8], stext="Offline", sint=None, scol="#9AAABD", scap=244.5 - 238.9,
         snote="Grey 'offline' status.",
         act=[153.0, 229.9, 165.1, 242.1], acol="#F4FFFF", state="normal"),
]
for i, r in enumerate(rows):
    if i == 0:
        el("side.item_0", "tile", r["fill"], fill="#2C4464FF", radius=4, anchor="tl", group="side", state="selected", remove=True,
           pad=1, notes="Highlighted (selected/hover) row: lighter navy #2C4464 (left side ~#283E60, right ~#364866), flush with panel sides. Row pitch 36 px.")
    else:
        el("side.item_%d" % i, "tile", r["fill"], anchor="tl", group="side", state="normal", remove=True, pad=0,
           notes="Unhighlighted row (no own fill; panel fill shows through). Row pitch ~36 px.")
    el("side.item_%d_icon" % i, "avatar", r["av"], stroke=r["ring"], anchor="tl", group="side", remove=True, pad=1.5,
       notes="Round avatar photo (diameter ~24.6, centre x~31.9) with a ~1.5 px light ring (%s). Portraits: %s." % (
           r["ring"], ["young man, brown hair", "smiling woman, long brown hair", "man with dark hair, beard shadow", "woman with dark curly hair"][i]))
    lab = dict(text=r["ntext"], fontPx=8.5, weight="semibold", color=r["ncol"], anchor="tl", group="side", remove=True, pad=2,
               notes="User name. All four names share one style (~8.5 px font, cap ~6.1). Box includes underscore where present.")
    if r["ntext"] == "Sofia_9S":
        lab["intended"] = "Sofia_95"
        lab["notes"] += " Last glyph drawn ambiguous (S or 5)."
    el("side.item_%d_label" % i, "text", r["name"], **lab)
    st = dict(text=r["stext"], fontPx=fpx(r["scap"]), weight="medium", color=r["scol"], anchor="tl", group="side", remove=True, pad=2,
              notes=r["snote"])
    if r["sint"]:
        st["intended"] = r["sint"]
    el("side.item_%d_status" % i, "text", r["st"], **st)
    el("side.item_%d_action" % i, "button", r["act"], icon="user-plus", color=r["acol"], radius=6.5, anchor="tl", group="side",
       state=r["state"], remove=True, pad=2,
       notes="Circular outline action button (~12-13 px, 1.3 px white ring) with a small garbled glyph in the centre; intended most likely 'add friend' (user-plus) or 'message'. Centred vertically on the row, centre x~159.3." +
             (" Ring dimmer (#B7C5DC) on the highlighted row." if i == 0 else ""))

el("side.divider_0", "line", [46.0, 180.6, 173.5, 181.6], color="#2B3B53", anchor="tl", group="side", remove=True, pad=1,
   notes="Very faint 1 px row separator between item_1 and item_2 (+3 lum), inset from the left (starts under the name column).")
el("side.divider_1", "line", [46.0, 216.6, 173.5, 217.6], color="#283951", anchor="tl", group="side", remove=True, pad=1,
   notes="Very faint 1 px row separator between item_2 and item_3.")
el("side.divider_2", "line", [14.0, 252.6, 173.5, 253.6], color="#34445D", anchor="tl", group="side", remove=True, pad=1,
   notes="Lighter 1 px line under the last row (stronger in the middle, fades toward the ends) - looks like the panel's inner bottom bevel/highlight.")

spec = {
    "page": "friends",
    "size": [492, 285],
    "notes": "Friends page (Do'stlar va muloqot). Background: dark blurred navy, vertical/diagonal gradient - top-left #061422, around header #1F3A5A, right side #263D59..#2C4563, bottom #122032; warm reddish haze at far right mid (#3A3C46). No distinct nav bar. Layout: left column = side.panel (x 11.7-175.3), right content column x 185.6-478.8 (gutter 10.3, right margin ~13). Nav highlight drawn on 'Dunyo' by mistake - in game Do'stlar is selected. All UI except friends.hero_image is marked remove; hero_image is a separate asset whose overlay (title + CTA) is removed.",
    "elements": E,
    "removePolygons": [],
    "assets": [
        {"name": "friends.hero_image", "box": [186.0, 108.5, 479.0, 267.6],
         "notes": "Big picture: people sitting on a terrace seen from behind, looking at a futuristic city skyline at sunset. Remove the overlay text 'Birga dunyoni kashf eting' (friends.hero_title) and the 'Do'st topish' button (friends.hero_button + label + arrow, incl. its dark outline) before use. Corners rounded ~5.5 px; crop 1 px inside to drop the light rim. Button covers people's backs/legs and the bench at y 226-259 - inpaint needs to rebuild bench/cushions."}
    ],
}
os.makedirs(os.path.dirname(OUT), exist_ok=True)
with open(OUT, "w", encoding="utf-8") as f:
    json.dump(spec, f, ensure_ascii=False, indent=1)
print("wrote", OUT, len(E), "elements")
