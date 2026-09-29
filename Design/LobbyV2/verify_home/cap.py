from m import *
from edges2 import cross
import numpy as np
def capline(x0, x1, y0, y1, pol='light', bg=None, ink=None, arr=None):
    A = L if arr is None else arr
    sub = A[y0:y1, x0:x1]
    if bg is None: bg = np.median(np.concatenate([sub[:2].ravel(), sub[-2:].ravel()]))
    if ink is None: ink = np.percentile(sub, 97) if pol == 'light' else np.percentile(sub, 3)
    c = np.clip((sub - bg) / (ink - bg + 1e-6), 0, 1)
    rp = c.max(1)
    t = cross(rp, 0, 1); b = cross(rp, len(rp) - 1, -1)
    return round(y0 + t, 2), round(y0 + b, 2), float(bg), float(ink)
