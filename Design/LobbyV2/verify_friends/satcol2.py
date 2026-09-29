import numpy as np
from PIL import Image
P = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\friends.png"
im = np.asarray(Image.open(P).convert("RGB")).astype(float)
hx = lambda c: "#%02X%02X%02X" % tuple(int(round(v)) for v in c)
for name, box, ch in [("s0", (50, 130, 75, 138), 1), ("s1", (50, 166, 76, 174), 1), ("s2", (50, 202, 75, 210), 0)]:
    x0, y0, x1, y1 = box
    sub = im[y0:y1, x0:x1].reshape(-1, 3)
    # score: channel minus the mean of the other two, weighted by channel value -> bright AND hued
    oth = [c for c in range(3) if c != ch]
    s = sub[:, ch] - sub[:, oth].mean(1)
    s2 = sub[:, ch]
    mask = s > np.percentile(s, 70)
    o = np.argsort(np.where(mask, s2, -1))
    print(name, "hued&bright top8", hx(np.median(sub[o[-8:]], 0)), "top15", hx(np.median(sub[o[-15:]], 0)))
