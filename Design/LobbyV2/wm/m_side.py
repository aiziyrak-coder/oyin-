from ink import *
rows = [(115.8,145.5),(147.3,179.5),(180.5,212.5),(213.5,245.5),(246.5,278.5),(279.5,311.5),(312.5,345.5),(346.5,378.6)]
for i,(t,b) in enumerate(rows):
    y0=int(t)+2; y1=int(b)-1
    ic = ink(22,y0,48,y1, bgq=0.4)
    lb = ink(52,y0,125,y1, bgq=0.4)
    print(i, 'icon', ic['box'], ic['color'], 'label', lb['box'], lb['color'], lb['bg'])
    print('   rows', lb['rows'])
t = ink(15,55,160,85, bgq=0.4); print('title', t['box'], t['color'], t['rows'])
s = ink(15,86,135,103, bgq=0.4); print('sub', s['box'], s['color'], s['rows'])
