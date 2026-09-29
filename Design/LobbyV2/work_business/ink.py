"""ink.py x0 y0 x1 y1 [--frac 0.5] [--dark] [--bg auto|R,G,B] [--proj] [--chan L|R|G|B|S]
Ink bbox of bright (or dark) marks inside a region, relative to region-border background.
Prints bbox (pixel-edge coords, 1x), core colour, and optional column/row projections."""
import sys, numpy as np
from PIL import Image

PAGE = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\business.png"
a = sys.argv[1:]
def opt(n, d=None):
    if n in a:
        i = a.index(n); v = a[i + 1]; del a[i:i + 2]; return v
    return d
def flag(n):
    if n in a:
        a.remove(n); return True
    return False
frac = float(opt("--frac", 0.5)); dark = flag("--dark"); proj = flag("--proj"); chan = opt("--chan", "L")
bgopt = opt("--bg", "auto")
x0, y0, x1, y1 = map(int, a[:4])
im = np.asarray(Image.open(PAGE).convert("RGB")).astype(np.float32)
reg = im[y0:y1, x0:x1]
if chan == "L":
    L = reg @ np.array([0.299, 0.587, 0.114], np.float32)
elif chan == "S":
    L = reg.max(2) - reg.min(2)
else:
    L = reg[..., "RGB".index(chan)]
if bgopt == "auto":
    ring = np.concatenate([L[0], L[-1], L[:, 0], L[:, -1]])
    bg = np.median(ring)
    ringc = np.concatenate([reg[0], reg[-1], reg[:, 0], reg[:, -1]])
    bgc = np.median(ringc, 0)
else:
    bgc = np.array(list(map(float, bgopt.split(","))))
    bg = float(bgc @ np.array([0.299, 0.587, 0.114])) if chan == "L" else 0
c = (bg - L) if dark else (L - bg)
mx = np.percentile(c, 99.5)
m = c > frac * mx
ys, xs = np.nonzero(m)
if len(xs) == 0:
    print("no ink"); sys.exit()
print(f"bg={bg:.1f} bgRGB=#{int(bgc[0]):02X}{int(bgc[1]):02X}{int(bgc[2]):02X} max contrast={mx:.1f}")
print(f"bbox = [{x0 + xs.min()}, {y0 + ys.min()}, {x0 + xs.max() + 1}, {y0 + ys.max() + 1}]  (w={xs.max()-xs.min()+1} h={ys.max()-ys.min()+1})")
def edge(prof, off):
    p = np.clip(prof / mx, 0, 1.0)
    idx = np.nonzero(p > frac)[0]
    i0, i1 = idx.min(), idx.max()
    lead = i0 - (p[i0 - 1] if i0 > 0 and p[i0 - 1] >= 0.2 else 0)
    trail = i1 + 1 + (p[i1 + 1] if i1 < len(p) - 1 and p[i1 + 1] >= 0.2 else 0)
    return off + lead, off + trail
cprof = np.where(m.any(0) | True, c.max(0), 0); rprof = c.max(1)
ex0, ex1 = edge(cprof, x0); ey0, ey1 = edge(rprof, y0)
print(f"sub  = [{ex0:.1f}, {ey0:.1f}, {ex1:.1f}, {ey1:.1f}]")
core = c > 0.85 * mx
col = np.median(reg[core], 0)
print("core colour #%02X%02X%02X  n=%d" % (int(col[0]), int(col[1]), int(col[2]), core.sum()))
# brightest-25% colour
if proj:
    cp = m.sum(0); rp = m.sum(1)
    print("cols:", " ".join(f"{x0+i}:{v}" for i, v in enumerate(cp)))
    print("rows:", " ".join(f"{y0+i}:{v}" for i, v in enumerate(rp)))
