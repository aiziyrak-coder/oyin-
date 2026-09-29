"""mview.py mask.png out.png X0 Y0 X1 Y1 [scale] -> page crop enlarged (nearest), masked pixels dimmed to 25%, mask boundary in red."""
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont
PAGE = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\map.png"
m = np.asarray(Image.open(sys.argv[1]).convert('L')) > 127
out = sys.argv[2]
x0, y0, x1, y1 = map(int, sys.argv[3:7])
s = int(sys.argv[7]) if len(sys.argv) > 7 else 6
im = np.asarray(Image.open(PAGE).convert('RGB')).astype(float)
c = im[y0:y1, x0:x1].copy()
mm = m[y0:y1, x0:x1]
c[mm] = c[mm] * 0.2 + np.array([0, 0, 60]) * 0.8
edge = mm & ~(np.roll(mm, 1, 0) & np.roll(mm, -1, 0) & np.roll(mm, 1, 1) & np.roll(mm, -1, 1))
c[edge] = [255, 0, 0]
z = Image.fromarray(c.astype(np.uint8)).resize(((x1 - x0) * s, (y1 - y0) * s), Image.NEAREST)
d = ImageDraw.Draw(z, 'RGBA')
f = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 12)
for gx in range(x0 - x0 % 10 + 10, x1, 10):
    d.line([((gx - x0) * s, 0), ((gx - x0) * s, z.height)], fill=(0, 255, 255, 70))
    d.text(((gx - x0) * s + 2, 2), str(gx), fill=(0, 255, 255, 255), font=f)
for gy in range(y0 - y0 % 10 + 10, y1, 10):
    d.line([(0, (gy - y0) * s), (z.width, (gy - y0) * s)], fill=(255, 255, 0, 70))
    d.text((2, (gy - y0) * s + 2), str(gy), fill=(255, 255, 0, 255), font=f)
z.save(out)
print(out, z.size)
