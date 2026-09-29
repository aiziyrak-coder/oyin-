import json, os
OUT = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\spec\business.json"
E = []
def add(id, kind, box, **kw):
    e = {"id": id, "kind": kind, "box": [round(v, 1) for v in box]}
    e.update(kw)
    E.append(e)

# ---------------- NAV ----------------
add("nav.bar", "panel", [0, 0, 518, 33.5], group="nav", anchor="tc", remove=False,
    fill="#0A162866",
    notes="Full-width translucent dark band behind the nav (no border, no radius). Its lower edge is a visible step at y=33.5 "
          "(L drops ~30-40% above it at x 120-360, only ~10% at x>440). Composite colour inside the band: x0 #051222, x90 #101D2C, "
          "x190 #19273B, x290 #273850, x350 #314158, x440 #3F485E, x505 #2E425C. It is a darkening baked into the art, so it is NOT "
          "marked for inpainting: either keep it in the background and draw the Unity nav bar with ~transparent fill exactly 0..33.5, "
          "or draw a stronger #0A1628 ~40% bar that hides the step. No nav tab is highlighted on this page (no nav.tab_selected / underline).")
add("nav.brand_icon", "icon", [20, 11, 35, 27], group="nav", anchor="tl", remove=True, pad=2.5,
    icon="brand-c", fill="#3F76F5FF", color="#FFFFFF", radius=3.5,
    notes="Blue rounded-square app tile (#3F76F5, slightly lighter top) with a white concentric 'C'/spiral glyph (sampled #DAFBFF). "
          "Same brand tile as the other pages.")
add("nav.brand_text", "text", [40.3, 15.2, 78, 23.5], group="nav", anchor="tl", remove=True, pad=2.5,
    text="Lynxos", fontPx=9.6, weight="bold", color="#FFFFFF",
    notes="Box bottom includes the 'y' descender; baseline y=22.2, cap height ~6.9. Sampled core #FBFFFF.")
tabs = [("nav.tab_home", [98, 16, 137, 22.5], "Bosh sahifa", "baseline 22.4 (no descenders); h/f ascenders set the top"),
        ("nav.tab_world", [158.6, 16, 181, 24], "Dunyo", "bottom includes 'y' descender; baseline ~22.4"),
        ("nav.tab_friends", [203, 16, 230, 22.8], "Do'stlar", "baseline ~22.5; apostrophe is a straight/curly ' ; no descenders"),
        ("nav.tab_top", [251, 16, 277.4, 24], "Top-lar", "bottom includes 'p' descender; baseline ~22.4"),
        ("nav.tab_settings", [299, 16, 337, 22.8], "Sozlamalar", "baseline ~22.5; no descenders")]
for id_, b, t, n in tabs:
    add(id_, "text", b, group="nav", anchor="tc", remove=True, pad=2.5, text=t, fontPx=8.9, weight="medium",
        color="#D0DAEC", state="normal",
        notes=n + ". Unselected tab colour, sampled #CAD3E0..#D1DBF2 (anti-aliased thin text; true colour ~#D6DFEF, ~85% white). "
                  "Tab centres x: 117.5, 169.8, 216.5, 264.2, 318.0 (pitch ~50).")
add("nav.lang_globe", "icon", [369, 13.6, 381, 26], group="nav", anchor="tr", remove=True, pad=2.5, icon="globe",
    color="#DEEAF7", notes="Outline globe, ~1.3px stroke, 12x12.4.")
add("nav.lang_text", "text", [385, 16.8, 394.2, 23], group="nav", anchor="tr", remove=True, pad=2.5, text="UZ",
    fontPx=8.6, weight="semibold", color="#D2DDF0", notes="Caps only; top = cap top, bottom = baseline.")
add("nav.lang_chevron", "icon", [397.7, 18.7, 402, 21.2], group="nav", anchor="tr", remove=True, pad=2, icon="chevron-down",
    color="#B8C6DD", notes="Tiny chevron, ~1px stroke.")
add("nav.bell", "icon", [414.6, 14, 425, 26], group="nav", anchor="tr", remove=True, pad=2.5, icon="bell",
    color="#E5EEFF", notes="Outline bell, ~1.3px stroke.")
add("nav.bell_dot", "dot", [422, 13.8, 426.2, 17.2], group="nav", anchor="tr", remove=True, pad=1.5,
    color="#EF4460", notes="Red notification dot on the bell's top-right shoulder (sampled #B33D51 at 1x because it is only ~3.5px; "
                           "in the 3x view it reads as a saturated red/pink).")
add("nav.profile_pill", "pill", [434.3, 9.6, 503, 29.6], group="nav", anchor="tr", remove=True, pad=2,
    fill="#FFFFFF08", stroke="#FFFFFF1F", radius=10,
    notes="Almost transparent glass pill (inside #30435C vs outside #31415B) with a thin ~1px light stroke (top stroke row y=10, "
          "bottom y=29, right arc x~502). Contains avatar, name and chevron.")
add("nav.profile_avatar", "avatar", [434.5, 9.8, 452.5, 28.6], group="nav", anchor="tr", remove=True, pad=1.5, radius=9,
    stroke="#5A6478FF",
    notes="Circular photo avatar (young man, dark hair, warm skin #C58F64) with a light grey ring (~1.5px, most visible on the left, "
          "#5A6478). Inner dark photo disk ~[438.8,11.8,451.6,28.2]. The avatar ring shares the pill's left edge.")
add("nav.profile_name", "text", [456, 17.5, 488, 23.2], group="nav", anchor="tr", remove=True, pad=2,
    text="Lynxos_uaer", intended="Lynxos_user", fontPx=6.7, weight="medium", color="#C4D0E2",
    notes="Very small, garbled AI text. Bottom includes 'y' descender; baseline ~22.3.")
add("nav.profile_chevron", "icon", [493, 18.6, 498, 21.4], group="nav", anchor="tr", remove=True, pad=2, icon="chevron-down",
    color="#B8C6DD", notes="Small chevron, ~1.2px stroke. Right padding to pill edge ~5px.")

# ---------------- HEADER ----------------
add("header.title", "text", [21, 44, 132, 56.5], group="header", anchor="tl", remove=True, pad=3,
    text="Biznes markazi", fontPx=17, weight="bold", color="#FFFFFF",
    notes="No descenders; top = cap/ascender top (B,k,i-dots) y=44, bottom = baseline 56.5; x-height top ~48. Soft dark text shadow.")
add("header.subtitle", "text", [20.7, 64.8, 168.2, 75.5], group="header", anchor="tl", remove=True, pad=2.5,
    text="Yangi imkoniyatlar, yangi hamkorlilar", intended="Yangi imkoniyatlar, yangi hamkorliklar",
    fontPx=10, weight="regular", color="#BCC0CA",
    notes="Bottom includes descenders (g, y, comma); baseline ~72.2, cap height ~7.2. Muted light grey (~75% white). Last word is "
          "garbled in the art ('hamkorlilar').")

# ---------------- SIDE LIST ----------------
items = [
    ([19.8, 88.4, 126.4, 109.5], [30.6, 93.7, 41, 104], "home", "building with arched doorway (home/asosiy)",
     [52.3, 95, 75.8, 103.3], "Asosiy", "bottom includes 'y' descender; baseline ~101.5"),
    ([19.8, 110.8, 126, 132.8], [30.6, 116.6, 41, 128], "building", "office building / door",
     [52.8, 118, 76, 125], "Ofislar", "baseline 125, no descenders"),
    ([19.8, 134.8, 126, 156.8], [30, 140.6, 41, 151.5], "users", "group of three people (coworking)",
     [53, 142.7, 88, 151], "Kovorking", "bottom includes 'g' descender; baseline ~149.3"),
    ([19.8, 158.8, 126, 181.8], [30.6, 164, 41, 176], "rocket", "house/badge with a lightbulb-rocket inside (startups)",
     [53, 167, 87.3, 175], "Startaplar", "bottom includes 'p' descender; baseline ~173"),
    ([19.8, 182.8, 126, 205.8], [30, 189.5, 41, 200], "investor", "house/badge with coins+chart (investors)",
     [53, 191.3, 90, 198], "Investorlar", "baseline 198, no descenders"),
    ([19.8, 207.8, 126, 230.8], [30.6, 213.8, 41, 225], "calendar", "calendar",
     [52.6, 216, 84, 222.3], "Tadbirlar", "baseline 222.3, no descenders"),
    ([19.8, 232.8, 126, 256.8], [30, 239, 42, 251], "badge", "round badge/cog-circle with a small mark (services)",
     [52.8, 241, 86.2, 248], "Xizmatlar", "baseline 248, no descenders"),
]
for i, (rb, ib, icon, idesc, lb, lt, ln) in enumerate(items):
    sel = i == 0
    if sel:
        add(f"side.item_{i}", "button", rb, group="side", anchor="cl", state="selected", remove=True, pad=4,
            fill="#2264FCFF", radius=4.5,
            notes="Selected row: solid vivid blue (#2264FC, flat) with a soft blue outer glow ~3px (#2264FC ~35%). "
                  "A barely visible 2px mark at ~(117,98) on the right side of the row (maybe a faded chevron) - ignore or add a "
                  "subtle right chevron. Row pitch ~24.1 px (tops 88.4,110.8,134.8,158.8,182.8,207.8,232.8); gap ~1.5-2px.")
    else:
        add(f"side.item_{i}", "button", rb, group="side", anchor="cl", state="normal", remove=True, pad=2.5,
            fill="#1C2738D9", stroke="#FFFFFF14", radius=4.5,
            notes="Dark translucent glass row over the left scrim (composite mid colour ~#232D3B, left end ~#142232 because the scrim "
                  "behind is darker). 1px faint light border; rows are separated by a ~2px gap that shows as a lighter hairline "
                  "(~#2C3746) across the width. Heights grow slightly down the list (AI art): use a uniform 22px row + 2px gap.")
    add(f"side.item_{i}_icon", "icon", ib, group="side", anchor="cl", remove=True, pad=2, icon=icon,
        color="#FFFFFF" if sel else "#E2E9F5", state="selected" if sel else "normal",
        notes=f"Outline line icon ~1.3px stroke, ~11x11: {idesc}. AI-garbled glyph - use a clean equivalent.")
    add(f"side.item_{i}_label", "text", lb, group="side", anchor="cl", remove=True, pad=2, text=lt,
        fontPx=9.0, weight="semibold" if sel else "medium", color="#FFFFFF" if sel else "#DCE3EE",
        state="selected" if sel else "normal",
        notes=ln + ". Label x starts at 53 (icon x 30.5-41). " + ("Sampled #C3E8FF because thin white text blends with the blue." if sel else "Sampled ~#D0D9E8..#DFE6F2."))

# ---------------- ACTION TILES ----------------
tiles = [
    ([147.8, 210.4, 226.8, 262.9], [160, 221, 175, 237], "office-key", "building/bag with a keyhole-person (office rent)",
     [158, 245, 193, 253], "Ofis ijarasi", None, "bottom includes 'j' descender; baseline ~251"),
    ([233, 210.4, 312.8, 262.9], [243, 221, 260.5, 237], "users", "one person with two half-people behind (find partner)",
     [243, 245, 291, 253], "Hamkor topish", None, "bottom includes 'p' descender; baseline ~251"),
    ([318.3, 210.4, 403.8, 262.9], [329, 221, 345.2, 237], "startup", "hand/box holding a coin/rocket (place a startup)",
     [328.3, 245, 394, 253], "Startap joylashtirish", None, "bottom includes 'p','j','y' descenders; baseline ~251"),
    ([410, 210.4, 503.8, 262.9], [421, 221, 436, 236.4], "handshake", "person bust with raised hands / presenter (meet investors)",
     [419, 245, 497, 251], "Investorlar bilan uchrashuv", None, "no descenders; baseline 251; letters slightly condensed"),
]
for i, (tb, ib, icon, idesc, lb, lt, intended, ln) in enumerate(tiles):
    add(f"business.tile_{i}", "tile", tb, group="actions", anchor="bc", remove=True, pad=3,
        fill="#26324AC7", stroke="#FFFFFF26", radius=5,
        notes="Glass action tile: dark navy translucent fill (composite interior #2E3A4F / #273243, lighter where the bright scene "
              "is behind, e.g. tile_2 #404350 -> backdrop blur ~4px), 1px light stroke (~#62697B at the top, fainter elsewhere), "
              "faint 1px shadow below. Icon top-left, label bottom-left; left padding ~10px. Tiles are separated by ~6px gaps; "
              "row spans x 147.8-503.8, bottom margin ~22px. Widths differ in the art (79, 79.8, 85.5, 93.8) - equal widths are fine.")
    add(f"business.tile_{i}_icon", "icon", ib, group="actions", anchor="bc", remove=True, pad=2, icon=icon, color="#F2F8FF",
        notes=f"White outline icon ~1.5px stroke, ~16x16: {idesc}. AI-garbled - use a clean equivalent.")
    e = dict(group="actions", anchor="bc", remove=True, pad=2, text=lt, fontPx=8.3 if i < 3 else 7.8, weight="medium",
             color="#E2E8F5", notes=ln + ". Sampled #D2DAE8..#E1E3F2 (thin text), true ~#E6ECF7.")
    if i == 3:
        e["intended"] = "Investorlar bilan uchrashuv"
    add(f"business.tile_{i}_label", "text", lb, **e)

# ---------------- BACKGROUND ELEMENTS KEPT ----------------
add("business.wall_sign", "image", [340, 86, 444, 136], group="background", anchor="c", remove=False,
    notes="Illuminated wall sign that belongs to the background art (keep it). Blue 3D 'S' ribbon logo + white 'Lynxos' / 'Business' "
          "wordmark + tagline 'IDEAS - PEOPLE - GROWTH' with a soft cool glow on the dark wall panel. Nothing overlaps it.")
add("business.wall_sign_logo", "image", [343, 89, 365.3, 119], group="background", anchor="c", remove=False,
    color="#315EBF", notes="Blue glossy 'S'-ribbon logo (blue #315EBF edges, white #EFFFFF highlights, outer blue glow).")
add("business.wall_sign_title", "text", [373, 89.3, 428, 103.5], group="background", anchor="c", remove=False,
    text="Lynxos", fontPx=16.7, weight="medium", color="#FFFFFF",
    notes="Part of background art. Bottom includes 'y' descender; baseline ~101.3; lit white letters with glow.")
add("business.wall_sign_subtitle", "text", [373, 106.8, 429, 117.2], group="background", anchor="c", remove=False,
    text="Business", fontPx=14.4, weight="medium", color="#FFFFFF", notes="Part of background art; baseline 117.2.")
add("business.wall_sign_tagline", "text", [342, 127, 441, 133.5], group="background", anchor="c", remove=False,
    text="IDEAS · PEOPLE · GROWTH", fontPx=9, weight="semibold", color="#F3F3F9",
    notes="Part of background art; wide letter-spacing (~0.15em); middle dots drawn as short dashes.")
add("business.left_scrim", "panel", [0, 0, 175, 285], group="background", anchor="cl", remove=False,
    fill="#071422FF",
    notes="Dark navy readability gradient on the left, baked into the art (NOT inpainted): solid #071422 from x=0 to ~70, "
          "fading to transparent by x~170 (x100 ~#0D1B2B/#1B2632, x140 ~#121F31 at the bottom). Keep it in the background "
          "or recreate as a UI gradient; do not double it.")

spec = {
    "page": "business",
    "size": [518, 285],
    "elements": E,
    "removePolygons": [
        {"name": "frame_top_artifact", "points": [[0, 0], [518, 0], [518, 2.5], [0, 2.5]], "pad": 0},
        {"name": "frame_right_artifact", "points": [[515.5, 0], [518, 0], [518, 285], [515.5, 285]], "pad": 0},
    ],
    "assets": [
        {"name": "business_background", "box": [0, 0, 518, 285],
         "notes": "Office lounge (city-view windows left, sofas/table centre, lit wall sign right, plants right edge). Remove every "
                  "remove=true element (nav texts/icons/pill, title, subtitle, 7 side rows, 4 action tiles). Keep the wall sign, the "
                  "left dark scrim and the nav darkening band. Rows y=0-2 and columns x=515.5-518 contain the composite's frame edge "
                  "(light 1px line + rounded top-left corner) - inpaint (removePolygons) or crop."},
        {"name": "wall_sign", "box": [340, 86, 444, 136], "notes": "Keep in background; crop if a separate glowing sprite is wanted. No overlay on it."},
        {"name": "brand_icon", "box": [19, 10, 36, 28], "notes": "Nav brand tile; better redrawn as a vector/sprite (shared with other pages)."},
        {"name": "profile_avatar", "box": [434, 9, 453, 29], "notes": "Placeholder user photo; very low-res, use a real avatar sprite."},
    ],
}
os.makedirs(os.path.dirname(OUT), exist_ok=True)
json.dump(spec, open(OUT, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print(OUT, len(E), "elements")
