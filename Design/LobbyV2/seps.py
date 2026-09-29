import numpy as np
from PIL import Image
im = np.asarray(Image.open("pages.webp").convert("RGB")).astype(np.float32)
H, W, _ = im.shape
g = im.mean(axis=2)
# separator rows/cols: low variance lines
def lines(axis_mean_var, n, name):
    out = []
    for i, v in enumerate(axis_mean_var):
        out.append((i, v))
    return out
rowvar = g.std(axis=1); rowmean = g.mean(axis=1)
colvar_top = g[:410].std(axis=0); colmean_top = g[:410].mean(axis=0)
print("rows (y, mean, std) candidates:")
for y in range(H):
    if rowvar[y] < 12: print(y, round(rowmean[y],1), round(rowvar[y],1))
print("cols top:")
for x in range(W):
    if colvar_top[x] < 12: print(x, round(colmean_top[x],1), round(colvar_top[x],1))
