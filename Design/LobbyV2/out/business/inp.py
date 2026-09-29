"""LaMa driver for the business page (same lama.onnx as tools.py, better windowing).

- masked pixels are zeroed before resampling (no colour of the UI leaks into the fill)
- windows may be non-square / larger than the image: out-of-image area is edge-replicated and kept as
  known context
- hole (what LaMa is asked to fill) and paste (what is written back) can differ
"""
import os, sys
import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
V2 = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, V2)
import onnxruntime as ort  # noqa
ort.set_default_logger_severity(3)
import tools  # noqa

PAGE = os.path.join(V2, "pages", "business.png")


def load(p):
    return np.asarray(Image.open(p).convert("RGB")).astype(np.float32)


def loadm(p):
    return np.asarray(Image.open(p).convert("L")) > 127


def save(a, p):
    Image.fromarray(np.clip(np.round(a), 0, 255).astype(np.uint8)).save(p)


def _resize_f(a, w, h, rs=Image.BICUBIC):
    """resize float HxWx3 via per-channel float images"""
    ch = [np.asarray(Image.fromarray(a[..., c].astype(np.float32), "F").resize((w, h), rs)) for c in range(3)]
    return np.stack(ch, -1)


def lama_win(img, hole, win, size=512):
    """Run LaMa on window win=(x0,y0,x1,y1) (may exceed image; edge-replicated). hole: bool HxW (full image).
    Returns float array (y1-y0, x1-x0, 3) = LaMa output for the window at image resolution."""
    H, W = hole.shape
    x0, y0, x1, y1 = win
    ys = np.clip(np.arange(y0, y1), 0, H - 1)
    xs = np.clip(np.arange(x0, x1), 0, W - 1)
    crop = img[np.ix_(ys, xs)].copy()
    hc = hole[np.ix_(ys, xs)].copy()
    # hole only where the window is actually inside the image (replicated area is context)
    inside = np.zeros_like(hc)
    iy = (np.arange(y0, y1) >= 0) & (np.arange(y0, y1) < H)
    ix = (np.arange(x0, x1) >= 0) & (np.arange(x0, x1) < W)
    inside[np.ix_(iy, ix)] = True
    hc &= inside
    # fill holes with local mean before resampling so bicubic does not smear UI colour into the context
    tmp = crop.copy()
    if hc.any():
        known = ~hc
        tmp[hc] = crop[known].mean(0) if known.any() else 0
    h, w = hc.shape
    c = _resize_f(tmp, size, size)
    mc = np.asarray(Image.fromarray((hc * 255).astype(np.uint8)).resize((size, size), Image.BILINEAR)) > 8
    x = np.clip(c, 0, 255)
    x[mc] = 0
    x = (x[..., ::-1] / 255.0).transpose(2, 0, 1)[None].astype(np.float32)
    out = tools.lama().run(None, {"image": x, "mask": mc.astype(np.float32)[None, None]})[0][0]
    out = out.transpose(1, 2, 0)[..., ::-1]
    out = np.clip(out, 0, 255).astype(np.float32)
    return _resize_f(out, w, h), (ys, xs, inside)


def fill(img, hole, win, paste=None, feather=0.0):
    """Inpaint: LaMa sees `hole` inside win; pixels of `paste` (default hole) inside win are overwritten.
    feather>0: soft alpha (gaussian of paste mask, clipped so that paste pixels are fully replaced)."""
    res = img.copy()
    out, (ys, xs, inside) = lama_win(img, hole, win)
    x0, y0, x1, y1 = win
    H, W = hole.shape
    p = hole if paste is None else paste
    # region of window inside image
    iy0, iy1 = max(0, y0), min(H, y1)
    ix0, ix1 = max(0, x0), min(W, x1)
    o = out[iy0 - y0:iy1 - y0, ix0 - x0:ix1 - x0]
    pm = p[iy0:iy1, ix0:ix1]
    sub = res[iy0:iy1, ix0:ix1]
    if feather > 0:
        a = np.asarray(Image.fromarray((pm * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(feather))).astype(np.float32) / 255.0
        a = np.maximum(a, pm.astype(np.float32))
        sub[:] = sub * (1 - a[..., None]) + o * a[..., None]
    else:
        sub[pm] = o[pm]
    return res


def rect(shape, x0, y0, x1, y1):
    m = np.zeros(shape, bool)
    m[max(0, int(y0)):int(y1), max(0, int(x0)):int(x1)] = True
    return m


def zoom(a, p, s=3):
    im = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))
    im.resize((im.width * s, im.height * s), Image.LANCZOS).save(p)
