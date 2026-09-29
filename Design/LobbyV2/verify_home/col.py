import json, sys, numpy as np
from m import *
from edges import cov_map
def core_color(box, pol='light', thr=0.85, arr=None):
    c, X0, Y0, bg, ink = cov_map(box, 2, pol, arr=arr)
    H, W = c.shape
    sub = IM[Y0:Y0 + H, X0:X0 + W].reshape(-1, 3)
    m = (c.reshape(-1) >= thr)
    if m.sum() < 3: m = (c.reshape(-1) >= 0.7)
    return hexc(np.median(sub[m], 0)), int(m.sum())
if __name__ == '__main__':
    spec = json.load(open(sys.argv[1], encoding='utf-8'))
    for e in spec['elements']:
        if e['kind'] not in ('text', 'icon', 'line'): continue
        pol = 'dark' if e.get('color', '#FFFFFF').startswith('#1') else 'light'
        print('%-22s spec %-10s core %s' % (e['id'], e.get('color'), core_color(e['box'], pol)))
