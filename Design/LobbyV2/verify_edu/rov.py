"""rov.py spec.json out.png X0 Y0 X1 Y1 [--scale 8] [--ids a,b] [--nobox]
Crop region of education.png, enlarge (nearest or lanczos), draw grid every px (faint) / 5px / 10px labelled, draw element boxes."""
import sys, json
from PIL import Image, ImageDraw, ImageFont
P = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\education.png"
a = sys.argv[1:]
def opt(n, d=None):
    if n in a:
        i = a.index(n); v = a[i+1]; del a[i:i+2]; return v
    return d
sc = int(opt("--scale", 8)); ids = opt("--ids"); near = "--near" in a
if near: a.remove("--near")
nobox = "--nobox" in a
if nobox: a.remove("--nobox")
spec, out = a[0], a[1]; x0, y0, x1, y1 = map(int, a[2:6])
im = Image.open(P).convert("RGB").crop((x0, y0, x1, y1))
z = im.resize((im.width*sc, im.height*sc), Image.NEAREST if near else Image.LANCZOS).convert("RGBA")
ov = Image.new("RGBA", z.size, (0,0,0,0)); d = ImageDraw.Draw(ov)
try: f = ImageFont.truetype("arial.ttf", 11)
except: f = ImageFont.load_default()
for gx in range(x0, x1+1):
    X = (gx-x0)*sc
    col = (255,255,0,110) if gx % 10 == 0 else ((255,255,255,50) if gx % 5 == 0 else (255,255,255,18))
    d.line([(X,0),(X,z.height)], fill=col)
    if gx % 10 == 0: d.text((X+2, 1), str(gx), fill=(255,255,0,255), font=f)
for gy in range(y0, y1+1):
    Y = (gy-y0)*sc
    col = (255,255,0,110) if gy % 10 == 0 else ((255,255,255,50) if gy % 5 == 0 else (255,255,255,18))
    d.line([(0,Y),(z.width,Y)], fill=col)
    if gy % 10 == 0: d.text((1, Y+1), str(gy), fill=(255,255,0,255), font=f)
if not nobox:
    s = json.load(open(spec, encoding="utf-8"))
    cols = [(255,60,60),(60,255,60),(80,160,255),(255,0,255),(0,255,255),(255,160,0)]
    k = 0
    for e in s["elements"]:
        b = e["box"]
        if ids and not any(e["id"].startswith(p) for p in ids.split(",")): continue
        if b[2] < x0 or b[0] > x1 or b[3] < y0 or b[1] > y1: continue
        c = cols[k % len(cols)]; k += 1
        r = [(b[0]-x0)*sc, (b[1]-y0)*sc, (b[2]-x0)*sc, (b[3]-y0)*sc]
        d.rectangle(r, outline=c+(255,), width=1)
        d.text((r[0]+2, r[3]+1), e["id"].split(".")[-1], fill=c+(255,), font=f)
z = Image.alpha_composite(z, ov).convert("RGB"); z.save(out); print(out, z.size)
