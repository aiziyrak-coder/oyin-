"""ov.py x0 y0 x1 y1 out.png [scale] : zoomed crop with spec boxes (1px lines, exact sub-pixel positions)."""
import sys, json
from PIL import Image, ImageDraw
base = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2"
x0, y0, x1, y1 = map(float, sys.argv[1:5]); out = sys.argv[5]; s = int(sys.argv[6]) if len(sys.argv) > 6 else 8
im = Image.open(base + r"\pages\business.png").convert("RGB")
c = im.crop((int(x0), int(y0), int(x1), int(y1)))
z = c.resize((c.width * s, c.height * s), Image.NEAREST)
d = ImageDraw.Draw(z)
spec = json.load(open(base + r"\spec\business.json", encoding="utf-8"))
cols = [(255, 60, 60), (60, 255, 90), (60, 200, 255), (255, 220, 40), (255, 80, 255)]
for i, e in enumerate(spec["elements"]):
    b = e["box"]
    if b[2] < x0 or b[0] > x1 or b[3] < y0 or b[1] > y1 or e["id"] in ("business.left_scrim", "nav.bar"):
        continue
    r = [(b[0] - int(x0)) * s, (b[1] - int(y0)) * s, (b[2] - int(x0)) * s, (b[3] - int(y0)) * s]
    d.rectangle(r, outline=cols[i % 5])
z.save(out); print(out, z.size)
