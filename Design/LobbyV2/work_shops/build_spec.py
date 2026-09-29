import json, os

OUT = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\spec\shops.json"

E = []


def el(id, kind, box, **kw):
    d = {"id": id, "kind": kind, "box": [round(float(v), 1) for v in box]}
    d.update(kw)
    E.append(d)


# ---------------- NAV ----------------
el("nav.brand_icon", "icon", [13.0, 11.3, 28.6, 27.3], icon="lynxos-logo", fill="#3674ECFF", color="#D6F8FF",
   radius=4, anchor="tl", group="nav", remove=True, pad=2,
   notes="Blue rounded-square app icon (fill ~#3674EC, slight top-light gradient) with a white/ice-blue swirl 'C'/spiral glyph; glyph ink box ~[15.6,13.6,26.3,25.5].")
el("nav.brand_text", "text", [33.8, 15.1, 71.0, 24.3], text="Lynxos", fontPx=10.0, weight="bold", color="#F9FFFF",
   anchor="tl", group="nav", remove=True, pad=2,
   notes="Box includes 'y' descender (cap top 15.1, baseline ~22.3, descender 24.3). Pure white wordmark, rounded geometric sans.")
tabs = [
    ("nav.tab_home", [92.9, 16.3, 129.8, 22.5], "Bosh sahifa", "#C8D4EA", "no descenders; baseline 22.5"),
    ("nav.tab_world", [150.4, 16.5, 171.9, 23.9], "Dunyo", "#D0DCF3", "box includes 'y' descender; baseline ~22.4"),
    ("nav.tab_friends", [192.8, 16.5, 217.3, 22.4], "Do'stlar", "#CEDCF3", "no descenders; baseline 22.4"),
    ("nav.tab_top", [238.4, 16.4, 262.1, 23.4], "Top-lar", "#C8D4EA", "box includes 'p' descender; baseline ~22.4"),
    ("nav.tab_settings", [281.9, 16.2, 316.8, 22.6], "Sozlamalar", "#C9D3E8", "no descenders; baseline 22.6"),
]
for id_, b, t, c, n in tabs:
    el(id_, "tab", b, text=t, fontPx=8.5, weight="medium", color=c, anchor="tl", group="nav", state="normal",
       remove=True, pad=2,
       notes=n + ". No tab is highlighted on this page (Magazinlar is not a nav tab); light grey-blue text (~85% white), no pill/underline.")
el("nav.lang_globe", "icon", [335.3, 14.1, 346.8, 26.0], icon="globe", color="#DFE0EB", anchor="tr", group="nav",
   remove=True, pad=2, notes="Outline globe (meridian/parallel lines), ~1.3px stroke.")
el("nav.lang_text", "text", [350.0, 17.3, 358.0, 22.7], text="UZ", fontPx=7.5, weight="medium", color="#C8CAD4",
   anchor="tr", group="nav", remove=True, pad=2, notes="Caps, no descenders.")
el("nav.lang_chevron", "icon", [361.0, 19.0, 365.1, 21.1], icon="chevron-down", color="#B8B8C0", anchor="tr",
   group="nav", remove=True, pad=2, notes="Tiny thin chevron.")
el("nav.bell", "icon", [375.8, 14.1, 385.9, 25.7], icon="bell", color="#EBE4E9", anchor="tr", group="nav",
   remove=True, pad=2, notes="Outline bell, ~1.3px stroke, white.")
el("nav.bell_dot", "dot", [383.6, 13.6, 387.0, 17.4], color="#E5484D", anchor="tr", group="nav", remove=True, pad=2,
   notes="Red notification dot on the bell's top-right (sampled blurred peak #C24E4F; intended saturated red ~#E5484D/#FF3B4A). Diameter ~3.5.")
el("nav.profile_pill", "pill", [396.3, 9.7, 465.6, 29.5], fill="#3A4560E6", stroke="#FFFFFF14", radius=9.8,
   anchor="tr", group="nav", remove=True, pad=2,
   notes="Fully rounded dark slate-blue glass pill (measured fill #3C4660 over warm bg), avatar sits flush in its left end.")
el("nav.profile_avatar", "avatar", [396.3, 9.8, 413.8, 28.4], stroke="#7A8AA0FF", anchor="tr", group="nav",
   remove=True, pad=1.5,
   notes="Circular photo avatar (young man, dark hair, white shirt) with a ~1.2px light blue-grey ring (#647686..#7A8AA0). Diameter ~18.")
el("nav.profile_name", "text", [417.3, 18.4, 450.0, 23.1], text="Lynxos_user", fontPx=5.2, weight="medium",
   color="#C8CFE3", anchor="tr", group="nav", remove=True, pad=2,
   notes="Very small; box includes 'y'/'_' descenders (cap top 18.4, baseline ~21.8). Glyphs are AI-blurred but read 'Lynxos_user'.")
el("nav.profile_chevron", "icon", [455.1, 19.0, 460.0, 21.9], icon="chevron-down", color="#AAB4CA", anchor="tr",
   group="nav", remove=True, pad=2)

# ---------------- HEADER ----------------
el("header.title", "text", [14.6, 45.7, 100.2, 61.8], text="Magazinlar", fontPx=17.1, weight="bold",
   color="#FCFFFF", anchor="tl", group="header", remove=True, pad=2,
   notes="Box includes 'g' descender (cap top 45.9, baseline 58.2 -> cap height 12.3). White, bold rounded sans.")
el("header.subtitle", "text", [14.0, 66.9, 112.2, 74.0], text="Yuzlab brendlar siz uchun", fontPx=9.4,
   weight="regular", color="#A4AEC2", anchor="tl", group="header", remove=True, pad=2,
   notes="No descenders (baseline 74.0). Muted grey-blue. Last letters ('un') overhang the side panel's right edge (x~110).")

# ---------------- SIDE ----------------
el("side.panel", "panel", [0.0, 0.0, 109.8, 309.0], fill="#0B1828E0", radius=0, anchor="tl", group="side",
   remove=True, pad=1,
   notes="Frosted dark-navy glass sidebar spanning the full page height behind brand, header and category list. Horizontal "
         "gradient: ~#0B1828 at x=0 (near opaque) to ~#2A3548 at x~105 (more transparent, background blur visible). Hard right "
         "edge at x~109.8 is visible only for y~50-280; above it blends into the dark top of the art, below into the dark floor. "
         "No visible border. Inpainting this whole strip is optional if the game draws an equally dark panel here.")
items = [
    ("Barchasi", [12.3, 88.2, 102.1, 109.0], [22.1, 93.2, 32.9, 103.8], [43.0, 95.1, 70.0, 101.4], "grid",
     "all-categories (garbled cluster of 4 blobs; intended 'grid'/'apps')", "#FFFFFF", "#F2FAFF", False,
     "no descenders (baseline ~100.8)"),
    ("Kiyimlar", [12.5, 109.8, 102.5, 133.5], [22.2, 117.0, 33.0, 127.0], [43.0, 119.0, 68.8, 126.2], "shirt",
     "t-shirt outline", "#E4EAF2", "#E6EDF6", False, "box includes 'y' descender (baseline ~124.6)"),
    ("Elektronika", [12.5, 134.5, 102.5, 157.8], [22.2, 141.8, 32.7, 151.9], [43.0, 143.3, 79.3, 149.5],
     "headphones", "rounded rect with two circles at the bottom (AI-garbled; reads like headphones/device)", "#E4EAF2",
     "#E2EAF4", False, "no descenders"),
    ("Aksessuarlar", [12.5, 158.8, 102.5, 182.6], [22.4, 166.3, 32.9, 175.0], [42.8, 168.1, 85.0, 174.0],
     "handbag", "arch/'A' shape like a bag handle", "#E4EAF2", "#E8EFFA", False, "no descenders"),
    ("Uy jihozlari", [12.5, 183.5, 102.5, 207.7], [22.5, 191.0, 32.6, 200.9], [43.0, 192.9, 79.2, 200.1],
     "sofa", "garbled blob cluster; intended home-appliance/sofa icon", "#E4EAF2", "#EDF2FE", False,
     "box includes 'y'/'j' descenders (baseline ~198.6)"),
    ("Go'zallik", [12.5, 208.7, 102.5, 232.4], [23.0, 215.7, 32.3, 225.8], [43.0, 217.6, 70.1, 223.8],
     "sparkles", "garbled flower/lipstick glyph; intended beauty icon (lipstick/sparkles)", "#E4EAF2", "#EFF6FF",
     False, "no descenders"),
    ("Sport", [12.5, 233.4, 102.5, 257.2], [22.6, 240.9, 32.7, 250.4], [43.0, 242.2, 60.9, 249.4], "dumbbell",
     "garbled figure; intended sport icon (dumbbell/ball/runner)", "#E4EAF2", "#F2FAFF", False,
     "box includes 'p' descender (baseline ~247.8)"),
    ("Boshqa", [12.5, 258.2, 102.5, 282.4], [22.6, 265.2, 32.6, 276.0], [43.0, 267.8, 67.6, 274.8], "box",
     "rounded square with a small heart/dot inside (intended 'other'/box)", "#E4EAF2", "#ECF3FE", False,
     "box includes 'q' descender (baseline ~273.4)"),
]
for i, (label, rb, ib, lb, icon, icon_note, lc, icc, _, lnote) in enumerate(items):
    sel = i == 0
    if sel:
        el(f"side.item_{i}", "button", rb, fill="#2F66FCFF", radius=3.5, anchor="tl", group="side", state="selected",
           remove=True, pad=3,
           notes="Selected row: saturated blue, horizontal gradient #1A58FC (left) -> #416EFC (right), soft blue glow ~3px "
                 "(#2F6BFF80) around it, no border. Shorter (h 20.8) than the other rows (h ~23.8); row pitch 24.75.")
    else:
        el(f"side.item_{i}", "button", rb, fill="#FFFFFF0D", stroke="#FFFFFF1F", radius=3.5, anchor="tl",
           group="side", state="normal", remove=True, pad=1.5,
           notes="Dark translucent row (~5% white over the navy panel: #172233 at left, #353C47 at right) with a 1px faint "
                 "light rim (~12% white); rows touch with ~1px gap. Row pitch 24.75 px.")
    el(f"side.item_{i}_icon", "icon", ib, icon=icon, color="#FFFFFF" if sel else "#E6EDF6", anchor="tl", group="side",
       state="selected" if sel else "normal", remove=True, pad=2,
       notes="Outline icon, ~1.3px white stroke with a soft dark halo: " + icon_note + f". Sampled core {icc}.")
    el(f"side.item_{i}_label", "text", lb, text=label, fontPx=8.5, weight="medium",
       color="#FFFFFF" if sel else lc, anchor="tl", group="side", state="selected" if sel else "normal", remove=True,
       pad=2, notes=lnote + (". Sampled core #BDE8FF (white text on blue)." if sel else ". Near-white (sampled core ~#D0DAE6, blurred)."))

# ---------------- CONTENT: search + filter ----------------
el("shops.search", "input", [164.6, 42.7, 368.6, 66.5], fill="#FFFFFF38", stroke="#FFFFFF26", radius=4.5,
   anchor="tc", group="content", remove=True, pad=2,
   notes="Light frosted-glass search field (~22% white + background blur; reads #5A6783 over sky, #767180 over warm areas), "
         "faint lighter top rim and a thin dark shadow line just above/below. Stretches horizontally between the side panel "
         "and the filter button.")
el("shops.search_icon", "icon", [173.1, 50.3, 182.5, 59.8], icon="search", color="#C8D4ED", anchor="tc",
   group="content", remove=True, pad=2, notes="Magnifier outline, ~1.3px stroke.")
el("shops.search_placeholder", "text", [190.0, 52.3, 285.6, 59.0], text="Mahsulot yoki brend qidirish...",
   fontPx=7.4, weight="regular", color="#A9B7D0", anchor="tc", group="content", remove=True, pad=2,
   notes="Placeholder, box includes 'y'/'q' descenders (cap top 52.3, baseline ~57.5). Glyphs AI-blurred; ends with an ellipsis. "
         "= 'Search product or brand...'")
el("shops.filter", "button", [375.4, 43.1, 465.6, 65.7], fill="#3A3D4AD9", stroke="#FFFFFF1A", radius=4.5,
   anchor="tr", group="content", remove=True, pad=2,
   notes="Category filter dropdown: darker slate glass (#3B3D4A..#42434C) than the search field, faint light rim. "
         "Top/bottom nearly aligned with the search field (search 42.7-66.5).")
el("shops.filter_icon", "icon", [382.8, 50.3, 392.0, 59.9], icon="shirt", color="#EFF1F7", anchor="tr",
   group="content", remove=True, pad=2, notes="T-shirt/hanger-like outline icon, white.")
el("shops.filter_label", "text", [398.0, 52.1, 425.7, 58.0], text="Barchasi", fontPx=8.2, weight="medium",
   color="#DBDCE4", anchor="tr", group="content", remove=True, pad=2, notes="No descenders; baseline 58.0. = 'All'.")
el("shops.filter_caret", "icon", [441.9, 54.0, 444.5, 56.1], icon="caret-down", color="#BDBBC3", anchor="tr",
   group="content", remove=True, pad=2,
   notes="Tiny faint duplicate caret (AI artefact next to the real chevron); the game can drop it.")
el("shops.filter_chevron", "icon", [453.2, 53.2, 458.3, 56.3], icon="chevron-down", color="#DCDCEA", anchor="tr",
   group="content", remove=True, pad=2)

# ---------------- background sign (keep) ----------------
el("shops.mall_sign", "text", [234.3, 117.3, 316.8, 131.7], text="Lynxos Mall", fontPx=15.3, weight="bold",
   color="#F9FFFF", anchor="c", group="background", remove=False,
   notes="PART OF THE BACKGROUND ART - keep, do not inpaint. White bold sans on the mall's entrance fascia; box includes 'y' "
         "descender (cap top 117.3, baseline ~128.3).")

# ---------------- brand tiles ----------------
tiles = [
    [118.8, 187.6, 197.0, 226.9], [202.5, 187.6, 280.7, 226.9], [285.6, 187.6, 364.0, 226.9], [368.6, 187.6, 447.6, 226.9],
    [118.8, 231.5, 197.0, 270.3], [202.5, 231.5, 280.7, 270.3], [285.6, 231.5, 364.0, 270.3], [368.6, 231.5, 447.6, 270.3],
]
logos = [
    ([141.9, 200.8, 174.8, 214.2], "#050507", "black serif wordmark (real trademark ZARA)"),
    ([222.1, 198.6, 260.8, 218.1], "#000000", "black wordmark + swoosh (real trademark NIKE)"),
    ([308.9, 197.0, 341.0, 218.6], "#030303", "black three-stripe mountain + wordmark (real trademark adidas)"),
    ([383.4, 203.7, 433.8, 211.3], "#1428A0", "dark-blue bold wordmark (real trademark SAMSUNG; sampled #0E1E69)"),
    ([148.8, 239.2, 167.0, 262.1], "#000001", "black apple glyph (real trademark Apple)"),
    ([225.0, 241.0, 256.9, 262.0], "#D7141A", "red script wordmark (real trademark H&M; sampled #BC0F13)"),
    ([299.9, 246.5, 350.0, 255.9], "#1B1B1B", "black spaced serif wordmark (real trademark GUCCI)"),
    ([395.1, 238.0, 422.0, 264.9], "#0F5AAA", "blue circle with white letters (real trademark hp)"),
]
for i, (tb, (lb, lc, ln)) in enumerate(zip(tiles, logos)):
    el(f"shops.brand_{i}", "tile", tb, fill="#F9F9FBFF", radius=3.5, anchor="bc", group="cards", remove=True, pad=3,
       notes=f"White brand tile, grid row {i // 4}, col {i % 4} (4x2 grid, col gaps ~5, row gap 4.6). Very subtle top-to-bottom "
             "tint (#F3F4F7 -> #FBFBFD) and a thin dark contact shadow ~1px outside the edge. Measure geometry only: the game "
             "shows fictional brands here.")
    el(f"shops.brand_{i}_logo", "image", lb, color=lc, anchor="bc", group="cards", remove=True, pad=2,
       notes="Logo ink box, " + ln + " - DO NOT reproduce; replace with a fictional brand logo centred in the tile.")

# ---------------- view all link ----------------
el("shops.view_all_label", "text", [370.6, 285.8, 433.3, 292.0], text="Barchasini ko'rish", fontPx=8.6,
   weight="semibold", color="#B4CEFF", anchor="br", group="content", remove=True, pad=2,
   notes="Link text, light periwinkle blue (sampled core #BED6FF..#CAE0FF, blurred). No descenders; baseline 292.0. = 'View all'.")
el("shops.view_all_arrow", "icon", [440.2, 286.0, 446.9, 291.9], icon="arrow-right", color="#6FA0F0", anchor="br",
   group="content", remove=True, pad=2,
   notes="Thin right arrow, more saturated blue than the label (sampled blurred #4C78AF..#6E96D1). Right edge aligns with the "
         "tile grid's right edge (447.6).")

spec = {
    "page": "shops",
    "size": [483, 309],
    "elements": E,
    "removePolygons": [
        {"name": "page_frame_top", "points": [[0, 0], [483, 0], [483, 2.2], [0, 2.2]], "pad": 0},
        {"name": "page_frame_bottom", "points": [[0, 306.6], [483, 306.6], [483, 309], [0, 309]], "pad": 0},
        {"name": "page_frame_right", "points": [[479.6, 0], [483, 0], [483, 309], [479.6, 309]], "pad": 0},
        {"name": "page_frame_left", "points": [[0, 0], [1.0, 0], [1.0, 309], [0, 309]], "pad": 0},
    ],
    "assets": [
        {"name": "shops_background", "box": [0, 0, 483, 309],
         "notes": "Full-bleed mall-interior background (two-storey atrium, glass shopfronts, plants, glossy floor, dusk sky at the "
                  "top). Remove every element with remove=true (nav, header, side panel + list, search, filter, 8 brand tiles "
                  "and their logos, view-all link). KEEP the 'Lynxos Mall' fascia sign (shops.mall_sign). The top ~40 px are "
                  "naturally dark (dusk sky/ceiling + soft top darkening) - there is no hard nav bar to remove. The 1-2 px "
                  "composite page frame (bevel lines at y<=1, y>=307, x>=480) is listed in removePolygons; cropping to "
                  "[2,2,480,307] and rescaling is an alternative. Tiles cover the plaza floor: expect inpainting of floor "
                  "reflections there. A small background figure (shopper) at ~x440-456, y145-192 is part of the art."},
        {"name": "mall_sign_region", "box": [226, 110, 325, 136],
         "notes": "'Lynxos Mall' sign text on the entrance fascia - background art, must survive inpainting untouched "
                  "(no UI overlaps it)."},
    ],
}

os.makedirs(os.path.dirname(OUT), exist_ok=True)
with open(OUT, "w", encoding="utf-8") as f:
    json.dump(spec, f, ensure_ascii=False, indent=1)
print("wrote", OUT, len(E), "elements")
