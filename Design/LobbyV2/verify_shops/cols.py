import json, math, numpy as np
from meas import IM, L
spec=json.load(open(r"../spec/shops.json",encoding="utf-8"))
def hexc(v): return '#%02X%02X%02X'%tuple(int(round(x)) for x in v)
def top(box, frac=0.03, dark=False):
    x0=int(math.floor(box[0])); y0=int(math.floor(box[1])); x1=int(math.ceil(box[2])); y1=int(math.ceil(box[3]))
    l=L[y0:y1,x0:x1].reshape(-1); c=IM[y0:y1,x0:x1].reshape(-1,3)
    o=np.argsort(l)
    k=max(3,int(len(o)*frac))
    sel=o[:k] if dark else o[-k:]
    return hexc(np.median(c[sel],0)), hexc(c[sel].max(0) if not dark else c[sel].min(0))
for e in spec['elements']:
    if e['kind'] in ('text','icon','tab','dot') or e['id'].endswith('_logo'):
        dark = e['id'].endswith('_logo')
        print(f"{e['id']:26s} spec {e.get('color')}  top3% {top(e['box'],0.03,dark)}  top10% {top(e['box'],0.10,dark)[0]}")
