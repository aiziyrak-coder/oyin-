import json, copy
SRC = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\spec\shops.json"
DST = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\spec\shops.verified.json"
spec = json.load(open(SRC, encoding="utf-8"))
E = {e["id"]: e for e in spec["elements"]}


def upd(i, **kw):
    E[i].update(kw)


# --- search / filter -------------------------------------------------------
upd("shops.search", box=[165.0, 43.3, 368.8, 66.95], radius=5.5,
    notes="Light frosted-glass search field (~20-22% white + background blur; reads #536380 over sky, #716B71 over warm areas). "
          "1px lighter inner rim (brightest along the top, y=44) and a 1px dark shadow line just OUTSIDE the fill (y=42, y=67, x=369) - "
          "the box is the fill edge, the shadow is covered by pad. Corner radius ~5.5-6. Stretches horizontally between the side panel and the filter button.")
upd("shops.filter", box=[375.7, 43.0, 466.1, 66.0], radius=5.5,
    notes="Category filter dropdown: darker slate glass (#373843..#4D4C56) than the search field, 1px faint light rim at the top (y=43). "
          "Right edge aligns with the profile pill (x~466). Slightly shorter than the search field (search 43.3-66.95); the builder may equalise them.")

# --- nav profile ---------------------------------------------------------------
upd("nav.profile_pill", box=[396.0, 9.9, 465.9, 29.5], radius=9.8)
upd("nav.profile_avatar", box=[396.3, 9.8, 413.8, 28.1])
upd("nav.bell_dot", box=[383.8, 13.8, 387.4, 17.6])
upd("nav.profile_name", fontPx=6.0,
    notes="Very small and AI-blurred but reads 'Lynxos_user'. Box includes 'y'/'_' descenders (cap top of 'L' 18.2, baseline ~22.4, cap height ~4.2 -> 5.8 px). "
          "Text width 32.7 px equals the other pages' profile names (fontPx 6.5-7.0), so ~6-6.5 px is the consistent choice.")

# --- side list -------------------------------------------------------------------
upd("side.item_0", box=[13.0, 88.85, 102.8, 108.8], radius=5.0,
    notes="Selected row: saturated blue, horizontal gradient #1A58FC (left) -> #416EFC (right), soft blue glow ~2-3px (#2F6BFF80) around it, no border. "
          "Fill edge measured on the blue channel (top 88.85, bottom 108.8, left 13.0, right ~102.8). Corner radius ~5. "
          "Shorter (h 20.0) than the other rows (h ~23.7) as drawn; row pitch of the normal rows 24.75.")
rows = {1: (110.0, 134.0), 2: (135.0, 158.0), 3: (159.0, 183.0), 4: (184.0, 208.0), 5: (209.0, 232.5), 6: (233.5, 258.0), 7: (259.0, 283.0)}
for n in range(1, 8):
    upd(f"side.item_{n}", box=[12.5, rows[n][0], 102.5, rows[n][1]], fill="#1C293BB8", stroke="#FFFFFF14", radius=4.5,
        notes="Normal row: dark navy translucent fill (~#1C293B at ~72% alpha over the side panel; least-squares fit over all 7 rows: "
              "reads #182535 at the left, where the panel behind is near-black, and #343B46..#363D49 at the right, where it is DARKER than the "
              "semi-transparent panel beside it - so it is NOT a light overlay). Faint 1px light rim (~8% white, brightest on the top and left edges). "
              "Rows are separated by ~1px; row pitch 24.75 px; corner radius ~4.5.")
upd("side.item_3_icon", icon="bag")

# --- brand tiles -----------------------------------------------------------------
tiles = {
    0: [119.3, 188.1, 197.2, 227.1], 1: [202.9, 188.2, 280.6, 227.0], 2: [286.1, 188.1, 364.0, 227.0], 3: [369.3, 188.0, 447.8, 227.1],
    4: [119.3, 232.1, 197.1, 270.8], 5: [202.9, 232.1, 280.8, 270.8], 6: [286.1, 232.1, 364.1, 270.7], 7: [369.5, 232.1, 447.7, 270.8],
}
for n, b in tiles.items():
    r, c = divmod(n, 4)
    upd(f"shops.brand_{n}", box=b, radius=4.5,
        notes=f"White brand tile, grid row {r}, col {c} (4x2 grid; tile ~78 x 38.8; column gaps ~5.4-5.7, row gap ~5.0). "
              "Fill #F9FAFC with a very subtle top-to-bottom tint (#F3F4F7 -> #FBFBFD); 1px dark contact line just outside the edge (covered by pad). "
              "Corner radius ~4.5 (fits 4-5). Geometry only: the game shows fictional brands here.")
upd("shops.brand_3_logo", color="#0A1960",
    notes="Logo ink box, dark-blue bold wordmark (real trademark SAMSUNG; sampled core #0A1960) - DO NOT reproduce; replace with a fictional brand logo centred in the tile.")
upd("shops.brand_5_logo", color="#BA0F13",
    notes="Logo ink box, red script wordmark (real trademark H&M; sampled core #BA0F13) - DO NOT reproduce; replace with a fictional brand logo centred in the tile.")
upd("shops.brand_6_logo", color="#0A0B0C")
upd("shops.brand_0_logo", color="#020303")

# --- view-all link ------------------------------------------------------------------
upd("shops.view_all_label", color="#C4DCFF",
    notes="Link text, light periwinkle blue (sampled core top-3% #CCE2FF, top-10% #BDD5FF; blurred). No descenders; baseline 292.0. = 'View all'. Thin dark halo (covered by pad).")
upd("shops.view_all_arrow",
    notes="Thin (1px) right arrow, more saturated blue than the label (raw stroke samples #6488C0..#6B9AD3, peak #B7E5FF; #6FA0F0 is the de-blurred estimate). "
          "Right edge aligns with the tile grid's right edge (447.7).")

# --- mask helper: close the holes where four rounded tile corners meet --------------------
spec["removePolygons"].append({
    "name": "brand_grid_gaps",
    "points": [[116.3, 185.1], [450.8, 185.1], [450.8, 273.8], [116.3, 273.8]],
    "pad": 0,
})

spec.setdefault("verification", {})
spec["verification"] = {
    "verifiedFrom": "spec/shops.json",
    "method": "independent 50%-crossing ink boxes, averaged edge profiles (luminance / blue channel), per-row corner fits, least-squares glass-fill fit, core-pixel colour sampling",
}
json.dump(spec, open(DST, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print("written", DST, len(spec["elements"]), "elements")
