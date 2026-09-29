"""Region overlay: python rov.py spec.json out.png X0 Y0 X1 Y1 [--scale 8] [--nearest] [--ids a,b] [--nolabel]
Draws spec boxes (exact float edges) over an enlarged crop, grid every 5 px (labels every 10)."""
import json, sys, os
import numpy as np
from PIL import Image, ImageDraw, ImageFont

PAGE = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\shops.png"
args = sys.argv[1:]
spec = json.load(open(args[0], encoding="utf-8"))
out = args[1]
x0, y0, x1, y1 = map(float, args[2:6])
scale = 8
nearest = "--nearest" in args
nolabel = "--nolabel" in args
ids = None
if "--scale" in args:
    scale = int(args[args.index("--scale") + 1])
if "--ids" in args:
    ids = args[args.index("--ids") + 1].split(",")
im = Image.open(PAGE).convert("RGB")
X0, Y0, X1, Y1 = int(x0), int(y0), int(np.ceil(x1)), int(np.ceil(y1))
c = im.crop((X0, Y0, X1, Y1))
z = c.resize((c.width * scale, c.height * scale), Image.NEAREST if nearest else Image.LANCZOS)
d = ImageDraw.Draw(z, "RGBA")
f = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 12)
for gx in range(X0 - X0 % 5 + 5, X1, 5):
    X = (gx - X0) * scale
    d.line([(X, 0), (X, z.height)], fill=(0, 255, 255, 60 if gx % 10 else 110))
    if gx % 10 == 0:
        d.text((X + 2, 2), str(gx), fill=(0, 255, 255, 255), font=f)
for gy in range(Y0 - Y0 % 5 + 5, Y1, 5):
    Y = (gy - Y0) * scale
    d.line([(0, Y), (z.width, Y)], fill=(255, 255, 0, 60 if gy % 10 else 110))
    if gy % 10 == 0:
        d.text((2, Y + 2), str(gy), fill=(255, 255, 0, 255), font=f)
pal = [(255, 60, 60), (60, 255, 90), (255, 80, 255), (255, 200, 40), (60, 160, 255), (40, 230, 230)]
for i, e in enumerate(spec["elements"]):
    if ids and not any(e["id"] == k or e["id"].startswith(k) for k in ids):
        continue
    b = e["box"]
    if b[2] < x0 or b[0] > x1 or b[3] < y0 or b[1] > y1:
        continue
    col = pal[i % len(pal)]
    r = [(b[0] - X0) * scale, (b[1] - Y0) * scale, (b[2] - X0) * scale, (b[3] - Y0) * scale]
    d.rectangle(r, outline=col + (255,), width=1)
    if not nolabel:
        d.text((r[0] + 2, r[1] - 14), e["id"].split(".")[-1], fill=col + (255,), font=f)
z.save(out)
print(out, z.size)
