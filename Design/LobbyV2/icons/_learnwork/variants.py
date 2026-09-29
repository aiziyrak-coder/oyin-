# alt variants for comparison; executed after icons_def via gen-like harness
exec(open("gen.py", encoding="utf-8").read().split("exec(open(")[0])
exec(open("icons_def.py", encoding="utf-8").read())
icons.clear()

def fit_rot(parts, circles, ang=45, box=18.6, c=(12, 12)):
    ca, sa = math.cos(math.radians(ang)), math.sin(math.radians(ang))
    tf = lambda p: (p[0] * ca - p[1] * sa, p[0] * sa + p[1] * ca)
    allp = [tf(p) for pp, _ in parts for p in pp]
    mnx, mxx = min(p[0] for p in allp), max(p[0] for p in allp)
    mny, mxy = min(p[1] for p in allp), max(p[1] for p in allp)
    s = box / max(mxx - mnx, mxy - mny)
    ox, oy = c[0] - s * (mnx + mxx) / 2, c[1] - s * (mny + mxy) / 2
    out = []
    for pp, cl in parts:
        q = [(x * s + ox, y * s + oy) for x, y in map(tf, pp)]
        out.append((Z if cl else L)(*q))
    for (x, y, r) in circles:
        tx, ty = tf((x, y))
        out.append(C(tx * s + ox, ty * s + oy, r * s))
    return out, s

def rocketA():
    body = "M-3 4.5 L-4 1 V-1.5 C-4 -5.5 -2.4 -8.5 0 -11 C2.4 -8.5 4 -5.5 4 -1.5 V1 L3 4.5 Z"
    fl = "M-4 -0.5 L-7 3 V6.5 L-3 4.5"
    fr = "M4 -0.5 L7 3 V6.5 L3 4.5"
    flame = "M-1.6 7 L0 10 L1.6 7"
    return fit_rot([(svgpts(body), True), (svgpts(fl), False), (svgpts(fr), False), (svgpts(flame), False)], [(0, -3.2, 2)])

def rocketB():
    # fins merged into silhouette (single outline), exhaust as short line
    out = "M0 -11 C2.6 -8.5 4 -5.5 4 -1.5 V0.5 L7 3.5 V7 L3.5 5 H-3.5 L-7 7 V3.5 L-4 0.5 V-1.5 C-4 -5.5 -2.6 -8.5 0 -11 Z"
    flame = "M0 7.5 V10.5"
    return fit_rot([(svgpts(out), True), (svgpts(flame), False)], [(0, -3, 2)])

def rocketC():
    # Lucide-like: body + separate curved fins + exhaust teardrop
    body = "M-3 4 L-3.8 0 C-3.8 -5 -2.2 -8.6 0 -11 C2.2 -8.6 3.8 -5 3.8 0 L3 4 Z"
    fl = "M-3.7 -1 C-6 0 -7 2.5 -7 5.5 L-3.2 3.2"
    fr = "M3.7 -1 C6 0 7 2.5 7 5.5 L3.2 3.2"
    flame = "M-1.8 6 C-1.8 8 -0.6 9.5 0 10.5 C0.6 9.5 1.8 8 1.8 6"
    return fit_rot([(svgpts(body), True), (svgpts(fl), False), (svgpts(fr), False), (svgpts(flame), False)], [(0, -3.2, 1.9)])

for nm, fn in (("RocketA", rocketA), ("RocketB", rocketB), ("RocketC", rocketC)):
    p, s = fn(); print(nm, "scale", round(s, 3)); icon(nm, p)
p, s = fit_rot([(svgpts("M-3 4.5 L-4 1 V-1.5 C-4 -5.5 -2.4 -8.5 0 -11 C2.4 -8.5 4 -5.5 4 -1.5 V1 L3 4.5 Z"), True),
                (svgpts("M-4 -0.5 L-7 3 V6.5 L-3 4.5"), False), (svgpts("M4 -0.5 L7 3 V6.5 L3 4.5"), False),
                (svgpts("M-1.6 7 L0 10 L1.6 7"), False)], [(0, -3.2, 2)], ang=0)
icon("RocketUp", p)

# ---- coins variants
def stack(cx, ytop, n, t, rx, ry, offs, hidden=lambda x, y: False):
    out = []
    ys = [ytop + i * t for i in range(n)]
    # top face of top coin
    out += clip(arcpts(cx + offs[0], ys[0], rx, ry, 0, 360, 5)[:-1], hidden, closed=True)
    for i in range(n):
        x = cx + offs[i]
        out += clip(arcpts(x, ys[i] + t, rx, ry, 0, 180, 5), hidden)
        out += clip([(x - rx, ys[i]), (x - rx, ys[i] + t)], hidden) + clip([(x + rx, ys[i]), (x + rx, ys[i] + t)], hidden)
    return out

# V1 staggered stack + front coin
cc = (16.4, 15.4, 5.1)
hid = lambda x, y: math.hypot(x - cc[0], y - cc[1]) < cc[2] + 1.8
icon("CoinsV1", stack(9, 5, 4, 3.3, 6, 2, [0.6, -0.4, 0.5, -0.3], hid) + [C(*cc), C(cc[0], cc[1], 2.3)])
# V2 two stacks (back tall left, front short right)
def front_hidden(x, y):  # region of front stack (right), center 15.5, rx 5.5, top 11.5, 3 coins x 3
    return (10 - 1.6 < x < 21 + 1.6 and y > 11.5 - 2 - 1.6) and ((x - 15.5) ** 2 / (5.5 + 1.6) ** 2 + (y - 11.5) ** 2 / (2 + 1.6) ** 2 < 1 or y > 11.5)
icon("CoinsV2", stack(8.5, 4.5, 4, 3.2, 5.5, 2, [0, 0, 0, 0], front_hidden) + stack(15.5, 11.5, 2, 3.4, 5.5, 2, [0, 0]))
# V3 two front coins
icon("CoinsV3", [C(9, 9, 6)] + clip(arcpts(15, 15, 6, 6, 0, 360, 4)[:-1], lambda x, y: math.hypot(x - 9, y - 9) < 6 + 1.7, closed=True) +
     [L((8, 7), (9, 6.3), (9, 11.7))])
# V4 staggered stack only (bigger, 5 coins)
icon("CoinsV4", stack(12, 4.5, 5, 3.2, 8, 2.2, [0.8, -0.6, 0.6, -0.8, 0.4]))
with open("var.json", "w") as fh:
    json.dump({"icons": icons}, fh)
