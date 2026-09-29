"""Local test: same as tools.py inpaint but masked pixels are zeroed before LaMa (to test for content leak)."""
import sys, numpy as np
sys.path.insert(0, r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2")
import tools
from PIL import Image
orig = tools.inpaint_window
def patched(image, m, x0, y0, sw, sh):
    crop = image[y0:y0 + sh, x0:x0 + sw]
    mc = m[y0:y0 + sh, x0:x0 + sw]
    saved = crop.copy()
    crop[mc] = 0
    orig(image, m, x0, y0, sw, sh)
tools.inpaint_window = patched
tools.cmd_inpaint(sys.argv[1], sys.argv[2], sys.argv[3])
