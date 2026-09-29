import sys, json, numpy as np
from PIL import Image, ImageDraw
HERE = r"C:/Users/alocomputers/AppData/Local/Temp/claude/D--Game1/4f1a5cc1-c33d-4082-8691-871fd7139403/scratchpad/v2"
spec = json.load(open(sys.argv[1], encoding='utf-8'))
out = sys.argv[2]; x0, y0, x1, y1 = map(int, sys.argv[3:7]); s = int(sys.argv[7])
im = Image.open(HERE + '/pages/home.png').convert('RGB')
W, H = im.size
S = 4
m = Image.new('L', (W * S, H * S), 0); d = ImageDraw.Draw(m)
for p in spec['removePolygons']:
    pts = [(x * S, y * S) for x, y in p['points']]
    d.polygon(pts, fill=255)
    g = float(p.get('pad', 3))
    d.line(pts + [pts[0]], fill=255, width=int(g * 2 * S), joint='curve')
m = m.resize((W, H), Image.BOX).point(lambda v: 255 if v > 20 else 0)
c = im.crop((x0, y0, x1, y1)).resize(((x1 - x0) * s, (y1 - y0) * s), Image.LANCZOS)
mc = m.crop((x0, y0, x1, y1)).resize(((x1 - x0) * s, (y1 - y0) * s), Image.NEAREST)
a = np.asarray(c).astype(float); mm = np.asarray(mc) > 0
# draw only mask boundary in magenta
edge = mm ^ np.roll(mm, 1, 0) | mm ^ np.roll(mm, 1, 1)
a[edge] = [255, 0, 255]
o = Image.fromarray(a.astype(np.uint8)); dr = ImageDraw.Draw(o)
for gx in range(x0, x1):
    if gx % 10 == 0: dr.line([((gx - x0) * s, 0), ((gx - x0) * s, o.height)], fill=(0, 255, 255)); dr.text(((gx - x0) * s + 2, 2), str(gx), fill=(0, 255, 255))
for gy in range(y0, y1):
    if gy % 10 == 0: dr.line([(0, (gy - y0) * s), (o.width, (gy - y0) * s)], fill=(255, 255, 0)); dr.text((2, (gy - y0) * s + 2), str(gy), fill=(255, 255, 0))
o.save(out); print(out, o.size)
