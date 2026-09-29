"""Generator for the 'learnwork' icon group -> part_learnwork.json (DSL of icon_render.py)."""
import json, math, re, sys, os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "part_learnwork.json")


def f(v):
    s = ("%.2f" % v).rstrip("0").rstrip(".")
    return "0" if s in ("-0", "") else s


def P(pts):
    return " ".join(f(x) + " " + f(y) for x, y in pts)


def L(*pts):
    return "L " + P(pts)


def Z(*pts):
    return "Z " + P(pts)


def A(cx, cy, rx, ry, a0, a1):
    return "A " + " ".join(f(v) for v in (cx, cy, rx, ry, a0, a1))


def C(cx, cy, r):
    return "C %s %s %s" % (f(cx), f(cy), f(r))


def D(cx, cy, r):
    return "D %s %s %s" % (f(cx), f(cy), f(r))


def R(x, y, w, h, r):
    return "R " + " ".join(f(v) for v in (x, y, w, h, r))


def F(*pts):
    return "F " + P(pts)


def arcpts(cx, cy, rx, ry, a0, a1, step=6):
    n = max(2, int(math.ceil(abs(a1 - a0) / step)))
    return [(cx + math.cos(math.radians(a0 + (a1 - a0) * i / n)) * rx,
             cy + math.sin(math.radians(a0 + (a1 - a0) * i / n)) * ry) for i in range(n + 1)]


def rot(pts, ang, c=(12, 12)):
    a = math.radians(ang)
    ca, sa = math.cos(a), math.sin(a)
    return [(c[0] + (x - c[0]) * ca - (y - c[1]) * sa, c[1] + (x - c[0]) * sa + (y - c[1]) * ca) for x, y in pts]


def ellipse_rot(cx, cy, rx, ry, ang, n=72):
    pts = [(cx + math.cos(2 * math.pi * i / n) * rx, cy + math.sin(2 * math.pi * i / n) * ry) for i in range(n)]
    return rot(pts, ang, (cx, cy))


def cubic(p0, p1, p2, p3, n=10):
    out = []
    for i in range(1, n + 1):
        t = i / n
        u = 1 - t
        out.append((u ** 3 * p0[0] + 3 * u * u * t * p1[0] + 3 * u * t * t * p2[0] + t ** 3 * p3[0],
                    u ** 3 * p0[1] + 3 * u * u * t * p1[1] + 3 * u * t * t * p2[1] + t ** 3 * p3[1]))
    return out


def quad(p0, p1, p2, n=8):
    out = []
    for i in range(1, n + 1):
        t = i / n
        u = 1 - t
        out.append((u * u * p0[0] + 2 * u * t * p1[0] + t * t * p2[0], u * u * p0[1] + 2 * u * t * p1[1] + t * t * p2[1]))
    return out


def svg_arc(p0, rx, ry, phi, large, sweep, p1):
    """SVG endpoint arc -> points (excluding p0). Assumes phi=0."""
    x1, y1 = p0
    x2, y2 = p1
    if rx == 0 or ry == 0 or (x1 == x2 and y1 == y2):
        return [p1]
    dx, dy = (x1 - x2) / 2, (y1 - y2) / 2
    rx, ry = abs(rx), abs(ry)
    lam = dx * dx / (rx * rx) + dy * dy / (ry * ry)
    if lam > 1:
        rx *= math.sqrt(lam)
        ry *= math.sqrt(lam)
    num = rx * rx * ry * ry - rx * rx * dy * dy - ry * ry * dx * dx
    den = rx * rx * dy * dy + ry * ry * dx * dx
    co = math.sqrt(max(0, num / den)) * (-1 if large == sweep else 1)
    cxp, cyp = co * rx * dy / ry, -co * ry * dx / rx
    cx, cy = cxp + (x1 + x2) / 2, cyp + (y1 + y2) / 2
    t1 = math.atan2((dy - cyp) / ry, (dx - cxp) / rx)
    t2 = math.atan2((-dy - cyp) / ry, (-dx - cxp) / rx)
    dt = t2 - t1
    if sweep and dt < 0:
        dt += 2 * math.pi
    if not sweep and dt > 0:
        dt -= 2 * math.pi
    n = max(2, int(abs(math.degrees(dt)) / 8) + 1)
    return [(cx + rx * math.cos(t1 + dt * i / n), cy + ry * math.sin(t1 + dt * i / n)) for i in range(1, n + 1)]


def svg(d, xf=None):
    """Flatten an SVG path (M L H V C S Q A Z, abs/rel) into DSL L/Z strings."""
    toks = re.findall(r"[MmLlHhVvCcSsQqAaZz]|-?\d*\.?\d+(?:e-?\d+)?", d)
    i, cmd = 0, None
    cur, start = (0.0, 0.0), (0.0, 0.0)
    subs, pts, closed = [], [], []
    lastc = None

    def num():
        nonlocal i
        v = float(toks[i]); i += 1
        return v

    def flush(cl):
        if len(pts) > 1:
            subs.append((list(pts), cl))

    while i < len(toks):
        if re.match(r"[A-Za-z]", toks[i]):
            cmd = toks[i]; i += 1
            if cmd in "Zz":
                flush(True); pts.clear(); cur = start; lastc = None
                continue
        rel = cmd.islower()
        c = cmd.upper()
        ox, oy = cur if rel else (0, 0)
        if c == "M":
            flush(False); pts.clear()
            cur = (ox + num(), oy + num()); start = cur; pts.append(cur)
            cmd = "l" if rel else "L"
            lastc = None
        elif c == "L":
            cur = (ox + num(), oy + num()); pts.append(cur); lastc = None
        elif c == "H":
            cur = ((cur[0] if rel else 0) + num(), cur[1]); pts.append(cur); lastc = None
        elif c == "V":
            cur = (cur[0], (cur[1] if rel else 0) + num()); pts.append(cur); lastc = None
        elif c == "C":
            p1 = (ox + num(), oy + num()); p2 = (ox + num(), oy + num()); p3 = (ox + num(), oy + num())
            pts.extend(cubic(cur, p1, p2, p3)); cur = p3; lastc = p2
        elif c == "S":
            p1 = (2 * cur[0] - lastc[0], 2 * cur[1] - lastc[1]) if lastc else cur
            p2 = (ox + num(), oy + num()); p3 = (ox + num(), oy + num())
            pts.extend(cubic(cur, p1, p2, p3)); cur = p3; lastc = p2
        elif c == "Q":
            p1 = (ox + num(), oy + num()); p2 = (ox + num(), oy + num())
            pts.extend(quad(cur, p1, p2)); cur = p2; lastc = None
        elif c == "A":
            rx, ry, phi, la, sw = num(), num(), num(), num(), num()
            p1 = (ox + num(), oy + num())
            pts.extend(svg_arc(cur, rx, ry, phi, int(la), int(sw), p1)); cur = p1; lastc = None
    flush(False)
    out = []
    for sp, cl in subs:
        if xf:
            sp = [xf(p) for p in sp]
        # drop consecutive duplicates
        clean = [sp[0]]
        for p in sp[1:]:
            if abs(p[0] - clean[-1][0]) > 1e-3 or abs(p[1] - clean[-1][1]) > 1e-3:
                clean.append(p)
        out.append(("Z " if cl else "L ") + P(clean))
    return out


# ---------------------------------------------------------------------------------------------------------------
icons = []


def icon(name, paths, stroke=1.5):
    icons.append({"name": name, "stroke": stroke, "paths": paths})


exec(open(os.path.join(HERE, "icons_def.py"), encoding="utf-8").read())

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else OUT
    with open(out, "w", encoding="utf-8") as fh:
        fh.write('{ "icons": [\n')
        fh.write(",\n".join(" " + json.dumps(ic, ensure_ascii=False) for ic in icons))
        fh.write("\n]}\n")
    print("wrote", out, len(icons))
