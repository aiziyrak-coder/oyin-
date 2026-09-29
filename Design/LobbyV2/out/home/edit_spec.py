import json
s=json.load(open('mask_spec.json',encoding='utf-8'))
for e in s['elements']:
    if e['id']=='home.kirish':
        e['box'][2]=226.0
        e['notes_mask']='mask copy: right edge pulled in to 226 (+pad 8 = 234) so the lit pillar at x 230-242 stays as context; blue halo on that side ends at ~233'
ids={e['id'] for e in s['elements']}
extra=[
 {"id":"home.wordmark_stray","kind":"text","box":[50,63,56,72],"remove":True,"pad":1,"notes":"faint garbled glyph left of the spaced wordmark"},
 {"id":"edge.bottom_border","kind":"line","box":[0,413.2,766,415],"remove":True,"pad":0,"notes":"1 px dark composite border row (y=414) - not art"},
 {"id":"edge.right_border","kind":"line","box":[763.6,0,766,415],"remove":True,"pad":0,"notes":"light/dark composite border fringe cols 764-765 - not art"},
]
for x in extra:
    if x['id'] not in ids: s['elements'].append(x)
json.dump(s,open('mask_spec.json','w',encoding='utf-8'),ensure_ascii=False,indent=1)
