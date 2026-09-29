"""Generator for part_wardrobe.json (Lynxos wardrobe icon group). Builds DSL strings with helper geometry."""
import json, math, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(os.path.dirname(HERE), "part_wardrobe.json")


def f(v):
    s = ("%.2f" % v).rstrip("0").rstrip(".")
    return "0" if s in ("-0", "") else s


def pts_str(cmd, pts):
    out = []
    for x, y in pts:
        out += [f(x), f(y)]
    return cmd + " " + " ".join(out)


def dedupe(pts, eps=1e-3):
    res = []
    for p in pts:
        if not res or math.hypot(p[0] - res[-1][0], p[1] - res[-1][1]) > eps:
            res.append(p)
    return res


def fillet(corners, closed):
    """corners: list of (x, y, r). Returns polyline points with circular fillets of radius r at each corner."""
    n = len(corners)
    out = []
    for i in range(n):
        x, y, r = corners[i]
        if r <= 0 or (not closed and (i == 0 or i == n - 1)):
            out.append((x, y))
            continue
        ax, ay, _ = corners[(i - 1) % n]
        bx, by, _ = corners[(i + 1) % n]
        ux, uy = ax - x, ay - y
        vx, vy = bx - x, by - y
        lu, lv = math.hypot(ux, uy), math.hypot(vx, vy)
        ux, uy, vx, vy = ux / lu, uy / lu, vx / lv, vy / lv
        cosang = max(-1, min(1, ux * vx + uy * vy))
        ang = math.acos(cosang)
        if ang < 1e-3 or abs(ang - math.pi) < 1e-3:
            out.append((x, y))
            continue
        t = r / math.tan(ang / 2)
        t = min(t, lu * 0.5, lv * 0.5)
        r_eff = t * math.tan(ang / 2)
        t1 = (x + ux * t, y + uy * t)
        t2 = (x + vx * t, y + vy * t)
        bxs, bys = ux + vx, uy + vy
        lb = math.hypot(bxs, bys)
        d = r_eff / math.sin(ang / 2)
        cx, cy = x + bxs / lb * d, y + bys / lb * d
        a0 = math.atan2(t1[1] - cy, t1[0] - cx)
        a1 = math.atan2(t2[1] - cy, t2[0] - cx)
        da = a1 - a0
        while da > math.pi:
            da -= 2 * math.pi
        while da < -math.pi:
            da += 2 * math.pi
        steps = max(3, int(abs(da) / math.radians(12)))
        for k in range(steps + 1):
            a = a0 + da * k / steps
            out.append((cx + math.cos(a) * r_eff, cy + math.sin(a) * r_eff))
    return dedupe(out)


def arc(cx, cy, rx, ry, a0, a1, step=8):
    n = max(4, int(abs(a1 - a0) / step))
    return [(cx + math.cos(math.radians(a0 + (a1 - a0) * i / n)) * rx,
             cy + math.sin(math.radians(a0 + (a1 - a0) * i / n)) * ry) for i in range(n + 1)]


def quad(p0, c, p1, n=10):
    return [((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * c[0] + t * t * p1[0],
             (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * c[1] + t * t * p1[1]) for t in (i / n for i in range(n + 1))]


def cubic(p0, c0, c1, p1, n=14):
    res = []
    for i in range(n + 1):
        t = i / n
        a, b, c, d = (1 - t) ** 3, 3 * (1 - t) ** 2 * t, 3 * (1 - t) * t * t, t ** 3
        res.append((a * p0[0] + b * c0[0] + c * c1[0] + d * p1[0], a * p0[1] + b * c0[1] + c * c1[1] + d * p1[1]))
    return res


def mirror(pts, axis=12.0):
    return [(2 * axis - x, y) for x, y in pts]


def sparkle(cx, cy, rx, ry, k=0.2, n=8):
    tips = [(cx, cy - ry), (cx + rx, cy), (cx, cy + ry), (cx - rx, cy)]
    ctrls = [(cx + k * rx, cy - k * ry), (cx + k * rx, cy + k * ry), (cx - k * rx, cy + k * ry), (cx - k * rx, cy - k * ry)]
    pts = []
    for i in range(4):
        seg = quad(tips[i], ctrls[i], tips[(i + 1) % 4], n)
        pts += seg if not pts else seg[1:]
    return dedupe(pts[:-1])


icons = []


def translate(path, dx, dy):
    """Shift one DSL primitive by (dx, dy) (used for optical centring)."""
    t = path.split()
    cmd, v = t[0], [float(x) for x in t[1:]]
    if cmd in ("L", "Z", "F"):
        v = [c + (dx if i % 2 == 0 else dy) for i, c in enumerate(v)]
    else:                                   # A, C, D, R, FR: first two numbers are a position
        v[0] += dx
        v[1] += dy
    return cmd + " " + " ".join(f(c) for c in v)


def add(name, paths, stroke=1.5, dx=0.0, dy=0.0):
    if dx or dy:
        paths = [translate(p, dx, dy) for p in paths]
    icons.append({"name": name, "stroke": stroke, "paths": paths})


# ---------------------------------------------------------------- Shirt (t-shirt, front)
collar = arc(12, 3.5, 3, 2.4, 180, 0)          # left neck (9,3.5) dipping to (12,5.9) up to right neck (15,3.5)
body = fillet([(15, 3.5, 0), (19.6, 5.1, 1.2), (21.5, 9.6, 1.0), (18.5, 11, 0.8), (18.5, 20.5, 1.6),
               (5.5, 20.5, 1.6), (5.5, 11, 0.8), (2.5, 9.6, 1.0), (4.4, 5.1, 1.2), (9, 3.5, 0)], closed=True)
add("Shirt", [pts_str("L", collar + body[1:])])

# ---------------------------------------------------------------- Shoe (low sneaker, side view, toe to the right)
shoe = fillet([(2.5, 18.5, 1.2), (2.5, 7, 1.2), (7, 7, 1.0), (9, 10, 1.0), (14, 11, 1.5), (19, 12, 2.5),
               (21.5, 15.3, 1.5), (21.5, 18.5, 1.2)], closed=True)
add("Shoe", [pts_str("Z", shoe),
             "L 2.5 15.3 21.5 15.3",                      # sole line
             "L 10.6 12.6 11.7 10.3", "L 13.6 13 14.5 10.8"],  # laces
    dy=-0.5)

# ---------------------------------------------------------------- Cap (baseball cap, side view, peak to the right)
crown = cubic((2.5, 15), (2.5, 9.5), (6.5, 6.5), (10.5, 6.5)) + cubic((10.5, 6.5), (15, 6.5), (17.5, 10), (17.5, 15))[1:]
add("Cap", [pts_str("L", crown),
            "L 2.5 15 21.5 15",                                            # band + top edge of the peak
            pts_str("L", quad((21.5, 15), (20.8, 17.6), (14.5, 16.8))),    # peak underside
            pts_str("L", quad((10.5, 6.5), (7.5, 9), (7.2, 15)))])         # panel seam

# ---------------------------------------------------------------- Glasses
temple = fillet([(2.7, 13.3, 0), (5, 7.8, 1.6), (8, 6.6, 0)], False)
add("Glasses", ["C 6.5 14.5 4", "C 17.5 14.5 4",
                pts_str("L", arc(12, 14.5, 1.5, 1.4, 180, 360)),   # bridge
                pts_str("L", temple), pts_str("L", mirror(temple))])

# ---------------------------------------------------------------- Hair (head + bob hairstyle with side fringe, shoulders)
hair = [(4, 15.5)] + quad((4, 15.5), (4.8, 14.3), (4.6, 10.5))[1:] + arc(12, 10.3, 7.4, 7.8, 180, 360)[1:] \
       + quad((19.4, 10.5), (19.2, 14.3), (20, 15.5))[1:]
add("Hair", [pts_str("L", hair),
             pts_str("L", arc(12, 10.5, 4.3, 6, 0, 180)),                  # jaw / chin
             pts_str("L", quad((16.4, 6.2), (10.2, 6), (7.7, 11))),         # side-swept fringe
             pts_str("L", quad((16.4, 6.2), (16.5, 8.3), (16.3, 10.5))),    # right hairline
             pts_str("L", arc(12, 23.5, 7.5, 4.5, 205, 335))])              # shoulders

# ---------------------------------------------------------------- Hanger
hook = arc(12, 5.4, 2.3, 2.3, 180, 360) + arc(12, 5.4, 2.3, 2.3, 0, 60)[1:]
add("Hanger", [pts_str("L", hook + [(12, 10)]),
               pts_str("Z", fillet([(12, 10, 0.6), (21.5, 17, 1.2), (21.5, 18.5, 0.7), (2.5, 18.5, 0.7), (2.5, 17, 1.2)], True))], dy=0.5)

# ---------------------------------------------------------------- Mannequin (dress form on a stand)
right = [(12, 5.2)] + quad((12, 5.2), (16.5, 5.2), (17.8, 7.2))[1:] \
        + cubic((17.8, 7.2), (18.8, 9.5), (15.2, 10.8), (15.4, 12.5))[1:] \
        + cubic((15.4, 12.5), (15.6, 14), (17.6, 14.4), (17.2, 16.5))[1:]
left = mirror(right)
torso = left[::-1][:-1] + right                 # left hip -> over the shoulders -> right hip (Z closes the bottom)
add("Mannequin", ["R 10.5 2.5 3 2.7 1",
                  pts_str("Z", torso),
                  "L 12 16.5 12 21.5",
                  "L 8.5 21.5 15.5 21.5"])

# ---------------------------------------------------------------- Bookmark
add("Bookmark", [pts_str("Z", fillet([(5, 21, 0.6), (5, 3, 2), (19, 3, 2), (19, 21, 0.6), (12, 16.5, 0.8)], True))])

# ---------------------------------------------------------------- Laptop
add("Laptop", ["R 4 4.5 16 11 1.6",
               pts_str("Z", fillet([(4, 15.5, 0), (20, 15.5, 0), (21.8, 19.5, 0.9), (2.2, 19.5, 0.9)], True))])

# ---------------------------------------------------------------- Sofa
back = fillet([(5, 10, 0), (5, 5, 2), (19, 5, 2), (19, 10, 0)], False)
seat = fillet([(6.5, 12, 0), (6.5, 14, 0.4), (17.5, 14, 0.4), (17.5, 12, 0)], False)
base = fillet([(21.5, 12, 0), (21.5, 18, 1.4), (2.5, 18, 1.4), (2.5, 12, 0)], False)
sofa = arc(4.5, 12, 2, 2, 180, 360) + seat[1:] + arc(19.5, 12, 2, 2, 180, 360)[1:] + base[1:-1]   # arms + seat + base
add("Sofa", [pts_str("L", back), pts_str("Z", sofa), "L 5 18 5 20", "L 19 18 19 20"], dy=-0.5)

# ---------------------------------------------------------------- Sparkle (beauty)
add("Sparkle", [pts_str("Z", sparkle(10, 13.5, 7.5, 8, 0.2)),
                pts_str("Z", sparkle(18.5, 5.5, 3, 3, 0.25))])

# ---------------------------------------------------------------- Ball (basketball)
r = 9.0
p = (12 + r * math.cos(math.radians(225)), 12 + r * math.sin(math.radians(225)))
lc = (-1.0, 12.0)
lr = math.hypot(p[0] - lc[0], p[1] - lc[1])
la = math.degrees(math.atan2(p[1] - lc[1], p[0] - lc[0]))
add("Ball", ["C 12 12 9", "L 12 3 12 21", "L 3 12 21 12",
             pts_str("L", arc(lc[0], lc[1], lr, lr, la, -la, 5)),
             pts_str("L", mirror(arc(lc[0], lc[1], lr, lr, la, -la, 5)))])

# ---------------------------------------------------------------- Dots (other)
add("Dots", ["D 5 12 1.75", "D 12 12 1.75", "D 19 12 1.75"])

# ---------------------------------------------------------------- Watch
strap_t = fillet([(8.4, 6.8, 0), (9.3, 2.5, 1.0), (14.7, 2.5, 1.0), (15.6, 6.8, 0)], False)
strap_b = [(x, 24 - y) for x, y in strap_t]
add("Watch", ["C 12 12 6.5", pts_str("L", strap_t), pts_str("L", strap_b), "L 12 8.8 12 12 14.3 13.4"])

# ---------------------------------------------------------------- Headphones
add("Headphones", [pts_str("L", [(3, 15)] + arc(12, 12, 9, 9, 180, 360) + [(21, 15)]),
                   "R 3 13.5 5 7 1.8", "R 16 13.5 5 7 1.8"])

with open(OUT, "w", encoding="utf-8") as fh:
    fh.write('{ "icons": [\n')
    fh.write(",\n".join(" " + json.dumps(ic, ensure_ascii=False) for ic in icons))
    fh.write("\n]}\n")
print("wrote", OUT, len(icons))
