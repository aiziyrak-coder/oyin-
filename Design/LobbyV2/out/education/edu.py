"""Helpers for the education page clean-up (hero crop inpaint variants, masks, compositing).
Run with the scratchpad venv python.  Usage: python edu.py <step>
"""
import json, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
V2 = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, V2)
import tools  # noqa: E402

PAGE = os.path.join(V2, "pages", "education.png")
WORK = os.path.join(HERE, "work")
os.makedirs(WORK, exist_ok=True)

HERO = (134, 43, 492, 179)  # asset box (picture inside the 1px frame), integer crop


def load(p):
    return np.asarray(Image.open(p).convert("RGB")).astype(np.float32)


def save(a, p):
    Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).save(p)


def rrect_mask(W, H, boxes, S=4, thr=20):
    """boxes: list of (x0,y0,x1,y1,radius) in pixel coords of the target image."""
    m = Image.new("L", (W * S, H * S), 0)
    d = ImageDraw.Draw(m)
    for (x0, y0, x1, y1, r) in boxes:
        r = max(0, min(r, (x1 - x0) / 2, (y1 - y0) / 2))
        d.rounded_rectangle([x0 * S, y0 * S, x1 * S, y1 * S], radius=r * S, fill=255)
    m = m.resize((W, H), Image.BOX)
    return np.asarray(m) > thr


def lama_rect(img, mask, x0, y0, w, h):
    """Run LaMa on an arbitrary rectangle (resampled to 512x512, may be anisotropic); fills masked px in place."""
    crop = img[y0:y0 + h, x0:x0 + w]
    mc = mask[y0:y0 + h, x0:x0 + w]
    c = Image.fromarray(np.clip(crop, 0, 255).astype(np.uint8)).resize((512, 512), Image.BICUBIC)
    mm = Image.fromarray((mc * 255).astype(np.uint8)).resize((512, 512), Image.BILINEAR)
    mm = (np.asarray(mm) > 0).astype(np.float32)[None, None]
    x = (np.asarray(c).astype(np.float32)[..., ::-1] / 255.0).transpose(2, 0, 1)[None]
    out = tools.lama().run(None, {"image": x, "mask": mm})[0][0].transpose(1, 2, 0)[..., ::-1]
    out = np.clip(out, 0, 255).astype(np.uint8)
    out = np.asarray(Image.fromarray(out).resize((w, h), Image.LANCZOS)).astype(np.float32)
    crop[mc] = out[mc]


def hero_crop():
    return load(PAGE)[HERO[1]:HERO[3], HERO[0]:HERO[2]].copy()


def hero_mask(card_box=(335.0, 49.0, 495.0, 171.0), card_r=8.0, ri_box=(130.0, 43.0, 142.6, 64.0)):
    """Mask inside the hero crop: overlay card (+rim/shadow) and the header 'ri' letters."""
    W, H = HERO[2] - HERO[0], HERO[3] - HERO[1]
    ox, oy = HERO[0], HERO[1]
    boxes = [(card_box[0] - ox, card_box[1] - oy, card_box[2] - ox, card_box[3] - oy, card_r),
             (ri_box[0] - ox, ri_box[1] - oy, ri_box[2] - ox, ri_box[3] - oy, 1.0)]
    return rrect_mask(W, H, boxes)


if __name__ == "__main__":
    step = sys.argv[1]
    if step == "hero_variants":
        m = hero_mask()
        save(np.where(m[..., None], 255, 0).repeat(3, 2) if False else m.astype(np.float32)[..., None].repeat(3, 2) * 255,
             os.path.join(WORK, "hero_mask.png"))
        base = hero_crop()
        H, W = m.shape
        # V2: whole crop stretched to 512x512 once
        a = base.copy(); mk = m.copy()
        lama_rect(a, mk, 0, 0, W, H)
        save(a, os.path.join(WORK, "hero_v2.png"))
        # V3: right 272 px window (2:1)
        a = base.copy(); mk = m.copy()
        lama_rect(a, mk, W - 272, 0, 272, H)
        save(a, os.path.join(WORK, "hero_v3.png"))
        # V4: progressive vertical strips, each window 2:1 with the strip in its right part
        a = base.copy(); mk = m.copy()
        xs = np.nonzero(mk.any(0))[0]
        # only the card component (x >= 150 in crop)
        cx0 = int(xs[xs > 150].min())
        step_w = 40
        x = cx0
        while x < W:
            x1 = min(W, x + step_w)
            sub = np.zeros_like(mk); sub[:, x:x1] = mk[:, x:x1]
            wx1 = x1; wx0 = max(0, wx1 - 272)
            lama_rect(a, sub, wx0, 0, wx1 - wx0, H)
            mk[:, x:x1] = False
            x = x1
        # remaining 'ri'
        lama_rect(a, mk, 0, 0, 136, H)
        save(a, os.path.join(WORK, "hero_v4.png"))
        print("ok")


def mirror_fill(a_axis, x_start=333, y_end=171, top_replace=True):
    """Fill the hero right part (x >= x_start page px, rows 43..y_end-1) with the left part mirrored about page x=a_axis."""
    img = hero_crop()
    ox, oy = HERO[0], HERO[1]
    H, W = img.shape[:2]
    out = img.copy()
    y0 = 0 if top_replace else 48 - oy
    y1 = y_end - oy
    for xp in range(x_start, HERO[2]):
        src = int(round(2 * a_axis - xp))
        out[y0:y1, xp - ox] = img[y0:y1, src - ox]
    return out


def run_mirror(tag, a_axis, x_start=333):
    img = mirror_fill(a_axis, x_start)
    ox, oy = HERO[0], HERO[1]
    H, W = img.shape[:2]
    # thin blend bands: vertical seam (if axis != seam), horizontal seam at the card bottom, and the 'ri' letters
    m = np.zeros((H, W), bool)
    if abs(a_axis - x_start) > 0.5:
        m[:, x_start - 3 - ox:x_start + 4 - ox] = True
        m[127:, :] = m[127:, :] & False
    m[168 - oy:174 - oy, x_start - 3 - ox:] = True
    m |= rrect_mask(W, H, [(130 - ox, 43 - oy, 142.6 - ox, 64 - oy, 1.0)])
    # LaMa passes: each band separately with a local square-ish window
    ri = rrect_mask(W, H, [(130 - ox, 43 - oy, 142.6 - ox, 64 - oy, 1.0)])
    lama_rect(img, ri.copy(), 0, 0, 100, 100)
    rest = m & ~ri
    # horizontal band in 2 windows (each 136 wide, full height)
    for wx0 in (x_start - 3 - ox - 20, W - 136):
        sub = np.zeros_like(rest); sub[:, wx0:wx0 + 136] = rest[:, wx0:wx0 + 136]
        lama_rect(img, sub, wx0, 0, 136, H)
        rest &= ~sub
    if rest.any():
        lama_rect(img, rest, max(0, x_start - ox - 68), 0, 136, H)
    save(img, os.path.join(WORK, f"hero_mir_{tag}.png"))
    save(m.astype(np.float32)[..., None].repeat(3, 2) * 255, os.path.join(WORK, f"hero_mir_{tag}_mask.png"))


if __name__ == "__main__" and sys.argv[1] == "mirror":
    for a in sys.argv[2:]:
        run_mirror(a, float(a))
    print("mirror ok")
