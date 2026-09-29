"""Measuring helpers for shops page.
usage: python m.py ink X0 Y0 X1 Y1 [bright|dark] [thr_frac]
       python m.py col X0 Y0 X1 Y1 [bright|dark] [pct]   -> color of core pixels
       python m.py prof X0 Y0 X1 Y1   -> row/col mean luminance profiles
       python m.py px X Y             -> pixel rgb
"""
import sys
import numpy as np
from PIL import Image

IM = np.asarray(Image.open(r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\shops.png").convert("RGB")).astype(np.float32)
L = IM @ np.array([0.299, 0.587, 0.114], dtype=np.float32)


def region(x0, y0, x1, y1):
    return L[y0:y1, x0:x1], IM[y0:y1, x0:x1]


def cross(profile, start_from_left=True):
    """first position where profile crosses 0.5 (linear interp); profile is coverage 0..1"""
    p = profile
    n = len(p)
    idx = range(n) if start_from_left else range(n - 1, -1, -1)
    prev = None
    for i in idx:
        if p[i] >= 0.5:
            if prev is None:
                return float(i) if start_from_left else float(i + 1)
            # interpolate between prev (below) and i (above)
            a, b = p[prev], p[i]
            t = (0.5 - a) / (b - a) if b != a else 0
            if start_from_left:
                # pixel centers at prev+0.5 and i+0.5; boundary point
                return prev + 0.5 + t * (i - prev)
            else:
                return prev + 0.5 - t * (prev - i)
        prev = i
    return None


def ink(x0, y0, x1, y1, mode="bright", thr=0.5, bg=None):
    l, c = region(x0, y0, x1, y1)
    if bg is None:
        border = np.concatenate([l[0, :], l[-1, :], l[:, 0], l[:, -1]])
        bg = float(np.median(border))
    if mode == "bright":
        fg = float(np.percentile(l, 99.5))
        cov = (l - bg) / max(fg - bg, 1e-3)
    else:
        fg = float(np.percentile(l, 0.5))
        cov = (bg - l) / max(bg - fg, 1e-3)
    cov = np.clip(cov, 0, 1)
    colp = cov.max(axis=0)
    rowp = cov.max(axis=1)
    lx = cross(colp, True); rx = cross(colp, False)
    ty = cross(rowp, True); by = cross(rowp, False)
    res = None
    if lx is not None:
        res = [round(x0 + lx, 1), round(y0 + ty, 1), round(x0 + rx, 1), round(y0 + by, 1)]
    return res, bg, fg, colp, rowp


def color(x0, y0, x1, y1, mode="bright", pct=90):
    l, c = region(x0, y0, x1, y1)
    flat = l.flatten(); cf = c.reshape(-1, 3)
    if mode == "bright":
        sel = flat >= np.percentile(flat, pct)
    elif mode == "dark":
        sel = flat <= np.percentile(flat, 100 - pct)
    else:
        sel = np.ones_like(flat, bool)
    med = np.median(cf[sel], axis=0)
    return "#%02X%02X%02X" % tuple(int(round(v)) for v in med)


if __name__ == "__main__":
    cmd = sys.argv[1]
    a = sys.argv[2:]
    if cmd == "ink":
        x0, y0, x1, y1 = map(int, a[:4])
        mode = a[4] if len(a) > 4 else "bright"
        res, bg, fg, colp, rowp = ink(x0, y0, x1, y1, mode)
        print("box", res, "bg %.0f fg %.0f" % (bg, fg))
        print("rows", " ".join("%d:%.2f" % (y0 + i, v) for i, v in enumerate(rowp)))
        print("cols", " ".join("%d:%.2f" % (x0 + i, v) for i, v in enumerate(colp)))
    elif cmd == "col":
        x0, y0, x1, y1 = map(int, a[:4])
        mode = a[4] if len(a) > 4 else "bright"
        pct = float(a[5]) if len(a) > 5 else 90
        print(color(x0, y0, x1, y1, mode, pct))
    elif cmd == "prof":
        x0, y0, x1, y1 = map(int, a[:4])
        l, c = region(x0, y0, x1, y1)
        print("rowmean", " ".join("%d:%.0f" % (y0 + i, v) for i, v in enumerate(l.mean(axis=1))))
        print("colmean", " ".join("%d:%.0f" % (x0 + i, v) for i, v in enumerate(l.mean(axis=0))))
    elif cmd == "px":
        x, y = int(a[0]), int(a[1])
        print(IM[y, x])
