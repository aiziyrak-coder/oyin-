"""references/<ID>.png (3 ko'rinishli reference rasm) dan o'yin uchun rasmlar tayyorlaydi:
fon olib tashlanadi va har bir ko'rinish (old, yon, orqa) alohida PNG qilib qirqiladi.

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
PAD = 12  # piksel


def split_columns(alpha):
    """Uchta figurani ajratuvchi ikki ustunni topadi: 1/3 va 2/3 atrofidagi eng bo'sh ustunlar."""
    cols = alpha.sum(axis=0)
    w = alpha.shape[1]
    cuts = []
    for frac in (1 / 3, 2 / 3):
        lo, hi = int(w * frac - w * 0.12), int(w * frac + w * 0.12)
        cuts.append(lo + int(np.argmin(cols[lo:hi])))
    return [0] + cuts + [w]


def process(avatar_id, session):
    path = os.path.join(src_dir, f"{avatar_id}.png")
    image = Image.open(path).convert("RGB")
    cut = remove(image, session=session)  # RGBA
    alpha = np.asarray(cut)[..., 3].astype(np.float32) / 255
    bounds = split_columns(alpha)
    for i, view in enumerate(VIEWS):
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
    ids = sys.argv[1:] or sorted(f[:-4] for f in os.listdir(src_dir) if f.endswith(".png"))
    session = new_session("isnet-general-use")
    for avatar_id in ids:
        process(avatar_id, session)
