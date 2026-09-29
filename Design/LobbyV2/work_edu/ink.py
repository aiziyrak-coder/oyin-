"""Helpers: python ink.py <cmd> ...
  ink X0 Y0 X1 Y1 [--mode bright|dark|blue|red|sat] [--thr T]   -> ink bbox (subpixel-ish) + core color
  prof X0 Y0 X1 Y1 [--axis x|y] [--mode ...]                      -> per-column/row max contrast
  px X Y                                                           -> pixel values around
  med X0 Y0 X1 Y1                                                  -> median color of region
"""
import sys, numpy as np
from PIL import Image
P = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\education.png"
im = np.asarray(Image.open(P).convert("RGB")).astype(np.float32)


def opt(a, n, d=None):
    if n in a:
        i = a.index(n); v = a[i + 1]; del a[i:i + 2]; return v
    return d


def metric(c, mode):
    r, g, b = c[..., 0], c[..., 1], c[..., 2]
    L = 0.299 * r + 0.587 * g + 0.114 * b
    if mode == "bright":
        return L
    if mode == "dark":
        return 255 - L
    if mode == "blue":
        return b - r
    if mode == "red":
        return r - (g + b) / 2
    if mode == "sat":
        return c.max(-1) - c.min(-1)
    raise SystemExit("mode?")


def hexc(c):
    return "#%02X%02X%02X" % tuple(int(round(v)) for v in c)


def ink(x0, y0, x1, y1, mode="bright", thr=None):
    c = im[y0:y1, x0:x1]
    m = metric(c, mode)
    bg = np.percentile(m, 20)
    fg = np.percentile(m, 99.5)
    if thr is None:
        thr = bg + 0.5 * (fg - bg)
    cov = np.clip((m - bg) / max(1e-3, fg - bg), 0, 1)
    mask = m > thr
    if not mask.any():
        print("no ink"); return
    ys, xs = np.nonzero(mask)
    colc = cov.max(0); rowc = cov.max(1)
    xa, xb, ya, yb = xs.min(), xs.max(), ys.min(), ys.max()
    # subpixel: extend by coverage of neighbouring column/row
    fx0 = x0 + xa - (colc[xa - 1] if xa > 0 else 0)
    fx1 = x0 + xb + 1 + (colc[xb + 1] if xb + 1 < len(colc) else 0)
    fy0 = y0 + ya - (rowc[ya - 1] if ya > 0 else 0)
    fy1 = y0 + yb + 1 + (rowc[yb + 1] if yb + 1 < len(rowc) else 0)
    core = c[m >= np.percentile(m[mask], 70)] if mask.sum() > 4 else c[mask]
    bgpx = c[m <= np.percentile(m, 20)]
    print(f"thr={thr:.1f} bg={bg:.1f} fg={fg:.1f}")
    print(f"int box [{x0+xa}, {y0+ya}, {x0+xb+1}, {y0+yb+1}]  sub box [{fx0:.1f}, {fy0:.1f}, {fx1:.1f}, {fy1:.1f}]  h={fy1-fy0:.1f} w={fx1-fx0:.1f}")
    print("core color", hexc(np.median(core, 0)), " bg color", hexc(np.median(bgpx, 0)))


def prof(x0, y0, x1, y1, axis="x", mode="bright"):
    c = im[y0:y1, x0:x1]
    m = metric(c, mode)
    if axis == "x":
        v = m.max(0); base = x0
    else:
        v = m.max(1); base = y0
    print(" ".join(f"{base+i}:{int(x)}" for i, x in enumerate(v)))


if __name__ == "__main__":
    a = sys.argv[1:]
    cmd = a.pop(0)
    mode = opt(a, "--mode", "bright")
    thr = opt(a, "--thr"); thr = float(thr) if thr else None
    axis = opt(a, "--axis", "x")
    if cmd == "ink":
        ink(*map(int, a[:4]), mode=mode, thr=thr)
    elif cmd == "prof":
        prof(*map(int, a[:4]), axis=axis, mode=mode)
    elif cmd == "px":
        x, y = map(int, a[:2])
        for yy in range(y - 2, y + 3):
            print(yy, " ".join(hexc(im[yy, xx]) for xx in range(x - 3, x + 4)))
    elif cmd == "med":
        x0, y0, x1, y1 = map(int, a[:4])
        c = im[y0:y1, x0:x1].reshape(-1, 3)
        print(hexc(np.median(c, 0)), "min", hexc(c.min(0)), "max", hexc(c.max(0)))
