"""Intro qatlamlarini kesib oladi va protsedurali teksturalarni (nur, zarra, yaltirash) yaratadi."""
import json, sys
import numpy as np
from PIL import Image

src, dst = sys.argv[1], sys.argv[2]  # ishlatish: make_textures.py <render_papkasi> <Art_papkasi> <layout.json>
SCALE = 2          # PNG'lar 2x (4K uchun tiniq)
CX, CY = 960, 540  # 1920x1080 ekran markazi
rng = np.random.default_rng(7)
layout = {}

def crop_layer(name, out_name, pad=6):
    im = Image.open(f"{src}/_full_{name}.png").convert("RGBA")
    l, t, r, b = im.getchannel("A").getbbox()
    l, t = max(l - pad, 0), max(t - pad, 0)
    r, b = min(r + pad, im.width), min(b + pad, im.height)
    # juft o'lcham: markaz yarim pikselga tushmasligi uchun
    if (r - l) % 2: r += 1
    if (b - t) % 2: b += 1
    im.crop((l, t, r, b)).save(f"{dst}/{out_name}.png", optimize=True)
    w, h = (r - l) / SCALE, (b - t) / SCALE
    cx, cy = (l + r) / 2 / SCALE, (t + b) / 2 / SCALE
    layout[out_name] = {"w": w, "h": h, "x": cx - CX, "y": CY - cy}

crop_layer("emblem", "CraDev_Emblem")
crop_layer("wordglow", "CraDev_WordmarkGlow")
crop_layer("wordmark", "CraDev_Wordmark", pad=2)
crop_layer("divider", "CraDev_Divider")
crop_layer("tagline", "CraDev_Tagline")

def save_rgba(rgb, alpha, path):
    a = np.clip(alpha * 255 + rng.uniform(-0.5, 0.5, alpha.shape), 0, 255).astype(np.uint8)  # dithering: yo'l-yo'l bo'lmasligi uchun
    img = np.dstack([rgb.astype(np.uint8), a])
    Image.fromarray(img, "RGBA").save(path, optimize=True)

def grid(w, h):
    y, x = np.mgrid[0:h, 0:w]
    return (x + 0.5) / w - 0.5, (y + 0.5) / h - 0.5

# Logo ortidagi katta yumshoq nur (markazda moviy, chetda binafsha)
n = 1024
x, y = grid(n, n)
r = np.sqrt(x * x + y * y)
alpha = np.exp(-(r / 0.24) ** 2) * (1 - np.clip((r - 0.40) / 0.10, 0, 1))
t = np.clip(r / 0.5, 0, 1)[..., None]
rgb = (1 - t) * np.array([70, 200, 255]) + t * np.array([150, 90, 255])
save_rgba(rgb, alpha, f"{dst}/CraDev_Glow.png")
layout["CraDev_Glow"] = {"w": n / SCALE * 2.2, "h": n / SCALE * 2.2}

# Zarra (uchqun) - oq yumshoq nuqta, rangi Unity'da beriladi
n = 64
x, y = grid(n, n)
r = np.sqrt(x * x + y * y)
alpha = np.clip(np.exp(-(r / 0.16) ** 2) + 0.6 * np.exp(-(r / 0.05) ** 2), 0, 1) * (1 - np.clip((r - 0.44) / 0.06, 0, 1))
save_rgba(np.full((n, n, 3), 255), alpha, f"{dst}/CraDev_Particle.png")

# Yaltirash chizig'i - yozuv ustidan o'tadigan oq nur
w, h = 128, 512
x, y = grid(w, h)
alpha = 0.85 * np.exp(-(x / 0.17) ** 2)
save_rgba(np.full((h, w, 3), 255), alpha, f"{dst}/CraDev_Shine.png")

json.dump(layout, open(sys.argv[3], "w"), indent=2)
print(json.dumps(layout, indent=2))
