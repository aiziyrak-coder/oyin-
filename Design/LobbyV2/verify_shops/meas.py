"""Independent re-measurement of ink boxes: python meas.py [ids-prefix...] [--exp 3] [--dark]"""
import json, sys, math
import numpy as np
from PIL import Image

PAGE = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\shops.png"
IM = np.asarray(Image.open(PAGE).convert("RGB")).astype(np.float32)
L = IM @ np.array([0.299, 0.587, 0.114], dtype=np.float32)


def cross(p, left=True):
    n = len(p)
    idx = range(n) if left else range(n - 1, -1, -1)
    prev = None
    for i in idx:
        if p[i] >= 0.5:
            if prev is None:
                return float(i) if left else float(i + 1)
            a, b = p[prev], p[i]
            t = (0.5 - a) / (b - a) if b != a else 0
            return prev + 0.5 + t * (i - prev) if left else prev + 0.5 - t * (prev - i)
        prev = i
    return None


def ink(box, mode="bright", exp=3, bgpct=50, fgpct=99.0):
    x0 = max(0, int(math.floor(box[0])) - exp); y0 = max(0, int(math.floor(box[1])) - exp)
    x1 = min(L.shape[1], int(math.ceil(box[2])) + exp); y1 = min(L.shape[0], int(math.ceil(box[3])) + exp)
    l = L[y0:y1, x0:x1]
    border = np.concatenate([l[0, :], l[-1, :], l[:, 0], l[:, -1]])
    bg = float(np.percentile(border, bgpct))
    if mode == "bright":
        fg = float(np.percentile(l, fgpct)); cov = (l - bg) / max(fg - bg, 1e-3)
    else:
        fg = float(np.percentile(l, 100 - fgpct)); cov = (bg - l) / max(bg - fg, 1e-3)
    cov = np.clip(cov, 0, 1)
    cp = cov.max(axis=0); rp = cov.max(axis=1)
    r = [x0 + cross(cp, True), y0 + cross(rp, True), x0 + cross(cp, False), y0 + cross(rp, False)]
    return [round(v, 1) for v in r], bg, fg


if __name__ == "__main__":
    spec = json.load(open(r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\spec\shops.json", encoding="utf-8"))
    a = sys.argv[1:]
    exp = 3
    if "--exp" in a:
        exp = int(a[a.index("--exp") + 1]); a = [x for x in a if x not in ("--exp", str(exp))]
    mode = "dark" if "--dark" in a else "bright"
    a = [x for x in a if x != "--dark"]
    for e in spec["elements"]:
        if a and not any(e["id"].startswith(p) for p in a):
            continue
        if e["kind"] in ("panel", "button", "tile", "pill", "input") and not a:
            continue
        try:
            r, bg, fg = ink(e["box"], mode, exp)
        except Exception as ex:
            print(e["id"], "ERR", ex); continue
        d = [round(r[i] - e["box"][i], 1) for i in range(4)]
        flag = " <<" if max(abs(v) for v in d) > 0.5 else ""
        print(f"{e['id']:28s} spec {e['box']}  meas {r}  d {d} bg{bg:.0f} fg{fg:.0f}{flag}")
