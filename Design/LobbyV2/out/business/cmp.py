"""cmp.py a.png b.png out.png x0 y0 x1 y1 [scale]  -> side by side (or stacked) zoom of two images with grid"""
import sys
from PIL import Image, ImageDraw, ImageFont
a, b, out = sys.argv[1:4]
x0, y0, x1, y1 = map(int, sys.argv[4:8])
S = int(sys.argv[8]) if len(sys.argv) > 8 else 4
f = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 12)
zs = []
for p in (a, b):
    im = Image.open(p).convert("RGB").crop((x0, y0, x1, y1))
    z = im.resize((im.width * S, im.height * S), Image.LANCZOS)
    d = ImageDraw.Draw(z, "RGBA")
    for gx in range(x0 - x0 % 10 + 10, x1, 10):
        X = (gx - x0) * S
        d.line([(X, 0), (X, z.height)], fill=(0, 255, 255, 40))
        if gx % 20 == 0: d.text((X + 2, 2), str(gx), fill=(0, 255, 255, 255), font=f)
    for gy in range(y0 - y0 % 10 + 10, y1, 10):
        Y = (gy - y0) * S
        d.line([(0, Y), (z.width, Y)], fill=(255, 255, 0, 40))
        if gy % 20 == 0: d.text((2, Y + 2), str(gy), fill=(255, 255, 0, 255), font=f)
    zs.append(z)
z0, z1 = zs
if z0.width > z0.height * 1.3:
    o = Image.new("RGB", (z0.width, z0.height * 2 + 4), (255, 255, 255)); o.paste(z0, (0, 0)); o.paste(z1, (0, z0.height + 4))
else:
    o = Image.new("RGB", (z0.width * 2 + 4, z0.height), (255, 255, 255)); o.paste(z0, (0, 0)); o.paste(z1, (z0.width + 4, 0))
o.save(out); print(out, o.size)
