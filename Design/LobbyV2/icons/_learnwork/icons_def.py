# executed inside gen.py (helpers: L Z A C D R F svg arcpts rot ellipse_rot icon ...)

def densify(pts, step=0.25):
    out = [pts[0]]
    for (x1, y1), (x2, y2) in zip(pts, pts[1:]):
        n = max(1, int(math.hypot(x2 - x1, y2 - y1) / step))
        for i in range(1, n + 1):
            out.append((x1 + (x2 - x1) * i / n, y1 + (y2 - y1) * i / n))
    return out


def simplify(pts, tol=0.02):
    """drop nearly collinear points"""
    if len(pts) < 3:
        return pts
    out = [pts[0]]
    for i in range(1, len(pts) - 1):
        (x0, y0), (x1, y1), (x2, y2) = out[-1], pts[i], pts[i + 1]
        cross = abs((x1 - x0) * (y2 - y0) - (y1 - y0) * (x2 - x0))
        if cross / max(1e-9, math.hypot(x2 - x0, y2 - y0)) > tol:
            out.append(pts[i])
    out.append(pts[-1])
    return out


def clip(pts, hidden, closed=False):
    """split polyline, removing points where hidden(x,y) is True -> list of L strings"""
    if closed:
        pts = pts + [pts[0]]
    pts = densify(pts)
    runs, cur = [], []
    for p in pts:
        if hidden(*p):
            if len(cur) > 1:
                runs.append(cur)
            cur = []
        else:
            cur.append(p)
    if len(cur) > 1:
        runs.append(cur)
    if closed and len(runs) > 1 and not hidden(*pts[0]):
        runs[0] = runs.pop() + runs[0][1:]
    return [L(*simplify(r)) for r in runs if len(r) > 1]


def svgpts(d):
    """first subpath of svg() as point list"""
    s = svg(d)[0].split()[1:]
    v = [float(x) for x in s]
    return list(zip(v[0::2], v[1::2]))


# ---------------------------------------------------------------- Medical
icon("Medical", [
    R(3, 3, 18, 18, 4),
    Z((10, 6.5), (14, 6.5), (14, 10), (17.5, 10), (17.5, 14), (14, 14), (14, 17.5), (10, 17.5), (10, 14), (6.5, 14),
      (6.5, 10), (10, 10)),
])

# ---------------------------------------------------------------- Atom
icon("Atom", [Z(*ellipse_rot(12, 12, 9.5, 3.6, a)) for a in (0, 60, 120)] + [D(12, 12, 1.5)])

# ---------------------------------------------------------------- Language
b1 = "M4.5 2.5 H13.5 A2 2 0 0 1 15.5 4.5 V11.5 A2 2 0 0 1 13.5 13.5 H8 L4.5 16.5 V13.5 A2 2 0 0 1 2.5 11.5 V4.5 A2 2 0 0 1 4.5 2.5 Z"
b2 = "M10.5 9 H19.5 A2 2 0 0 1 21.5 11 V17.5 A2 2 0 0 1 19.5 19.5 H19 V22 L16 19.5 H10.5 A2 2 0 0 1 8.5 17.5 V11 A2 2 0 0 1 10.5 9 Z"
p1 = svgpts(b1)


def in_b1(x, y, g=1.9):
    return 2.5 - g < x < 15.5 + g and 2.5 - g < y < 13.5 + g


icon("Language", svg(b1) + clip(svgpts(b2), in_b1, closed=True) + [
    L((6.4, 11), (9, 5.2), (11.6, 11)), L((7.3, 9), (10.7, 9))])

# ---------------------------------------------------------------- Code
icon("Code", [L((8, 6.5), (2.5, 12), (8, 17.5)), L((16, 6.5), (21.5, 12), (16, 17.5)), L((14, 4), (10, 20))])

# ---------------------------------------------------------------- Palette
icon("Palette", svg("M12 21 A9 9 0 0 1 12 3 A9 8.1 0 0 1 21 11.1 A4.5 4.5 0 0 1 16.5 15.6 H14.48 "
                    "A1.58 1.58 0 0 0 13.22 18.12 L13.49 18.48 A1.58 1.58 0 0 1 12.23 21 Z") +
     [D(8.6, 8, 1.35), D(13.4, 7, 1.35), D(17, 10.6, 1.35), D(7, 12.8, 1.35)])

# ---------------------------------------------------------------- BookOpen
icon("BookOpen", svg("M12 7 A4 4 0 0 0 8 3 H4 A1 1 0 0 0 3 4 V17 A1 1 0 0 0 4 18 H9 A3 3 0 0 1 12 21") +
     svg("M12 7 A4 4 0 0 1 16 3 H20 A1 1 0 0 1 21 4 V17 A1 1 0 0 1 20 18 H15 A3 3 0 0 0 12 21") + [L((12, 7), (12, 21))])

# ---------------------------------------------------------------- Office
icon("Office", [R(5, 3, 14, 18, 2)] + [D(x, y, 0.95) for y in (7, 10.5, 14) for x in (9, 12, 15)] +
     [L((10, 21), (10, 17.5), (14, 17.5), (14, 21))])

# ---------------------------------------------------------------- Coworking
icon("Coworking", [C(7, 5.6, 2.3), C(17, 5.6, 2.3), A(7, 14.5, 4.3, 4, 180, 360), A(17, 14.5, 4.3, 4, 180, 360),
                   L((2, 14.5), (22, 14.5)), L((5, 14.5), (5, 21)), L((19, 14.5), (19, 21))])

# ---------------------------------------------------------------- Rocket (drawn upright, rotated 45 deg, fitted)
def rocket():
    body = "M-2.6 4.5 L-3.6 1 V-2 C-3.6 -6 -2 -8.8 0 -11 C2 -8.8 3.6 -6 3.6 -2 V1 L2.6 4.5 Z"
    finL = "M-3.6 -0.5 L-6.3 2.5 V6.3 L-3 4.2"
    finR = "M3.6 -0.5 L6.3 2.5 V6.3 L3 4.2"
    flame = "M-1.5 6.8 L0 10 L1.5 6.8"
    parts = [(svgpts(body), True), (svgpts(finL), False), (svgpts(finR), False), (svgpts(flame), False)]
    win = (0, -3.3, 1.8)
    s, ang = 0.86, 45
    ca, sa = math.cos(math.radians(ang)), math.sin(math.radians(ang))

    def tf(p):
        x, y = p[0] * s, p[1] * s
        return (x * ca - y * sa, x * sa + y * ca)
    allp = [tf(p) for pp, _ in parts for p in pp]
    mnx, mxx = min(p[0] for p in allp), max(p[0] for p in allp)
    mny, mxy = min(p[1] for p in allp), max(p[1] for p in allp)
    ox, oy = 12 - (mnx + mxx) / 2, 12 - (mny + mxy) / 2
    out = []
    for pp, cl in parts:
        q = [(x + ox, y + oy) for x, y in map(tf, pp)]
        out.append((Z if cl else L)(*q))
    wx, wy = tf(win[:2])
    out.append(C(wx + ox, wy + oy, win[2] * s))
    return out


icon("Rocket", rocket())

# ---------------------------------------------------------------- Coins (stack + front coin)
cc = (16.3, 15.2, 5.2)


def hid_coin(x, y):
    return math.hypot(x - cc[0], y - cc[1]) < cc[2] + 1.8


stack = []
sx, rx, ry = 9, 6, 2.2
stack += clip(arcpts(sx, 5.5, rx, ry, 0, 360, 5)[:-1], hid_coin, closed=True)
for yb in (9.5, 13.5, 17.5):
    stack += clip(arcpts(sx, yb, rx, ry, 0, 180, 5), hid_coin)
stack += clip([(sx - rx, 5.5), (sx - rx, 17.5)], hid_coin) + clip([(sx + rx, 5.5), (sx + rx, 17.5)], hid_coin)
icon("Coins", stack + [C(*cc), C(cc[0], cc[1], 2.4)])

# ---------------------------------------------------------------- Handshake (first try: Lucide-like structure)
icon("Handshake", svg("m11 17 2 2a1 1 0 1 0 3-3") +
     svg("m14 14 2.5 2.5a1 1 0 1 0 3-3l-3.88-3.88a3 3 0 0 0-4.24 0l-.88.88a1 1 0 1 1-3-3l2.81-2.81a5.79 5.79 0 0 1 7.06-.87l.47.28a2 2 0 0 0 1.42.25L21 4") +
     svg("m21 3 1 11h-2") + svg("M3 3 2 14l6.5 6.5a1 1 0 1 0 3-3") + svg("M3 4h8"))

# ---------------------------------------------------------------- Key
icon("Key", [C(7.5, 16.5, 4.5), L((10.68, 13.32), (20.5, 3.5)), L((17.5, 6.5), (20, 9)), L((14.5, 9.5), (16.5, 11.5))])

# ---------------------------------------------------------------- Presentation
icon("Presentation", [L((2, 3.5), (22, 3.5))] + svg("M3.5 3.5 V14 A1.5 1.5 0 0 0 5 15.5 H19 A1.5 1.5 0 0 0 20.5 14 V3.5") +
     [L((12, 15.5), (12, 17)), L((8.5, 20.5), (12, 17), (15.5, 20.5)),
      L((7, 12), (10, 9), (12.5, 11), (16.5, 7)), L((14, 7), (16.5, 7), (16.5, 9.5))])
