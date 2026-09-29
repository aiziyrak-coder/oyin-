"""
Lynxos lobby v2 image tools (run with scratchpad\\venv\\Scripts\\python.exe).

  python tools.py crops                                  # canonical 1x page crops -> pages/<page>.png (+ _3x zoom)
  python tools.py zoom  in.png out.png X0 Y0 X1 Y1 [--scale 6]   # crop (1x coords) and enlarge for inspection, with a 10px grid
  python tools.py overlay page.png spec.json out.png [--scale 3] # draw spec element boxes + ids over the page
  python tools.py mask  spec.json out_mask.png [--pad 3]        # white = remove: every element with "remove": true (box, radius,
                                                                 # "pad") and every polygon in spec["removePolygons"]
  python tools.py inpaint in.png mask.png out.png                # LaMa, windowed, only masked pixels change
  python tools.py upscale in.png out.png (--width W | --height H | --scale S)   # Real-ESRGAN x4 (tiled) then Lanczos resize
  python tools.py blend in.png mask.png out.png                  # debug: red tint where the mask is

All coordinates are 1x page pixels (the canonical crop), float allowed, y down.
"""
import json, os, sys
from collections import deque
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
REF = os.path.join(HERE, "..", "ref")

# Canonical page rectangles inside pages.webp (1536x1024): inner content, borders excluded
PAGES = {
    "home": (0, 0, 766, 415),
    "map": (777, 0, 1536, 415),
    "wardrobe": (0, 422, 519, 731),
    "shops": (530, 422, 1013, 731),
    "education": (1024, 422, 1533, 731),
    "business": (0, 739, 518, 1024),
    "friends": (530, 739, 1022, 1024),
    "settings": (1034, 739, 1533, 1024),
}


def load(path):
    return Image.open(path).convert("RGB")


def cmd_crops():
    src = load(os.path.join(HERE, "pages.webp"))
    os.makedirs(os.path.join(HERE, "pages"), exist_ok=True)
    for name, box in PAGES.items():
        c = src.crop(box)
        c.save(os.path.join(HERE, "pages", name + ".png"))
        c.resize((c.width * 3, c.height * 3), Image.LANCZOS).save(os.path.join(HERE, "pages", name + "_3x.png"))
        print(name, c.size)


def font(size):
    for f in ("C:/Windows/Fonts/segoeui.ttf", "C:/Windows/Fonts/arial.ttf"):
        if os.path.exists(f):
            return ImageFont.truetype(f, size)
    return ImageFont.load_default()


def cmd_zoom(inp, out, x0, y0, x1, y1, scale=6):
    im = load(inp)
    x0, y0, x1, y1 = map(float, (x0, y0, x1, y1))
    c = im.crop((int(x0), int(y0), int(np.ceil(x1)), int(np.ceil(y1))))
    z = c.resize((c.width * scale, c.height * scale), Image.LANCZOS)
    d = ImageDraw.Draw(z, "RGBA")
    f = font(max(10, scale * 2))
    for gx in range(int(x0) - int(x0) % 10 + 10, int(np.ceil(x1)), 10):
        X = (gx - int(x0)) * scale
        d.line([(X, 0), (X, z.height)], fill=(0, 255, 255, 90))
        d.text((X + 2, 2), str(gx), fill=(0, 255, 255, 255), font=f)
    for gy in range(int(y0) - int(y0) % 10 + 10, int(np.ceil(y1)), 10):
        Y = (gy - int(y0)) * scale
        d.line([(0, Y), (z.width, Y)], fill=(255, 255, 0, 90))
        d.text((2, Y + 2), str(gy), fill=(255, 255, 0, 255), font=f)
    z.save(out)
    print(out, z.size)


def cmd_overlay(page, spec_path, out, scale=3):
    im = load(page)
    spec = json.load(open(spec_path, encoding="utf-8"))
    z = im.resize((im.width * scale, im.height * scale), Image.LANCZOS)
    d = ImageDraw.Draw(z, "RGBA")
    f = font(11)
    palette = [(255, 60, 60), (60, 220, 90), (60, 160, 255), (255, 200, 40), (220, 80, 255), (40, 230, 230)]
    for i, e in enumerate(spec.get("elements", [])):
        b = e.get("box")
        if not b:
            continue
        col = palette[i % len(palette)]
        r = [v * scale for v in b]
        d.rectangle(r, outline=col + (255,), width=1)
        d.text((r[0] + 2, r[1] + 1), e["id"], fill=col + (255,), font=f)
    for poly in spec.get("removePolygons", []):
        pts = [(p[0] * scale, p[1] * scale) for p in poly["points"]]
        d.polygon(pts, outline=(255, 0, 255, 255))
    ch = spec.get("character")
    if ch:
        for key in ("headTopY", "feetY", "horizonY"):
            if key in ch:
                Y = ch[key] * scale
                d.line([(0, Y), (z.width, Y)], fill=(255, 0, 255, 160))
                d.text((4, Y + 2), key, fill=(255, 0, 255, 255), font=f)
        if "centerX" in ch:
            X = ch["centerX"] * scale
            d.line([(X, 0), (X, z.height)], fill=(255, 0, 255, 160))
    z.save(out)
    print(out, z.size)


def rounded_box(d, box, radius, fill):
    x0, y0, x1, y1 = box
    radius = max(0, min(radius, (x1 - x0) / 2, (y1 - y0) / 2))
    d.rounded_rectangle([x0, y0, x1, y1], radius=radius, fill=fill)


def cmd_mask(spec_path, out, pad=3.0):
    spec = json.load(open(spec_path, encoding="utf-8"))
    W, H = spec["size"]
    S = 4  # supersample
    m = Image.new("L", (W * S, H * S), 0)
    d = ImageDraw.Draw(m)
    for e in spec.get("elements", []):
        if not e.get("remove"):
            continue
        b = e["box"]
        p = float(e.get("pad", pad))
        box = [(b[0] - p) * S, (b[1] - p) * S, (b[2] + p) * S, (b[3] + p) * S]
        rounded_box(d, box, (float(e.get("radius", 0)) + p) * S, 255)
    for poly in spec.get("removePolygons", []):
        pts = [(x * S, y * S) for x, y in poly["points"]]
        d.polygon(pts, fill=255)
        grow = float(poly.get("pad", pad))
        if grow > 0:
            d.line(pts + [pts[0]], fill=255, width=int(grow * 2 * S), joint="curve")
    m = m.resize((W, H), Image.BOX)
    m = m.point(lambda v: 255 if v > 20 else 0)
    m.save(out)
    print(out, "masked px:", int((np.asarray(m) > 0).sum()))


def cmd_blend(inp, mask, out):
    im = np.asarray(load(inp)).astype(np.float32)
    m = np.asarray(Image.open(mask).convert("L")) > 127
    im[m] = im[m] * 0.4 + np.array([255, 0, 0]) * 0.6
    Image.fromarray(im.astype(np.uint8)).save(out)


_lama = None


def lama():
    global _lama
    if _lama is None:
        import onnxruntime as ort
        o = ort.SessionOptions()
        o.intra_op_num_threads = 4
        _lama = ort.InferenceSession(os.path.join(REF, "lama.onnx"), o, providers=["CPUExecutionProvider"])
    return _lama


def components(m):
    H, W = m.shape
    seen = np.zeros_like(m)
    out = []
    ys, xs = np.nonzero(m)
    for y, x in zip(ys, xs):
        if seen[y, x]:
            continue
        q = deque([(y, x)])
        seen[y, x] = True
        x0 = x1 = x
        y0 = y1 = y
        while q:
            cy, cx = q.popleft()
            x0, x1, y0, y1 = min(x0, cx), max(x1, cx), min(y0, cy), max(y1, cy)
            for ny, nx in ((cy + 1, cx), (cy - 1, cx), (cy, cx + 1), (cy, cx - 1)):
                if 0 <= ny < H and 0 <= nx < W and m[ny, nx] and not seen[ny, nx]:
                    seen[ny, nx] = True
                    q.append((ny, nx))
        out.append((x0, y0, x1, y1))
    return out


def inpaint_window(image, m, x0, y0, sw, sh):
    crop = image[y0:y0 + sh, x0:x0 + sw]
    mcrop = m[y0:y0 + sh, x0:x0 + sw].copy()
    c = Image.fromarray(crop.astype(np.uint8)).resize((512, 512), Image.BICUBIC)
    mc = Image.fromarray((mcrop * 255).astype(np.uint8)).resize((512, 512), Image.NEAREST)
    x = (np.asarray(c).astype(np.float32)[..., ::-1] / 255.0).transpose(2, 0, 1)[None]  # LaMa: BGR, 0..1
    mm = (np.asarray(mc) > 0).astype(np.float32)[None, None]
    out = lama().run(None, {"image": x, "mask": mm})[0][0].transpose(1, 2, 0)[..., ::-1]
    out = np.clip(out, 0, 255).astype(np.uint8)
    out = np.asarray(Image.fromarray(out).resize((sw, sh), Image.BICUBIC)).astype(np.float32)
    crop[mcrop] = out[mcrop]


def cmd_inpaint(inp, mask_path, out):
    img = np.asarray(load(inp)).astype(np.float32)
    mask = np.asarray(Image.open(mask_path).convert("L")) > 127
    H, W = mask.shape
    result = img.copy()
    passes = 0
    while mask.any() and passes < 8:
        passes += 1
        comps = components(mask)
        print(f"pass {passes}: {len(comps)} regions")
        for (x0, y0, x1, y1) in sorted(comps, key=lambda b: -(b[2] - b[0]) * (b[3] - b[1])):
            if not mask[y0:y1 + 1, x0:x1 + 1].any():
                continue
            w, h = x1 - x0 + 1, y1 - y0 + 1
            # square context window ~2.2x the region (min 160 px, max the image), resampled to 512
            size = int(min(max(160, 2.2 * max(w, h)), min(H, W)))
            cx, cy = (x0 + x1) // 2, (y0 + y1) // 2
            wx = int(np.clip(cx - size // 2, 0, W - size))
            wy = int(np.clip(cy - size // 2, 0, H - size))
            inpaint_window(result, mask, wx, wy, size, size)
            mask[wy:wy + size, wx:wx + size] = False
    Image.fromarray(result.astype(np.uint8)).save(out)
    print("inpaint ok ->", out)


def cmd_upscale(inp, out, width=None, height=None, scale=None):
    import onnxruntime as ort
    o = ort.SessionOptions()
    o.intra_op_num_threads = 4
    sr = ort.InferenceSession(os.path.join(REF, "esrgan_x4v3.onnx"), o, providers=["CPUExecutionProvider"])
    img = np.asarray(load(inp)).astype(np.float32) / 255.0
    H, W, _ = img.shape
    pad, tile = 16, 192
    up = np.zeros((H * 4, W * 4, 3), np.float32)
    for y0 in range(0, H, tile):
        for x0 in range(0, W, tile):
            x1, y1 = min(W, x0 + tile), min(H, y0 + tile)
            px0, py0, px1, py1 = max(0, x0 - pad), max(0, y0 - pad), min(W, x1 + pad), min(H, y1 + pad)
            t = img[py0:py1, px0:px1].transpose(2, 0, 1)[None]
            r = sr.run(None, {"input": t})[0][0].transpose(1, 2, 0)
            oy, ox = (y0 - py0) * 4, (x0 - px0) * 4
            up[y0 * 4:y1 * 4, x0 * 4:x1 * 4] = r[oy:oy + (y1 - y0) * 4, ox:ox + (x1 - x0) * 4]
    res = Image.fromarray(np.clip(up * 255.0, 0, 255).astype(np.uint8))
    if width:
        res = res.resize((int(width), round(res.height * int(width) / res.width)), Image.LANCZOS)
    elif height:
        res = res.resize((round(res.width * int(height) / res.height), int(height)), Image.LANCZOS)
    elif scale:
        res = res.resize((round(W * float(scale)), round(H * float(scale))), Image.LANCZOS)
    res.save(out)
    print("upscale ok ->", out, res.size)


def opt(args, name, default=None):
    if name in args:
        i = args.index(name)
        v = args[i + 1]
        del args[i:i + 2]
        return v
    return default


if __name__ == "__main__":
    a = sys.argv[1:]
    c = a.pop(0)
    if c == "crops":
        cmd_crops()
    elif c == "zoom":
        s = int(opt(a, "--scale", 6))
        cmd_zoom(*a, scale=s)
    elif c == "overlay":
        s = int(opt(a, "--scale", 3))
        cmd_overlay(*a, scale=s)
    elif c == "mask":
        p = float(opt(a, "--pad", 3))
        cmd_mask(*a, pad=p)
    elif c == "inpaint":
        cmd_inpaint(*a)
    elif c == "upscale":
        w, h, s = opt(a, "--width"), opt(a, "--height"), opt(a, "--scale")
        cmd_upscale(*a, width=w, height=h, scale=s)
    elif c == "blend":
        cmd_blend(*a)
    else:
        print(__doc__)
