"""check_ink.py spec.json [--m 3] [--frac 0.5] [--ids a,b] [--chan L]
For text/icon/dot elements: ink bbox (subpixel, coverage-based) inside the spec box grown by m px,
contrast relative to the median of the grown region's border ring. Prints spec vs measured."""
import sys, json, numpy as np
from PIL import Image

PAGE = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\business.png"
a = sys.argv[1:]
def opt(n, d=None):
    if n in a:
        i = a.index(n); v = a[i + 1]; del a[i:i + 2]; return v
    return d
m = float(opt("--m", 3)); frac = float(opt("--frac", 0.5)); ids = opt("--ids"); chan = opt("--chan", "L")
kinds = opt("--kinds", "text,icon,dot").split(",")
ids = ids.split(",") if ids else None
spec = json.load(open(a[0], encoding="utf-8"))
im = np.asarray(Image.open(PAGE).convert("RGB")).astype(np.float32)
H, W = im.shape[:2]

def measure(box, m, frac, chan, dark=False):
    x0 = max(0, int(np.floor(box[0] - m))); y0 = max(0, int(np.floor(box[1] - m)))
    x1 = min(W, int(np.ceil(box[2] + m))); y1 = min(H, int(np.ceil(box[3] + m)))
    reg = im[y0:y1, x0:x1]
    if chan == "L":
        L = reg @ np.array([0.299, 0.587, 0.114], np.float32)
    elif chan == "S":
        L = reg.max(2) - reg.min(2)
    else:
        L = reg[..., "RGB".index(chan)]
    ring = np.concatenate([L[0], L[-1], L[:, 0], L[:, -1]])
    bg = np.median(ring)
    c = (bg - L) if dark else (L - bg)
    mx = np.percentile(c, 99.5)
    msk = c > frac * mx
    if not msk.any():
        return None
    def edges(prof, off):
        p = np.clip(prof / mx, 0, 1)
        idx = np.nonzero(p > frac)[0]
        i0, i1 = idx.min(), idx.max()
        # extend with partial coverage of neighbours (coverage ~ p/1 normalised to half-threshold)
        lead = i0 - min(1, p[i0 - 1] / frac * 0.5) if i0 > 0 else i0
        trail = i1 + 1 + (min(1, p[i1 + 1] / frac * 0.5) if i1 < len(p) - 1 else 0)
        return off + lead, off + trail
    ex0, ex1 = edges(c.max(0), x0); ey0, ey1 = edges(c.max(1), y0)
    core = c > 0.85 * mx
    col = np.median(reg[core], 0)
    return [round(ex0, 1), round(ey0, 1), round(ex1, 1), round(ey1, 1)], "#%02X%02X%02X" % tuple(int(v) for v in col), bg, mx

for e in spec["elements"]:
    if e["kind"] not in kinds: continue
    if ids and e["id"] not in ids: continue
    r = measure(e["box"], m, frac, chan)
    if r is None:
        print(e["id"], "no ink"); continue
    mb, col, bg, mx = r
    d = [mb[i] - e["box"][i] for i in range(4)]
    flag = " <<" if max(abs(v) for v in d) > 0.5 else ""
    print(f'{e["id"]:28s} spec={e["box"]} meas={mb} d={[round(v,1) for v in d]} col={col} (spec {e.get("color")}) bg={bg:.0f} c={mx:.0f}{flag}')
