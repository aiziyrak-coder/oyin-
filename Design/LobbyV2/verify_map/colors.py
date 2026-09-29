import json, numpy as np
from PIL import Image
IM=np.asarray(Image.open(r"..\pages\map.png").convert("RGB")).astype(float)
L=IM@[0.299,0.587,0.114]
spec=json.load(open('work.json',encoding='utf-8'))
for e in spec['elements']:
    if e['kind'] not in ('text','icon','dot','line'): continue
    b=e['box']; x0,y0,x1,y1=int(np.floor(b[0])),int(np.floor(b[1])),int(np.ceil(b[2])),int(np.ceil(b[3]))
    c=IM[y0:y1,x0:x1].reshape(-1,3); l=L[y0:y1,x0:x1].reshape(-1)
    if e['kind']=='dot':
        sat=c.max(1)-c.min(1); sel=sat>=np.percentile(sat,85)
    else:
        sel=l>=np.percentile(l,92)
    med=np.median(c[sel],0).astype(int); mx=c[np.argmax(l)].astype(int)
    print(f"{e['id']:28s} spec {e.get('color')}  core92 #{med[0]:02X}{med[1]:02X}{med[2]:02X}  max #{mx[0]:02X}{mx[1]:02X}{mx[2]:02X}")
