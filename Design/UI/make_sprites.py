"""UI uchun 9-slice spritelar: yumaloq burchakli to'rtburchak (to'la va chiziqli) va "pill".
Ishlatish: make_sprites.py <chiqish_papkasi>
Rasmlar 2x: 24 px radius ekranda 12 birlik bo'ladi (Image.pixelsPerUnitMultiplier = 2)."""
import sys
import numpy as np
from PIL import Image

dst = sys.argv[1]


def rounded_sdf(size, radius):
    y, x = np.mgrid[0:size, 0:size] + 0.5
    c = size / 2
    qx = np.abs(x - c) - (c - radius)
    qy = np.abs(y - c) - (c - radius)
    outside = np.sqrt(np.maximum(qx, 0) ** 2 + np.maximum(qy, 0) ** 2)
    inside = np.minimum(np.maximum(qx, qy), 0)
    return outside + inside - radius  # < 0 ichkarida


def save(alpha, name):
    a = (np.clip(alpha, 0, 1) * 255).astype(np.uint8)
    rgb = np.full(a.shape + (3,), 255, np.uint8)
    Image.fromarray(np.dstack([rgb, a]), "RGBA").save(f"{dst}/{name}.png", optimize=True)


d = rounded_sdf(64, 24)
save(0.5 - d, "UI_Round12_Fill")
save(np.minimum(0.5 - d, d + 3.5), "UI_Round12_Stroke")   # 3 px (1.5 birlik) chiziq
d = rounded_sdf(64, 32)
save(0.5 - d, "UI_Pill_Fill")
save(np.minimum(0.5 - d, d + 3.5), "UI_Pill_Stroke")
print("ok")
