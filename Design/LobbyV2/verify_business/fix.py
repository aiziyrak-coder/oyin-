"""Build business.verified.json from the original spec + verified corrections."""
import json, copy, sys
base = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2"
NAVBAR_REMOVE = (sys.argv[1] == "1") if len(sys.argv) > 1 else False
out_path = sys.argv[2] if len(sys.argv) > 2 else base + r"\spec\business.verified.json"
spec = json.load(open(base + r"\spec\business.json", encoding="utf-8"))
E = {e["id"]: e for e in spec["elements"]}
log = []

def setv(i, **kw):
    e = E[i]
    for k, v in kw.items():
        if k == "notes_add":
            e["notes"] = (e.get("notes", "") + " " + v).strip()
        elif v is None:
            e.pop(k, None)
        else:
            e[k] = v

# ---------------- nav ----------------
setv("nav.bar",
     remove=NAVBAR_REMOVE, pad=0,
     box=[0, 0, 518, 33.6],
     notes="Full-width translucent dark band behind the nav (no border, no radius). Hard lower edge at y=33.6 (row 33 is the "
           "anti-aliased step: L 42->56 at x150, 48->88 at x200, 73->117 at x300; almost no step at x<100 where the left scrim "
           "is already dark and at x>500). Composite colour inside the band: x0 #051222, x90 #101D2C, x190 #19273B, x290 #273850, "
           "x350 #314158, x440 #3F485E, x505 #2E425C, i.e. roughly the scene darkened by #0A1628 at ~40%. Strictly it is nav UI, "
           "but it stays remove=false: a verifier test inpaint of the full 518x34 strip (LaMa, tools.py) came out streaky with "
           "hard vertical seams, far worse than the kept band (the nav texts/icons inside it are still removed). Unity: keep "
           "the background top-anchored so the baked edge stays at the nav bar's bottom, and draw the nav bar 0..33.6 "
           "(stretch width) with ~0-10% #0A1628 fill; if the background is scaled/cropped differently, draw the bar at "
           "#0A1628 ~45% instead and extend it 1-2px past the baked edge to hide it. "
           "No nav tab is highlighted on this page (no nav.tab_selected / underline).")
setv("nav.brand_icon", box=[19.9, 10.9, 35.2, 27.1],
     notes="Blue rounded-square app tile (#3F76F5, slightly lighter top) with a white concentric 'C'/spiral glyph (sampled core #D2F1FF, "
           "drawn white). Tile edges measured at half-max of B-R contrast; ~2px dark-blue soft shadow around it (pad 2.5 covers it). "
           "Same brand tile as the other pages.")
setv("nav.brand_text", box=[40.5, 14.9, 78.1, 24.2], fontPx=10.1,
     notes="Box bottom = 'y' descender (row 23 full, row 24 ~20%); baseline y=22.2; 'L' cap top 14.9 -> cap height 7.3 -> fontPx 10.1. "
           "Sampled core #F9FEFF (white).")
setv("nav.tab_home", text="Boah sahifa", intended="Bosh sahifa",
     notes="AI-garbled: the 3rd glyph is drawn as an 'a' (closed bowl + right stem, unlike the 's' in 'sahifa'), so the art reads "
           "'Boah sahifa'; use 'Bosh sahifa'. Baseline 22.4 (no descenders); h/f ascenders set the top. Unselected tab colour, "
           "sampled brightest #C3CCDA..#D0DBF1 (thin anti-aliased text; ~80-85% white). Tab centres x: 117.5, 169.8, 216.5, 264.2, 318.0 (pitch ~50).")
setv("nav.bell_dot", box=[422.2, 14.0, 425.9, 17.2],
     notes="Red notification dot on the bell's top-right shoulder, ~3.7x3.2px. Brightest sampled core #BB3F54 (tiny + blurred, so darker "
           "than drawn); intended colour a saturated red/pink #EF4460 (other pages sample #CC494F / #CF4151, same dot).")

# ---------------- header ----------------
setv("header.title", box=[21.4, 44.0, 131.7, 56.6], fontPx=16.5,
     notes="No descenders; top = last 'i' dot (y 44.0; 'B' cap top 44.75), bottom = baseline 56.6; cap height 11.85 -> fontPx 16.5. "
           "Left = 'B' stem at 21.4 (x=20 is the dark shadow, not ink). Soft dark text shadow ~1px down/left (pad 3 covers it).")
setv("header.subtitle", box=[20.7, 65.0, 168.0, 73.9], fontPx=9.4,
     notes="Bottom = descenders of g/y/comma (row 73 ~90%, row 74 background); baseline 72.0; 'Y' cap top 65.3 -> cap 6.7 -> fontPx 9.4; "
           "l/k ascenders set top 65.0. NOTE: the bright blob at x148-149, y74-80 is a lit plant stem in the background, not text. "
           "Behaves like white at ~72% opacity (reaches L~180 on the dark scrim, ~205 over the plant); on the dark scrim this equals "
           "#BCC0CA. Last word is garbled in the art ('hamkorlilar').")

# ---------------- side list ----------------
rows = {
    "side.item_0": [19.8, 88.6, 126.3, 109.4],
    "side.item_1": [19.8, 110.8, 126.4, 133.0],
    "side.item_2": [19.8, 135.0, 126.4, 157.0],
    "side.item_3": [19.8, 159.0, 126.4, 181.4],
    "side.item_4": [19.8, 183.6, 126.4, 206.0],
    "side.item_5": [19.8, 208.0, 126.4, 230.6],
    "side.item_6": [19.8, 232.8, 126.4, 257.2],
}
for k, b in rows.items():
    setv(k, box=b)
glass_note = ("Dark translucent glass row (composite interior ~#232D3B mid-row, ~#142232 at the left end where the scrim behind is darker) "
              "with a faint 1px lighter stroke (visible at x=20 and on the rounded corners). Rows are separated by a 2px GAP (y 133-135, "
              "157-159, 181.4-183.6, 206-208, 230.6-232.8) through which the lighter background shows as a hairline. Measured row "
              "extents: tops 110.8/135/159/183.6/208/232.8, bottoms 133/157/181.4/206/230.6/257.2 (heights 22.2, 22, 22.4, 22.4, 22.6, "
              "24.4 - AI drift); use a uniform 22px row + 2px gap in Unity. Right edge 126.4 = same as the selected row.")
for k in ["side.item_%d" % i for i in range(1, 7)]:
    setv(k, notes=glass_note)
setv("side.item_0",
     notes="Selected row: solid vivid blue #2264FC (flat) with a soft blue glow that is asymmetric: ~7px on the LEFT (x 12..19.8, "
           "B-R still +5..+45 above the scrim), ~3px on the right/top/bottom. Pad 4 + removePolygon 'side_item0_glow_left' cover it. "
           "A barely visible 3x3px bump (+6 L) at ~(117,98) near the right end is noise, not a chevron - no element. Row pitch ~24.1; "
           "the gap to item_1 is ~1.4px.")
setv("side.item_1_label", box=[52.8, 118.4, 76.0, 125.0],
     notes="baseline 125, no descenders; top = O/f/l ascenders (row 118 ~55%). Label x starts at 53 (icon x 30.5-41). Sampled brightest ~#DFE6F3.")
setv("side.item_3_label", box=[52.9, 167.0, 87.3, 174.6],
     notes="bottom = 'p' descender (row 174 ~55%); baseline ~173.2. Label x starts at 53 (icon x 30.5-41). Sampled brightest ~#DFE6F1.")
setv("side.item_4_label", box=[53.0, 191.6, 90.1, 198.0],
     notes="baseline 198, no descenders; top = 'I'/'l'/'t' (row 191 ~40%). Label x starts at 53 (icon x 30.5-41). Sampled brightest ~#D0D9E8.")
setv("side.item_0_label", box=[52.6, 95.0, 75.6, 103.3],
     notes="Smudged in the art (glyphs read roughly 'Aaoriy'), intended 'Asosiy'. Bottom = 'y' descender; baseline ~101.5. "
           "Label x starts at 53 (icon x 30.5-41). White text; samples only reach #C5EAFF because thin white strokes blend with the blue.",
     intended="Asosiy")
setv("side.item_3_icon", icon="lightbulb",
     notes="Outline line icon ~1.3px stroke, ~11x12: house/badge outline with a lightbulb inside (startups; 'rocket' also fits). "
           "AI-garbled glyph - use a clean equivalent.")
setv("side.item_4_icon",
     notes="Outline line icon ~1.3px stroke, ~11x11: house/badge outline with coins + small chart inside (investors; e.g. "
           "'chart-coins' / 'piggy-bank'). AI-garbled glyph - use a clean equivalent.")
setv("side.item_6_icon",
     notes="Outline line icon ~1.3px stroke, ~12x12: scalloped seal/badge (gear-like) circle with a small mark inside (services; "
           "'gear' also fits). AI-garbled glyph - use a clean equivalent.")
for i in range(0, 7):
    for suf in ("", "_icon", "_label"):
        E["side.item_%d%s" % (i, suf)]["anchor"] = "tl"

# ---------------- tiles ----------------
tiles = {
    "business.tile_0": [148.0, 210.0, 227.0, 263.0],
    "business.tile_1": [233.2, 210.0, 313.0, 263.0],
    "business.tile_2": [318.0, 210.0, 403.8, 263.0],
    "business.tile_3": [409.2, 210.0, 504.0, 263.0],
}
tile_note = ("Glass action tile: dark navy translucent fill (composite interior #2E3A4F / #273243, lighter where the bright scene is "
             "behind, e.g. tile_2 #404350 -> backdrop blur ~4px), 1px light stroke (~#62697B; the top stroke is pixel row 210, e.g. "
             "L 96 vs 80 above / 55 below on tile_3), faint 1px dark shadow below (row 263). Icon top-left, label bottom-left; left "
             "padding ~10px. Measured x: 148-227, 233.2-313, 318-403.8, 409.2-504 (widths 79, 79.8, 85.8, 94.8; gaps 6.2, 5, 5.4) - "
             "equal widths with ~6px gaps are fine; row bottom margin ~22px.")
for k, b in tiles.items():
    setv(k, box=b, notes=tile_note)
for i in range(3):
    setv("business.tile_%d_label" % i, fontPx=7.9)
setv("business.tile_0_label",
     notes="bottom = 'j' descender (253.0); baseline 251.0; 'O' cap top ~245.3 -> cap 5.7 -> fontPx 7.9. Sampled brightest #D2DAE9 (thin text), true ~#E6ECF7.")
setv("business.tile_1_label",
     notes="bottom = 'p' descender (~252.8); baseline 251.0; 'H' cap top 245.35 -> fontPx 7.9. Sampled brightest #DAE2F5 (thin text), true ~#E6ECF7.")
setv("business.tile_2_label", box=[328.6, 245.0, 394.0, 252.8],
     notes="bottom = p/j/y descenders (~252.7); baseline 251.0; 'S'/'t'/'j'-dot set the top. Smudged but readable. Sampled brightest #D9DFED, true ~#E6ECF7.")
setv("business.tile_3_label", box=[419.3, 245.6, 496.5, 251.2], intended=None,
     notes="no descenders; baseline 251.2; 'I'/'l'/'b'/'h' tops ~245.6 (cap 5.6 -> fontPx 7.8); letters condensed to fit the tile. "
           "Smudged but reads 'Investorlar bilan uchrashuv'. Sampled brightest #D5D9E7, true ~#E6ECF7.")
setv("business.tile_0_icon",
     notes="White outline icon ~1.5px stroke, ~15x16: shopping-bag/building outline with a keyhole-person inside (office rent; 'key' / "
           "'building' also fit). AI-garbled - use a clean equivalent.")
setv("business.tile_2_icon",
     notes="White outline icon ~1.5px stroke, ~16x16: open hand holding a box/coin with a small swirl on top (place a startup; "
           "'hand-coin' / 'rocket' also fit). AI-garbled - use a clean equivalent.")
setv("business.tile_3_icon", icon="presenter",
     notes="White outline icon ~1.5px stroke, ~15x15: person bust inside a frame with raised arms / small chart (meet investors; "
           "'user-tie' / 'handshake' also fit). AI-garbled - use a clean equivalent.")

# ---------------- wall sign (kept) ----------------
setv("business.wall_sign_logo", box=[343.0, 88.9, 365.3, 118.8])
setv("business.wall_sign_title", box=[373.0, 89.2, 428.1, 105.0],
     notes="Part of background art. Bottom = 'y' descender (rows 103-104 full, row 105 background); baseline ~101.9; lit white letters with glow.")
setv("business.wall_sign_subtitle", box=[373.3, 106.3, 428.6, 117.0],
     notes="Part of background art; top = 'i' dot (106.3; 'B' cap top 107.5); baseline 117.0.")
setv("business.wall_sign_tagline", box=[342.4, 127.0, 441.1, 133.4])

# ---------------- polygons / assets ----------------
spec["removePolygons"] = [
    {"name": "frame_top_artifact", "points": [[0, 0], [518, 0], [518, 3.0], [0, 3.0]], "pad": 0},
    {"name": "frame_right_artifact", "points": [[515.5, 0], [518, 0], [518, 285], [515.5, 285]], "pad": 0},
    {"name": "frame_corner_tl", "points": [[0, 0], [6, 0], [6, 3], [3, 6], [0, 6]], "pad": 0},
    {"name": "frame_corner_bl", "points": [[0, 279.5], [3, 282], [5, 285], [0, 285]], "pad": 0},
    {"name": "frame_corner_br", "points": [[518, 279.5], [515, 282], [512.5, 285], [518, 285]], "pad": 0},
    {"name": "side_item0_glow_left", "points": [[11.5, 84.5], [21, 84.5], [21, 113.5], [11.5, 113.5]], "pad": 0},
]
for a in spec["assets"]:
    if a["name"] == "business_background":
        a["notes"] = ("Office lounge (city-view windows left, sofas/table centre, lit wall sign right, plants right edge). Remove every "
                      "remove=true element (nav texts/icons/pill, title, subtitle, 7 side rows incl. the selected row's glow, 4 action "
                      "tiles) and the removePolygons (composite frame: rows y 0-3, columns x 515.5-518 and the three rounded frame corners; "
                      "the selected row's left glow). Keep the wall sign, the left dark scrim"
                      + (" (the nav band is inpainted too)." if NAVBAR_REMOVE else " and the nav darkening band (y 0-33.6)."))
    if a["name"] == "brand_icon":
        a["box"] = [19, 10, 36, 28]

json.dump(spec, open(out_path, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print("wrote", out_path, len(spec["elements"]), "elements,", sum(1 for e in spec["elements"] if e.get("remove")), "remove=true")
