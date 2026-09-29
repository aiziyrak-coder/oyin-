import json, sys, numpy as np
from m import *
from edges import cov_map
def cross(prof, start, step):
    # walk from outside inward; return sub-pixel position where profile (sampled at centres) crosses 0.5
    n = len(prof); i = start
    while 0 <= i < n and prof[i] < 0.5: i += step
    if not (0 <= i < n): return None
    j = i - step
    pj = prof[j] if 0 <= j < n else 0.0
    pi = prof[i]
    t = (0.5 - pj) / (pi - pj + 1e-9)
    cj = j + 0.5; ci = i + 0.5
    return cj + (ci - cj) * t
def edges50(box, ex=3, pol='light', bg=None, ink=None, arr=None):
    c, X0, Y0, bg, ink = cov_map(box, ex, pol, bg, ink, arr)
    colm = c.max(0); rowm = c.max(1)
    l = cross(colm, 0, 1); r = cross(colm, len(colm) - 1, -1)
    t = cross(rowm, 0, 1); b = cross(rowm, len(rowm) - 1, -1)
    if None in (l, r, t, b): return None
    return [round(X0 + l, 1), round(Y0 + t, 1), round(X0 + r, 1), round(Y0 + b, 1)], bg, ink
if __name__ == '__main__':
    spec = json.load(open(sys.argv[1], encoding='utf-8'))
    pref = sys.argv[2] if len(sys.argv) > 2 else ''
    ex = int(sys.argv[3]) if len(sys.argv) > 3 else 3
    for e in spec['elements']:
        if e['kind'] not in ('text', 'icon', 'dot', 'line'): continue
        if not e['id'].startswith(pref): continue
        pol = 'dark' if e.get('color', '#FFFFFF').startswith('#1') else 'light'
        res = edges50(e['box'], ex=ex, pol=pol)
        if res is None: print(e['id'], 'none'); continue
        nb, bg, ink = res
        d = [round(a - b, 1) for a, b in zip(nb, e['box'])]
        flag = '  <<' if max(abs(v) for v in d) > 0.5 else ''
        print('%-22s spec %-28s auto %-28s d %-24s bg %.0f ink %.0f%s' % (e['id'], e['box'], nb, d, bg, ink, flag))
