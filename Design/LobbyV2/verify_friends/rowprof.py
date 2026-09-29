import sys, numpy as np
from PIL import Image
P = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\friends.png"
im = np.asarray(Image.open(P).convert("RGB")).astype(float)
x0, y0, x1, y1 = map(int, sys.argv[1:5])
sub = im[y0:y1, x0:x1]
bg = np.median(np.concatenate([sub[0], sub[-1]]), 0)
d = np.sqrt(((sub - bg) ** 2).sum(2))
# per row: count of strong px and sum
peak = np.percentile(d, 98)
print(" ".join("%d:%.2f/%d" % (y0 + i, r.max() / peak, (r > 0.5 * peak).sum()) for i, r in enumerate(d)))
