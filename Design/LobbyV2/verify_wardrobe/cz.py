# contrast-stretched zoom with 1px grid: cz.py x0 y0 x1 y1 scale out [lo hi]
import sys, numpy as np
from PIL import Image, ImageDraw, ImageFont
a=sys.argv[1:]
x0,y0,x1,y1,sc=map(int,a[:5]); out=a[5]
page=a[8] if len(a)>8 else 'wardrobe'
im=np.asarray(Image.open(rf"C:/Users/alocomputers/AppData/Local/Temp/claude/D--Game1/4f1a5cc1-c33d-4082-8691-871fd7139403/scratchpad/v2/pages/{page}.png").convert('RGB')).astype(np.float32)
c=im[y0:y1,x0:x1]
lo=float(a[6]) if len(a)>6 else np.percentile(c,1); hi=float(a[7]) if len(a)>7 else np.percentile(c,99)
c=np.clip((c-lo)/(hi-lo)*255,0,255).astype(np.uint8)
z=Image.fromarray(c).resize(((x1-x0)*sc,(y1-y0)*sc),Image.NEAREST)
d=ImageDraw.Draw(z,'RGBA'); f=ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf",max(10,sc*2))
for gx in range(x0,x1):
    X=(gx-x0)*sc
    d.line([(X,0),(X,z.height)],fill=(0,255,255,150 if gx%10==0 else (60 if gx%5==0 else 22)))
    if gx%10==0: d.text((X+2,2),str(gx),fill=(0,255,255,255),font=f)
for gy in range(y0,y1):
    Y=(gy-y0)*sc
    d.line([(0,Y),(z.width,Y)],fill=(255,255,0,150 if gy%10==0 else (60 if gy%5==0 else 22)))
    if gy%10==0: d.text((2,Y+2),str(gy),fill=(255,255,0,255),font=f)
z.save(out); print(out,z.size)
