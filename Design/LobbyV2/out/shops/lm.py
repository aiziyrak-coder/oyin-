"""Custom LaMa helpers for the shops page (uses tools.lama())."""
import os, sys
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
V2 = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, V2)
import onnxruntime as ort
ort.set_default_logger_severity(3)
import tools  # noqa

PAGE = os.path.join(V2, "pages", "shops.png")


def load(p):
    return np.asarray(Image.open(p).convert("RGB")).astype(np.float32)


def loadm(p):
    return np.asarray(Image.open(p).convert("L")) > 127


def save(a, p):
    Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).save(p)


def run512(crop, mcrop, resample=Image.BICUBIC):
    """crop HxWx3 float, mcrop HxW bool -> LaMa output resized back to HxW (float)."""
    h, w = mcrop.shape
    c = Image.fromarray(np.clip(crop, 0, 255).astype(np.uint8)).resize((512, 512), resample)
    # mask: resize with box filter and threshold low so the hole never shrinks
    mc = Image.fromarray((mcrop * 255).astype(np.uint8)).resize((512, 512), Image.BILINEAR)
    mm = (np.asarray(mc) > 10).astype(np.float32)
    x = np.asarray(c).astype(np.float32)
    x[mm > 0] = 0
    x = (x[..., ::-1] / 255.0).transpose(2, 0, 1)[None]
    out = tools.lama().run(None, {"image": x, "mask": mm[None, None]})[0][0].transpose(1, 2, 0)[..., ::-1]
    out = np.clip(out, 0, 255).astype(np.uint8)
    return np.asarray(Image.fromarray(out).resize((w, h), Image.BICUBIC)).astype(np.float32)


def fill(img, mask, win, paste=None):
    """Inpaint mask inside window win=(x0,y0,x1,y1) (ints, exclusive end). paste: bool mask of pixels to
    overwrite (default: mask within window). Returns new image."""
    x0, y0, x1, y1 = [int(round(v)) for v in win]
    res = img.copy()
    crop = img[y0:y1, x0:x1]
    mc = mask[y0:y1, x0:x1]
    out = run512(crop, mc)
    p = mc if paste is None else paste[y0:y1, x0:x1]
    sub = res[y0:y1, x0:x1]
    sub[p] = out[p]
    return res


def rect(mask_shape, x0, y0, x1, y1):
    m = np.zeros(mask_shape, bool)
    m[int(y0):int(y1), int(x0):int(x1)] = True
    return m


def zoom(a, p, s=3):
    im = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))
    im.resize((im.width * s, im.height * s), Image.LANCZOS).save(p)
