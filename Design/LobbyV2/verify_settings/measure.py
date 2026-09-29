"""measure.py spec.json [--ids a,b] [--m 3] [--frac 0.5]
Independent sub-pixel ink box for text/icon/dot elements: window = spec box grown by m px; bg = median of window border;
ink strength = colour distance to bg; profile = per-column / per-row max strength normalised by p97 of strength;
edge = linear-interpolated crossing of `frac` between pixel centres. Prints spec box, measured box, deltas."""
import sys, json
import numpy as np
from PIL import Image

P = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\settings.png"
im = np.asarray(Image.open(P).convert("RGB")).astype(np.float32)
H, W = im.shape[:2]


def crossing(prof, frac, fwd=True):
    """prof normalised 0..1 indexed by pixel; return continuous coordinate (relative to prof index 0) of first crossing."""
    n = len(prof)
    idx = range(n) if fwd else range(n - 1, -1, -1)
    prev = None
    for i in idx:
        if prof[i] >= frac:
            if prev is None:
                return (i if fwd else i + 1)
            cp = prof[prev]
            ci = prof[i]
            t = (frac - cp) / max(1e-6, ci - cp)
            # centres: prev + 0.5 -> i + 0.5
            cprev = prev + 0.5
            ci_ = i + 0.5
            return cprev + t * (ci_ - cprev)
        prev = i
    return None


def measure(box, m=3, frac=0.5, mode="bright", excl=None):
    x0 = max(0, int(np.floor(box[0] - m))); y0 = max(0, int(np.floor(box[1] - m)))
    x1 = min(W, int(np.ceil(box[2] + m))); y1 = min(H, int(np.ceil(box[3] + m)))
    sub = im[y0:y1, x0:x1]
    bcol = np.concatenate([sub[0, :], sub[-1, :], sub[:, 0], sub[:, -1]])
    bg = np.median(bcol, axis=0)
    lum = sub @ np.array([0.299, 0.587, 0.114], np.float32)
    bgl = float(bg @ np.array([0.299, 0.587, 0.114], np.float32))
    if mode == "bright":
        s = np.clip(lum - bgl, 0, None)
    elif mode == "dark":
        s = np.clip(bgl - lum, 0, None)
    else:
        s = np.sqrt(((sub - bg) ** 2).sum(axis=2))
    peak = np.percentile(s, 97) if s.max() > 0 else 1
    peak = max(peak, 1e-3)
    cp = np.clip(s.max(axis=0) / peak, 0, 1.5)
    rp = np.clip(s.max(axis=1) / peak, 0, 1.5)
    L = crossing(cp, frac, True); R = crossing(cp, frac, False)
    T = crossing(rp, frac, True); B = crossing(rp, frac, False)
    if None in (L, R, T, B):
        return None
    core = s >= 0.85 * peak
    col = np.median(sub[core], axis=0) if core.any() else bg
    return dict(box=[round(x0 + L, 2), round(y0 + T, 2), round(x0 + R, 2), round(y0 + B, 2)],
                bg="#%02X%02X%02X" % tuple(int(round(v)) for v in bg),
                core="#%02X%02X%02X" % tuple(int(round(v)) for v in col), peak=round(float(peak), 1))


if __name__ == "__main__":
    a = sys.argv[1:]
    def opt(n, d=None):
        if n in a:
            i = a.index(n); v = a[i + 1]; del a[i:i + 2]; return v
        return d
    ids = opt("--ids"); m = float(opt("--m", 3)); frac = float(opt("--frac", 0.5)); mode = opt("--mode", "bright")
    spec = json.load(open(a[0], encoding="utf-8"))
    sel = set(ids.split(",")) if ids else None
    for e in spec["elements"]:
        if sel and e["id"] not in sel:
            continue
        if not sel and e["kind"] not in ("text", "tab", "icon", "dot"):
            continue
        r = measure(e["box"], m, frac, mode)
        if r is None:
            print(e["id"], "none"); continue
        d = [round(r["box"][i] - e["box"][i], 2) for i in range(4)]
        flag = " <<<" if max(abs(v) for v in d) > 0.5 else ""
        print(f'{e["id"]:28s} spec {e["box"]} meas {r["box"]} d {d} core {r["core"]} bg {r["bg"]}{flag}')
