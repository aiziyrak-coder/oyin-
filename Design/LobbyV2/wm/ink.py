"""Ink / color measurement helpers for map page.
usage (python): from ink import *; ink(x0,y0,x1,y1, mode='bright')
"""
import sys
import numpy as np
from PIL import Image

IM = np.asarray(Image.open(r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\map.png").convert("RGB")).astype(np.float32)
H, W, _ = IM.shape
LUM = IM @ np.array([0.299, 0.587, 0.114], np.float32)


def hexc(c):
    c = np.clip(np.round(c), 0, 255).astype(int)
    return "#%02X%02X%02X" % tuple(c)


def ink(x0, y0, x1, y1, mode="bright", thr=0.15, bgq=None, fgq=None, chan=None, ref=None):
    """Return subpixel bbox of ink within region [x0,x1)x[y0,y1) (ints).
    mode bright: ink lighter than bg; dark: darker; 'dist': color distance from ref bg color.
    alpha = (v - bg) / (fg - bg), bg = low quantile, fg = high quantile."""
    x0, y0, x1, y1 = int(x0), int(y0), int(x1), int(y1)
    sub = IM[y0:y1, x0:x1]
    if mode == "dist":
        refc = np.array(ref if ref is not None else np.median(sub.reshape(-1, 3), axis=0), np.float32)
        v = np.sqrt(((sub - refc) ** 2).sum(-1))
    elif chan is not None:
        v = sub[..., chan]
    else:
        v = LUM[y0:y1, x0:x1]
    if mode == "dark":
        v = -v
    bg = np.quantile(v, bgq if bgq is not None else 0.3)
    fg = np.quantile(v, fgq if fgq is not None else 0.995)
    a = np.clip((v - bg) / max(1e-3, fg - bg), 0, 1)
    colA = a.max(axis=0)
    rowA = a.max(axis=1)
    cs = np.where(colA > thr)[0]
    rs = np.where(rowA > thr)[0]
    if len(cs) == 0:
        return None
    L = cs[0] + 1 - min(1, colA[cs[0]] / 0.9)
    R = cs[-1] + min(1, colA[cs[-1]] / 0.9)
    T = rs[0] + 1 - min(1, rowA[rs[0]] / 0.9)
    B = rs[-1] + min(1, rowA[rs[-1]] / 0.9)
    core = sub[a > 0.85]
    col = hexc(np.median(core, axis=0)) if len(core) else None
    bgc = hexc(np.median(sub[a < 0.05], axis=0)) if (a < 0.05).any() else None
    return dict(box=[round(x0 + L, 1), round(y0 + T, 1), round(x0 + R, 1), round(y0 + B, 1)], color=col, bg=bgc,
                rows=[round(float(r), 2) for r in rowA], cols=None)


def med(x0, y0, x1, y1):
    sub = IM[int(y0):int(y1), int(x0):int(x1)].reshape(-1, 3)
    return hexc(np.median(sub, axis=0))


def prof_rows(x0, y0, x1, y1):
    return [round(float(v)) for v in LUM[int(y0):int(y1), int(x0):int(x1)].mean(axis=1)]


def prof_cols(x0, y0, x1, y1):
    return [round(float(v)) for v in LUM[int(y0):int(y1), int(x0):int(x1)].mean(axis=0)]


def px(x, y):
    return hexc(IM[int(y), int(x)])
