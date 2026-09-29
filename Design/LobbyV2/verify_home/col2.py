import json, sys, numpy as np
from m import *
from edges import cov_map
def top_color(box, pol='light', q=90):
    c, X0, Y0, bg, ink = cov_map(box, 2, pol)
    H, W = c.shape
    sub = IM[Y0:Y0 + H, X0:X0 + W].reshape(-1, 3)
    l = L[Y0:Y0 + H, X0:X0 + W].reshape(-1)
    m = c.reshape(-1) >= 0.5
    ls = l[m]; ss = sub[m]
    if pol == 'light': sel = ls >= np.percentile(ls, q)
    else: sel = ls <= np.percentile(ls, 100 - q)
    return hexc(np.median(ss[sel], 0)), int(sel.sum())
if __name__ == '__main__':
    spec = json.load(open(sys.argv[1], encoding='utf-8'))
    for e in spec['elements']:
        if e['kind'] not in ('text', 'icon'): continue
        pol = 'dark' if e.get('color', '#FFFFFF').startswith('#1') else 'light'
        print('%-22s spec %-10s top10 %s top25 %s' % (e['id'], e.get('color'), top_color(e['box'], pol, 90), top_color(e['box'], pol, 75)))
