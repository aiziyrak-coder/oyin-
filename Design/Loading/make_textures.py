"""Loading ekrani teksturalari: olti burchak ramka, aylanuvchi "kometa" yoy va progress chizig'i.
Ishlatish: make_textures.py <Art_papkasi>"""
import math, os, sys
import numpy as np
from PIL import Image, ImageDraw
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "tools"))
from texlib import grid, save_rgba

dst = sys.argv[1]
WHITE = np.array([255, 255, 255])

# Olti burchak ramka (uchi tepada), 4x kattalikda chizib kichraytiriladi - silliq chetlar uchun
S, SS = 480, 4
big = Image.new("L", (S * SS, S * SS), 0)
c, r = S * SS / 2, S * SS / 2 - 10 * SS
pts = [(c + r * math.cos(math.radians(a)), c + r * math.sin(math.radians(a))) for a in range(-90, 270, 60)]
ImageDraw.Draw(big).line(pts + [pts[0], pts[1]], fill=255, width=5 * SS, joint="curve")
alpha = np.asarray(big.resize((S, S), Image.LANCZOS), dtype=np.float32) / 255
save_rgba(WHITE, alpha, f"{dst}/Loading_Hex.png")

# Kometa yoy: 300 gradus davomida shaffofdan to'liqgacha, boshi yumaloq
n = 512
x, y = grid(n, n)
rr = np.sqrt(x * x + y * y)
ang = (np.degrees(np.arctan2(x, -y)) + 360) % 360           # 0 = tepa, soat mili bo'yicha
R, W = 0.46, 0.012
ring = np.clip((W - np.abs(rr - R)) / (1.0 / n) + 0.5, 0, 1)
tail = np.clip(ang / 300, 0, 1) ** 1.8 * (ang <= 300)
head_x, head_y = R * math.sin(math.radians(300)), -R * math.cos(math.radians(300))
head = np.clip((W * 1.6 - np.sqrt((x - head_x) ** 2 + (y - head_y) ** 2)) / (1.0 / n) + 0.5, 0, 1)
save_rgba(WHITE, np.maximum(ring * tail, head), f"{dst}/Loading_Arc.png")

# Progress chizig'i: moviydan binafshagacha gradient
w, h = 1024, 8
t = np.linspace(0, 1, w)[None, :, None]
rgb = (1 - t) * np.array([46, 230, 255]) + t * np.array([157, 92, 255])
save_rgba(np.broadcast_to(rgb, (h, w, 3)), np.ones((h, w)), f"{dst}/Loading_BarFill.png")
print("ok")
