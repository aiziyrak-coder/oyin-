import json, copy, os
os.chdir(os.path.dirname(os.path.abspath(__file__)))
s = json.load(open('work.json', encoding='utf-8'))
els = s['elements']
E = {e['id']: e for e in els}
log = []


def L(m):
    log.append(m)


def setbox(i, b):
    E[i]['box'] = [round(v, 1) for v in b]


# ---- nav
setbox('nav.brand_icon', [18.9, 15.3, 39.1, 36.3])
L('nav.brand_icon box [19.4,15.6,38.6,36.2] -> [18.9,15.3,39.1,36.3] (square measured 20.2 x 21.0 at 50% edge; old box was 0.5 px narrow on both sides)')
setbox('nav.bell', [571.5, 17.9, 586.3, 34.8])
L('nav.bell top 16.8 -> 17.9 (the pixel at y16 is dark shadow; the knob ink starts at y~17.9)')
E['nav.profile_pill']['box'] = [605.3, 11.0, 729.8, 42.0]
L('nav.profile_pill box -> [605.3,11.0,729.8,42.0] (right edge 729.6 -> 729.8 and top/bottom +0.2; the right column edge is x=730 for pill, search, zoom and card)')

cols = {'nav.brand_text': '#FBFEFF', 'nav.tab_home': '#C6D1E1', 'nav.tab_world': '#D5EEFF', 'nav.tab_friends': '#CCD7EE',
        'nav.tab_top': '#CAD9ED', 'nav.tab_settings': '#CDDDF4', 'nav.lang_globe': '#E1F1FF', 'nav.lang_text': '#C9DEFB',
        'nav.lang_chevron': '#C5DAFB', 'nav.bell': '#EBFBFF', 'nav.profile_name': '#DCEAFF', 'nav.profile_chevron': '#DEECFE',
        'header.title': '#FBFFFF', 'header.subtitle': '#99A5BA',
        'side.item_0_icon': '#DAF8FF', 'side.item_0_label': '#D2F4FF', 'side.item_1_icon': '#F1F8FF', 'side.item_1_label': '#DCE6F5',
        'side.item_2_icon': '#F2FBFF', 'side.item_2_label': '#DEE8F6', 'side.item_3_icon': '#F0F7FF', 'side.item_3_label': '#DDE5F4',
        'side.item_4_icon': '#F4FAFF', 'side.item_4_label': '#E0E8F6', 'side.item_5_icon': '#EDF9FF', 'side.item_5_label': '#DCE2F0',
        'side.item_6_icon': '#EEF5FF', 'side.item_6_label': '#DBE5F3', 'side.item_7_icon': '#F2FAFF', 'side.item_7_label': '#E0E7F3',
        'map.card_title': '#E6F0FC', 'map.card_subtitle': '#94A8B7', 'map.pin_0_label': '#F8F8FF', 'map.pin_1_label': '#EAF7FF',
        'map.pin_2_icon': '#F7FFFF', 'map.pin_2_label': '#E4F9FF', 'map.pin_3_label': '#EAFAFF', 'map.pin_4_label': '#F8F5F6',
        'map.pin_5_label': '#FFF8FF', 'map.pin_6_label': '#EEFFFF', 'map.pin_0_icon': '#FFFDFC', 'map.pin_1_icon': '#F8FFFC',
        'map.pin_3_icon': '#FFFBFF', 'map.pin_4_icon': '#FEFCF6', 'map.pin_5_icon': '#FFFDFF', 'map.pin_6_icon': '#F9FFFF',
        'map.zoom_out_icon': '#E1F1FF', 'nav.bell_dot': '#CD2F4C',
        'map.pin_0_stem': '#DC7A70', 'map.pin_2_stem': '#63C1F8', 'map.pin_3_stem': '#88A7EA', 'map.pin_4_stem': '#E1B278'}
for k, v in cols.items():
    E[k]['color'] = v
L('colours: text/icon colours re-sampled as the median of the brightest 8% of pixels in each box (old values sat on anti-aliased edges and were 3-10 levels dark). '
  'Stem colours are now sampled from the stem pixels; the old values were copied from the bubble stroke. Changed: pin_0_stem #B07080->#DC7A70, pin_2_stem #5CA1E1->#63C1F8, '
  'pin_3_stem #7792C0->#88A7EA, pin_4_stem #CDA477->#E1B278')

# ---- side list
rows = {1: [147.4, 180.0], 2: [181.0, 213.0], 3: [214.0, 246.0], 4: [247.0, 279.0], 5: [280.0, 312.0], 6: [313.0, 346.0], 7: [347.0, 379.0]}
for n, (a, b) in rows.items():
    E[f'side.item_{n}']['box'] = [18.6, a, 153.0, b]
    E[f'side.item_{n}']['notes'] = ('Dark glass row (composite ~#192C42 at right, ~#0E1D2F at left because the panel gradient is darker there); 1px light border (#FFFFFF1F) '
                                    'with a 1px darker gap between rows. Row pitch 33 px (rows 32-33 px tall, e.g. item_3 y 214-246); border pixels sit just inside the box edges.')
E['side.item_0']['box'] = [18.6, 115.8, 153.0, 145.5]
L('side.item_1..7 boxes: shifted +0.5 px down (the old edges were half a pixel early). Pitch is 33 px, with a 1 px dark gap: rows y 147.4-180, 181-213, 214-246, '
  '247-279, 280-312, 313-346, 347-379. Old values were 147.3-179.5, 180.5-212.5, ... 346.5-378.6')
L('side.item_0..7 right edge 152.8 -> 153.0 (the row border is the pixel column x=152)')
p = E['side.panel']
p['box'] = [0.0, 46.0, 153.8, 415.0]
p['pad'] = 1
p['notes'] = (p['notes'] + ' Verified: right hairline = pixel column x=153 (visible y~55-415, clearest at y 392-412 over dark sea), i.e. 1 px right of the row borders (x=152). '
              'pad raised 0 -> 1 so the hairline anti-aliasing (x=154) is masked. Top y=46 is an estimate (the panel edge is invisible above y~55 where the map is also dark). '
              'The title "Dunyo xaritasi" overhangs the panel edge by ~0.5 px (its right edge is x~154).')
L('side.panel right edge 152.8 -> 153.8 and pad 0 -> 1. The panel hairline is the pixel column x=153, 1 px right of the row borders, so the old box left the hairline and its anti-aliasing unmasked')
c = E['side.item_0_chevron']
c['box'] = [139.0, 129.0, 141.8, 133.6]
c['notes'] = ('Tiny chevron-right drawn at very low contrast (#3874FF on #215BFD, peak lum +23) at the right end of the selected row; ink x 139-141, y 129-133. '
              'Intended: white chevron-right at ~35% opacity, or omit.')
L('side.item_0_chevron box refined to [139.0,129.0,141.8,133.6] (ink pixels x 139-141, y 129-133)')

# ---- zoom
E['map.zoom_panel']['box'] = [703.9, 126.4, 730.0, 174.6]
E['map.zoom_panel']['notes'] = ('Vertical dark-glass capsule holding +/- (composite ~#263B57, core lum ~56). Edges measured at 50%: left 703.9 (pixel 703 is background), '
                                'right 730.0, top 126.4, bottom 174.6. Split into two 26.1 x 24.1 halves at y=150.5.')
E['map.zoom_in']['box'] = [703.9, 126.4, 730.0, 150.5]
E['map.zoom_out']['box'] = [703.9, 150.5, 730.0, 174.6]
d = E['map.zoom_divider']
d['box'] = [704.5, 150.0, 721.5, 151.0]
d['notes'] = ('Faint 1px separator on pixel row y=150 (lum +7 over the panel), clearly visible only for x 705-721 and fading out to the right. '
              'Intended as a full-width inner divider (x ~706-728) at ~8% white.')
L('map.zoom_panel [703.4,126.4,730.0,173.9] -> [703.9,126.4,730.0,174.6]. The left edge was 0.5 px too far out and the bottom was 0.7 px short. zoom_in/zoom_out halves recomputed and split at y=150.5')
L('map.zoom_divider [705.0,149.6,728.5,150.6] -> [704.5,150.0,721.5,151.0] (the line is the pixel row y=150 and is only visible up to x~721)')

E['map.search']['box'] = [532.0, 59.0, 730.0, 90.0]
L('map.search [531.8,58.9,729.6,90.0] -> [532.0,59.0,730.0,90.0]. Pixel 729 is still frosted fill; 730 is map. Pixel 531 is a darker drop shadow, not the fill')

# ---- card
E['map.card']['box'] = [533.0, 355.4, 730.0, 405.0]
L('map.card right edge 729.5 -> 730.0 and left 533.2 -> 533.0 (pixel 729 is card fill; 730 is map)')
E['map.card_image']['box'] = [540.8, 363.2, 580.0, 397.6]
L('map.card_image top 362.8 -> 363.2 (pixel 362 is the dark rim; the photo starts partway into 363)')
cs = E['map.card_subtitle']
cs['text'] = 'Hocir onlayn: 1,248'
cs['intended'] = 'Hozir onlayn: 1,248'
cs['notes'] = ('AI-garbled: the z of "Hozir" is drawn as a "c" (reads "Hocir"), and the a of "onlayn" is narrow and looks like an "o". Intended "Hozir onlayn: 1,248". '
               'Box bottom = y descender; baseline 391.4 (cap 6.0). The number is dynamic (online count).')
L('map.card_subtitle: the drawn text reads "Hocir onlayn: 1,248" (z drawn as c). Set text to the drawn form and intended "Hozir onlayn: 1,248"')
for a in s['assets']:
    if a['name'] == 'map_card_thumb':
        a['box'] = [540.8, 363.2, 580.0, 397.6]

# ---- pins
bub = {0: [253.4, 151.0, 336.0, 184.9], 1: [481.3, 111.8, 582.4, 144.1], 2: [318.6, 89.6, 411.0, 122.5], 3: [538.6, 187.7, 646.5, 221.1],
       4: [254.5, 236.6, 338.3, 270.1], 5: [402.4, 283.0, 480.2, 318.4], 6: [565.7, 298.8, 644.8, 333.0]}
tips = {0: ([251.5, 185.5, 255.0, 188.7], [253.1, 188.6]), 2: ([316.0, 124.6, 321.2, 129.5], [318.8, 129.3]),
        3: ([535.6, 222.2, 539.6, 228.0], [537.6, 227.8]), 4: ([252.0, 271.5, 256.0, 275.4], [253.8, 275.3]),
        5: ([400.4, 320.6, 404.0, 324.6], [402.2, 324.4])}
E['map.pin_3_circle']['box'] = [519.4, 185.3, 557.8, 223.8]
E['map.pin_3_circle']['radius'] = 19.2
L('map.pin_3_circle [519.3,184.8,559.3,224.8] (40 px) -> [519.4,185.3,557.8,223.8] (38.4 px), radius 20.0 -> 19.2. Measured: outer stroke top at y 185.3 (x 536-540), '
  'bottom 222.0-222.3 at x 530/546 (so about 223.8 at centre), left 519.4, disk centre x 538.5')
E['map.pin_3']['box'] = [519.4, 185.3, 646.5, 223.8]
for n in range(7):
    pin = E[f'map.pin_{n}']
    pin['notes'] = (pin['notes'].split(" bubble box")[0] + f" bubble box {bub[n]} (see map.pin_{n}_bubble)."
                    " 'point' = tip of the bubble's bottom stem when one is drawn; otherwise (pins 1, 5, 6) the x-centre of the whole pin box on the bubble's bottom edge"
                    " (the drawn stems sit at 47-58% of the pin width, so this matches them). 'circleTip' = the small teardrop pointer under the icon circle"
                    " (alternative anchor; pins 1 and 6 have none, so it is the circle bottom).")
E['map.pin_1']['point'] = [522.6, 144.1]
E['map.pin_5']['point'] = [431.4, 318.4]
E['map.pin_6']['point'] = [595.7, 333.0]
E['map.pin_3']['point'] = [578.6, 227.5]
E['map.pin_3_stem']['box'] = [575.5, 221.0, 581.5, 227.6]
E['map.pin_2']['point'] = [358.8, 125.8]
L('Pin points: for pins 1, 5 and 6 the note said "bubble bottom centre", but the stored values were the x-centre of the whole pin box. I kept the values and made the note match. '
  'Their y moved onto the measured bubble bottom: pin_1 144.0 -> 144.1, pin_5 318.2 -> 318.4, pin_6 332.8 -> 333.0. Stem tips were refined: '
  'pin_3 point [578.7,227.0] -> [578.6,227.5] (stem box bottom 227.3 -> 227.6), pin_2 [359.0,126.0] -> [358.8,125.8]')
for n, (tb, pt) in tips.items():
    E[f'map.pin_{n}']['circleTip'] = pt
E['map.pin_1']['circleTip'] = [481.2, 147.0]
E['map.pin_6']['circleTip'] = [565.7, 335.0]
L('circleTip refined from the tip pixels: pin_0 [253.3,188.4]->[253.1,188.6], pin_2 [318.6,128.3]->[318.8,129.3], pin_4 [254.5,274.5]->[253.8,275.3], '
  'pin_5 [402.4,321.3]->[402.2,324.4]. Pin 5 does have a 3 px teardrop below the circle, down to y 324.5')
new = []
for e in els:
    new.append(e)
    for n in range(7):
        if e['id'] == f'map.pin_{n}_circle':
            pin = E[f'map.pin_{n}']
            lab = E[f'map.pin_{n}_label']['box']
            cir = E[f'map.pin_{n}_circle']['box']
            new.append({"id": f"map.pin_{n}_bubble", "kind": "panel", "box": bub[n], "fill": pin['fill'], "stroke": pin['stroke'], "radius": 7,
                        "group": "pins", "anchor": "c", "pad": 4, "remove": True,
                        "notes": (f"Label bubble (rounded rect, ~1.5px stroke) whose left end is hidden under the icon circle (starts at the circle centre x). "
                                  f"Outer edges measured at two columns each (+/-0.4 px). Label starts {round(lab[0] - cir[2], 1)} px right of the circle's right edge "
                                  f"and ends {round(bub[n][2] - lab[2], 1)} px before the bubble's right edge.")})
            if n in tips:
                tb, pt = tips[n]
                new.append({"id": f"map.pin_{n}_tip", "kind": "line", "box": tb, "color": pin['stroke'][:7], "group": "pins", "anchor": "c", "pad": 2,
                            "remove": True,
                            "notes": f"Small teardrop/triangle pointer under the icon circle (stroke-coloured, brightest in the centre column). Its lowest point = circleTip {pt}."})
els[:] = new
for e in els:
    if e['id'].startswith('map.pin_'):
        e['group'] = 'pins'
L('Added map.pin_N_bubble (7 elements) with the measured bubble boxes. The old notes had pin_0 bubble top 150.3 (measured 151.0) and pin_5 bubble top 283.6 (measured 283.0); the others were within 0.4 px')
L('Added map.pin_N_tip for the teardrop pointers under the icon circles of pins 0, 2, 3, 4 and 5. They are now explicit elements with remove:true, so the mask covers them even though the pin_3 circle was shrunk')
s['notes'] = (s.get('notes', '') + ' Verified pass: right column edge is x=730.0 for search, zoom, card and profile pill (29 px margin); left rows 18.6-153.0; side panel hairline at x~153.3. '
              'Pins are anchored in map space. Convention: point = bubble stem tip (or pin-box centre x on bubble bottom when no stem is drawn); circleTip = teardrop under the icon circle.')
json.dump(s, open('fixed.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
open('log.txt', 'w', encoding='utf-8').write('\n'.join(log))
print(len(els), 'elements')

# late fix
s = json.load(open('fixed.json', encoding='utf-8'))
for e in s['elements']:
    if e['id'] == 'map.pin_4_icon':
        e['box'] = [246.2, 245.4, 262.8, 262.0]
json.dump(s, open('fixed.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
with open('log.txt', 'a', encoding='utf-8') as f:
    f.write('\nmap.pin_4_icon top 246.0 -> 245.4 (the roof apex at x=254 reaches 56% ink at pixel 245)')
