"""Zoom inspector: each icon at 192px with 2..22 guide box + centre cross, and 24px/16px upscaled (nearest) to judge pixels."""
import json, sys, os
sys.path.insert(0, os.path.dirname(__file__))
from icon_render import render
from PIL import Image, ImageDraw, ImageFont
import numpy as np

data = json.load(open(sys.argv[1], encoding="utf-8-sig"))
names = sys.argv[3].split(",") if len(sys.argv) > 3 else None
icons = [i for i in data["icons"] if names is None or i["name"] in names]
S = 192
cw, ch = S + 24 * 4 + 30, S + 30
cols = 4
rows = (len(icons) + cols - 1) // cols
bg = Image.new("RGB", (cols * cw, rows * ch), (22, 30, 44))
d = ImageDraw.Draw(bg)
f = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 13)
for k, ic in enumerate(icons):
    x, y = (k % cols) * cw + 6, (k // cols) * ch + 4
    u = S / 24
    d.rectangle([x, y, x + S - 1, y + S - 1], outline=(40, 52, 74))
    d.rectangle([x + 2 * u, y + 2 * u, x + 22 * u, y + 22 * u], outline=(70, 50, 50))
    d.line([x + 12 * u, y, x + 12 * u, y + S], fill=(45, 60, 85))
    d.line([x, y + 12 * u, x + S, y + 12 * u], fill=(45, 60, 85))
    im = render(ic, S)
    bg.paste(im, (x, y), im)
    a = np.array(im)[..., 3]
    ys, xs = np.nonzero(a > 60)
    bb = (xs.min() / u, ys.min() / u, (xs.max() + 1) / u, (ys.max() + 1) / u)
    t = render(ic, 24).resize((96, 96), Image.NEAREST)
    bg.paste(t, (x + S + 8, y), t)
    t2 = render(ic, 16).resize((64, 64), Image.NEAREST)
    bg.paste(t2, (x + S + 8, y + 104), t2)
    d.text((x, y + S + 4), "%s  ink %.1f,%.1f-%.1f,%.1f  c(%.1f,%.1f)" % (ic["name"], *bb, (bb[0] + bb[2]) / 2, (bb[1] + bb[3]) / 2),
           fill=(200, 210, 230), font=f)
bg.save(sys.argv[2])
print(sys.argv[2], bg.size)
