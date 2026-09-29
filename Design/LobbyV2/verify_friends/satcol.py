import sys, numpy as np
from PIL import Image
P = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\friends.png"
im = np.asarray(Image.open(P).convert("RGB")).astype(float)
hx = lambda c: "#%02X%02X%02X" % tuple(int(round(v)) for v in c)
for name, box, ch in [("s0", (50, 130, 75, 138), "g"), ("s1", (50, 166, 76, 174), "g"), ("s2", (50, 202, 75, 210), "r"), ("s3", (50, 238, 72, 245), "l"), ("bell_dot", (395, 14, 401, 18), "r")]:
    x0, y0, x1, y1 = box
    sub = im[y0:y1, x0:x1].reshape(-1, 3)
    R, G, B = sub.T
    if ch == "g": s = G - np.maximum(R, B)
    elif ch == "r": s = R - np.maximum(G, B)
    else: s = sub @ [0.299, 0.587, 0.114]
    o = np.argsort(s)
    print(name, "top5%", hx(np.median(sub[o[-max(2, len(o)//20):]], 0)), "top10%", hx(np.median(sub[o[-max(3, len(o)//10):]], 0)), "max", hx(sub[o[-1]]), "n", len(o))
