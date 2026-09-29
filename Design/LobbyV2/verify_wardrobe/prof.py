import json,sys,numpy as np
from m import *
s=json.load(open('work.json',encoding='utf-8'))
for e in s['elements']:
    if e['kind']!='text': continue
    x0,y0,x1,y1=e['box']
    X0,X1=int(np.floor(x0)),int(np.ceil(x1))
    Y0,Y1=int(np.floor(y0))-2,int(np.ceil(y1))+2
    sub=L[Y0:Y1,X0:X1]
    bg=np.percentile(sub,20); ink=np.percentile(sub,98)
    c=np.clip((sub-bg)/(ink-bg),0,1)
    print(e['id'],e['box'],e.get('fontPx'),'bg',int(bg),'ink',int(ink))
    print('   ', ' '.join(f"{y}:{c[y-Y0].max():.2f}/{(c[y-Y0]>0.5).mean():.2f}" for y in range(Y0,Y1)))
