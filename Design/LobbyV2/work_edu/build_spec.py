import json, os

OUT = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\spec\education.json"
E = []


def el(id, kind, box, **kw):
    d = {"id": id, "kind": kind, "box": [round(v, 1) for v in box]}
    d.update(kw)
    E.append(d)


# ---------------- NAV ----------------
el("nav.brand_icon", "icon", [12.7, 11.5, 28.3, 27.3], icon="lynxos-logo", fill="#3A74E3FF", radius=4, color="#D7F6FF",
   anchor="tl", group="nav", remove=True, pad=2,
   notes="Rounded-square app logo, blue vertical gradient #457DE6 (top-left) -> #306BD7 (bottom-right), white concentric 'C'/swirl glyph (Lynxos mark) centered. No visible nav.bar: nav sits directly on the blurred page background.")
el("nav.brand_text", "text", [33.0, 14.9, 70.3, 24.3], text="Lynxos", fontPx=10.3, weight="bold", color="#F8FDFF",
   anchor="tl", group="nav", remove=True, pad=1.5,
   notes="Box = cap top to descender of 'y'; baseline y=22.3, cap height 7.4.")
tabs = [("nav.tab_home", [89.6, 16.0, 126.1, 22.4], "Bosh sahifa", "baseline"),
        ("nav.tab_world", [145.9, 15.9, 167.5, 24.1], "Dunyo", "descender 'y', baseline 22.4"),
        ("nav.tab_friends", [187.9, 15.9, 213.1, 22.4], "Do'stlar", "baseline"),
        ("nav.tab_top", [232.9, 16.0, 258.1, 24.1], "Top-lar", "descender 'p', baseline 22.4"),
        ("nav.tab_settings", [277.8, 16.0, 313.1, 22.5], "Sozlamalar", "baseline")]
cols = ["#BCC6D8", "#C9D1DF", "#C7D4E9", "#C6D3E6", "#C8D7F2"]
for (i, b, t, n), c in zip(tabs, cols):
    el(i, "tab", b, text=t, fontPx=8.9, weight="medium", color=c, anchor="tl", group="nav", state="normal", remove=True, pad=1.5,
       notes=f"Tab label ink box (bottom = {n}); cap height 6.4. No tab is selected on this page (education is not a nav tab): no pill/underline drawn. Same light grey-blue for all tabs (~#C6D2E6).")
el("nav.lang_globe", "icon", [347.9, 13.8, 360.0, 26.1], icon="globe", color="#DBE1EE", anchor="tr", group="nav", remove=True, pad=1.5,
   notes="Outline globe, ~1.3px stroke.")
el("nav.lang_text", "text", [365.0, 17.0, 374.5, 23.1], text="UZ", fontPx=8.3, weight="semibold", color="#CED5E2", anchor="tr", group="nav", remove=True, pad=1.5,
   notes="Cap top to baseline.")
el("nav.lang_chevron", "icon", [378.5, 18.8, 383.1, 21.4], icon="chevron-down", color="#D1D8E8", anchor="tr", group="nav", remove=True, pad=1.5)
el("nav.bell", "icon", [398.7, 13.7, 409.4, 26.0], icon="bell", color="#D6DCEC", anchor="tr", group="nav", remove=True, pad=1.5,
   notes="Outline bell, ~1.3px stroke; the red dot overlaps its top-right.")
el("nav.bell_dot", "dot", [406.2, 13.7, 410.3, 17.8], color="#CC494F", anchor="tr", group="nav", remove=True, pad=1,
   notes="Notification dot, diameter ~4. Sampled core #CC494F (anti-aliased); intended probably a brighter red ~#E5484D.")
el("nav.profile_pill", "pill", [419.5, 10.0, 492.8, 30.0], fill="#32405BFF", stroke="#FFFFFF1A", radius=10, anchor="tr", group="nav", remove=True, pad=2,
   notes="Glassy slate pill (composite #32405B over bg #1C2B3F; ~ #3A4A68 @ 85%). Subtle lighter 1px edge.")
el("nav.profile_avatar", "avatar", [420.8, 10.8, 439.6, 29.2], anchor="tr", group="nav", remove=True, pad=1,
   notes="Circular avatar (~18.5px) at the pill's left end: portrait of a young man (dark hair, white shirt) on dark bg, with a light grey ring (#6A7080) on the left side.")
el("nav.profile_name", "text", [443.6, 17.9, 476.2, 23.4], text="Lynxos_user", fontPx=7.0, weight="medium", color="#C7D2E6", anchor="tr", group="nav", remove=True, pad=1.5,
   notes="Drawn garbled ('Lyrxos_user'); box bottom = underscore (baseline ~22.5). Small, cap ~4.7.")
el("nav.profile_chevron", "icon", [481.0, 18.8, 486.2, 22.0], icon="chevron-down", color="#B1BDD7", anchor="tr", group="nav", remove=True, pad=1.5)

# ---------------- HEADER ----------------
el("header.title", "text", [14.0, 45.6, 140.5, 62.3], text="O'quv markazlari", fontPx=17.5, weight="bold", color="#F9FEFF", anchor="tl", group="header",
   remove=True, pad=2,
   notes="Box bottom = descender of 'q'; baseline y=58.4, cap height ~12.6. The trailing 'ri' (x 133-140.5) overlaps the hero image's top-left corner -> must be removed from the hero art too. Uses the Uzbek o-tutuq (O‘) glyph.")
el("header.subtitle", "text", [14.0, 66.6, 96.4, 75.4], text="Bilim — kelajak kalti", intended="Bilim — kelajak kaliti", fontPx=10.1, weight="regular",
   color="#97A5BC", anchor="tl", group="header", remove=True, pad=1.5,
   notes="Box bottom = descender of 'j'; baseline ~74.0, cap ~7.3. Muted blue-grey.")

# ---------------- SIDE LIST ----------------
rows = [[12.7, 88.5, 116.4, 108.7], [12.7, 109.5, 116.4, 133.2], [12.7, 134.1, 116.4, 157.8], [12.7, 158.7, 116.4, 182.5],
        [12.7, 183.5, 116.4, 207.4], [12.7, 208.5, 116.4, 232.1], [12.7, 233.3, 116.4, 257.3], [12.7, 258.4, 116.4, 282.6]]
fills = ["#2264FCFF", "#1F3046E6", "#1E2F42E6", "#1C2D40E6", "#1D2C3FE6", "#1A293DE6", "#182839E6", "#182839E6"]
labels = [("Barchasi", None, [44.0, 95.0, 72.0, 101.3], "baseline"),
          ("Tibbiyot", None, [44.0, 118.7, 71.2, 126.2], "descender 'y'; baseline ~125.0"),
          ("Teznologiya", "Texnologiya", [44.0, 142.9, 84.3, 151.0], "descenders 'g','y'; baseline ~149.3"),
          ("Til kurslari", None, [43.9, 167.7, 78.4, 174.0], "baseline"),
          ("IT & Dasturlash", None, [43.9, 191.9, 95.1, 199.0], "baseline"),
          ("Bimes", "Biznes", [44.0, 217.0, 66.0, 223.4], "baseline"),
          ("San'at", None, [44.5, 242.0, 65.4, 249.1], "baseline"),
          ("Boshqa", None, [44.0, 267.0, 69.3, 275.1], "descender 'q'; baseline ~273.4")]
lcol = ["#FFFFFF", "#C4CDDA", "#C9D2E0", "#BBC5D2", "#CCD4E3", "#D3DCE8", "#D5DEEC", "#D2D7E3"]
icons = [([22.9, 92.9, 34.1, 103.5], "grid", "#FFFFFF", "drawn as a rounded box/house with a grid inside ('all categories')"),
         ([23.0, 116.0, 34.1, 127.0], "medical", "#EAF2FC", "drawn as a T-shirt/medical gown outline"),
         ([22.9, 141.0, 34.1, 151.5], "atom", "#E1E9F4", "drawn as a looped knot (like the ⌘ symbol) = technology/atom"),
         ([23.0, 166.0, 34.0, 176.0], "chat", "#E2E8F4", "two overlapping speech bubbles (language)"),
         ([23.0, 190.9, 34.1, 201.0], "laptop", "#DCE7F2", "rounded window/laptop with code mark"),
         ([23.0, 214.9, 34.0, 226.0], "briefcase", "#E9EFF6", "rounded briefcase/bag with handle"),
         ([23.0, 240.0, 33.5, 251.0], "brush", "#F5FBFF", "rounded square with a diagonal brush/arrow stroke"),
         ([23.0, 264.9, 33.5, 276.0], "more", "#E6F0FA", "rounded square/tag with a chevron-down inside")]
for n in range(8):
    sel = n == 0
    el(f"side.item_{n}", "button", rows[n], fill=fills[n], stroke=("#3B7BFF80" if sel else "#2A3B52FF"), radius=4, anchor="tl", group="side",
       state="selected" if sel else "normal", remove=True, pad=2,
       notes=("Selected row: solid bright blue #2264FC with soft blue glow below (~2px, #1E4FD0 55%). Shorter than other rows (20.2 vs ~23.8)."
              if sel else "Dark glass row (composite colour shown; ~#2A3E58 @ 55% over bg #0A1727), 1px lighter border. Rows are separate rounded cards, ~0.8-1px gap, pitch ~24.9. No enclosing side.panel."))
    ib, iname, icol, inote = icons[n]
    el(f"side.item_{n}_icon", "icon", ib, icon=iname, color=icol, anchor="tl", group="side", remove=True, pad=1.5,
       notes="Outline icon, ~1.3px rounded stroke; " + inote + ("; on blue row (sampled #D9F5FF due to AA)" if sel else ""))
    t, intended, lb, bn = labels[n]
    d = dict(text=t, fontPx=8.8, weight="medium", color=lcol[n], anchor="tl", group="side", remove=True, pad=1.5,
             notes=f"Label ink box, bottom = {bn}; cap ~6.3." + (" White on blue (sampled #B5DEFF because of AA)." if sel else ""))
    if intended:
        d["intended"] = intended
    el(f"side.item_{n}_label", "text", lb, **d)
el("side.item_0_chevron", "icon", [107.8, 96.5, 110.2, 99.2], icon="chevron-right", color="#3A7BFF", anchor="tl", group="side", state="selected", remove=True, pad=1.5,
   notes="Very faint tiny chevron at the right end of the selected row (lighter blue on blue). Optional.")

# ---------------- HERO ----------------
el("education.hero_image", "image", [133.5, 42.9, 492.6, 179.2], radius=5, stroke="#3F557499", anchor="tr", group="hero",
   notes="ART ASSET (not removed from page). Futuristic education campus atrium: curved glass balconies, palm trees, warm interior lights, blue sky oval at top. Thin light-blue 1px border. Stretches between side list and right margin (right margin ~16.4 like nav).")
el("education.hero_card", "panel", [336.6, 50.8, 492.5, 167.5], fill="#1C3049EB", stroke="#FFFFFF1F", radius=6, anchor="tr", group="hero", remove=True, pad=3,
   notes="Dark navy glass card overlaying the right part of the hero (right edge flush with hero right edge, 8px inset top/bottom). Composite fill #1D314A (top) .. #27303F (bottom, image showing through). Soft shadow; 1px lighter top border.")
el("education.hero_card_title", "text", [351.7, 72.9, 452.4, 85.2], text="Lynxos Education", fontPx=13.2, weight="semibold", color="#F5FEFF", anchor="tr",
   group="hero", remove=True, pad=1.5, notes="Box bottom = descender 'y'; baseline ~82.4, cap 9.5.")
el("education.hero_card_subtitle", "text", [351.5, 91.8, 444.2, 101.1], text="Zamonaviy bilm markazi", intended="Zamonaviy bilim markazi", fontPx=9.4,
   weight="regular", color="#A4B4CE", anchor="tr", group="hero", remove=True, pad=1.5, notes="Box bottom = descender 'y'; baseline ~98.6, cap 6.8.")
el("education.hero_card_button", "button", [350.3, 119.8, 478.6, 149.8], fill="#2159FCFF", radius=6, anchor="tr", group="hero", remove=True, pad=3,
   notes="Primary CTA, saturated royal blue #2159FC (nearly flat, very slight lighter top-left #215DFD), outer blue glow ~2px (#2159FC 45%).")
el("education.hero_card_button_label", "text", [370.7, 131.0, 434.0, 140.1], text="Tashrif buyurish", fontPx=9.6, weight="semibold", color="#EAF6FF",
   anchor="tr", group="hero", remove=True, pad=1.5, notes="White text (sampled #CEEFFF due to AA on blue). Box bottom = descender 'y'; baseline ~137.9, cap 6.9. Left-centred: label starts 20px from button left.")
el("education.hero_card_button_arrow", "icon", [456.6, 130.9, 465.4, 138.4], icon="arrow-right", color="#EAF6FF", anchor="tr", group="hero", remove=True, pad=1.5,
   notes="Thin arrow '->' right-aligned inside button (13px from right edge).")

# ---------------- RECOMMENDED ----------------
el("education.rec_title", "text", [133.8, 193.7, 204.2, 204.0], text="Tavsiya etilgan", fontPx=10.7, weight="semibold", color="#EFF8FF", anchor="tl",
   group="cards", remove=True, pad=1.5, notes="Section heading; box bottom = descenders 'y','g'; baseline ~201.4, cap 7.7. Left-aligned with the hero.")
el("education.rec_all", "text", [449.6, 194.8, 478.0, 201.1], text="Barchasi", fontPx=8.8, weight="medium", color="#8FB2E6", anchor="tr", group="cards",
   remove=True, pad=1.5, notes="'See all' link, light blue (sampled #97B7E7 brightest; reads as ~#6F9BEA blue link). Cap top to baseline.")
el("education.rec_all_arrow", "icon", [484.8, 194.9, 491.0, 201.1], icon="arrow-right", color="#7C9DCE", anchor="tr", group="cards", remove=True, pad=1.5)
cards = [[132.7, 213.5, 218.8, 292.0], [223.8, 213.5, 309.8, 292.0], [314.8, 213.5, 400.8, 292.0], [406.4, 213.5, 491.8, 292.0]]
titles = [("IT Academy", [138.9, 276.9, 176.4, 284.4], "descender 'y'", "#D5DFEB"),
          ("Medical Center", [230.7, 276.9, 278.5, 283.2], "baseline", "#D6DFEB"),
          ("Language Hob", [321.5, 276.9, 368.1, 285.0], "descenders 'g'", "#D1DAE6"),
          ("Business School", [412.8, 276.9, 464.0, 283.1], "baseline", "#D3DAEA")]
cap_fill = ["#172638F2", "#132333F2", "#102033F2", "#1A2839F2"]
img_notes = ["IT Academy interior: dark modern hall, long white desks, monitors, blue screens",
             "Medical Center: symmetric white modern buildings flanking a walkway, blue sky, green lawns",
             "Language Hub: bright white lobby with columns, plants, lounge seating",
             "Business School: modern towers along a canal with trees, blue sky"]
for n, b in enumerate(cards):
    el(f"education.rec_{n}", "tile", b, stroke="#394A5BFF", radius=5, anchor="tc", group="cards",
       notes="Recommended centre card: picture on top (rounded top corners) + dark caption bar; 1px light border #394A5B. Width ~86, gap ~5. Not removed as a whole (picture is art) - its caption bar is removed via education.rec_%d_caption." % n)
    el(f"education.rec_{n}_image", "image", [b[0] + 0.4, 213.8, b[2] - 0.4, 269.3], radius=5, anchor="tc", group="cards",
       notes="ART ASSET, picture part only (top corners rounded r~5, bottom edge straight). " + img_notes[n])
    el(f"education.rec_{n}_caption", "panel", [b[0], 269.3, b[2], 292.0], fill=cap_fill[n], radius=5, anchor="tc", group="cards", remove=True, pad=1,
       notes="Caption bar of the card (bottom corners rounded, top straight), dark navy glass.")
    t, tb, bn, tc = titles[n]
    extra = {"intended": "Language Hub"} if t == "Language Hob" else {}
    el(f"education.rec_{n}_title", "text", tb, text=t, **extra, fontPx=8.5, weight="semibold", color=tc, anchor="tc", group="cards", remove=True, pad=1.5,
       notes=f"Caption, left inset ~6px; box bottom = {bn}; baseline ~283.0, cap 6.1.")

spec = {
    "page": "education",
    "size": [509, 309],
    "background": {"notes": "Dark blurred navy page background (blurred city/interior bokeh), horizontal gradient: left #0A1727 -> centre #18293B -> right #202F42; lower right slightly bluer #1B3048. Top-left corner darkest #061422. Thin light frame line at y=1 and y=307 is the page-crop border, not UI."},
    "elements": E,
    "removePolygons": [],
    "assets": [
        {"name": "education.hero_image", "box": [133.5, 42.9, 492.6, 179.2],
         "notes": "Hero picture (futuristic campus atrium). Remove the 'Lynxos Education' overlay card [336.6,50.8,492.5,167.5] incl. its title/subtitle/'Tashrif buyurish' button, and the header title's trailing 'ri' letters overlapping the top-left corner (x 133-140.5, y 45.6-59). The card covers ~43% of the width (right side) -> needs large inpaint of the right third (sky, glass balconies, palm, lobby floor continue there). Keep rounded corners r~5."},
    ] + [{"name": f"education.rec_{n}_image", "box": [cards[n][0] + 0.4, 213.8, cards[n][2] - 0.4, 269.3],
          "notes": img_notes[n] + ". Picture part only (caption bar excluded); no overlay on it; top corners rounded r~5 + 1px border to trim."} for n in range(4)],
}
os.makedirs(os.path.dirname(OUT), exist_ok=True)
with open(OUT, "w", encoding="utf-8") as f:
    json.dump(spec, f, ensure_ascii=False, indent=1)
print("elements:", len(E))
