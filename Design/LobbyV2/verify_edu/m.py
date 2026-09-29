"""m.py ink X0 Y0 X1 Y1 [--mode bright|dark|blue|red|sat|bdiff] [--bgp 15] [--fgp 99]
   m.py prof X0 Y0 X1 Y1 [--axis x|y] [--mode ..] [--agg max|mean]
   m.py line x|y C A B        (RGB along row y=C for x in A..B, or column x=C for y in A..B)
   m.py med X0 Y0 X1 Y1
   m.py check spec.json [--grow 2] [--ids prefix,...]   ink boxes vs spec for text/icon/dot"""
import sys, json, numpy as np
from PIL import Image
P = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\education.png"
im = np.asarray(Image.open(P).convert("RGB")).astype(np.float32)
def opt(a, n, d=None):
    if n in a:
        i = a.index(n); v = a[i+1]; del a[i:i+2]; return v
    return d
def metric(c, mode):
    r, g, b = c[...,0], c[...,1], c[...,2]
    L = 0.299*r + 0.587*g + 0.114*b
    return {"bright": L, "dark": 255-L, "blue": b-r, "red": r-(g+b)/2, "sat": c.max(-1)-c.min(-1),
            "white": np.minimum(np.minimum(r,g),b)}[mode]
def hexc(c): return "#%02X%02X%02X" % tuple(int(round(v)) for v in c)
def cross(v, base):
    # v: coverage profile. returns 50% crossing left/right with pixel centres at i+0.5
    idx = np.nonzero(v >= 0.5)[0]
    if len(idx) == 0: return None, None
    i, j = idx[0], idx[-1]
    a = v[i-1] if i > 0 else 0.0; b = v[i]
    L = (i - 0.5) + (0.5 - a)/max(1e-6, b - a) if i > 0 else float(i)
    a2 = v[j+1] if j+1 < len(v) else 0.0; b2 = v[j]
    R = (j + 0.5) + (b2 - 0.5)/max(1e-6, b2 - a2) if j+1 < len(v) else float(j+1)
    return base + L, base + R
def ink(x0, y0, x1, y1, mode="bright", bgp=15, fgp=99, quiet=False):
    c = im[y0:y1, x0:x1]; m = metric(c, mode)
    bg = np.percentile(m, bgp); fg = np.percentile(m, fgp)
    cov = np.clip((m-bg)/max(1e-3, fg-bg), 0, 1)
    X0, X1 = cross(cov.max(0), x0); Y0, Y1 = cross(cov.max(1), y0)
    core = c[cov >= 0.85]; bgpx = c[cov <= 0.05]
    res = dict(box=[X0, Y0, X1, Y1], core=hexc(np.median(core, 0)) if len(core) else None,
               bg=hexc(np.median(bgpx, 0)) if len(bgpx) else None, fg=float(fg), bgv=float(bg))
    if not quiet:
        print("box [%.1f, %.1f, %.1f, %.1f] w=%.1f h=%.1f core %s bg %s (bg=%.0f fg=%.0f)" % (X0, Y0, X1, Y1, X1-X0, Y1-Y0, res["core"], res["bg"], bg, fg))
    return res
if __name__ == "__main__":
    a = sys.argv[1:]; cmd = a.pop(0)
    mode = opt(a, "--mode", "bright"); axis = opt(a, "--axis", "x"); agg = opt(a, "--agg", "max")
    bgp = float(opt(a, "--bgp", 15)); fgp = float(opt(a, "--fgp", 99))
    grow = float(opt(a, "--grow", 2)); ids = opt(a, "--ids")
    if cmd == "ink":
        ink(*map(int, a[:4]), mode=mode, bgp=bgp, fgp=fgp)
    elif cmd == "prof":
        x0, y0, x1, y1 = map(int, a[:4]); m = metric(im[y0:y1, x0:x1], mode)
        f = np.max if agg == "max" else np.mean
        v = f(m, 0) if axis == "x" else f(m, 1); base = x0 if axis == "x" else y0
        print(" ".join(f"{base+i}:{int(round(x))}" for i, x in enumerate(v)))
    elif cmd == "line":
        ax = a[0]; C, A, B = map(int, a[1:4])
        for t in range(A, B):
            p = im[C, t] if ax == "y" else im[t, C]
            print(t, hexc(p), int(0.299*p[0]+0.587*p[1]+0.114*p[2]))
    elif cmd == "med":
        x0, y0, x1, y1 = map(int, a[:4]); c = im[y0:y1, x0:x1].reshape(-1, 3)
        print(hexc(np.median(c, 0)), "p10", hexc(np.percentile(c, 10, 0)), "p90", hexc(np.percentile(c, 90, 0)))
    elif cmd == "check":
        s = json.load(open(a[0], encoding="utf-8"))
        for e in s["elements"]:
            if ids and not any(e["id"].startswith(p) for p in ids.split(",")): continue
            if e["kind"] not in ("text", "tab", "icon", "dot"): continue
            b = e["box"]; g = grow
            X0, Y0, X1, Y1 = int(np.floor(b[0]-g)), int(np.floor(b[1]-g)), int(np.ceil(b[2]+g)), int(np.ceil(b[3]+g))
            md = "red" if e["kind"] == "dot" else mode
            r = ink(X0, Y0, X1, Y1, mode=md, bgp=bgp, fgp=fgp, quiet=True)
            nb = r["box"]
            if nb[0] is None: print(e["id"], "no ink"); continue
            dif = max(abs(nb[i]-b[i]) for i in range(4))
            flag = "  <<<" if dif > 0.5 else ""
            print("%-34s spec [%.1f %.1f %.1f %.1f]  ink [%.1f %.1f %.1f %.1f]  core %s spec %s%s" % (e["id"], *b, *nb, r["core"], e.get("color"), flag))
