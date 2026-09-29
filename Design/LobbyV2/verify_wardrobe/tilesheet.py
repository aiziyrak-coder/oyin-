import json, numpy as np, sys
from PIL import Image, ImageDraw, ImageFont
im=np.asarray(Image.open(r"C:/Users/alocomputers/AppData/Local/Temp/claude/D--Game1/4f1a5cc1-c33d-4082-8691-871fd7139403/scratchpad/v2/pages/wardrobe.png").convert('RGB')).astype(np.float32)
s=json.load(open(sys.argv[1],encoding='utf-8')); E={e['id']:e for e in s['elements']}
S=6; ids=[int(v) for v in sys.argv[3].split(',')] if len(sys.argv)>3 else list(range(12))
f=ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf",12)
tiles=[]
for i in ids:
    t=E[f'wardrobe.tile_{i}']['box']
    X0,Y0,X1,Y1=int(t[0])-2,int(t[1])-2,int(np.ceil(t[2]))+2,int(np.ceil(t[3]))+2
    c=im[Y0:Y1,X0:X1]; lo=np.percentile(c,1); hi=np.percentile(c,99.5)
    c=np.clip((c-lo)/(hi-lo)*255,0,255)
    c=np.clip(255*(c/255)**0.6,0,255).astype(np.uint8)
    z=Image.fromarray(c).resize(((X1-X0)*S,(Y1-Y0)*S),Image.NEAREST)
    d=ImageDraw.Draw(z,'RGBA')
    for gx in range(X0,X1):
        if gx%5==0: d.line([((gx-X0)*S,0),((gx-X0)*S,z.height)],fill=(0,255,255,110 if gx%10==0 else 40))
        if gx%10==0: d.text(((gx-X0)*S+2,2),str(gx),fill=(0,255,255,255),font=f)
    for gy in range(Y0,Y1):
        if gy%5==0: d.line([(0,(gy-Y0)*S),(z.width,(gy-Y0)*S)],fill=(255,255,0,110 if gy%10==0 else 40))
        if gy%10==0: d.text((2,(gy-Y0)*S+2),str(gy),fill=(255,255,0,255),font=f)
    for k,col in ((f'wardrobe.tile_{i}_image',(255,60,60,255)),(f'wardrobe.tile_{i}',(60,255,60,255)),(f'wardrobe.tile_{i}_badge',(255,0,255,255))):
        b=E[k]['box']; d.rectangle([(b[0]-X0)*S,(b[1]-Y0)*S,(b[2]-X0)*S,(b[3]-Y0)*S],outline=col)
    d.text((4,z.height-16),f'tile {i}',fill=(255,255,255,255),font=f)
    tiles.append(z)
W=sum(t.width for t in tiles[:4]); 
n=len(tiles); per=min(4,n)
rowsN=(n+per-1)//per
H=max(t.height for t in tiles)
sheet=Image.new('RGB',(max(t.width for t in tiles)*per,H*rowsN),(0,0,0))
for k,t in enumerate(tiles):
    sheet.paste(t,((k%per)*tiles[0].width,(k//per)*H))
sheet.save(sys.argv[2]); print(sheet.size)
