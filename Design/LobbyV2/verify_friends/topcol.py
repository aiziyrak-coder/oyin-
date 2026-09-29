import sys, numpy as np
from PIL import Image
P = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\friends.png"
im = np.asarray(Image.open(P).convert("RGB")).astype(float)
hx = lambda c: "#%02X%02X%02X" % tuple(int(round(v)) for v in c)
x0, y0, x1, y1 = map(int, sys.argv[1:5])
sub = im[y0:y1, x0:x1].reshape(-1, 3); L = sub @ [0.299, 0.587, 0.114]
o = np.argsort(L)
for p in (50, 25, 10, 5, 2):
    k = max(1, len(o) * p // 100)
    print(f"top{p}%", hx(np.median(sub[o[-k:]], 0)), end="  ")
print()
