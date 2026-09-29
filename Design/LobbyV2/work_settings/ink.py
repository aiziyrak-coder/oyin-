"""ink.py x0 y0 x1 y1 [--thr T] [--mode bright|dark|diff] [--split GAP]
Tight ink box of content inside region (1x coords) against the region's border background.
Prints: bg color, bbox (sub-pixel-ish via coverage), core color, column segments."""
import sys, json
import numpy as np
from PIL import Image

P = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\settings.png"
im = np.asarray(Image.open(P).convert("RGB")).astype(np.float32)
lum = im @ np.array([0.299, 0.587, 0.114], dtype=np.float32)


def analyze(x0, y0, x1, y1, thr=25.0, mode="bright", split=2, verbose=True):
    sub = im[y0:y1, x0:x1]
    L = lum[y0:y1, x0:x1]
    border = np.concatenate([L[0, :], L[-1, :], L[:, 0], L[:, -1]])
    bcol = np.concatenate([sub[0, :], sub[-1, :], sub[:, 0], sub[:, -1]])
    bg = np.median(border)
    bgc = np.median(bcol, axis=0)
    if mode == "bright":
        d = L - bg
    elif mode == "dark":
        d = bg - L
    else:
        d = np.abs(sub - bgc).sum(axis=2) / 1.5
    m = d > thr
    if not m.any():
        print("no ink");
        return None
    ys, xs = np.nonzero(m)
    bx0, bx1, by0, by1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
    # half-coverage refinement: fraction of max diff at the edge rows/cols
    dmax = np.percentile(d[m], 90)
    colcov = np.clip(d, 0, None).max(axis=0) / dmax
    rowcov = np.clip(d, 0, None).max(axis=1) / dmax
    fx0 = bx0 + (1 - min(1, colcov[bx0]))
    fx1 = bx1 - (1 - min(1, colcov[bx1 - 1]))
    fy0 = by0 + (1 - min(1, rowcov[by0]))
    fy1 = by1 - (1 - min(1, rowcov[by1 - 1]))
    core = d > np.percentile(d[m], 70)
    core &= m
    cc = np.median(sub[core], axis=0)
    # column segments
    cols = m.any(axis=0)
    segs = []
    x = 0
    W = cols.size
    while x < W:
        if cols[x]:
            s = x
            gap = 0
            while x < W and gap < split:
                if cols[x]:
                    gap = 0
                    e = x
                else:
                    gap += 1
                x += 1
            segs.append((s + x0, e + 1 + x0))
        else:
            x += 1
    rows = m.any(axis=1)
    rprof = m.sum(axis=1)
    res = dict(bg="#%02X%02X%02X" % tuple(int(round(v)) for v in bgc), bgL=round(float(bg), 1),
               box=[int(bx0 + x0), int(by0 + y0), int(bx1 + x0), int(by1 + y0)],
               fbox=[round(float(fx0 + x0), 1), round(float(fy0 + y0), 1), round(float(fx1 + x0), 1), round(float(fy1 + y0), 1)],
               color="#%02X%02X%02X" % tuple(int(round(v)) for v in cc), peak="#%02X%02X%02X" % tuple(int(round(v)) for v in np.median(sub[m & (d >= np.percentile(d[m], 92))], axis=0)), segs=segs)
    if verbose:
        print(json.dumps(res))
        print("row profile:", {int(y0 + i): int(v) for i, v in enumerate(rprof) if v})
    return res


if __name__ == "__main__":
    a = sys.argv[1:]
    thr = 25.0; mode = "bright"; split = 2
    if "--thr" in a:
        i = a.index("--thr"); thr = float(a[i + 1]); del a[i:i + 2]
    if "--mode" in a:
        i = a.index("--mode"); mode = a[i + 1]; del a[i:i + 2]
    if "--split" in a:
        i = a.index("--split"); split = int(a[i + 1]); del a[i:i + 2]
    x0, y0, x1, y1 = map(int, a[:4])
    analyze(x0, y0, x1, y1, thr, mode, split)

