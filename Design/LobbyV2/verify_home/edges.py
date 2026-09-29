import json, sys, numpy as np
from m import *
def cov_map(box, ex=3, pol='light', bg=None, ink=None, arr=None):
    A = L if arr is None else arr
    x0, y0, x1, y1 = box
    X0, Y0, X1, Y1 = int(np.floor(x0)) - ex, int(np.floor(y0)) - ex, int(np.ceil(x1)) + ex, int(np.ceil(y1)) + ex
    sub = A[Y0:Y1, X0:X1]
    inner = A[int(np.floor(y0)):int(np.ceil(y1)), int(np.floor(x0)):int(np.ceil(x1))]
    if bg is None:
        ring = np.concatenate([sub[:ex].ravel(), sub[-ex:].ravel(), sub[:, :ex].ravel(), sub[:, -ex:].ravel()])
        bg = np.median(ring)
    if ink is None:
        ink = np.percentile(inner, 97) if pol == 'light' else np.percentile(inner, 3)
    c = np.clip((sub - bg) / (ink - bg + 1e-6), 0, 1)
    return c, X0, Y0, bg, ink
def edges(box, ex=3, pol='light', thr=0.12, bg=None, ink=None, arr=None):
    c, X0, Y0, bg, ink = cov_map(box, ex, pol, bg, ink, arr)
    colm = c.max(0); rowm = c.max(1)
    cols = np.where(colm > thr)[0]; rows = np.where(rowm > thr)[0]
    if not len(cols): return None
    l = cols[0]; r = cols[-1]; t = rows[0]; b = rows[-1]
    e = [X0 + l + 1 - colm[l], Y0 + t + 1 - rowm[t], X0 + r + colm[r], Y0 + b + rowm[b]]
    return [round(v, 1) for v in e], bg, ink
if __name__ == '__main__':
    spec = json.load(open(sys.argv[1], encoding='utf-8'))
    pref = sys.argv[2] if len(sys.argv) > 2 else ''
    for e in spec['elements']:
        if e['kind'] not in ('text', 'icon', 'dot', 'line'): continue
        if not e['id'].startswith(pref): continue
        pol = 'dark' if e.get('color', '#FFFFFF') < '#555555' and e['kind'] == 'text' and e['color'].startswith('#1') else 'light'
        res = edges(e['box'], pol=pol)
        if res is None: print(e['id'], 'none'); continue
        nb, bg, ink = res
        d = [round(a - b, 1) for a, b in zip(nb, e['box'])]
        flag = '  <<' if max(abs(v) for v in d) > 0.5 else ''
        print('%-24s spec %-28s auto %-28s d %s bg %.0f ink %.0f%s' % (e['id'], e['box'], nb, d, bg, ink, flag))
