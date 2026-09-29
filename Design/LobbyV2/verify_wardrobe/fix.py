import json, copy
s=json.load(open('work.json',encoding='utf-8'))
E={e['id']:e for e in s['elements']}
def setb(i,b): E[i]['box']=[round(v,1) for v in b]
# --- nav
setb('nav.brand_icon',[17.3,10.7,32.3,26.5])
E['nav.brand_text']['fontPx']=10.3
E['nav.brand_text']['notes']="Box = cap top of 'L' (14.6) to bottom of 'y' descender (24.0); baseline 22.0 (cap height 7.4)."
setb('nav.profile_name',[456.0,17.4,487.7,22.8])
E['nav.profile_name']['fontPx']=6.4
E['nav.profile_name']['notes']="AI-garbled at 1x (reads like 'Lynxos_user'); box = cap top of 'L' (17.4) to the 'y' descender/underscore (22.8); baseline 22.0, cap height ~4.6 -> fontPx ~6.4. Sampled core #B6C0CC (blurred), intended #C8D0DC."
setb('nav.profile_chevron',[492.4,18.4,496.9,21.3])
E['nav.profile_chevron']['notes']="Small thin chevron-down, stroke ~1px."
for k in ('nav.tab_home','nav.tab_world','nav.tab_friends','nav.tab_top','nav.tab_settings'):
    E[k]['fontPx']=8.3
    E[k]['notes']=E[k]['notes'].replace('Cap height ~6.1.','Cap height ~6.0 (B/D/T/S tops 15.9-16.2, baseline 22.0).')
E['nav.lang_text']['fontPx']=7.6
E['nav.lang_text']['notes']="Cap top 16.6 to baseline 22.1 (cap height 5.5)."
E['nav.bell_dot']['box']=[421.7,13.7,425.6,17.5]
# --- header
setb('header.subtitle',[18.0,67.3,144.5,76.1])
E['header.subtitle']['fontPx']=9.0
E['header.subtitle']['notes']=("Box includes descenders (g, y): top = 'O'/'l'/'b' tops ~67.3-67.5, baseline ~74.0 (cap height ~6.5, x-height ~5.0), descender bottom ~76.1. "
  "The last glyphs ('ng') run over the bright lit door frame at x>=140, so the right edge (~144.5) is approximate. Muted grey (sampled core #ACAFB9, intended #B4BAC6 = white ~70%).")
# --- side rows (measured from gap rows 114,141,168-169,196,223 and outer edges)
rows={0:[17.0,92.1,122.3,113.5],1:[17.0,115.0,122.0,141.0],2:[17.0,142.4,122.0,168.0],3:[17.0,170.0,122.0,196.0],4:[17.0,197.0,122.0,223.0],5:[17.0,224.0,122.0,250.5]}
for n,b in rows.items(): setb(f'side.item_{n}',b)
E['side.item_0']['notes']=("Selected row: solid royal blue (#1F5CFB, very slightly lighter top), soft blue glow ~2px. NOTE: drawn only ~21.4 px tall (92.1-113.5) "
  "while unselected rows are 26.0 px (row tops 115.0,142.4,170.0,197.0,224.0 -> pitch ~27.2, gaps 1-2 px); builder should equalise to 26.")
for n in range(1,6):
    E[f'side.item_{n}']['fill']="#1B2737BA"
    E[f'side.item_{n}']['notes']=("Dark translucent glass row: sampled #162131 over the dark left wall (#08121E) and #222B38 over the lighter right bg (#34333C) "
      "-> fill ~#1B2737 at alpha ~0.73. 1px lighter border (#2C354B) visible on left/top/bottom; right edge 122.0 has no visible border. Rows stack with 1-2 px gaps, pitch ~27.2.")
E['side.item_0']['fill']="#1F5CFBFF"
base={0:105.3,1:131.2,2:158.8,3:185.5,4:213.1,5:240.1}
desc={0:"cap top to 'y' descender",1:"cap top to 'y' descender",2:"no descenders",3:"cap top to 'g' descender",4:"no descenders",5:"cap top to 'q'/'g' descenders"}
for n in range(6):
    e=E[f'side.item_{n}_label']; e['fontPx']=8.0
    e['notes']=f"Ink box: {desc[n]}; baseline ~{base[n]}. Cap height ~5.7-6.0 (K/P/A/S/O measured), x-height ~4.3 -> fontPx ~8.0."
setb('side.item_0_label',[48.3,99.4,75.5,107.2])
# --- style tabs
setb('wardrobe.tabs_bar',[278.4,55.9,501.5,78.0])
E['wardrobe.tabs_bar']['notes']=("Segmented control: dark glass bar (sampled #2F4054..#314056 inside, page bg around ~#5A5E70). Segment cells: [278.4-323.3] Hammasi (selected pill), "
  "[323.3-372.5] Kostyumlar, [372.5-416.6] Sportcha, [416.6-460.0] Kundalik, [460.0-501.5] Maxsus (divider centres 372.5, 416.6, 460.0). Faint slightly lighter bottom rim (row 77).")
setb('wardrobe.tab_selected',[278.4,55.7,323.3,78.2])
E['wardrobe.tab_selected']['fill']="#438DFDFF"
E['wardrobe.tab_selected']['notes']="Selected segment: bright azure blue (#438DFD, lighter than the side-list blue #1F5CFB), slightly lighter 1px rim, soft glow ~1.5px."
E['wardrobe.tab_selected']['pad']=3
for n in range(5):
    e=E[f'wardrobe.tab_{n}']; e['fontPx']=7.6
    e['notes']=e['notes'].replace('cap height ~6.0','cap height ~5.2-5.8 (H/K/S/M), x-height ~4.3 -> fontPx ~7.6')
setb('wardrobe.tab_sep_0',[372.0,58.0,373.1,76.8])
setb('wardrobe.tab_sep_1',[416.0,58.0,417.2,76.8])
setb('wardrobe.tab_sep_2',[459.4,58.0,460.6,76.8])
for n in range(3):
    E[f'wardrobe.tab_sep_{n}']['notes']="~1px vertical divider between unselected segments (slightly lighter than bar, ~#3A485C); spans y 58-76.8 (not the full bar height)."
# --- grid
cols=[(279.3,331.0),(336.8,388.0),(393.7,445.0),(450.0,502.0)]
rws=[(89.0,142.0),(149.0,202.0),(208.0,259.0)]
for i in range(12):
    x0,x1=cols[i%4]; y0,y1=rws[i//4]
    setb(f'wardrobe.tile_{i}',[x0,y0,x1,y1])
    E[f'wardrobe.tile_{i}']['radius']=4.0
    E[f'wardrobe.tile_{i}']['notes']=E[f'wardrobe.tile_{i}']['notes'].replace("1px light slate border (#5F6D82)","1-2px light slate border (#5A6678..#627085), 1px darker inner rim, radius ~4")
for b in range(12):
    E[f'wardrobe.tile_{b}_badge']['color']="#E4FBFF"
# refined item boxes
img={0:[289,98.5,322,138],2:[402,97,436,137],9:[346,217,379,252]}
for k,v in img.items(): setb(f'wardrobe.tile_{k}_image',v)
for n in (0,2,9): s['assets'][[a['name'] for a in s['assets']].index(f'tile_{n}_item')]['box']=E[f'wardrobe.tile_{n}_image']['box']
# --- bottom
setb('wardrobe.swatch_bar',[278.2,265.5,386.4,293.4])
E['wardrobe.swatch_bar']['notes']="Neutral dark-grey glass bar (sampled #40404B, over warm floor #676067..#746566) holding 5 colour swatches; darker 1px rim at right/bottom edge."
json.dump(s,open('fixed_stage1.json','w',encoding='utf-8'),ensure_ascii=False,indent=1)
print('ok')
