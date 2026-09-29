import sys, numpy as np
from PIL import Image
IM=np.asarray(Image.open(r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\map.png").convert("RGB")).astype(float)
L=IM@[0.299,0.587,0.114]
# prof.py h Y0 Y1 X0 X1 -> mean over rows Y0..Y1 for each x ; prof.py v X0 X1 Y0 Y1 -> mean over cols for each y
m=sys.argv[1]; a,b,c,d=map(int,sys.argv[2:6])
if m=='h':
    v=L[a:b, c:d].mean(0); print(' '.join(f"{c+i}:{x:.0f}" for i,x in enumerate(v)))
else:
    v=L[c:d, a:b].mean(1); print(' '.join(f"{c+i}:{x:.0f}" for i,x in enumerate(v)))
