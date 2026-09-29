"""colors.py spec.json [ids] -> for every text/icon/dot/line: bg (border median), p90/p97 brightest-contrast core colour, current spec color"""
import sys, json, numpy as np
from PIL import Image
P = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\friends.png"
im = np.asarray(Image.open(P).convert("RGB")).astype(float)
spec = json.load(open(sys.argv[1], encoding="utf-8"))
sel = set(sys.argv[2].split(",")) if len(sys.argv) > 2 else None
hx = lambda c: "#%02X%02X%02X" % tuple(int(round(v)) for v in c)
for e in spec["elements"]:
    if sel and e["id"] not in sel: continue
    if not sel and e["kind"] not in ("text", "icon", "dot", "line", "button"): continue
    b = e["box"]
    x0, y0, x1, y1 = int(np.floor(b[0])), int(np.floor(b[1])), int(np.ceil(b[2])), int(np.ceil(b[3]))
    sub = im[y0:y1, x0:x1].reshape(-1, 3)
    ring = np.concatenate([im[y0-2, x0-2:x1+2], im[y1+1, x0-2:x1+2], im[y0-2:y1+2, x0-2], im[y0-2:y1+2, x1+1]])
    bg = np.median(ring, axis=0)
    d = np.sqrt(((sub - bg) ** 2).sum(1))
    o = np.argsort(d)
    top10 = sub[o[-max(3, len(o)//10):]]
    top3 = sub[o[-max(2, len(o)//33):]]
    print(f'{e["id"]:28s} spec {e.get("color") or e.get("fill")}  p90 {hx(np.median(top10,0))}  p97 {hx(np.median(top3,0))}  bg {hx(bg)}')
