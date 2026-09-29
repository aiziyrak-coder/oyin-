import numpy as np
from PIL import Image
g = np.asarray(Image.open("pages.webp").convert("L")).astype(np.float32)
H, W = g.shape
def gapcols(y0, y1, xa, xb, thr=30):
    return [x for x in range(xa, xb) if np.percentile(g[y0:y1, x], 90) < thr]
def gaprows(x0, x1, ya, yb, thr=30):
    return [y for y in range(ya, yb) if np.percentile(g[y, x0:x1], 90) < thr]
print("row gaps (whole width)", gaprows(0, W, 400, 440), gaprows(0, W, 720, 760))
print("top rows", gaprows(0, W, 0, 8), "bottom rows", gaprows(0, W, 1010, 1024))
print("row1 cols", gapcols(20, 400, 740, 800), "edges", gapcols(20,400,0,8), gapcols(20,400,1526,1536))
print("row2 cols", gapcols(440, 715, 500, 560), gapcols(440, 715, 990, 1050), "edges", gapcols(440,715,0,8), gapcols(440,715,1526,1536))
print("row3 cols", gapcols(760, 1010, 500, 560), gapcols(760, 1010, 990, 1060), "edges", gapcols(760,1010,0,8), gapcols(760,1010,1526,1536))
