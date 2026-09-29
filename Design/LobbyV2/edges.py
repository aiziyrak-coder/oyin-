import numpy as np
from PIL import Image
im = np.asarray(Image.open("pages.webp").convert("RGB")).astype(np.float32)
g = im.mean(axis=2)
H, W = g.shape
# For each page region guess, print mean brightness profile near edges to locate inner borders
def prof(name, x0, y0, x1, y1):
    sub = g[y0:y1, x0:x1]
    cols = sub.mean(axis=0); rows = sub.mean(axis=1)
    print(name, "left cols", [round(v) for v in cols[:8]], "right", [round(v) for v in cols[-8:]])
    print(name, "top rows", [round(v) for v in rows[:8]], "bottom", [round(v) for v in rows[-8:]])
prof("p1", 0, 0, 772, 420)
prof("p2", 770, 0, 1536, 420)
prof("p3", 0, 415, 526, 740)
prof("p4", 516, 415, 1024, 740)
prof("p5", 1012, 415, 1536, 740)
prof("p6", 0, 728, 526, 1024)
prof("p7", 516, 728, 1030, 1024)
prof("p8", 1018, 728, 1536, 1024)
import os
print(os.cpu_count())
