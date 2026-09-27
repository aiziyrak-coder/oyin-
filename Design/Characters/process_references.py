"""references/<ID>.png (3 ko'rinishli reference rasm) dan o'yin uchun rasmlar tayyorlaydi:
fon olib tashlanadi va har bir ko'rinish (old, yon, orqa) alohida PNG qilib qirqiladi.
Ixtiyoriy references/<ID>_quarter.png (2 ko'rinish: 45° old va 135° orqa) bo'lsa, u ham qo'shiladi:
qahramon aylanganda o'tishlar silliqroq bo'ladi.

Ishlatish: python3 process_references.py            (hamma rasmlar)
           python3 process_references.py M4          (faqat bittasi)
Talab: pip install "rembg[cpu]"  (model birinchi ishga tushishda yuklanadi)
"""
import os, sys
import numpy as np
from PIL import Image
from rembg import remove, new_session

here = os.path.dirname(os.path.abspath(__file__))
src_dir = os.path.join(here, "references")
dst_dir = os.path.join(here, "..", "..", "Assets", "CraDev", "Avatars", "Photos")
VIEWS = ["Front", "Side", "Back"]
QUARTER_VIEWS = ["FrontQuarter", "BackQuarter"]
PAD = 12  # piksel


def split_columns(alpha, count):
    """count ta figurani ajratuvchi ustunlarni topadi: har bir chegara atrofidagi eng bo'sh ustun."""
    cols = alpha.sum(axis=0)
    w = alpha.shape[1]
    cuts = []
    for k in range(1, count):
        frac = k / count
        lo, hi = int(w * frac - w * 0.12), int(w * frac + w * 0.12)
        cuts.append(lo + int(np.argmin(cols[lo:hi])))
    return [0] + cuts + [w]


def process(avatar_id, session):
    cut_sheet(os.path.join(src_dir, f"{avatar_id}.png"), avatar_id, VIEWS, session)
    quarter = os.path.join(src_dir, f"{avatar_id}_quarter.png")
    if os.path.exists(quarter):
        cut_sheet(quarter, avatar_id, QUARTER_VIEWS, session)


def cut_sheet(path, avatar_id, views, session):
    image = Image.open(path).convert("RGB")
    cut = remove(image, session=session)  # RGBA
    alpha = np.asarray(cut)[..., 3].astype(np.float32) / 255
    bounds = split_columns(alpha, len(views))
    for i, view in enumerate(views):
        x0, x1 = bounds[i], bounds[i + 1]
        part = alpha[:, x0:x1] > 0.05
        ys, xs = np.nonzero(part)
        left, right = max(xs.min() + x0 - PAD, x0), min(xs.max() + x0 + PAD, x1)
        top, bottom = max(ys.min() - PAD, 0), min(ys.max() + PAD, alpha.shape[0])
        piece = cut.crop((left, top, right, bottom))
        out = os.path.join(dst_dir, f"{avatar_id}_{view}.png")
        piece.save(out, optimize=True)
        print(f"{avatar_id} {view}: {piece.size[0]}x{piece.size[1]}")


if __name__ == "__main__":
    os.makedirs(dst_dir, exist_ok=True)
    ids = sys.argv[1:] or sorted(f[:-4] for f in os.listdir(src_dir) if f.endswith(".png") and "_" not in f)
    session = new_session("isnet-general-use")
    for avatar_id in ids:
        process(avatar_id, session)
