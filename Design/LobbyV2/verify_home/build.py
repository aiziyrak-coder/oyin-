import json
SRC = '../spec/home.json'; DST = '../spec/home.verified.json'
spec = json.load(open(SRC, encoding='utf-8'))
E = {e['id']: e for e in spec['elements']}


def upd(i, **kw):
    e = E[i]
    for k, v in kw.items():
        if v is None:
            e.pop(k, None)
        else:
            e[k] = v


TXT = 'Box = 50%-coverage ink edges. '
# ---------------- nav ----------------
upd('nav.brand_plate', box=[29.7, 12.6, 62.5, 44.9], fill='#2A2F37B0',
    notes='Faint dark-grey rounded-square backplate / soft shadow behind the brand icon only (not behind the word). Left/top/bottom edges measurable (lum ~45-50 vs ~10 outside); the right edge dissolves into the brighter ceiling so 62.5 is approximate. Builder may render a 33x32 rounded rect r~8, #2A2F37 ~70%, or skip it.')
upd('nav.brand_icon', box=[37.0, 17.5, 57.6, 38.9],
    notes='App logo: blue rounded square r~4 (#306EEA top -> #3E70ED bottom, measured) with a white concentric C/spiral glyph (two nested open rings opening to the right), glyph core #E0FAFF. Sharp edges on a ~1 px darker surround.')
upd('nav.brand_text', box=[65.2, 23.0, 115.5, 35.8], fontPx=13.5,
    notes=TXT + "Includes the 'y' descender (to 35.8). Cap top 23.0, baseline 32.75 (cap 9.75 px), x-height top ~25.1. Geometric sans (Poppins/Montserrat-like), bold, white, with a 1 px dark outline/shadow.")
upd('nav.tab_selected', box=[210.1, 12.3, 275.7, 39.7],
    notes='Selected-tab background: rounded rect r~7, vertical gradient top #223967 -> bottom #2255BE (opaque), faint lighter top rim, right side slightly soft. Tab label centred horizontally; label cap-centre ~24.8.')
upd('nav.tab_underline', box=[214.8, 36.9, 271.3, 38.3],
    notes='Light-blue glowing underline (~1.5 px core; row y=37 is saturated near-white) inside the bottom of the selected pill, inset ~4.5 px from each side; soft blue glow (#3C61B8) 1-2 px above/below.')
for i, b in (('nav.tab_home', [220.1, 21.0, 265.0, 28.5]), ('nav.tab_world', [289.8, 21.2, 315.1, 30.3]),
             ('nav.tab_friends', [340.9, 21.2, 370.7, 28.5]), ('nav.tab_top', [395.8, 21.1, 424.8, 30.1]),
             ('nav.tab_settings', [449.5, 21.1, 492.8, 28.4])):
    upd(i, box=b, fontPx=10.0)
upd('nav.tab_home', notes=TXT + 'Cap top 21.2, baseline 28.45 (cap 7.2 px), no descenders. Near-white with slight blue tint; subtle 1 px dark text shadow (row above the caps is darker than the pill).')
upd('nav.tab_world', notes=TXT + "Includes 'y' descender (to 30.3). Cap top 21.2, baseline 28.4. Subtle 1 px dark text shadow.")
upd('nav.tab_friends', notes=TXT + "Cap top 21.2, baseline 28.4 (no descenders). Apostrophe drawn as a curly right quote (Do’stlar). Subtle 1 px dark text shadow.")
upd('nav.tab_top', notes=TXT + "Includes 'p' descender (to 30.1). Cap top 21.1, baseline 28.1-28.4. Subtle 1 px dark text shadow.")
upd('nav.tab_settings', notes=TXT + 'Cap top 21.1, baseline 28.4 (no descenders). Subtle 1 px dark text shadow.')
upd('nav.lang_globe', box=[545.9, 18.8, 560.1, 34.0])
upd('nav.lang_text', box=[567.7, 22.2, 578.9, 29.7], fontPx=10.1, notes=TXT + 'Cap top 22.2 (Z), baseline 29.6.')
upd('nav.lang_chevron', box=[584.4, 25.0, 590.0, 28.0])
upd('nav.bell', box=[609.8, 18.1, 623.1, 34.0])
upd('nav.bell_dot', box=[620.1, 18.5, 624.2, 22.8],
    notes='Red notification dot overlapping the bell top-right; measured (R-G channel, 50% level) diameter ~4.2, centre (622.2, 20.6). Core #D82548.')
upd('nav.profile_pill', box=[638.8, 12.0, 736.0, 40.0], radius=14.0,
    notes='Dark translucent glass capsule (fully rounded ends, r = h/2 = 14). Reads #3D5579-#435A7F over the sky (sky above #345B8F, below #7096C9); fill ~#1E2A40 @55% + backdrop blur. 1 px light rim is visible on the upper-left curve and along the top edge.')
upd('nav.profile_avatar', box=[643.8, 16.6, 664.1, 36.9], radius=10.15, pad=2,
    notes='Circular avatar photo (young man, dark hair, light shirt), diameter ~20.3, centre (654.0, 26.75). A light-grey crescent (#8090A0) shows inside the left side of the circle and a very faint 1 px darker ring sits ~1 px outside it (diameter ~23); pad 2 covers the ring.')
upd('nav.profile_name', box=[670.0, 23.1, 712.7, 30.8], fontPx=8.3, text='Lynxos_uoer', intended='Lynxos_user',
    notes=TXT + "Cap top 23.1, baseline 29.1 (cap 6.0 px); box includes 'y' descender and the underscore. AI-drawn glyphs are blurry: the 's' of 'user' is drawn like 'o'.")
upd('nav.profile_chevron', box=[720.0, 24.4, 727.0, 28.4])
# ---------------- header ----------------
upd('home.wordmark_spaced', box=[64.6, 63.9, 181.2, 76.7], fontPx=13.3, intended='Lynxos',
    notes="Faint, widely letter-spaced echo of the brand above the big title. Drawn mixed-case with thin ~1 px strokes: 'L' then lowercase 'y n k x o s' (the 'k' is a garbled extra glyph), letter advance ~12.3 px (tracking ~0.35 em). Clean glyphs span x 64.6-142.8; 3 trailing garbled lowercase-height glyphs (x 152.9-181.2, look like 'e v v') fade into the lit wall - drop them. L cap top 63.9, baseline 73.5, x-height top ~66.0, 'y' descender to 76.7. Render 'Lynxos' (not upper-case) in light weight, grey #747986 (~70% opacity look).")
upd('header.title', box=[41.4, 84.3, 192.0, 116.9], fontPx=36.1,
    notes="Big hero wordmark. Cap top 84.3, baseline 110.3 (cap 26.0 px), x-height top 90.2 (x-height ~0.77 cap: geometric heavy sans such as Poppins ExtraBold / Montserrat Black), 'y' descender to 116.9. All letters white EXCEPT the 'x' (see header.title_x). Tight letter spacing; 1 px dark outline around the glyphs.")
upd('header.title_x', box=[116.5, 90.3, 140.8, 110.2], color='#4378F0',
    notes="Full ink box of the 'x' inside header.title (render as a rich-text colour span). Two-tone as drawn: the left '>'-shaped half (top-left + bottom-left arms up to the crossing) is blue #4378F0, with a lighter #5D76D8 highlight on the upper-right arm tip; the lower-right and most of the upper-right arm are white like the rest of the title. Simplest faithful render: the whole x in blue #4378F0, or a left->right blue->white gradient.")
upd('header.subtitle', box=[41.6, 131.1, 180.3, 144.0], fontPx=13.8,
    notes=TXT + "Ascender top (d/k) 132.1, 'O' 131.6-141.9, 'f' top 131.1, baseline 141.9, x-height top ~134.8, descenders to 144.0. Apostrophe drawn as a curly quote (O’z). Light grey.")
upd('home.kirish', box=[36.6, 164.5, 229.5, 206.0],
    notes='Primary CTA. Rounded rect r~5, horizontal gradient left #2A7AFC -> right #2258FC (centre #2061FD), slightly desaturated 1 px top rim (#2A67E8). Thin dark-blue outer halo/glow ~4-6 px (strongest below). Mask pad 8 removes it.')
upd('home.kirish_icon', box=[56.0, 177.0, 69.9, 193.3])
upd('home.kirish_label', box=[86.0, 180.1, 117.5, 190.1], fontPx=13.1,
    notes=TXT + "K cap top 180.6, 'h' ascender top 180.1, baseline 190.0 (cap 9.45 px), no descenders. White.")
upd('home.kirish_arrow', box=[200.9, 179.8, 212.9, 190.9])
# ---------------- tagline ----------------
upd('home.tagline_line1', box=[614.8, 83.8, 718.7, 94.7], fontPx=11.3, color='#0D1021',
    notes=TXT + "Dark navy text over bright sunset sky. Cap/ascender top 84.0, baseline 92.15 (cap 8.15 px), x-height top ~86.0, 'y' descender to 94.7. Left-aligned at x 614.8.")
upd('home.tagline_line2', box=[615.0, 100.1, 729.0, 111.9], fontPx=12.2, weight='semibold', color='#100C18',
    notes=TXT + "Cap top 100.5, baseline 109.3 (cap 8.8 px), x-height top ~102.8, 'y' descender to 111.9. Drawn ~7% larger and a little bolder than line 1 (baseline pitch 17.15 px); a builder may use one style for both lines. Near-black (core #100C18 over the pink sky).")
upd('home.tagline_line', box=[614.2, 127.8, 657.4, 130.3],
    notes='Blue accent bar (progress-like fill), ~2.5 px thick, rounded ends, #4368BA, slightly lighter towards its right end (#6580C6). Together with home.tagline_track it reads as a progress bar ~53% filled (track fades out at x~695).')
upd('home.tagline_track', box=[657.4, 128.2, 695.0, 130.0],
    notes='Very faint darker track (~1.7 px, ~9% black) continuing the blue bar to the right; fades out between x~688 and ~696.')
# ---------------- event ----------------
upd('home.event_card', box=[575.8, 210.9, 741.8, 274.6],
    notes='Dark glass card (reads #454B57-#575661 over the lit city). Fill ~#1E2A42 @63% plus backdrop blur, 1 px light rim (white ~15%, clearest on the left edge at x~576).')
upd('home.event_thumb', box=[584.7, 218.8, 630.0, 267.3])
upd('home.event_label', box=[639.0, 221.2, 690.9, 229.9], fontPx=9.3,
    notes=TXT + "Cap top 221.2, baseline 227.9, 'g' descenders to 229.9. Light grey.")
upd('home.event_title', box=[638.9, 240.8, 699.3, 248.0], fontPx=9.5,
    notes=TXT + 'Cap top 241.0, baseline 247.9. Near-white with a strong soft blue glow (#3D6BFF ~35% at the glyphs, fading over ~8-10 px; it stays inside the card, so the card mask removes it).')
upd('home.event_time', box=[639.0, 257.4, 660.6, 264.1], fontPx=8.8,
    notes=TXT + "Digit top 257.8, baseline 264.1. Light blue. Separator is a period (baseline dot), not a colon: '20.00' as drawn ('20:00' is the usual Uzbek time format).")
# ---------------- cards ----------------
upd('home.card_0', box=[29.9, 347.9, 155.9, 398.8], pad=8,
    notes='SELECTED card: navy-tinted glass (#1F2F50) with a ~1.5-2 px light-blue border (#BEE0FF; box = outer edge of the border) and blue outer glow ~5-7 px (#2F6BFF, reaches y~405 below) plus faint inner blue glow. Mask pad 8 removes the glow. Row of 5 cards spans x 29.9-736.7; AI-drawn widths 126-138 px, gaps 8.7-10.2 px, tops 347.9-349.8 - builder may equalise (e.g. 5 x 133.4 px with 9.4 px gaps).')
upd('home.card_1', box=[164.6, 349.1, 295.8, 398.7])
upd('home.card_2', box=[306.0, 349.5, 442.4, 399.2])
upd('home.card_3', box=[452.3, 349.8, 589.9, 399.6])
upd('home.card_4', box=[599.0, 349.2, 736.7, 399.3])
for n in range(1, 5):
    upd('home.card_%d' % n, notes='Dark glass card, 1 px faint light rim. Row of 5 cards spans x 29.9-736.7; AI-drawn widths 126-138 px, gaps 8.7-10.2 px - builder may equalise (e.g. 5 x 133.4 px with 9.4 px gaps).')
card_txt = {
    'home.card_0_title': ([84.7, 367.0, 109.2, 375.8], "Cap top 367.0, baseline 373.85, 'y' descender to 375.8."),
    'home.card_0_subtitle': ([84.4, 381.0, 128.7, 388.6], "Cap top 381.0, baseline 387.2, 'y' descender to 388.6."),
    'home.card_1_title': ([219.0, 367.2, 261.9, 376.1], "Cap top 367.4, baseline 374.0, 'g' descender to 376.1."),
    'home.card_1_subtitle': ([219.0, 381.7, 275.9, 387.9], 'Ascender top 381.7, baseline 387.4-387.9 (no descenders).'),
    'home.card_2_title': ([360.6, 367.3, 426.0, 375.9], "Cap top 367.3 (O), baseline 374.5, 'q' descender to 375.9; apostrophe curly (O’quv)."),
    'home.card_2_subtitle': ([360.5, 381.8, 421.8, 389.3], "Cap top 381.8, baseline 388.0, 'j' descender to 389.3."),
    'home.card_3_title': ([507.5, 367.7, 565.9, 374.9], 'Cap top 367.85, baseline 374.7 (no descenders).'),
    'home.card_3_subtitle': ([507.3, 382.1, 561.2, 388.2], 'Cap top 382.1, baseline 388.0 (no descenders); capital I drawn like a lowercase l.'),
    'home.card_4_title': ([653.6, 367.9, 722.4, 376.9], "Cap top 368.0, baseline 374.75, 'g' descender to 376.9; apostrophe curly (Ko’ngilochar)."),
    'home.card_4_subtitle': ([653.2, 382.1, 710.8, 389.9], "Cap top 382.1, baseline 388.2, 'y' descender to 389.9; apostrophe curly (O’yin)."),
}
for i, (b, nt) in card_txt.items():
    t = i.endswith('title')
    upd(i, box=b, fontPx=9.4 if t else 8.5,
        notes=TXT + nt + (' Same style on all 5 cards (measured caps 6.6-6.9 px -> fontPx 9.4).' if t else ' Same style on all 5 cards (measured caps ~6.1 px -> fontPx 8.5).'))
upd('home.card_2_icon', box=[320.0, 362.3, 344.7, 387.8])
upd('home.card_3_icon', box=[467.9, 362.4, 491.8, 387.3])
upd('home.card_4_icon', box=[612.1, 364.7, 638.2, 385.2])
# ---------------- character / assets ----------------
ch = spec['character']
ch['centerX'] = 357.2
ch['headTopY'] = 75.8
ch['feetY'] = 332.6
ch['horizonY'] = 236.0
ch['notes'] = ('Standing avatar (red track jacket with white zip/stripes, white tee, dark grey trousers, white sneakers), facing camera. '
               'centerX = midpoint between the shoes (left shoe x ~320.5-345, right shoe x ~369-394, gap 345-369 -> 357.1; trouser inner edges 349/364 -> 356.5); the face centre is ~361. '
               'headTopY = 50% edge of the dark hair at x 357-364 (a faint 1 px rim-light halo sits above it at y~74.5). '
               'feetY = bottom of the dark sole/contact line (left shoe ~332.4, right ~332.8). Body height ~256.8 px. '
               'horizonY ~236 (+/-4): the far shoreline/promenade where the city meets the water is a bright level line at y~236.5-237 (x 430-570) and the distant horizon sits at or just above it; '
               'the sofa-back tops (y~228-235) are drawn flat, the terrace/planter edges on the right slope down away from it and the ceiling lines rise above it. '
               'So the camera is low (about hip height of the avatar, crotch at y~246): a slight hero low angle. The glossy floor shows a reflection of the shoes/trousers from y~333 down to ~348 (inside the removal polygon); below that card_2 covers it.')
for a in spec['assets']:
    if a['name'] == 'event_thumb':
        a['box'] = [584.7, 218.8, 630.0, 267.3]
json.dump(spec, open(DST, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
print('written', DST, len(spec['elements']))
