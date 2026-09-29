"""ov.py spec.json x0 y0 x1 y1 out.png [scale] [--ids a,b] : nearest-zoomed crop with 1px grid every 1px (faint) / 10px (strong) and spec boxes."""
import sys, json
from PIL import Image, ImageDraw, ImageFont
base = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2"
a = sys.argv[1:]
ids = None
if "--ids" in a:
    i = a.index("--ids"); ids = a[i+1].split(","); del a[i:i+2]
sp = a[0]; x0, y0, x1, y1 = map(float, a[1:5]); out = a[5]; s = int(a[6]) if len(a) > 6 else 8
im = Image.open(base + r"\pages\business.png").convert("RGB")
X0, Y0 = int(x0), int(y0)
c = im.crop((X0, Y0, int(x1), int(y1)))
z = c.resize((c.width * s, c.height * s), Image.NEAREST)
d = ImageDraw.Draw(z, "RGBA")
for gx in range(X0, int(x1) + 1):
    X = (gx - X0) * s
    d.line([(X, 0), (X, z.height)], fill=(255, 255, 255, 90 if gx % 10 == 0 else 18))
for gy in range(Y0, int(y1) + 1):
    Y = (gy - Y0) * s
    d.line([(0, Y), (z.width, Y)], fill=(255, 255, 255, 90 if gy % 10 == 0 else 18))
try:
    f = ImageFont.truetype("arial.ttf", 11)
except Exception:
    f = None
for gx in range(X0, int(x1) + 1):
    if gx % (10 if s < 10 else 5) == 0:
        d.text(((gx - X0) * s + 1, 1), str(gx), fill=(255, 255, 0, 255), font=f)
for gy in range(Y0, int(y1) + 1):
    if gy % (10 if s < 10 else 5) == 0:
        d.text((1, (gy - Y0) * s + 1), str(gy), fill=(255, 255, 0, 255), font=f)
spec = json.load(open(sp, encoding="utf-8"))
cols = [(255, 60, 60), (60, 255, 90), (60, 200, 255), (255, 220, 40), (255, 80, 255)]
for i, e in enumerate(spec["elements"]):
    b = e["box"]
    if ids and e["id"] not in ids: continue
    if b[2] < x0 or b[0] > x1 or b[3] < y0 or b[1] > y1 or e["id"] in ("business.left_scrim", "nav.bar"):
        continue
    r = [round((b[0] - X0) * s), round((b[1] - Y0) * s), round((b[2] - X0) * s), round((b[3] - Y0) * s)]
    d.rectangle(r, outline=cols[i % 5] + (255,))
z.save(out); print(out, z.size)
