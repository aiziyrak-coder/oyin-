import sys, numpy as np
from PIL import Image
IM = np.asarray(Image.open(r"C:/Users/alocomputers/AppData/Local/Temp/claude/D--Game1/4f1a5cc1-c33d-4082-8691-871fd7139403/scratchpad/v2/pages/home.png").convert("RGB")).astype(np.float32)
R, G, B = IM[..., 0], IM[..., 1], IM[..., 2]
L = 0.299 * R + 0.587 * G + 0.114 * B
SAT = IM.max(-1) - IM.min(-1)


def region(x0, y0, x1, y1):
    return IM[y0:y1, x0:x1]


def bbox(x0, y0, x1, y1, mask_fn, minrow=1, mincol=1):
    sub = IM[y0:y1, x0:x1]
    m = mask_fn(sub[..., 0], sub[..., 1], sub[..., 2])
    rows = np.where(m.sum(1) >= minrow)[0]
    cols = np.where(m.sum(0) >= mincol)[0]
    if len(rows) == 0:
        return None
    return (x0 + cols[0], y0 + rows[0], x0 + cols[-1] + 1, y0 + rows[-1] + 1, int(m.sum()))


def med(x0, y0, x1, y1, mask_fn=None):
    sub = IM[y0:y1, x0:x1].reshape(-1, 3)
    if mask_fn is not None:
        m = mask_fn(sub[:, 0], sub[:, 1], sub[:, 2])
        sub = sub[m]
    if len(sub) == 0:
        return None
    c = np.median(sub, 0)
    return '#%02X%02X%02X' % tuple(int(round(v)) for v in c), len(sub)


def hexc(c):
    return '#%02X%02X%02X' % tuple(int(round(v)) for v in c)


def dump(x0, y0, x1, y1, arr=None, step=1):
    a = L if arr is None else arr
    print('     ' + ''.join('%4d' % x for x in range(x0, x1, step)))
    for y in range(y0, y1, step):
        print('%4d ' % y + ''.join('%4d' % int(a[y, x]) for x in range(x0, x1, step)))


def profile_rows(x0, y0, x1, y1, mask_fn):
    sub = IM[y0:y1, x0:x1]
    m = mask_fn(sub[..., 0], sub[..., 1], sub[..., 2])
    for i, s in enumerate(m.sum(1)):
        print(y0 + i, int(s))


def profile_cols(x0, y0, x1, y1, mask_fn):
    sub = IM[y0:y1, x0:x1]
    m = mask_fn(sub[..., 0], sub[..., 1], sub[..., 2])
    out = []
    for i, s in enumerate(m.sum(0)):
        out.append((x0 + i, int(s)))
    print(' '.join('%d:%d' % t for t in out))
