"""CraDev intro qatlamlarini qirqadi va protsedurali teksturalarni (nur, uchqun, yaltirash) yaratadi.
Ishlatish: make_textures.py <render_papkasi> <Art_papkasi> <layout.json>"""
import os, sys
import numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "tools"))
from texlib import crop_layer, grid, save_rgba, radial_glow, write_layout

src, dst, layout_path = sys.argv[1:4]
layout = {}
crop_layer(src, dst, layout, "emblem", "CraDev_Emblem")
crop_layer(src, dst, layout, "wordglow", "CraDev_WordmarkGlow")
crop_layer(src, dst, layout, "wordmark", "CraDev_Wordmark", pad=2)
crop_layer(src, dst, layout, "divider", "CraDev_Divider")
crop_layer(src, dst, layout, "tagline", "CraDev_Tagline")

# Logo ortidagi katta yumshoq nur (markazda moviy, chetda binafsha)
radial_glow(f"{dst}/CraDev_Glow.png", [70, 200, 255], [150, 90, 255])

# Uchqun: oq yumshoq nuqta, rangi Unity'da beriladi (boshqa sahnalarda ham ishlatiladi)
n = 64
x, y = grid(n, n)
r = np.sqrt(x * x + y * y)
alpha = np.clip(np.exp(-(r / 0.16) ** 2) + 0.6 * np.exp(-(r / 0.05) ** 2), 0, 1) * (1 - np.clip((r - 0.44) / 0.06, 0, 1))
save_rgba(np.array([255, 255, 255]), alpha, f"{dst}/CraDev_Particle.png")

# Yaltirash chizig'i: yozuv ustidan o'tadigan oq nur
x, y = grid(128, 512)
save_rgba(np.array([255, 255, 255]), 0.85 * np.exp(-(x / 0.17) ** 2), f"{dst}/CraDev_Shine.png")

write_layout(layout, layout_path)
