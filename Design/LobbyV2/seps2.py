import numpy as np
from PIL import Image
im = np.asarray(Image.open("pages.webp").convert("RGB")).astype(np.float32)
g = im.mean(axis=2)
for name, (y0, y1) in {"row2": (425, 728), "row3": (742, 1020)}.items():
    s = g[y0:y1].std(axis=0); m = g[y0:y1].mean(axis=0)
    xs = [x for x in range(400, 1200) if s[x] < 8]
    print(name, xs)
