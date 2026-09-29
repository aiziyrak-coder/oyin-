import sys, json, numpy as np
from PIL import Image, ImageDraw, ImageFont
HERE = r"C:/Users/alocomputers/AppData/Local/Temp/claude/D--Game1/4f1a5cc1-c33d-4082-8691-871fd7139403/scratchpad/v2"
def font(s):
    return ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", s)
def main():
    a = sys.argv[1:]
    spec_path, out = a[0], a[1]
    x0, y0, x1, y1 = map(float, a[2:6])
    scale = int(a[6]) if len(a) > 6 else 6
    ids = a[7].split(',') if len(a) > 7 and a[7] != '-' else None
    im = Image.open(HERE + "/pages/wardrobe.png").convert("RGB")
    X0, Y0, X1, Y1 = int(x0), int(y0), int(np.ceil(x1)), int(np.ceil(y1))
    c = im.crop((X0, Y0, X1, Y1))
    z = c.resize((c.width * scale, c.height * scale), Image.NEAREST if '--nn' in a else Image.LANCZOS)
    d = ImageDraw.Draw(z, "RGBA")
    f = font(max(10, scale * 2))
    for gx in range(X0, X1):
        if gx % 5 == 0:
            X = (gx - X0) * scale
            d.line([(X, 0), (X, z.height)], fill=(0, 255, 255, 110 if gx % 10 == 0 else 45))
            if gx % 10 == 0: d.text((X + 2, 2), str(gx), fill=(0, 255, 255, 255), font=f)
    for gy in range(Y0, Y1):
        if gy % 5 == 0:
            Y = (gy - Y0) * scale
            d.line([(0, Y), (z.width, Y)], fill=(255, 255, 0, 110 if gy % 10 == 0 else 45))
            if gy % 10 == 0: d.text((2, Y + 2), str(gy), fill=(255, 255, 0, 255), font=f)
    spec = json.load(open(spec_path, encoding="utf-8"))
    pal = [(255, 60, 60), (60, 230, 90), (80, 170, 255), (255, 200, 40), (230, 90, 255), (40, 240, 240)]
    fl = font(12)
    for i, e in enumerate(spec["elements"]):
        if ids and not any(e["id"].startswith(p) for p in ids): continue
        b = e["box"]
        if b[2] < x0 or b[0] > x1 or b[3] < y0 or b[1] > y1: continue
        col = pal[i % len(pal)]
        r = [(b[0] - X0) * scale, (b[1] - Y0) * scale, (b[2] - X0) * scale, (b[3] - Y0) * scale]
        d.rectangle(r, outline=col + (255,), width=1)
        d.text((r[0] + 2, r[3] + 1), e["id"].split('.')[-1], fill=col + (255,), font=fl)
    if '--poly' in a:
        for p in spec.get("removePolygons", []):
            pts = [((x - X0) * scale, (y - Y0) * scale) for x, y in p["points"]]
            d.polygon(pts, outline=(255, 0, 255, 255))
    z.save(out)
    print(out, z.size)
main()
