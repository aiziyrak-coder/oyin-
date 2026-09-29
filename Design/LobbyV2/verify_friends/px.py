"""px.py X0 Y0 X1 Y1 [lum|hex] -> print pixel grid (lum or hex) for region"""
import sys, numpy as np
from PIL import Image
P = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\friends.png"
im = np.asarray(Image.open(P).convert("RGB")).astype(int)
x0, y0, x1, y1 = map(int, sys.argv[1:5]); mode = sys.argv[5] if len(sys.argv) > 5 else "lum"
print("     " + " ".join(("%4d" if mode == "lum" else "%7d") % x for x in range(x0, x1)))
for y in range(y0, y1):
    row = []
    for x in range(x0, x1):
        r, g, b = im[y, x]
        if mode == "lum": row.append("%4d" % round(0.299*r + 0.587*g + 0.114*b))
        elif mode == "r": row.append("%4d" % (r - b))
        else: row.append(" %02X%02X%02X" % (r, g, b))
    print("%4d " % y + " ".join(row))
