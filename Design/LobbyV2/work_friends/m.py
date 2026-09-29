"""Measurement helper for friends page.
  m.py ink X0 Y0 X1 Y1 [thr] [mode]   -> tight box of pixels differing from region-border median by > thr
        mode: diff (default) | bright (lum > bg+thr) | dark (lum < bg-thr) | sat (saturation-based)
        also prints subpixel box (coverage-weighted edges) and core color
  m.py col X0 Y0 X1 Y1               -> median color of region
  m.py rows X0 Y0 X1 Y1              -> mean lum per row
  m.py cols X0 Y0 X1 Y1              -> mean lum per column
  m.py px X Y                        -> pixel color
"""
import sys
import numpy as np
from PIL import Image

IM = np.asarray(Image.open(r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\friends.png").convert("RGB")).astype(np.float32)
LUM = IM @ np.array([0.299, 0.587, 0.114])


def hexc(c):
    c = np.clip(np.round(c), 0, 255).astype(int)
    return "#%02X%02X%02X" % tuple(c)


def region(x0, y0, x1, y1):
    return IM[y0:y1, x0:x1], LUM[y0:y1, x0:x1]


def border_bg(sub):
    b = np.concatenate([sub[0], sub[-1], sub[:, 0], sub[:, -1]])
    return np.median(b, axis=0)


def ink(x0, y0, x1, y1, thr=40, mode="diff"):
    sub, lum = region(x0, y0, x1, y1)
    bg = border_bg(sub)
    bgl = float(bg @ np.array([0.299, 0.587, 0.114]))
    if mode == "diff":
        d = np.sqrt(((sub - bg) ** 2).sum(axis=2))
    elif mode == "bright":
        d = lum - bgl
    elif mode == "dark":
        d = bgl - lum
    elif mode == "green":
        d = sub[:, :, 1] - sub[:, :, 2]
    elif mode == "red":
        d = sub[:, :, 0] - sub[:, :, 2]
    elif mode == "sat":
        mx = sub.max(axis=2); mn = sub.min(axis=2)
        d = mx - mn
    m = d > thr
    if not m.any():
        print("no ink"); return
    ys, xs = np.nonzero(m)
    bx = tuple(int(v) for v in (x0 + xs.min(), y0 + ys.min(), x0 + xs.max() + 1, y0 + ys.max() + 1))
    # subpixel: coverage per column/row = max d / peak
    peak = np.percentile(d[m], 90)
    cov = np.clip(d / max(peak, 1), 0, 1)
    colc = cov.max(axis=0); rowc = cov.max(axis=1)
    def edge(arr, start):
        # first index where coverage>0.5, refine with fraction
        idx = np.nonzero(arr > 0.5)[0]
        return idx
    ci = np.nonzero(colc > 0.5)[0]; ri = np.nonzero(rowc > 0.5)[0]
    sb = tuple(int(v) for v in (x0 + ci.min(), y0 + ri.min(), x0 + ci.max() + 1, y0 + ri.max() + 1))
    def frac(arr):
        idx = np.nonzero(arr > 0.5)[0]
        i0, i1 = idx.min(), idx.max()
        # left edge: interpolate between i0-1 and i0
        a = arr[i0 - 1] if i0 > 0 else 0.0
        lo = i0 + (0.5 - a) / max(arr[i0] - a, 1e-6) - 0.5
        b = arr[i1 + 1] if i1 + 1 < len(arr) else 0.0
        hi = i1 + 0.5 + (arr[i1] - 0.5) / max(arr[i1] - b, 1e-6)
        return lo, hi
    fx = frac(colc); fy = frac(rowc)
    fb = tuple(round(float(v), 1) for v in (x0 + fx[0], y0 + fy[0], x0 + fx[1], y0 + fy[1]))
    core = sub[d > np.percentile(d[m], 75)] if m.sum() > 8 else sub[m]
    top = sub[d >= np.percentile(d[m], 93)] if m.sum() > 8 else sub[m]
    print("P93col", hexc(np.median(top, axis=0)))
    print("FRAC", fb, "rowcov", " ".join("%d:%.2f" % (y0 + i, v) for i, v in enumerate(rowc) if v > 0.05))
    print("bg", hexc(bg), "bgl %.0f" % bgl, "| box>thr", bx, "| box cov>0.5", sb, "| core", hexc(np.median(core, axis=0)), "n", int(m.sum()))


def main():
    a = sys.argv[1:]
    cmd = a[0]
    if cmd == "ink":
        x0, y0, x1, y1 = map(int, a[1:5])
        thr = float(a[5]) if len(a) > 5 else 40
        mode = a[6] if len(a) > 6 else "diff"
        ink(x0, y0, x1, y1, thr, mode)
    elif cmd == "col":
        x0, y0, x1, y1 = map(int, a[1:5])
        sub, _ = region(x0, y0, x1, y1)
        print(hexc(np.median(sub.reshape(-1, 3), axis=0)), "mean", hexc(sub.reshape(-1, 3).mean(axis=0)))
    elif cmd == "rows":
        x0, y0, x1, y1 = map(int, a[1:5])
        sub, lum = region(x0, y0, x1, y1)
        for i, r in enumerate(sub):
            print(y0 + i, "%.0f" % lum[i].mean(), hexc(np.median(r, axis=0)))
    elif cmd == "cols":
        x0, y0, x1, y1 = map(int, a[1:5])
        sub, lum = region(x0, y0, x1, y1)
        for i in range(sub.shape[1]):
            print(x0 + i, "%.0f" % lum[:, i].mean(), hexc(np.median(sub[:, i], axis=0)))
    elif cmd == "px":
        x, y = int(a[1]), int(a[2])
        print(hexc(IM[y, x]))


main()
