"""zb.py img mask out x0 y0 x1 y1 [scale]
Zoom of img with the mask EDGE drawn (magenta 1px outline around masked pixels) + 10px grid; top: plain, bottom: outlined."""
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

img, mask, out = sys.argv[1:4]
x0, y0, x1, y1 = map(int, sys.argv[4:8])
S = int(sys.argv[8]) if len(sys.argv) > 8 else 6
im = Image.open(img).convert("RGB").crop((x0, y0, x1, y1))
m = np.asarray(Image.open(mask).convert("L").crop((x0, y0, x1, y1))) > 127
z = im.resize((im.width * S, im.height * S), Image.NEAREST)
z2 = z.copy()
d = ImageDraw.Draw(z2, "RGBA")
H, W = m.shape
for y in range(H):
    for x in range(W):
        if not m[y, x]:
            continue
        X, Y = x * S, y * S
        if x == 0 or not m[y, x - 1]:
            d.line([(X, Y), (X, Y + S)], fill=(255, 0, 255, 255), width=2)
        if x == W - 1 or not m[y, x + 1]:
            d.line([(X + S - 1, Y), (X + S - 1, Y + S)], fill=(255, 0, 255, 255), width=2)
        if y == 0 or not m[y - 1, x]:
            d.line([(X, Y), (X + S, Y)], fill=(255, 0, 255, 255), width=2)
        if y == H - 1 or not m[y + 1, x]:
            d.line([(X, Y + S - 1), (X + S, Y + S - 1)], fill=(255, 0, 255, 255), width=2)
try:
    f = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 12)
except Exception:
    f = ImageFont.load_default()
for zz in (z, z2):
    dd = ImageDraw.Draw(zz, "RGBA")
    for gx in range(x0 - x0 % 10 + 10, x1, 10):
        X = (gx - x0) * S
        dd.line([(X, 0), (X, zz.height)], fill=(0, 255, 255, 60))
        dd.text((X + 2, 2), str(gx), fill=(0, 255, 255, 255), font=f)
    for gy in range(y0 - y0 % 10 + 10, y1, 10):
        Y = (gy - y0) * S
        dd.line([(0, Y), (zz.width, Y)], fill=(255, 255, 0, 60))
        dd.text((2, Y + 2), str(gy), fill=(255, 255, 0, 255), font=f)
if z.width >= z.height:
    o = Image.new("RGB", (z.width, z.height * 2 + 4), (255, 255, 255))
    o.paste(z, (0, 0)); o.paste(z2, (0, z.height + 4))
else:
    o = Image.new("RGB", (z.width * 2 + 4, z.height), (255, 255, 255))
    o.paste(z, (0, 0)); o.paste(z2, (z.width + 4, 0))
o.save(out)
print(out, o.size)
