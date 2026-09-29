import sys, numpy as np
from PIL import Image
P = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\settings.png"
im = np.asarray(Image.open(P).convert("RGB")).astype(int)
x0,y0,x1,y1 = map(int, sys.argv[1:5])
ch = sys.argv[5] if len(sys.argv)>5 else "L"
print("     " + " ".join(f"{x:>4d}" for x in range(x0,x1)))
for y in range(y0,y1):
    row=[]
    for x in range(x0,x1):
        r,g,b = im[y,x]
        v = {"L": int(0.299*r+0.587*g+0.114*b), "R": r, "G": g, "B": b, "RB": r-b, "RG": r-g}[ch]
        row.append(f"{v:>4d}")
    print(f"{y:>4d} " + " ".join(row))
