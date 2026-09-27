"""Intro teksturalari uchun umumiy yordamchi funksiyalar."""
import json
from PIL import Image

SCALE = 2          # PNG'lar 2x (4K ekranda tiniq)
CX, CY = 960, 540  # 1920x1080 ekran markazi


def crop_layer(src, dst, layout, layer_id, out_name, pad=6):
    """_full_<id>.png ni shaffof bo'lmagan qismiga qirqadi va joylashuvini layout ga yozadi."""
    im = Image.open(f"{src}/_full_{layer_id}.png").convert("RGBA")
    l, t, r, b = im.getchannel("A").getbbox()
    l, t = max(l - pad, 0), max(t - pad, 0)
    r, b = min(r + pad, im.width), min(b + pad, im.height)
    if (r - l) % 2: r += 1  # juft o'lcham: markaz yarim pikselga tushmasligi uchun
    if (b - t) % 2: b += 1
    im.crop((l, t, r, b)).save(f"{dst}/{out_name}.png", optimize=True)
    cx, cy = (l + r) / 2 / SCALE, (t + b) / 2 / SCALE
    layout[out_name] = {"w": (r - l) / SCALE, "h": (b - t) / SCALE, "x": cx - CX, "y": CY - cy}


def write_layout(layout, path):
    with open(path, "w") as f:
        json.dump(layout, f, indent=2)
