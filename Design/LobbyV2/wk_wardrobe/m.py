"""measure helper for wardrobe page.
python m.py ink x0 y0 x1 y1 [dark] [thr=0.5] [bg=auto|r,g,b] [ch=lum|b|r|g|sat]
python m.py col x0 y0 x1 y1            -> median / mean color
python m.py prof x0 y0 x1 y1 [axis=x|y] [ch=lum]  -> projected profile
python m.py px x y
"""
import sys
import numpy as np
from PIL import Image

IM = np.asarray(Image.open(r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\wardrobe.png").convert("RGB")).astype(np.float32)


def chan(sub, ch):
    if ch == "lum":
        return sub @ np.array([0.299, 0.587, 0.114], np.float32)
    if ch in "rgb":
        return sub[..., "rgb".index(ch)]
    if ch == "sat":
        return sub.max(-1) - sub.min(-1)
    if ch == "blue":  # blueness
        return sub[..., 2] - (sub[..., 0] + sub[..., 1]) / 2
    if ch == "red":
        return sub[..., 0] - (sub[..., 1] + sub[..., 2]) / 2
    raise ValueError(ch)


def hexc(c):
    c = np.clip(np.round(c), 0, 255).astype(int)
    return "#%02X%02X%02X" % tuple(c)


def edge(cov):
    # 50% crossing (linear interp) from the start of a 1-D coverage profile
    idx = np.where(cov >= 0.5)[0]
    if len(idx) == 0:
        return None
    k = idx[0]
    if k == 0:
        return 0.0
    a, b = cov[k - 1], cov[k]
    return (k - 1) + (0.5 - a) / (b - a) + 0.5


def edge_area(cov):
    idx = np.where(cov >= 0.95)[0]
    if len(idx) == 0:
        k = int(np.argmax(cov))
        return k + 1 - cov[k] if cov[k] > 0 else None
    k = idx[0]
    return k - float(np.clip(cov[:k], 0, 1).sum())


def ink(x0, y0, x1, y1, dark=False, thr=0.5, bg=None, ch="lum"):
    sub = IM[y0:y1, x0:x1]
    L = chan(sub, ch)
    if bg is None:
        border = np.concatenate([L[0], L[-1], L[:, 0], L[:, -1]])
        b = np.median(border)
    else:
        b = float(bg)
    if dark:
        top = np.percentile(L, 3)
    else:
        top = np.percentile(L, 99.5)
    cov = (L - b) / (top - b + 1e-6)
    cov = np.clip(cov, 0, 1)
    m = cov >= thr
    ys, xs = np.where(m)
    if len(xs) == 0:
        print("no ink"); return
    bx0, bx1, by0, by1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
    # subpixel: projection profiles (max)
    cx = cov[by0:by1].max(0)
    cy = cov[:, bx0:bx1].max(1)
    l = edge(cx); r = len(cx) - edge(cx[::-1]); t = edge(cy); bb = len(cy) - edge(cy[::-1])
    core = sub[cov >= 0.85]
    print("bg lum %.1f ink %.1f" % (b, top))
    print("thr box   [%d, %d, %d, %d]" % (x0 + bx0, y0 + by0, x0 + bx1, y0 + by1))
    print("area box  [%.1f, %.1f, %.1f, %.1f]  w=%.1f h=%.1f" % (x0 + l, y0 + t, x0 + r, y0 + bb, r - l, bb - t))
    if len(core):
        print("core color median", hexc(np.median(core, 0)), "n=%d" % len(core))
    # row profile
    print("rows cov:", " ".join("%d:%.2f" % (y0 + i, v) for i, v in enumerate(cy) if v > 0.02))
    print("cols cov:", " ".join("%d:%.2f" % (x0 + i, v) for i, v in enumerate(cx) if v > 0.02))


def col(x0, y0, x1, y1):
    sub = IM[y0:y1, x0:x1].reshape(-1, 3)
    print("median", hexc(np.median(sub, 0)), "mean", hexc(sub.mean(0)), "min", hexc(sub.min(0)), "max", hexc(sub.max(0)))


def prof(x0, y0, x1, y1, axis="x", ch="lum", red="mean"):
    sub = chan(IM[y0:y1, x0:x1], ch)
    f = getattr(np, red)
    if axis == "x":
        p = f(sub, 0); base = x0
    else:
        p = f(sub, 1); base = y0
    print(" ".join("%d:%.0f" % (base + i, v) for i, v in enumerate(p)))


if __name__ == "__main__":
    a = sys.argv[1:]
    cmd = a[0]
    nums = [int(v) for v in a[1:5]] if len(a) > 4 else None
    kw = {}
    flags = set()
    for t in a[5:]:
        if "=" in t:
            k, v = t.split("=", 1); kw[k] = v
        else:
            flags.add(t)
    if cmd == "ink":
        ink(*nums, dark="dark" in flags, thr=float(kw.get("thr", 0.5)), bg=kw.get("bg"), ch=kw.get("ch", "lum"))
    elif cmd == "col":
        col(*nums)
    elif cmd == "prof":
        prof(*nums, axis=kw.get("axis", "x"), ch=kw.get("ch", "lum"), red=kw.get("red", "mean"))
    elif cmd == "px":
        x, y = int(a[1]), int(a[2]); print(hexc(IM[y, x]))
