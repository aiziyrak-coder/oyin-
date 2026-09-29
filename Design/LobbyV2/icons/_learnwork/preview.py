"""Inspection sheet: big render with 24-grid + safe box, 24px x4 nearest, 32px x3 nearest, and a row of all icons at 24px."""
import json, sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import icon_render as ir
from PIL import Image, ImageDraw, ImageFont

data = json.load(open(sys.argv[1], encoding="utf-8-sig"))
icons = data["icons"]
if len(sys.argv) > 3:
    names = sys.argv[3].split(",")
    icons = [i for i in icons if i["name"] in names]
BIG = 240
cols = 3
cell_w, cell_h = BIG + 96 + 96 + 40, BIG + 30
rows = (len(icons) + cols - 1) // cols
bg = Image.new("RGB", (cols * cell_w, rows * cell_h + 60), (22, 30, 44))
d = ImageDraw.Draw(bg)
font = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 14)
u = BIG / 24
for k, ic in enumerate(icons):
    x, y = (k % cols) * cell_w + 8, (k // cols) * cell_h + 6
    for g in range(25):
        c = (40, 52, 74) if g % 2 else (48, 62, 88)
        d.line([x + g * u, y, x + g * u, y + BIG], fill=c)
        d.line([x, y + g * u, x + BIG, y + g * u], fill=c)
    d.rectangle([x + 2 * u, y + 2 * u, x + 22 * u, y + 22 * u], outline=(120, 70, 70))
    d.rectangle([x + 3 * u, y + 3 * u, x + 21 * u, y + 21 * u], outline=(70, 110, 70))
    im = ir.render(ic, BIG)
    bg.paste(im, (x, y), im)
    s24 = ir.render(ic, 24).resize((96, 96), Image.NEAREST)
    bg.paste(s24, (x + BIG + 10, y), s24)
    s32 = ir.render(ic, 32).resize((96, 96), Image.NEAREST)
    bg.paste(s32, (x + BIG + 10 + 100, y), s32)
    s24n = ir.render(ic, 24)
    bg.paste(s24n, (x + BIG + 10, y + 110), s24n)
    s16 = ir.render(ic, 18)
    bg.paste(s16, (x + BIG + 50, y + 110), s16)
    d.text((x, y + BIG + 4), ic["name"], fill=(220, 225, 240), font=font)
# row of all at 24
yy = rows * cell_h + 10
for k, ic in enumerate(icons):
    s = ir.render(ic, 24)
    bg.paste(s, (10 + k * 40, yy), s)
    s = ir.render(ic, 20)
    bg.paste(s, (12 + k * 40, yy + 30), s)
bg.save(sys.argv[2])
print(sys.argv[2], bg.size)
