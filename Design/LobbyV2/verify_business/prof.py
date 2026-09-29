"""prof.py x0 y0 x1 y1 [--chan L|R|G|B|S] : print pixel values table of region (rows y, cols x) for chosen channel."""
import sys, numpy as np
from PIL import Image
a = sys.argv[1:]
chan = 'L'
if '--chan' in a:
    i = a.index('--chan'); chan = a[i + 1]; del a[i:i + 2]
x0, y0, x1, y1 = map(int, a[:4])
im = np.asarray(Image.open(r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\business.png").convert('RGB')).astype(float)
r = im[y0:y1, x0:x1]
if chan == 'L':
    v = r @ np.array([.299, .587, .114])
elif chan == 'BR':
    v = r[..., 2] - r[..., 0]
elif chan == 'S':
    v = r.max(2) - r.min(2)
else:
    v = r[..., 'RGB'.index(chan)]
print('     ' + ' '.join(f'{x:4d}' for x in range(x0, x1)))
for j, y in enumerate(range(y0, y1)):
    print(f'{y:4d} ' + ' '.join(f'{int(t):4d}' for t in v[j]))
