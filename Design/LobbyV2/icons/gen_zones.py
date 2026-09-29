"""Generator for part_zones.json (Lynxos 'zones' icon group). Emits DSL strings; curves are pre-flattened to polylines."""
import json, math, sys

SW = 1.5


def f(v):
    s = ("%.2f" % v).rstrip("0").rstrip(".")
    return "0" if s in ("-0", "") else s


def P(cmd, pts):
    return cmd + " " + " ".join(f(x) + " " + f(y) for x, y in pts)


def arc(cx, cy, rx, ry, a0, a1, step=8.0):
    n = max(2, int(math.ceil(abs(a1 - a0) / step)))
    return [(cx + math.cos(math.radians(a0 + (a1 - a0) * i / n)) * rx,
             cy + math.sin(math.radians(a0 + (a1 - a0) * i / n)) * ry) for i in range(n + 1)]


def bez(p0, p1, p2, p3, n=10):
    out = []
    for i in range(n + 1):
        t = i / n
        a, b, c, d = (1 - t) ** 3, 3 * (1 - t) ** 2 * t, 3 * (1 - t) * t * t, t ** 3
        out.append((a * p0[0] + b * p1[0] + c * p2[0] + d * p3[0], a * p0[1] + b * p1[1] + c * p2[1] + d * p3[1]))
    return out


def rpoly(verts, closed=True, step=10.0):
    """verts: list of (x, y, r). Each vertex filleted with radius r (0 = sharp; renderer joins are round anyway)."""
    out = []
    n = len(verts)
    for i in range(n):
        x, y, r = verts[i]
        if r <= 0 or (not closed and (i == 0 or i == n - 1)):
            out.append((x, y)); continue
        px, py = verts[i - 1][:2]
        nx, ny = verts[(i + 1) % n][:2]
        u1 = (px - x, py - y); l1 = math.hypot(*u1); u1 = (u1[0] / l1, u1[1] / l1)
        u2 = (nx - x, ny - y); l2 = math.hypot(*u2); u2 = (u2[0] / l2, u2[1] / l2)
        th = math.acos(max(-1, min(1, u1[0] * u2[0] + u1[1] * u2[1])))
        t = r / math.tan(th / 2)
        bis = (u1[0] + u2[0], u1[1] + u2[1]); lb = math.hypot(*bis); bis = (bis[0] / lb, bis[1] / lb)
        h = r / math.sin(th / 2)
        c = (x + bis[0] * h, y + bis[1] * h)
        t1 = (x + u1[0] * t, y + u1[1] * t)
        t2 = (x + u2[0] * t, y + u2[1] * t)
        a0 = math.degrees(math.atan2(t1[1] - c[1], t1[0] - c[0]))
        a1 = math.degrees(math.atan2(t2[1] - c[1], t2[0] - c[0]))
        d = (a1 - a0 + 540) % 360 - 180
        out += arc(c[0], c[1], r, r, a0, a0 + d, step)
    return out


def dedupe(pts):
    out = []
    for p in pts:
        if not out or math.hypot(p[0] - out[-1][0], p[1] - out[-1][1]) > 0.02:
            out.append(p)
    if len(out) > 2 and math.hypot(out[0][0] - out[-1][0], out[0][1] - out[-1][1]) < 0.02:
        out.pop()
    return out


icons = []


def add(name, paths, stroke=SW):
    icons.append({"name": name, "stroke": stroke, "paths": paths})


# ---------------------------------------------------------------- Grid (all)
add("Grid", ["R 3.25 3.25 7.25 7.25 1.75", "R 13.5 3.25 7.25 7.25 1.75",
             "R 3.25 13.5 7.25 7.25 1.75", "R 13.5 13.5 7.25 7.25 1.75"])

# ---------------------------------------------------------------- Bag
bag = rpoly([(5.2, 7.5, 1.2), (18.8, 7.5, 1.2), (20, 19, 2.2), (4, 19, 2.2)])
# bottom corners: extend body down to 20.75
bag = rpoly([(5.3, 7.5, 1.3), (18.7, 7.5, 1.3), (19.9, 20.75, 2.0), (4.1, 20.75, 2.0)])
add("Bag", [P("Z", dedupe(bag)), "A 12 7.5 3.6 4.25 180 360"])

# ---------------------------------------------------------------- GradCap
add("GradCap", ["Z 12 4.75 21.25 9.75 12 14.75 2.75 9.75",
                "L 6.5 11.78 6.5 15.5", "A 12 15.5 5.5 3.25 180 0", "L 17.5 15.5 17.5 11.78",
                "L 21.25 9.75 21.25 15.25"])

# ---------------------------------------------------------------- Briefcase
add("Briefcase", ["R 2.75 7 18.5 13.5 2.25", "R 8.5 3.5 7 3.5 1.4",
                  "L 2.75 12.75 21.25 12.75", "L 12 11.75 12 13.75"])

# ---------------------------------------------------------------- Gamepad
gp = []
gp += bez((7.2, 5.75), (7.2, 5.75), (16.8, 5.75), (16.8, 5.75), 1)
gp += bez((16.8, 5.75), (19.2, 5.75), (20.4, 7.2), (20.8, 9.4), 8)[1:]
gp += bez((20.8, 9.4), (21.2, 11.8), (21.6, 14.6), (21.6, 16.2), 8)[1:]
gp += bez((21.6, 16.2), (21.6, 18.2), (19.2, 19.2), (17.6, 17.8), 8)[1:]
gp += bez((17.6, 17.8), (16.6, 16.8), (16, 15.6), (14.6, 15.6), 6)[1:]
gp += [(9.4, 15.6)]
gp += bez((9.4, 15.6), (8, 15.6), (7.4, 16.8), (6.4, 17.8), 6)[1:]
gp += bez((6.4, 17.8), (4.8, 19.2), (2.4, 18.2), (2.4, 16.2), 8)[1:]
gp += bez((2.4, 16.2), (2.4, 14.6), (2.8, 11.8), (3.2, 9.4), 8)[1:]
gp += bez((3.2, 9.4), (3.6, 7.2), (4.8, 5.75), (7.2, 5.75), 8)[1:]
gp = [(12 + (x - 12) * 0.95, y) for x, y in gp]
add("Gamepad", [P("Z", dedupe(gp)), "L 6.25 10.75 10.25 10.75", "L 8.25 8.75 8.25 12.75", "D 15.1 11.75 1.05", "D 17.5 9.5 1.05"])

# ---------------------------------------------------------------- Home
house = rpoly([(3.25, 10.25, 1.2), (12, 3, 1.2), (20.75, 10.25, 1.2), (20.75, 20.75, 1.75), (3.25, 20.75, 1.75)])
# open at the door: draw as closed outline + door
add("Home", [P("Z", dedupe(house)), P("L", rpoly([(9.5, 20.75, 0), (9.5, 14.25, 1), (14.5, 14.25, 1), (14.5, 20.75, 0)], closed=False))])

# ---------------------------------------------------------------- Calendar
add("Calendar", ["R 3.25 4.75 17.5 16 2.25", "L 8 2.75 8 6.75", "L 16 2.75 16 6.75", "L 3.25 10 20.75 10",
                 "FR 10.4 13.75 3.2 3.2 0.8"])

# ---------------------------------------------------------------- Gear (8 teeth)
Ro, Ri = 9.25, 7.0
g = []
for k in range(8):
    th = -90 + 45 * k
    g += arc(12, 12, Ri, Ri, th - 22.5, th - 13.5, 4.5)[:-1]
    g += [(12 + math.cos(math.radians(th - 13.5)) * Ri, 12 + math.sin(math.radians(th - 13.5)) * Ri)]
    g += arc(12, 12, Ro, Ro, th - 9.5, th + 9.5, 4.75)
    g += [(12 + math.cos(math.radians(th + 13.5)) * Ri, 12 + math.sin(math.radians(th + 13.5)) * Ri)]
    g += arc(12, 12, Ri, Ri, th + 13.5, th + 22.5, 4.5)[1:-1]
add("Gear", [P("Z", dedupe(g)), "C 12 12 3"])

# ---------------------------------------------------------------- Buildings
tall = rpoly([(3.75, 21, 0), (3.75, 4.25, 1.25), (12.5, 2.75, 1.25), (12.5, 21, 0)], closed=False)
short = rpoly([(12.5, 9, 0), (19.25, 9, 1.25), (19.25, 21, 0)], closed=False)
add("Buildings", [P("L", tall), P("L", short), "L 2.75 21 21.25 21",
                  "L 7 8 9.25 8", "L 7 11.5 9.25 11.5", "L 7 15 9.25 15",
                  "L 15 13 16.75 13", "L 15 16.5 16.75 16.5"])

# ---------------------------------------------------------------- Pin
cx, cy, r = 12, 10, 7.25
a_l = 155  # tangent departure angles
pl = (cx + math.cos(math.radians(a_l)) * r, cy + math.sin(math.radians(a_l)) * r)
pr = (cx + math.cos(math.radians(180 - a_l)) * r, cy + math.sin(math.radians(180 - a_l)) * r)
tip = (12, 21.25)
# tangent direction at pr going "down" (toward decreasing angle for right side): (sin a, -cos a) for decreasing angle
ar = math.radians(180 - a_l)
tr = (math.sin(ar), -math.cos(ar))
tr = (-tr[0], -tr[1]) if tr[1] < 0 else tr
k1 = 3.2
pin = arc(cx, cy, r, r, a_l, 360 + 180 - a_l, 7)  # over the top
pin += bez(pr, (pr[0] + tr[0] * k1, pr[1] + tr[1] * k1), (13.6, 19.4), tip, 10)[1:]
pin += bez(tip, (10.4, 19.4), (pl[0] - tr[0] * k1, pl[1] + tr[1] * k1), pl, 10)[1:]
add("Pin", [P("Z", dedupe(pin)), "C 12 10 2.75"])

# ---------------------------------------------------------------- Chat
chat = rpoly([(3, 3.75, 3.5), (21, 3.75, 3.5), (21, 16.75, 3.5), (11.5, 16.75, 0.6), (6.5, 20.75, 0.6), (7.5, 16.75, 0.6), (3, 16.75, 3.5)])
add("Chat", [P("Z", dedupe(chat))])

# ---------------------------------------------------------------- Trophy
cup = [(6.75, 3.25)] + arc(12, 9.25, 5.25, 5.25, 180, 0, 8)[::-1][::-1]
cup = [(6.75, 3.25), (6.75, 9.25)] + arc(12, 9.25, 5.25, 5.25, 180, 0, 8)[1:] + [(17.25, 3.25)]
add("Trophy", [P("Z", dedupe(rpoly([(x, y, 0) for x, y in cup], True))),
               "A 6.75 7 3 2.75 90 270", "A 17.25 7 3 2.75 90 -90",
               "L 12 14.5 12 17.5", P("L", rpoly([(7.75, 21, 0), (8.25, 18.75, 0.8), (15.75, 18.75, 0.8), (16.25, 21, 0)], closed=False)),
               "L 6.5 21 17.5 21"])

# ---------------------------------------------------------------- Medal
add("Medal", ["C 12 15 5.75", "C 12 15 2.25",
              "L 8.4 10.5 5.25 3.25 9.25 3.25 12 9.25 14.75 3.25 18.75 3.25 15.6 10.5"])

# ---------------------------------------------------------------- Star
R, rr = 10.3, 10.3 * 0.46
scx, scy = 12, 12.7
st = []
for i in range(10):
    a = math.radians(-90 + 36 * i)
    rad = R if i % 2 == 0 else rr
    st.append((scx + math.cos(a) * rad, scy + math.sin(a) * rad, 0.9 if i % 2 == 0 else 0.5))
add("Star", [P("Z", dedupe(rpoly(st)))])

# ---------------------------------------------------------------- Fire
fl = []
fl += bez((12.2, 2.75), (12.8, 6.6), (18.75, 8.8), (18.75, 14.4), 12)
fl += arc(12, 14.4, 6.75, 6.75, 0, 180, 7)[1:]
fl += bez((5.25, 14.4), (5.25, 11.6), (6.6, 9.8), (8.2, 8.4), 8)[1:]
fl += bez((8.2, 8.4), (8.2, 10.6), (9.4, 11.6), (10.4, 11.6), 6)[1:]
fl += bez((10.4, 11.6), (11.2, 11.6), (11.6, 10.6), (11.4, 9.2), 5)[1:]
fl += bez((11.4, 9.2), (11.1, 7.2), (11.2, 4.6), (12.2, 2.75), 8)[1:-1]
inner = bez((12, 13.75), (13.2, 15.6), (14.75, 16.4), (14.75, 18.1), 8)
inner += arc(12, 18.1, 2.75, 2.75, 0, 180, 12)[1:]
inner += bez((9.25, 18.1), (9.25, 16.4), (10.8, 15.6), (12, 13.75), 8)[1:-1]
fl = [(12 + (x - 12) * 1.07, y) for x, y in fl]
add("Fire", [P("Z", dedupe(fl))])

# ---------------------------------------------------------------- Map (folded)
add("Map", [P("Z", dedupe(rpoly([(3, 5.75, 0.8), (9, 3.25, 0.8), (15, 5.75, 0.8), (21, 3.25, 0.8), (21, 18.25, 0.8),
                                   (15, 20.75, 0.8), (9, 18.25, 0.8), (3, 20.75, 0.8)]))),
            "L 9 3.25 9 18.25", "L 15 5.75 15 20.75"])

# ---------------------------------------------------------------- Ticket
tk = []
tk += rpoly([(2.75, 5.5, 2), (21.25, 5.5, 2), (21.25, 9.5, 0)], closed=False)[1:]
tk = [(4.75, 5.5)] + rpoly([(4.75, 5.5, 0), (21.25, 5.5, 2), (21.25, 9.5, 0)], closed=False)[1:-1]
tk += arc(21.25, 12, 2.5, 2.5, 270, 90, 10)
tk += rpoly([(21.25, 14.5, 0), (21.25, 18.5, 2), (2.75, 18.5, 2), (2.75, 14.5, 0)], closed=False)[1:-1]
tk += arc(2.75, 12, 2.5, 2.5, 90, -90, 10)
tk += rpoly([(2.75, 9.5, 0), (2.75, 5.5, 2), (21.25, 5.5, 0)], closed=False)[1:-1]
add("Ticket", [P("Z", dedupe(tk)), "L 15 5.5 15 7.5", "L 15 11 15 13", "L 15 16.5 15 18.5"])

out = sys.argv[1] if len(sys.argv) > 1 else "part_zones.json"
with open(out, "w", encoding="utf-8") as fh:
    fh.write('{ "icons": [\n' + ",\n".join(" " + json.dumps(i) for i in icons) + "\n]}\n")
print("wrote", out, len(icons))
