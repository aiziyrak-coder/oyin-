"""enh.py x0 y0 x1 y1 out scale lo hi [spec] : contrast-stretched nearest zoom (L in [lo,hi] -> 0..255) with grid and optional spec boxes"""
import sys, json, numpy as np
from PIL import Image, ImageDraw, ImageFont
a=sys.argv[1:]
x0,y0,x1,y1=map(int,a[:4]); out=a[4]; s=int(a[5]); lo=float(a[6]); hi=float(a[7]); sp=a[8] if len(a)>8 else None
im=np.asarray(Image.open(r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\business.png").convert('RGB')).astype(float)
r=im[y0:y1,x0:x1]
r=np.clip((r-lo)/(hi-lo)*255,0,255).astype(np.uint8)
z=Image.fromarray(r).resize(((x1-x0)*s,(y1-y0)*s),Image.NEAREST)
d=ImageDraw.Draw(z,'RGBA')
f=ImageFont.truetype('arial.ttf',11)
for gx in range(x0,x1+1):
    if gx%5==0:
        d.line([((gx-x0)*s,0),((gx-x0)*s,z.height)],fill=(255,255,0,110 if gx%10==0 else 50))
        if gx%10==0: d.text(((gx-x0)*s+1,1),str(gx),fill=(255,255,0,255),font=f)
for gy in range(y0,y1+1):
    if gy%5==0:
        d.line([(0,(gy-y0)*s),(z.width,(gy-y0)*s)],fill=(255,255,0,110 if gy%10==0 else 50))
        if gy%10==0: d.text((1,(gy-y0)*s+1),str(gy),fill=(255,255,0,255),font=f)
if sp:
    spec=json.load(open(sp,encoding='utf-8'))
    for i,e in enumerate(spec['elements']):
        b=e['box']
        if e['id'] in ('business.left_scrim','nav.bar'): continue
        if b[2]<x0 or b[0]>x1 or b[3]<y0 or b[1]>y1: continue
        d.rectangle([round((b[0]-x0)*s),round((b[1]-y0)*s),round((b[2]-x0)*s),round((b[3]-y0)*s)],outline=[(255,60,60,255),(60,255,90,255),(60,200,255,255),(255,80,255,255)][i%4])
z.save(out); print(out,z.size)
