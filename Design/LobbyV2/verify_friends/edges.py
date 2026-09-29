import numpy as np
from PIL import Image
P = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\friends.png"
im = np.asarray(Image.open(P).convert("RGB")).astype(float)
L = im @ [0.299, 0.587, 0.114]
H, W = L.shape
print("size", W, H)
for name, prof in [("top rows 0-5 (median over x 20..470)", [np.median(L[y, 20:470]) for y in range(6)]),
                   ("bottom rows H-6..H-1", [np.median(L[y, 20:470]) for y in range(H-6, H)]),
                   ("left cols 0-5 (median over y 20..265)", [np.median(L[20:265, x]) for x in range(6)]),
                   ("right cols W-6..W-1", [np.median(L[20:265, x]) for x in range(W-6, W)])]:
    print(name, " ".join("%.0f" % v for v in prof))
