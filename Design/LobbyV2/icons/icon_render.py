"""
Lynxos line-icon renderer (preview). Must match CraDevSceneBuilder.Icons.cs exactly.

Icon file format (UTF-8 JSON):
{ "icons": [ { "name": "Shirt", "stroke": 1.6, "paths": ["L 4 5 8 3 ...", "A 12 12 5 5 0 180", ...] }, ... ] }

Coordinates: 24x24 grid, origin TOP-LEFT, y DOWN (like SVG). Angles in degrees, 0 = +x (right), 90 = DOWN (+y),
measured clockwise on screen (SVG convention).
Commands (one primitive per string, numbers separated by spaces):
  L x1 y1 x2 y2 ...          open polyline (stroked)
  Z x1 y1 x2 y2 ...          closed polyline / polygon outline (stroked)
  A cx cy rx ry from to      elliptical arc from angle 'from' to 'to' (stroked); to may be < from
  C cx cy r                  circle outline (stroked)
  D cx cy r                  filled disc
  R x y w h r                rounded-rectangle outline (stroked), r = corner radius
  F x1 y1 x2 y2 ...          filled polygon (even-odd)
  FR x y w h r               filled rounded rectangle
"stroke" = stroke WIDTH in grid units (default 1.6 ~ Lucide 2px at 24px looks a bit bold; concept icons are thin: 1.5).

Usage:
  python icon_render.py icons.json out.png [--size 96] [--names A,B]   -> contact sheet (each icon at size px on dark bg,
                                                                           plus a 32px and 24px version to judge small sizes)
"""
import json, math, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont


def parse(path_str):
    t = path_str.split()
    return t[0], [float(v) for v in t[1:]]


def arc_points(cx, cy, rx, ry, a0, a1):
    n = max(8, int(abs(a1 - a0) / 6))
    return [(cx + math.cos(math.radians(a0 + (a1 - a0) * i / n)) * rx,
             cy + math.sin(math.radians(a0 + (a1 - a0) * i / n)) * ry) for i in range(n + 1)]


def rounded_rect_points(x, y, w, h, r):
    r = max(0.0, min(r, w / 2, h / 2))
    pts = []
    for (cx, cy, start) in ((x + w - r, y + r, -90), (x + w - r, y + h - r, 0), (x + r, y + h - r, 90), (x + r, y + r, 180)):
        for i in range(7):
            a = math.radians(start + 90 * i / 6)
            pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    return pts


def geometry(icon):
    """-> (segments, circles(cx,cy,r stroked), discs, polygons(filled))"""
    segs, circles, discs, polys = [], [], [], []

    def poly(pts, closed):
        for i in range(len(pts) - 1):
            segs.append((pts[i], pts[i + 1]))
        if closed and len(pts) > 2:
            segs.append((pts[-1], pts[0]))

    for p in icon["paths"]:
        cmd, v = parse(p)
        if cmd in ("L", "Z"):
            pts = list(zip(v[0::2], v[1::2]))
            poly(pts, cmd == "Z")
        elif cmd == "A":
            poly(arc_points(*v[:6]), False)
        elif cmd == "C":
            circles.append(tuple(v[:3]))
        elif cmd == "D":
            discs.append(tuple(v[:3]))
        elif cmd == "R":
            poly(rounded_rect_points(*v[:5]), True)
        elif cmd == "F":
            polys.append(list(zip(v[0::2], v[1::2])))
        elif cmd == "FR":
            polys.append(rounded_rect_points(*v[:5]))
        else:
            raise ValueError("unknown command " + cmd + " in " + icon["name"])
    return segs, circles, discs, polys


def seg_dist(px, py, a, b):
    ax, ay = a
    bx, by = b
    dx, dy = bx - ax, by - ay
    L = dx * dx + dy * dy
    t = np.clip(((px - ax) * dx + (py - ay) * dy) / max(L, 1e-9), 0, 1)
    return np.hypot(px - (ax + t * dx), py - (ay + t * dy))


def inside(px, py, pts):
    res = np.zeros(px.shape, bool)
    n = len(pts)
    for i in range(n):
        x1, y1 = pts[i]
        x2, y2 = pts[(i + 1) % n]
        cond = ((y1 > py) != (y2 > py)) & (px < (x2 - x1) * (py - y1) / ((y2 - y1) if y2 != y1 else 1e-9) + x1)
        res ^= cond
    return res


def poly_edge_dist(px, py, pts):
    d = np.full(px.shape, 1e9)
    for i in range(len(pts)):
        d = np.minimum(d, seg_dist(px, py, pts[i], pts[(i + 1) % len(pts)]))
    return d


def render(icon, size):
    """RGBA white icon, alpha coverage, size x size px (same math as the C# builder)."""
    segs, circles, discs, polys = geometry(icon)
    half = icon.get("stroke", 1.6) / 2.0
    unit_px = size / 24.0
    ys, xs = np.mgrid[0:size, 0:size]
    px = (xs + 0.5) / unit_px
    py = (ys + 0.5) / unit_px
    d = np.full((size, size), 1e9)
    for a, b in segs:
        d = np.minimum(d, seg_dist(px, py, a, b))
    for cx, cy, r in circles:
        d = np.minimum(d, np.abs(np.hypot(px - cx, py - cy) - r))
    alpha = np.clip((half - d) * unit_px + 0.5, 0, 1)          # stroke, 1px anti-aliasing
    for cx, cy, r in discs:
        alpha = np.maximum(alpha, np.clip((r - np.hypot(px - cx, py - cy)) * unit_px + 0.5, 0, 1))
    for pts in polys:
        inn = inside(px, py, pts)
        e = poly_edge_dist(px, py, pts)
        cov = np.where(inn, np.clip(e * unit_px + 0.5, 0, 1), np.clip(0.5 - e * unit_px, 0, 1))
        alpha = np.maximum(alpha, cov)
    img = np.zeros((size, size, 4), np.uint8)
    img[..., :3] = 255
    img[..., 3] = (alpha * 255).astype(np.uint8)
    return Image.fromarray(img, "RGBA")


def sheet(icons, out, size=96):
    cols = 6
    cell_w, cell_h = size + 32 + 24 + 40, size + 34
    rows = (len(icons) + cols - 1) // cols
    bg = Image.new("RGB", (cols * cell_w, rows * cell_h), (22, 30, 44))
    d = ImageDraw.Draw(bg)
    try:
        f = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 13)
    except OSError:
        f = ImageFont.load_default()
    for k, icon in enumerate(icons):
        x, y = (k % cols) * cell_w + 8, (k // cols) * cell_h + 6
        d.rectangle([x - 2, y - 2, x + size + 1, y + size + 1], outline=(50, 64, 90))
        bg.paste(render(icon, size), (x, y), render(icon, size))
        small = render(icon, 32)
        bg.paste(small, (x + size + 10, y), small)
        tiny = render(icon, 24)
        bg.paste(tiny, (x + size + 10, y + 40), tiny)
        d.text((x, y + size + 6), icon["name"], fill=(200, 210, 230), font=f)
    bg.save(out)
    print(out, bg.size, len(icons), "icons")


if __name__ == "__main__":
    args = sys.argv[1:]
    size = 96
    names = None
    if "--size" in args:
        i = args.index("--size"); size = int(args[i + 1]); del args[i:i + 2]
    if "--names" in args:
        i = args.index("--names"); names = set(args[i + 1].split(",")); del args[i:i + 2]
    data = json.load(open(args[0], encoding="utf-8-sig"))
    icons = [ic for ic in data["icons"] if names is None or ic["name"] in names]
    sheet(icons, args[1], size)
