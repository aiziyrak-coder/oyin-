import numpy as np
from PIL import Image
IM = np.asarray(Image.open(r"C:/Users/alocomputers/AppData/Local/Temp/claude/D--Game1/4f1a5cc1-c33d-4082-8691-871fd7139403/scratchpad/v2/pages/home.png").convert("RGB")).astype(np.float32)
R, G, B = IM[..., 0], IM[..., 1], IM[..., 2]
L = 0.299 * R + 0.587 * G + 0.114 * B
def hexc(c): return '#%02X%02X%02X' % tuple(int(round(v)) for v in c)
def dump(x0, y0, x1, y1, arr=None, w=4):
    a = L if arr is None else arr
    print('     ' + ''.join(('%'+str(w)+'d') % x for x in range(x0, x1)))
    for y in range(y0, y1):
        print('%4d ' % y + ''.join(('%'+str(w)+'d') % int(a[y, x]) for x in range(x0, x1)))
def med(x0, y0, x1, y1, fn=None):
    s = IM[y0:y1, x0:x1].reshape(-1, 3)
    if fn is not None: s = s[fn(s[:, 0], s[:, 1], s[:, 2])]
    return (hexc(np.median(s, 0)), len(s)) if len(s) else None
def bbox(x0, y0, x1, y1, fn):
    s = IM[y0:y1, x0:x1]; m = fn(s[..., 0], s[..., 1], s[..., 2])
    r = np.where(m.any(1))[0]; c = np.where(m.any(0))[0]
    if not len(r): return None
    return (x0 + c[0], y0 + r[0], x0 + c[-1] + 1, y0 + r[-1] + 1, int(m.sum()))
