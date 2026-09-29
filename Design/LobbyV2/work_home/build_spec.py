import json, os

OUT = r"C:/Users/alocomputers/AppData/Local/Temp/claude/D--Game1/4f1a5cc1-c33d-4082-8691-871fd7139403/scratchpad/v2/spec/home.json"

E = []


def fp(cap):
    return round(cap / 0.72, 1)


def add(**kw):
    kw.setdefault("remove", True)
    E.append(kw)


# ---------------------------------------------------------------- NAV
add(id="nav.brand_plate", kind="panel", box=[30.0, 12.0, 62.5, 45.5], fill="#2A2D35B0", radius=8, anchor="tl", group="nav", pad=2,
    notes="Faint dark-grey rounded-square backplate / soft shadow behind the brand icon only (not behind the word). Very low contrast; builder may render it as a 32x33 rounded rect, r~8, grey #2A2D35 ~70% or skip it.")
add(id="nav.brand_icon", kind="icon", box=[37.0, 17.5, 57.8, 38.8], color="#E0F9FF", fill="#396EEBFF", radius=4, icon="brand-c-spiral",
    anchor="tl", group="nav", pad=2,
    notes="App logo: blue (#396EEB, slightly lighter #4A80F5 at top) rounded square r~4 with a white concentric 'C'/spiral glyph (two nested open rings opening to the right).")
add(id="nav.brand_text", kind="text", box=[65.0, 23.0, 116.0, 36.0], text="Lynxos", fontPx=fp(10.0), weight="bold", color="#FCFFFF",
    anchor="tl", group="nav", pad=2,
    notes="Ink box includes the 'y' descender. Cap top 23.0, baseline 33.0 (cap height 10 px). Geometric sans (Poppins/Montserrat-like), bold, white.")
add(id="nav.tab_selected", kind="pill", box=[209.8, 11.5, 275.2, 39.8], fill="#2349A8F0", radius=7, anchor="tc", group="nav", state="selected", pad=3,
    notes="Selected-tab background: rounded rect r~7, vertical gradient top #22396C -> bottom #2456BD (fully opaque-looking), faint lighter top rim. Tab text sits centred horizontally, cap-centre ~24.6.")
add(id="nav.tab_underline", kind="line", box=[214.5, 36.6, 271.0, 38.4], color="#86B5FF", anchor="tc", group="nav", state="selected", pad=2,
    notes="Light-blue glowing underline (~1.8 px thick, rounded ends) inside the bottom of the selected pill, inset ~5 px from each side; soft blue glow below it (#3C61B8).")
add(id="nav.tab_home", kind="text", box=[219.6, 20.6, 265.0, 28.5], text="Bosh sahifa", fontPx=fp(7.9), weight="semibold", color="#D6E8FF",
    anchor="tc", group="nav", state="selected", pad=2, notes="Cap top 20.6, baseline 28.5 (no descenders). Near-white with slight blue tint.")
add(id="nav.tab_world", kind="text", box=[289.7, 20.6, 315.0, 30.4], text="Dunyo", fontPx=fp(7.9), weight="medium", color="#DEE0EB",
    anchor="tc", group="nav", state="normal", pad=2, notes="Box includes 'y' descender; baseline 28.5.")
add(id="nav.tab_friends", kind="text", box=[340.6, 20.6, 370.9, 28.5], text="Do'stlar", fontPx=fp(7.9), weight="medium", color="#D8DFF0",
    anchor="tc", group="nav", state="normal", pad=2, notes="Baseline 28.5. Apostrophe drawn as a curly right quote (Do\u2019stlar).")
add(id="nav.tab_top", kind="text", box=[395.3, 20.6, 425.0, 30.4], text="Top-lar", fontPx=fp(7.9), weight="medium", color="#D6DEEC",
    anchor="tc", group="nav", state="normal", pad=2, notes="Box includes 'p' descender; baseline 28.5.")
add(id="nav.tab_settings", kind="text", box=[449.0, 20.6, 493.0, 28.5], text="Sozlamalar", fontPx=fp(7.9), weight="medium", color="#DCE6F8",
    anchor="tc", group="nav", state="normal", pad=2, notes="Baseline 28.5.")
add(id="nav.lang_globe", kind="icon", box=[545.7, 18.4, 560.1, 34.1], icon="globe", color="#E0EEFF", anchor="tr", group="nav", pad=2,
    notes="Outline globe (circle + 2 meridians + 2 parallels), stroke ~1.4 px.")
add(id="nav.lang_text", kind="text", box=[567.5, 21.7, 579.0, 29.6], text="UZ", fontPx=fp(7.9), weight="semibold", color="#D0DFF8",
    anchor="tr", group="nav", pad=2, notes="Cap top 21.7, baseline 29.6.")
add(id="nav.lang_chevron", kind="icon", box=[584.0, 24.5, 590.0, 28.2], icon="chevron-down", color="#C8DCF8", anchor="tr", group="nav", pad=2,
    notes="Small thin chevron, stroke ~1.2 px.")
add(id="nav.bell", kind="icon", box=[609.5, 17.8, 623.0, 34.0], icon="bell", color="#DCEBFF", anchor="tr", group="nav", pad=2,
    notes="Outline bell with small clapper below (clapper bottom at 34.0); stroke ~1.5 px.")
add(id="nav.bell_dot", kind="dot", box=[619.8, 17.9, 624.6, 23.0], color="#D82A4C", anchor="tr", group="nav", pad=2,
    notes="Red notification dot overlapping the bell's top-right, diameter ~4.8, centre (622.2, 20.4).")
add(id="nav.profile_pill", kind="pill", box=[639.3, 12.3, 735.4, 39.6], fill="#1E2A408C", stroke="#FFFFFF14", radius=13.6, anchor="tr", group="nav", pad=3,
    notes="Dark translucent glass capsule (fully rounded ends). Seen over sky it reads as #415A7E; fill ~#1E2A40 @55%, maybe a faint 1 px light rim.")
add(id="nav.profile_avatar", kind="avatar", box=[642.8, 13.8, 664.8, 36.8], anchor="tr", group="nav", pad=1, radius=11,
    notes="Circular avatar photo (young man, dark hair, light shirt) with a thin light-grey ring (#8090A0) visible on the left side; diameter ~22-23.")
add(id="nav.profile_name", kind="text", box=[669.6, 22.6, 712.6, 30.6], text="Lynxos_user", fontPx=fp(6.4), weight="medium", color="#CCDCF6",
    anchor="tr", group="nav", pad=2, notes="Cap top 22.6, baseline 29.0; box includes 'y' descender and underscore. AI-drawn glyphs are blurry ('Lynxos_uoer').")
add(id="nav.profile_chevron", kind="icon", box=[719.5, 23.8, 726.6, 28.4], icon="chevron-down", color="#C8DCF8", anchor="tr", group="nav", pad=2)

# ---------------------------------------------------------------- HEADER (left column)
add(id="home.wordmark_spaced", kind="text", box=[63.5, 63.0, 182.0, 77.5], text="Lynkxos \u2295 \u2228 \u2228", intended="L Y N X O S",
    fontPx=fp(10.0), weight="light", color="#747986", anchor="cl", group="header", pad=3,
    notes="Faint, widely letter-spaced echo of the brand above the big title (letter advance ~12.3 px, i.e. tracking ~0.35 em). Clean glyphs 'L y n k x o s' span x 63.5-146; the trailing 3 garbled glyphs (x 150-182) fade into a lit wall. Cap top 63.5, baseline 73.5, 'y' descender to 77.5. Suggest rendering 'LYNXOS' or 'Lynxos' in light weight, grey #747986 ~70% opacity.")
add(id="header.title", kind="text", box=[40.8, 83.6, 191.9, 116.4], text="Lynxos", fontPx=fp(26.2), weight="extrabold", color="#F7F9FC",
    anchor="cl", group="header", pad=3,
    notes="Big hero wordmark. Cap top 83.6, baseline 109.8 (cap 26.2 px), x-height top 90.0 (very large x-height ~0.76 cap: geometric heavy sans such as Poppins ExtraBold / Montserrat Black), 'y' descender to 116.4. All letters white EXCEPT the 'x' which is blue (#467AEB, see header.title_x). Tight letter spacing.")
add(id="header.title_x", kind="text", box=[119.5, 89.5, 138.0, 110.0], text="x", fontPx=fp(26.2), weight="extrabold", color="#467AEB",
    anchor="cl", group="header", pad=3,
    notes="Accent letter inside header.title (render as rich-text colour span). Blue, slightly lighter towards the top-right stroke (#5B8CF5 -> #3D6FE6).")
add(id="header.subtitle", kind="text", box=[41.2, 131.3, 180.5, 144.2], text="O'z dunyongni kashf et", fontPx=fp(10.5), weight="regular", color="#B8C0D0",
    anchor="cl", group="header", pad=3,
    notes="Cap top 131.3, baseline 141.8, descenders to 144.2. Apostrophe drawn as a curly quote (O\u2019z). Light grey.")
add(id="home.kirish", kind="button", box=[36.7, 164.3, 228.9, 205.6], fill="#2466FCFF", radius=5, anchor="cl", group="header", pad=8,
    notes="Primary CTA. Solid blue, horizontal gradient left #2A7BFD -> right #2057FC (slightly lighter along the top edge). Blue outer glow ~5-6 px (#1E5BFF ~40%) all around - mask pad 8 removes it.")
add(id="home.kirish_icon", kind="icon", box=[55.9, 176.6, 69.8, 193.4], icon="play", color="#F2FAFF", anchor="cl", group="header", pad=2,
    notes="Outlined play triangle (not filled), stroke ~2 px, rounded joins.")
add(id="home.kirish_label", kind="text", box=[85.9, 179.6, 117.5, 190.2], text="Kirish", fontPx=fp(10.6), weight="medium", color="#F2FAFF",
    anchor="cl", group="header", pad=2, notes="Cap top 179.6, baseline 190.2 (no descenders). White.")
add(id="home.kirish_arrow", kind="icon", box=[200.8, 179.2, 212.9, 190.8], icon="arrow-right", color="#F2FAFF", anchor="cl", group="header", pad=2,
    notes="Thin arrow (stroke ~1.6 px), right-aligned ~16 px from the button's right edge.")

# ---------------------------------------------------------------- RIGHT TAGLINE
add(id="home.tagline_line1", kind="text", box=[614.5, 83.3, 718.8, 94.3], text="Cheksiz imkoniyatlar", fontPx=fp(8.4), weight="medium", color="#111528",
    anchor="tr", group="tagline", pad=3,
    notes="Dark navy text over bright sunset sky. Cap top 83.3, baseline 91.7, 'y' descender to 94.3. Left-aligned at x 614.5.")
add(id="home.tagline_line2", kind="text", box=[614.6, 100.2, 728.8, 111.6], text="Bitta virtual dunyoda", fontPx=fp(8.5), weight="medium", color="#111528",
    anchor="tr", group="tagline", pad=3,
    notes="Cap top 100.2, baseline 108.7 (line pitch 17.0 px from line 1), 'y' descender to 111.6.")
add(id="home.tagline_line", kind="line", box=[613.7, 127.6, 658.5, 129.8], color="#4368BA", radius=1, anchor="tr", group="tagline", pad=2,
    notes="Blue accent bar (progress-like), ~2.2 px thick, rounded ends, slightly lighter toward its right end (#6580C6).")
add(id="home.tagline_track", kind="line", box=[658.5, 128.0, 697.0, 130.0], color="#00000018", anchor="tr", group="tagline", pad=2,
    notes="Very faint darker track continuing the blue bar to the right and fading out by x~697 (~6-9% darker than the sky).")

# ---------------------------------------------------------------- EVENT CARD
add(id="home.event_card", kind="panel", box=[576.3, 210.6, 741.8, 274.7], fill="#1E2A42A0", stroke="#FFFFFF26", radius=6, anchor="cr", group="event", pad=3,
    notes="Dark glass card (reads #474B57 over the lit city). Fill ~#1E2A42 @63% plus backdrop blur, 1 px light rim (white ~15%).")
add(id="home.event_thumb", kind="image", box=[584.2, 218.3, 629.8, 266.8], radius=4, anchor="cr", group="event", pad=1,
    notes="Concert thumbnail: dark stage with purple/magenta lights and a blue spotlight beam; see assets.event_thumb.")
add(id="home.event_label", kind="text", box=[638.6, 220.6, 690.7, 230.0], text="Bugungi tadbir", fontPx=fp(7.2), weight="medium", color="#DEE0EA",
    anchor="cr", group="event", pad=2, notes="Cap top 220.6, baseline 227.8, 'g' descenders to 230.0. Light grey.")
add(id="home.event_title", kind="text", box=[638.3, 240.4, 699.3, 247.9], text="Virtual Concert", fontPx=fp(7.5), weight="semibold", color="#DDECFF",
    anchor="cr", group="event", pad=3, notes="Cap top 240.4, baseline 247.9. Near-white with a soft blue glow (#3D6BFF ~30%).")
add(id="home.event_time", kind="text", box=[638.6, 257.2, 660.5, 263.9], text="20.00", fontPx=fp(6.7), weight="medium", color="#9CBBEF",
    anchor="cr", group="event", pad=2, notes="Cap top 257.2, baseline 263.9. Light blue.")

# ---------------------------------------------------------------- BOTTOM CARDS
cards = [
    dict(box=[29.6, 347.6, 156.0, 399.0], state="selected", img=[39.8, 356.5, 74.3, 390.1], kind_img="image",
         title=("Dunyo", [83.6, 366.6, 108.6, 375.7], "#E4EAF4", "cap top 366.6, baseline 373.9, 'y' descender to 375.7"),
         sub=("Erkin sayohat", [83.6, 380.7, 128.6, 388.6], "#A2AABC", "cap top 380.7, baseline 387.4, 'y' descender to 388.6"),
         fill="#1C2E50D8", stroke="#BEE0FFFF", pad=7,
         notes="SELECTED card: navy-tinted glass (#1F2F50) with a 1.5 px light-blue border (#BEE0FF) and blue outer glow ~4-5 px (#2F6BFF) plus faint inner blue glow. Mask pad 7 removes the glow."),
    dict(box=[164.6, 348.8, 295.9, 398.6], state="normal", img=[174.4, 357.0, 208.4, 390.3], kind_img="image",
         title=("Magazinlar", [218.8, 366.8, 261.8, 376.3], "#E5E9EF", "cap top 366.8, baseline 374.0, 'g' descender to 376.3"),
         sub=("Xarid va brendlar", [218.6, 381.3, 275.8, 387.9], "#A6B0BE", "cap top 381.3, baseline 387.9 (no descenders)"),
         fill="#1C2332D0", stroke="#FFFFFF1A", pad=3, notes="Dark glass card, 1 px faint light rim."),
    dict(box=[305.8, 349.3, 441.5, 399.3], state="normal", icon=([319.8, 361.6, 344.7, 387.7], "open-door-building", "Outline icon of an open box/wardrobe-like building with a door (education centre); white, stroke ~2 px, slightly 3D."),
         title=("O'quv markazlari", [359.6, 366.8, 425.8, 375.9], "#EBF1FF", "cap top 366.8, baseline 374.3, 'q' descender to 375.9; apostrophe curly (O\u2019quv)"),
         sub=("Bilim va rhojlanish", [359.6, 381.4, 421.7, 389.5], "#A0ADC1", "cap top 381.4, baseline 387.8, 'j' descender to 389.5", "Bilim va rivojlanish"),
         fill="#1C2536D0", stroke="#FFFFFF1A", pad=3, notes="Dark glass card, 1 px faint light rim."),
    dict(box=[452.4, 349.6, 589.8, 399.5], state="normal", icon=([467.3, 361.6, 491.7, 387.4], "business-bars", "White glyph: three vertical rounded bars of different heights on a baseline (business/finance), with a small accent mark top-left; stroke/fill style."),
         title=("Biznes markazi", [506.6, 367.3, 565.8, 374.8], "#F2F8FF", "cap top 367.3, baseline 374.8 (no descenders)"),
         sub=("Ish va hamkorlik", [506.6, 381.6, 560.6, 387.9], "#A4AEC3", "cap top 381.6, baseline 387.9; capital I drawn like a lowercase l"),
         fill="#1C2536D0", stroke="#FFFFFF1A", pad=3, notes="Dark glass card, 1 px faint light rim."),
    dict(box=[599.0, 349.3, 736.6, 399.5], state="normal", icon=([611.8, 364.3, 638.3, 385.3], "gamepad", "Solid white gamepad with two dark circular 'eyes' (buttons) - filled glyph."),
         title=("Ko'ngilochar zona", [653.5, 367.7, 722.6, 376.7], "#EFF3FB", "cap top 367.7, baseline 374.6, 'g' descender to 376.7; apostrophe curly (Ko\u2019ngilochar)"),
         sub=("O'yin va tadbirlar", [652.6, 381.7, 710.6, 389.8], "#AEB6C6", "cap top 381.7, baseline 387.9, 'y' descender to 389.8"),
         fill="#1A2334D0", stroke="#FFFFFF1A", pad=3, notes="Dark glass card, 1 px faint light rim."),
]
for n, c in enumerate(cards):
    add(id="home.card_%d" % n, kind="tile", box=c["box"], fill=c["fill"], stroke=c["stroke"], radius=5, anchor="bc", group="cards",
        state=c["state"], pad=c["pad"], notes=c["notes"] + " Row of 5 cards spans x 29.6-736.6 (~29.5 px side margins), gaps ~8.6-10.9 px (AI-drawn, uneven; builder may equalise to ~9.7 px gaps / ~133 px width).")
    if "img" in c:
        add(id="home.card_%d_image" % n, kind="image", box=c["img"], radius=5, anchor="bc", group="cards", pad=1,
            notes="Rounded-square thumbnail; see assets.card_%d_image." % n)
    else:
        b, ic, nt = c["icon"]
        add(id="home.card_%d_icon" % n, kind="icon", box=b, icon=ic, color="#FAFFFF", anchor="bc", group="cards", pad=2, notes=nt)
    t = c["title"]
    add(id="home.card_%d_title" % n, kind="text", box=t[1], text=t[0], fontPx=10.2, weight="semibold", color=t[2], anchor="bc", group="cards", pad=2, notes=t[3] + ".")
    s = c["sub"]
    d = dict(id="home.card_%d_subtitle" % n, kind="text", box=s[1], text=s[0], fontPx=9.2, weight="regular", color=s[2], anchor="bc", group="cards", pad=2, notes=s[3] + ".")
    if len(s) > 4:
        d["intended"] = s[4]
    add(**d)

# ---------------------------------------------------------------- CHARACTER
character = {
    "centerX": 358.0,
    "headTopY": 75.5,
    "feetY": 332.0,
    "horizonY": 235.0,
    "notes": ("Standing avatar (red track jacket with white zip/stripes, white tee, dark grey trousers, white sneakers), facing camera. "
              "centerX = midpoint between the two shoes (left shoe x 320.6-345, right shoe x 367-394 -> 357.7); head/hair centre ~361, shoulders 324-393. "
              "headTopY = top of hair at x~358. feetY = bottom of the soles (left ~331.5, right ~332.5). Body height 256.5 px. "
              "horizonY ~235 (+/-4): the far shoreline/promenade where the city meets the water is a bright level line at y~237 and the true horizon sits just above it; "
              "the sofa-back tops in the room (y~228-235) are drawn flat (at eye level) and the terrace/planter edges on the right slope down away from y~235 while ceiling lines rise above it. "
              "So the camera is low (about hip height of the avatar, crotch at y~245) - a slight hero low-angle. "
              "The glossy floor shows a reflection of the shoes/trousers from y~332 down to ~348 (included in the removal polygon); the rest below is covered by card_2."),
}

remove_polygons = [{
    "name": "character",
    "pad": 3,
    "points": [
        [358, 74], [366, 74.5], [372, 77.5], [376.5, 82], [378.5, 89], [378, 97], [376, 104], [374, 110],
        [378, 115], [383, 120], [389, 123.5], [394, 128], [396.5, 134], [398, 142], [399.5, 152], [400.5, 162],
        [401.5, 172], [402.5, 182], [403.5, 192], [405.5, 200], [406, 208], [405, 216], [404, 224], [402, 230],
        [396, 231.5], [392.5, 226], [390.5, 216], [389.5, 226], [389.5, 240], [390, 260], [391, 280], [392, 298],
        [394, 312], [397, 319], [398, 327], [396, 334], [395, 341], [393, 349],
        [366, 349], [364.5, 341], [365, 334], [365.5, 318], [364.5, 300], [363, 280], [362, 262], [360, 246],
        [355.5, 246], [353.5, 262], [352, 280], [350.5, 300], [348, 313], [348.5, 324], [348, 334], [346.5, 341], [345, 349],
        [319, 349], [317, 341], [317, 333], [318, 323], [320.5, 314], [322.5, 300], [325, 282], [327, 262], [329, 244],
        [331.5, 233], [329, 232], [322, 231.5], [318, 227], [316, 218], [315, 208], [314, 198], [315, 186],
        [316, 174], [317, 162], [318.5, 151], [320, 141], [322.5, 133], [327, 126.5], [333, 122], [342, 118],
        [346, 113], [345, 106], [343.5, 99], [343, 91], [345, 84], [349, 78], [353, 75.5],
    ],
}]

assets = [
    {"name": "card_0_image", "box": [39.8, 356.5, 74.3, 390.1],
     "notes": "Thumbnail inside the 'Dunyo' card: a small avatar figure standing on a sunny park path with trees (image area only; corners rounded r~5, corner pixels contain the navy card fill). Nothing drawn over it."},
    {"name": "card_1_image", "box": [174.4, 357.0, 208.4, 390.3],
     "notes": "Thumbnail inside the 'Magazinlar' card: night shopping street / mall with pink-purple neon glow and blue sky (image area only; rounded r~5, thin dark frame on the left edge). Nothing drawn over it."},
    {"name": "event_thumb", "box": [584.2, 218.3, 629.8, 266.8],
     "notes": "'Bugungi tadbir' event thumbnail: concert stage, dark navy background, blue spotlight beam from top centre, purple/magenta crowd lights in the lower half (rounded r~4). Nothing drawn over it."},
]

spec = {"page": "home", "size": [766, 415], "elements": E, "character": character, "removePolygons": remove_polygons, "assets": assets}
os.makedirs(os.path.dirname(OUT), exist_ok=True)
with open(OUT, "w", encoding="utf-8") as f:
    json.dump(spec, f, ensure_ascii=False, indent=1)
print(len(E), "elements")
