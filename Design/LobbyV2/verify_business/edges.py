"""edges.py x0 y0 x1 y1 [--chan L] [--span 0.3,0.7] [--r 5]
Averaged perpendicular profiles across each edge of an approximate box. Prints the profile (pos:value) so edges can
be read sub-pixel; also a coverage estimate between outside (far) and inside (near) levels."""
import sys, numpy as np
from PIL import Image
a = sys.argv[1:]
def opt(n, d=None):
    if n in a:
        i = a.index(n); v = a[i + 1]; del a[i:i + 2]; return v
    return d
chan = opt("--chan", "L"); span = list(map(float, opt("--span", "0.3,0.7").split(","))); R = int(opt("--r", 5))
x0, y0, x1, y1 = map(float, a[:4])
im = np.asarray(Image.open(r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\business.png").convert("RGB")).astype(float)
if chan == "L": V = im @ np.array([.299, .587, .114])
elif chan == "S": V = im.max(2) - im.min(2)
elif chan == "BR": V = im[..., 2] - im[..., 0]
else: V = im[..., "RGB".index(chan)]
H, W = V.shape
def prof(axis, pos, lo, hi):
    c = int(round(pos)); rng = range(max(0, c - R), min((W if axis == 0 else H), c + R + 1))
    if axis == 0:  # vertical edge at x=pos, average rows lo..hi
        vals = [V[int(lo):int(hi), x].mean() for x in rng]
    else:
        vals = [V[y, int(lo):int(hi)].mean() for y in rng]
    return list(rng), vals
def cov(ps, vs, inside_positive):
    vs = np.array(vs); n = len(vs)
    out = vs[:2].mean() if inside_positive else vs[-2:].mean()
    inn = vs[-2:].mean() if inside_positive else vs[:2].mean()
    f = np.clip((vs - out) / (inn - out + 1e-9), 0, 1)
    if inside_positive:
        return ps[-1] + 1 - f.sum()
    else:
        return ps[0] + f.sum()
ly, hy = y0 + (y1 - y0) * span[0], y0 + (y1 - y0) * span[1]
lx, hx = x0 + (x1 - x0) * span[0], x0 + (x1 - x0) * span[1]
for name, axis, pos, lo, hi, ins in (("left", 0, x0, ly, hy, True), ("right", 0, x1, ly, hy, False),
                                      ("top", 1, y0, lx, hx, True), ("bottom", 1, y1, lx, hx, False)):
    ps, vs = prof(axis, pos, lo, hi)
    print(f"{name:6s} " + " ".join(f"{p}:{v:.0f}" for p, v in zip(ps, vs)) + f"   cov-edge={cov(ps, vs, ins):.2f}")
