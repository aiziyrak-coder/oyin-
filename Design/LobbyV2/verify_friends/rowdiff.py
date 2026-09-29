import sys, numpy as np
from PIL import Image
P = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\friends.png"
L = np.asarray(Image.open(P).convert("RGB")).astype(float) @ [0.299, 0.587, 0.114]
y = int(sys.argv[1]); x0, x1 = int(sys.argv[2]), int(sys.argv[3])
d = L[y, x0:x1] - (L[y-2, x0:x1] + L[y+2, x0:x1]) / 2
print(" ".join("%d:%+.0f" % (x0+i, v) for i, v in enumerate(d)))
