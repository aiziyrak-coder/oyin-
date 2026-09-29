"""Generator for part_misc.json (Lynxos misc icons). Curves are sampled into polylines for the DSL."""
import json, math, os

HERE = os.path.dirname(os.path.abspath(__file__))


def f(v):
    s = ("%.2f" % v).rstrip("0").rstrip(".")
    return "0" if s in ("-0", "") else s


def pts_str(cmd, pts):
    return cmd + " " + " ".join(f(x) + " " + f(y) for x, y in pts)


def cubic(p0, p1, p2, p3, n=16):
    out = []
    for i in range(n + 1):
        t = i / n
        a, b, c, d = (1 - t) ** 3, 3 * (1 - t) ** 2 * t, 3 * (1 - t) * t * t, t ** 3
        out.append((a * p0[0] + b * p1[0] + c * p2[0] + d * p3[0], a * p0[1] + b * p1[1] + c * p2[1] + d * p3[1]))
    return out


def arc(cx, cy, r, a0, a1, n=None):
    n = n or max(8, int(abs(a1 - a0) / 6))
    return [(cx + math.cos(math.radians(a0 + (a1 - a0) * i / n)) * r,
             cy + math.sin(math.radians(a0 + (a1 - a0) * i / n)) * r) for i in range(n + 1)]


def fillet(points, radii, closed=False):
    """Polyline with rounded corners. radii: number or list per vertex (0 = sharp)."""
    n = len(points)
    if not isinstance(radii, (list, tuple)):
        radii = [radii] * n
    out = []
    for i in range(n):
        p = points[i]
        r = radii[i]
        if (not closed and (i == 0 or i == n - 1)) or r <= 0:
            out.append(p)
            continue
        a = points[i - 1]
        b = points[(i + 1) % n]
        v1 = (a[0] - p[0], a[1] - p[1]); l1 = math.hypot(*v1); v1 = (v1[0] / l1, v1[1] / l1)
        v2 = (b[0] - p[0], b[1] - p[1]); l2 = math.hypot(*v2); v2 = (v2[0] / l2, v2[1] / l2)
        ang = math.acos(max(-1, min(1, v1[0] * v2[0] + v1[1] * v2[1])))
        t = r / math.tan(ang / 2)
        t = min(t, l1 / 2, l2 / 2)
        s = (p[0] + v1[0] * t, p[1] + v1[1] * t)
        e = (p[0] + v2[0] * t, p[1] + v2[1] * t)
        # quadratic bezier approx of fillet (control = corner)
        for k in range(9):
            u = k / 8
            out.append(((1 - u) ** 2 * s[0] + 2 * (1 - u) * u * p[0] + u * u * e[0],
                        (1 - u) ** 2 * s[1] + 2 * (1 - u) * u * p[1] + u * u * e[1]))
    return out


def mirror(pts, cx=12.0):
    return [(2 * cx - x, y) for x, y in pts]


icons = []


def add(name, paths, stroke=1.5):
    icons.append({"name": name, "stroke": stroke, "paths": paths})


# ---------------- Shield (security): rounded shield + check ----------------
half = (cubic((12, 2.6), (10.2, 4.2), (7.4, 5.3), (4.6, 5.4), 10)
        + cubic((4.6, 5.4), (4.6, 7.5), (4.6, 10.0), (4.6, 12.0), 4)[1:]
        + cubic((4.6, 12.0), (4.6, 16.8), (7.8, 19.6), (12, 21.4), 14)[1:])
shield = half[::-1] + mirror(half)[1:]
add("Shield", [pts_str("L", shield), "L 8.9 12.1 11 14.2 15.2 10"])

# ---------------- Volume: speaker + 2 waves ----------------
speaker = fillet([(2.8, 9.2), (6.2, 9.2), (10.8, 5.0), (10.8, 19.0), (6.2, 14.8), (2.8, 14.8)], [0.8, 0.6, 0.6, 0.6, 0.6, 0.8], closed=True)
add("Volume", [pts_str("Z", speaker), "A 12 12 4 4 -42 42", "A 12 12 8 8 -48 48"])

# ---------------- Help: circle + question mark ----------------
q = arc(12, 9.6, 2.7, 200, 360, 16) + cubic((14.7, 9.6), (14.7, 11.4), (12, 11.9), (12, 13.9), 10)[1:]
add("Help", ["C 12 12 9", pts_str("L", q), "D 12 17.2 1.0"])

# ---------------- Logout: door frame + arrow out ----------------
frame = fillet([(9.5, 3.2), (4.2, 3.2), (4.2, 20.8), (9.5, 20.8)], 2.2)
add("Logout", [pts_str("L", frame), "L 9.6 12 20.4 12", "L 16 7.6 20.4 12 16 16.4"])

# ---------------- Music: two beamed notes ----------------
add("Music", ["L 9.2 18 9.2 5.6 20.5 3.4 20.5 15.8", "C 6.4 18 2.8", "C 17.7 15.8 2.8"])

# ---------------- Film: clapperboard (reads better than a dense film strip at 16-24 px) ----------------
def clapper():
    a = math.radians(-16)
    u = (math.cos(a), math.sin(a)); nrm = (math.sin(a), -math.cos(a))
    p0 = (3.0, 10.6); Lb = 18.2; T = 3.6
    q = [p0, (p0[0] + Lb * u[0], p0[1] + Lb * u[1]),
         (p0[0] + Lb * u[0] + T * nrm[0], p0[1] + Lb * u[1] + T * nrm[1]), (p0[0] + T * nrm[0], p0[1] + T * nrm[1])]
    bar = fillet(q, [0, 0.8, 0.8, 0.8], closed=True)
    paths = [pts_str("Z", fillet([(3, 10.6), (21, 10.6), (21, 20.8), (3, 20.8)], [0, 0, 2, 2], closed=True)),
             pts_str("Z", bar)]
    for s in (6.0, 11.4):
        b0 = (p0[0] + s * u[0], p0[1] + s * u[1])
        b1 = (p0[0] + (s + 2.6) * u[0] + T * nrm[0], p0[1] + (s + 2.6) * u[1] + T * nrm[1])
        paths.append(pts_str("L", [b0, b1]))
    return paths


add("Film", clapper())

# ---------------- Crown ----------------
crown = fillet([(12, 4.0), (15.6, 10.2), (20.6, 6.3), (18.6, 16.6), (5.4, 16.6), (3.4, 6.3), (8.4, 10.2)],
               [0.9, 0.5, 0.9, 0.7, 0.7, 0.9, 0.5], closed=True)
add("Crown", [pts_str("Z", crown), "L 5.6 20.2 18.4 20.2"])


# ---------------- Heart: two lobes + tangents to bottom point ----------------
def heart():
    r, cy, cxl = 4.75, 8.7, 7.55
    P = (12, 20.4)
    # tangent from P to left circle, outer side
    dx, dy = cxl - P[0], cy - P[1]
    d = math.hypot(dx, dy)
    base = math.degrees(math.atan2(-dy, -dx))  # angle from circle centre towards P
    alpha = math.degrees(math.acos(r / d))
    t_ang = base + alpha  # outer tangent (left side, going clockwise angle > 90)
    # notch: intersection of circles at x=12
    h = math.sqrt(r * r - (12 - cxl) ** 2)
    notch_ang = math.degrees(math.atan2(-h, 12 - cxl)) + 360  # upper-right of left lobe
    left_arc = arc(cxl, cy, r, t_ang, notch_ang, 30)
    left = [P] + left_arc  # bottom -> tangent -> over top -> notch
    return left + mirror(left)[::-1][1:]


add("Heart", [pts_str("L", fillet(heart(), 0.0))])

# ---------------- Camera ----------------
cam = fillet([(2.6, 7.2), (7.4, 7.2), (9.2, 4.4), (14.8, 4.4), (16.6, 7.2), (21.4, 7.2), (21.4, 19.6), (2.6, 19.6)],
             [2.0, 0.6, 0.7, 0.7, 0.6, 2.0, 2.0, 2.0], closed=True)
add("Camera", [pts_str("Z", cam), "C 12 13.2 3.6"])

# ---------------- Upload: arrow up into tray ----------------
tray = fillet([(3.2, 14.6), (3.2, 20.4), (20.8, 20.4), (20.8, 14.6)], 2.0)
add("Upload", [pts_str("L", tray), "L 12 3.6 12 15", "L 7.4 8.2 12 3.6 16.6 8.2"])

# ---------------- Eye ----------------
top = cubic((2.2, 12), (5.4, 5.2), (18.6, 5.2), (21.8, 12), 24)
bot = cubic((21.8, 12), (18.6, 18.8), (5.4, 18.8), (2.2, 12), 24)
add("Eye", [pts_str("Z", fillet(top + bot[1:-1], 0)), "C 12 12 3.3"])

# ---------------- Mouse ----------------
add("Mouse", ["R 5.6 2.6 12.8 18.8 6.4", "L 12 6.4 12 10.2"])

# ---------------- Keyboard ----------------
kb = ["R 2.4 5.4 19.2 13.2 2.2"]
for x in (6.4, 10.0, 14.0, 17.6):
    kb.append("D %s 9.2 0.9" % f(x))
for x in (8.2, 12.0, 15.8):
    kb.append("D %s 12.2 0.9" % f(x))
kb.append("L 7.8 15.3 16.2 15.3")
add("Keyboard", kb)

# ---------------- Info ----------------
add("Info", ["C 12 12 9", "L 12 11 12 16.6", "D 12 7.6 1.0"])

# ---------------- Refresh: two circular arrows ----------------
R0 = 8.4
a1 = arc(12, 12, R0, 180, 300, 20)  # left -> top -> upper right
e = a1[-1]
tg = (-math.sin(math.radians(300)), math.cos(math.radians(300)))
a1 += cubic(e, (e[0] + tg[0] * 1.8, e[1] + tg[1] * 1.8), (19.3, 6.7), (20.6, 8.2), 8)[1:]
a2 = [(24 - x, 24 - y) for x, y in a1]
add("Refresh", [pts_str("L", a1), "L 20.6 3.6 20.6 8.2 16 8.2",
                pts_str("L", a2), "L 3.4 20.4 3.4 15.8 8 15.8"])

# ---------------- Location: city-centre crosshair ----------------
add("Location", ["C 12 12 7", "C 12 12 2.6", "L 12 2.4 12 5", "L 12 19 12 21.6", "L 2.4 12 5 12", "L 19 12 21.6 12"])

if __name__ == "__main__":
  with open(os.path.join(HERE, "part_misc.json"), "w", encoding="utf-8") as fh:
    fh.write('{ "icons": [\n')
    fh.write(",\n".join(" " + json.dumps(ic, ensure_ascii=False) for ic in icons))
    fh.write("\n]}\n")
print(len(icons), "icons")
