"""Apply verifier corrections to spec/friends.json -> spec/friends.verified.json"""
import json, copy, os

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "..", "spec", "friends.json")
OUT = os.path.join(HERE, "..", "spec", "friends.verified.json")

spec = json.load(open(SRC, encoding="utf-8"))
E = {e["id"]: e for e in spec["elements"]}
LOG = []


def upd(id, **kw):
    e = E[id]
    for k, v in kw.items():
        old = e.get(k)
        if k == "notes_add":
            e["notes"] = (e.get("notes", "") + " " + v).strip()
            continue
        if old != v:
            LOG.append(f"{id}: {k} {old} -> {v}")
        e[k] = v


# ------------------------------------------------------------------ NAV
upd("nav.brand_icon", box=[13.4, 11.4, 28.8, 26.8], pad=2.5, notes_add="Soft blue glow (#00175A) ~2 px outside the tile on the right/bottom and a dark 1 px shadow on the left - pad 2.5.")
upd("nav.tab_selected", box=[141.5, 12.5, 183.3, 31.5], pad=3, anchor="tl",
    notes="DRAWN ON 'Dunyo' BY MISTAKE - in game this highlight goes behind the active tab (Do'stlar). Not a solid pill: a vertical "
          "gradient glow that is fully transparent at the top (fades in from y~13; nothing visible at y<12.5) -> #152D5E (y~20) -> "
          "#1448A9 (y~29) right above the underline. Side edges x 141.5 / 183.3 (soft ~1 px). Soft blue glow spills ~2 px left/right "
          "and ~4 px below the underline (to y~35).")
upd("nav.tab_underline", box=[142.1, 30.0, 182.9, 31.4], pad=4, anchor="tl",
    notes="Bright glowing underline (core #96C9FF, ~1.4 px thick: row 30 full, row 31 ~40%) spanning the width of nav.tab_selected "
          "(inset ~0.5 px); blue glow (#0B317F..#112C56) below it down to y~35.5 - pad 4 covers it.")
for t in ("nav.tab_home", "nav.tab_world", "nav.tab_friends", "nav.tab_top", "nav.tab_settings"):
    upd(t, anchor="tl")
upd("nav.lang_text", color="#B4C0D6")
upd("nav.bell_dot", color="#BA4652",
    notes="Red notification dot on the bell's top-right, ~4.1 x 3 px, centre ~(397.8,16.0). Colour = sampled brightest core "
          "(#BA4652, blurred); intended a saturated red ~#E5484D.")
upd("nav.profile_pill", box=[406.5, 10.5, 479.0, 31.0], radius=10.25,
    notes="Fully rounded glass capsule; fill observed #283B53 (interior) vs bg #1D2C40 right of it; faint lighter 1 px rim "
          "(top row y=11 #2D3C56, bottom row y=30 #2F405A, right column x=478). The avatar circle (nav.profile_avatar) sits "
          "flush in its left cap; a 1 px darker line at x=406 separates it from the page bg.")
upd("nav.profile_avatar", box=[407.0, 11.0, 425.4, 29.3], radius=9.2, pad=1.5,
    notes="Full avatar circle, diameter ~18.4, centre ~(416.2,20.2): portrait photo (young man, dark hair, face at x~416-421) "
          "whose left third is a light grey ring/backdrop (#4E586C..#5E697E); faint light rim on the right edge. Same player "
          "portrait as the other pages; better replaced by the real avatar sprite.")
upd("nav.profile_chevron", color="#B0C2DA")

# ------------------------------------------------------------------ HEADER
upd("header.subtitle", color="#A2B0C4",
    notes="Box = cap/ascender top to g descender; baseline ~71.4. Muted blue-grey; colour = median of the brightest 2% "
          "(anti-aliased average #8694A8).")

# ------------------------------------------------------------------ SEGMENTED TABS
upd("friends.tabs_bar", box=[230.3, 48.0, 478.0, 70.5], radius=4.5,
    notes="Segmented control container: darker navy glass (#20344F interior) than the surrounding bg (#2C4563); 1 px lighter top "
          "rim on row y=48 (#30445F, +10 lum), bottom edge soft at y~70.5. Left end is hidden under friends.tab_selected (same left "
          "edge 230.3). 4 segments of ~61-64 px separated by faint dividers at x~357 and x~418.")
upd("friends.tab_selected", box=[230.3, 48.5, 294.85, 70.9], radius=5, pad=5,
    notes="Selected segment fill: bright blue, slight left->right gradient #3A7FFB (left) -> #3575FA (right), 1 px lighter left "
          "rim (#3F84FF). Soft blue outer glow ~4-5 px (visible above y 43-47 and left x 224-229) - pad 5 covers it. Occupies "
          "segment 0 (Do'stlar).")
upd("friends.tab_0", box=[230.3, 48.0, 294.85, 70.5])
upd("friends.tab_1", box=[294.85, 48.0, 357.0, 70.5])
upd("friends.tab_2", box=[357.0, 48.0, 418.0, 70.5])
upd("friends.tab_3", box=[418.0, 48.0, 478.0, 70.5])
upd("friends.tab_sep_0", box=[356.0, 50.5, 358.0, 69.0],
    notes="Very faint vertical divider between segments 1 and 2 (cols 356-357 +5 lum over the fill, col 354-355 1-2 lum darker).")
upd("friends.tab_sep_1", box=[417.0, 50.5, 419.0, 69.0],
    notes="Very faint vertical divider between segments 2 and 3 (cols 417-418 +5 lum).")

# ------------------------------------------------------------------ SEARCH
upd("friends.search", box=[185.9, 79.0, 479.2, 103.0])

# ------------------------------------------------------------------ HERO
upd("friends.hero_image", box=[186.0, 108.7, 479.0, 268.0], radius=6, remove=False,
    notes="Photo-style picture: group of young people sitting on a terrace, seen from behind, looking at a futuristic skyline at "
          "sunset (tree top-right). Thin light rim (~1 px, #8C96B0; left col 186, top row 109, right col 478) and a dark drop "
          "shadow row below (y 268). Corner radius ~5.5-6. remove=false (same convention as education.hero_image) so the inpainted "
          "page keeps the picture for the asset crop; the overlay title/button below are removed.")
upd("friends.hero_title", fontPx=12.4, anchor="tr",
    notes="Inside the hero: offset from hero_image top-left (+17.7, +20.1). White with a soft dark-blue text shadow (~1-2 px). "
          "Box = cap top to g descender; cap height 8.9 (B 128.8-137.7), baseline ~137.7.")
upd("friends.hero_button", box=[269.7, 229.6, 393.8, 257.0], anchor="tr",
    notes="Primary CTA inside the hero: horizontally centred on the hero (hero centre x=332.5, button centre x=331.75), bottom "
          "11.0 px above the hero bottom. Fill #1D5AFA (top #1854F8 -> bottom #225CFE), 1 px lighter top/left rim, dark navy "
          "outline/shadow (#000C3D..#0C1E45, 1-2 px) around it - pad 3 covers it.")
upd("friends.hero_button_label", anchor="tr")
upd("friends.hero_button_arrow", anchor="tr")

# ------------------------------------------------------------------ SIDE LIST
upd("side.panel", box=[12.0, 85.6, 175.5, 260.3])
upd("side.title", fontPx=9.4)
upd("side.item_0", box=[12.5, 110.2, 175.0, 145.3], fill="#304565FF",
    notes="Highlighted (selected/hover) row: lighter navy (median #304565; left ~#283E60, right ~#364866), flush with the panel's "
          "inner sides, top edge y~110.2 (row 110 ~80%), bottom 145.3. Row pitch 36 px.")
upd("side.item_0_icon", box=[19.3, 115.3, 44.8, 140.2],
    notes="Round avatar photo (diameter ~25.2, centre ~(32.0,127.8)) with a ~1.5 px light ring (#9397A9) and a 1 px dark outer "
          "line. Portrait: young man, brown hair.")
upd("side.item_3_icon", box=[19.15, 223.8, 44.85, 248.6])
for n, fs in ((0, 7.8), (1, 7.8), (2, 7.8), (3, 7.8)):
    upd(f"side.item_{n}_status", fontPx=fs)
upd("side.item_1_status", intended="Onlayn",
    notes="Green 'online' status; drawn heavily garbled (blob with an extra leading glyph) - intended 'Onlayn', same style as "
          "side.item_0_status (all four statuses share one style, ~7.8 px medium).")
upd("side.item_3_status", notes_add="Uzbek UI would read 'Oflayn' (pairs with 'Onlayn'); the drawing says 'Offline'.")
upd("side.divider_0", box=[46.0, 181.0, 173.5, 182.0],
    notes="Very faint 1 px row separator between item_1 and item_2 (row y=181 +3 lum, row 182 +1), inset from the left (starts "
          "under the name column).")
upd("side.divider_1", box=[46.0, 217.0, 173.5, 218.0],
    notes="Very faint 1 px row separator between item_2 and item_3 (row y=217 +3 lum, visible mostly from x~60).")
upd("side.divider_2", box=[38.0, 253.0, 173.5, 254.0],
    notes="Lighter 1 px line under the last row (row y=253, +4 at x 38-53, +9..+15 from x 54 to 157, fading to +6 toward the "
          "right end). Below it a slightly lighter 6 px footer strip (y 254-259) and the panel's bottom rim (y 259).")

# ------------------------------------------------------------------ PAGE LEVEL
spec["notes"] = (
    "Friends page (Do'stlar va muloqot). Background: dark blurred navy, vertical/diagonal gradient - top-left #061422, around "
    "header #1F3A5A, right side #263D59..#2C4563, bottom #122032; warm reddish haze at far right mid (#3A3C46). No distinct nav "
    "bar. Layout: left column = side.panel (x 12.0-175.5), right content column x 185.9-479.2 (gutter ~10.5, right margin ~13). "
    "Nav highlight drawn on 'Dunyo' by mistake - in game Do'stlar is selected. All UI except friends.hero_image is marked "
    "remove; hero_image is a separate asset whose overlay (title + CTA) is removed. The crop edges carry 1-2 px of the concept "
    "sheet's window frame (dark row 0 + light row 1 on top, dark col 0 on the left, light col 490 + dark col 491 on the right) - "
    "see removePolygons.")
spec["removePolygons"] = [
    {"name": "frame_top", "points": [[0, 0], [492, 0], [492, 2.2], [0, 2.2]], "pad": 0},
    {"name": "frame_left", "points": [[0, 0], [1.2, 0], [1.2, 285], [0, 285]], "pad": 0},
    {"name": "frame_right", "points": [[489.8, 0], [492, 0], [492, 285], [489.8, 285]], "pad": 0},
]
hero = E["friends.hero_image"]["box"]
spec["assets"] = [
    {"name": "friends.hero_image", "box": hero,
     "notes": "Big picture: people sitting on a terrace seen from behind, looking at a futuristic city skyline at sunset. Remove "
              "the overlay text 'Birga dunyoni kashf eting' (friends.hero_title [203.7,128.8,347.2,140.5] + its dark shadow) and "
              "the 'Do'st topish' button (friends.hero_button [269.7,229.6,393.8,257.0] + label + arrow, incl. its 1-2 px dark "
              "outline) before use. Corners rounded ~6 px; crop 1 px inside to drop the light rim. The button covers people's "
              "backs/legs and the bench/cushions at y 226-259 - inpaint needs to rebuild them."},
    {"name": "friends_background", "box": [0, 0, 492, 285],
     "notes": "Page backdrop: featureless dark blurred navy gradient (see page notes). Inpainting every remove=true element and "
              "the frame polygons still leaves the hero picture in place (hero_image is remove=false); for a clean backdrop also "
              "inpaint the hero rect, or simply draw the gradient procedurally (recommended)."},
    {"name": "nav.profile_avatar_photo", "box": E["nav.profile_avatar"]["box"],
     "notes": "Player portrait (tiny, blurry); circular crop. Prefer the shared player avatar sprite."},
]
for n in range(4):
    b = E[f"side.item_{n}_icon"]["box"]
    who = ["young man, brown hair", "smiling woman, long brown hair", "man with dark hair, beard shadow",
           "woman with dark curly hair"][n]
    spec["assets"].append({"name": f"side.item_{n}_avatar_photo", "box": b,
                           "notes": f"Recommended-friend portrait ({who}); circular crop, the ~1.5 px light ring is UI and "
                                    "should be drawn separately."})

json.dump(spec, open(OUT, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print("\n".join(LOG))
print("elements", len(spec["elements"]), "->", OUT)
