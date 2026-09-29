"""ov.py spec.json out.png X0 Y0 X1 Y1 [--scale 8] [--ids a,b,c] [--nolabel]
Crop region of the settings page, enlarge (nearest-ish lanczos), draw 1px grid every 1 px (faint) and every 10 px (labelled),
and draw spec boxes (continuous coords: pixel i spans [i,i+1]) of elements intersecting the region."""
import sys, json
from PIL import Image, ImageDraw, ImageFont

P = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\settings.png"
a = sys.argv[1:]
def opt(n, d=None):
    if n in a:
        i = a.index(n); v = a[i + 1]; del a[i:i + 2]; return v
    return d
scale = int(opt("--scale", 8))
ids = opt("--ids")
nolabel = "--nolabel" in a
if nolabel: a.remove("--nolabel")
spec_p, out = a[0], a[1]
x0, y0, x1, y1 = map(float, a[2:6])
spec = json.load(open(spec_p, encoding="utf-8"))
im = Image.open(P).convert("RGB")
c = im.crop((int(x0), int(y0), int(x1), int(y1)))
X0, Y0 = int(x0), int(y0)
z = c.resize((c.width * scale, c.height * scale), Image.NEAREST).convert("RGBA")
ov = Image.new("RGBA", z.size, (0, 0, 0, 0))
d = ImageDraw.Draw(ov)
try:
    f = ImageFont.truetype("arial.ttf", 11)
except Exception:
    f = ImageFont.load_default()
for gx in range(X0, int(x1) + 1):
    X = (gx - X0) * scale
    d.line([(X, 0), (X, z.height)], fill=(255, 255, 0, 110 if gx % 10 == 0 else 28), width=1)
    if gx % 10 == 0:
        d.text((X + 2, 2), str(gx), fill=(255, 255, 0, 255), font=f)
for gy in range(Y0, int(y1) + 1):
    Y = (gy - Y0) * scale
    d.line([(0, Y), (z.width, Y)], fill=(255, 255, 0, 110 if gy % 10 == 0 else 28), width=1)
    if gy % 10 == 0:
        d.text((2, Y + 2), str(gy), fill=(255, 255, 0, 255), font=f)
cols = [(255, 60, 60), (60, 255, 60), (80, 160, 255), (255, 80, 255), (0, 255, 255), (255, 170, 0)]
sel = set(ids.split(",")) if ids else None
k = 0
for e in spec["elements"]:
    if sel and e["id"] not in sel:
        continue
    b = e["box"]
    if b[2] < x0 or b[0] > x1 or b[3] < y0 or b[1] > y1:
        continue
    col = cols[k % len(cols)]; k += 1
    r = [(b[0] - X0) * scale, (b[1] - Y0) * scale, (b[2] - X0) * scale, (b[3] - Y0) * scale]
    d.rectangle(r, outline=col + (255,), width=1)
    if not nolabel:
        d.text((r[0] + 1, r[3] + 1), e["id"].split(".", 1)[-1], fill=col + (255,), font=f)
z = Image.alpha_composite(z, ov).convert("RGB")
z.save(out)
print(out, z.size)
