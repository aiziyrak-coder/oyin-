import sys, os, json, math
sys.argv = [sys.argv[0]]
exec(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "gen_wardrobe.py"), encoding="utf-8").read().split("icons = []")[0])
V = []
def add(name, paths, stroke=1.5): V.append({"name": name, "stroke": stroke, "paths": paths})

# Shoe variants
o = fillet([(3,19.5,1.2),(3,7.5,1.2),(8,7.5,1.0),(13,11.5,1.5),(18.5,12.7,2.5),(21.5,16.3,1.5),(21.5,19.5,1.2)], True)
def lace(t, L=2.4):
    sx, sy = 8 + (13-8)*t, 7.5 + (11.5-7.5)*t
    dx, dy = 5, 4; l = math.hypot(dx,dy); px, py = -dy/l, dx/l   # perpendicular, pointing down-left inward
    return "L %.2f %.2f %.2f %.2f" % (sx + px*L, sy + py*L, sx + px*0.3, sy + py*0.3)
add("ShoeA", [pts_str("Z", o), "L 3 16.3 21.5 16.3", lace(0.3), lace(0.7)])
add("ShoeB", [pts_str("Z", o), "L 3 16.3 21.5 16.3"])
o2 = fillet([(2.5,19.5,1.2),(2.5,8,1.2),(7,8,1.0),(9,11,1.0),(14,12,1.5),(19,13,2.5),(21.5,16.3,1.5),(21.5,19.5,1.2)], True)
add("ShoeC", [pts_str("Z", o2), "L 2.5 16.3 21.5 16.3", "L 11 12 12 9.8", "L 14 12.3 15 10.3"])
o3 = fillet([(2.5,19.5,1.2),(2.5,7,1.4),(6.5,7,1.0),(9.5,10.2,0.8),(13,10.6,1.2),(19,12.6,2.8),(21.5,16.3,1.5),(21.5,19.5,1.2)], True)
add("ShoeD", [pts_str("Z", o3), "L 2.5 16.3 21.5 16.3", "L 9.8 13.5 11 10.6", "L 12.8 13.8 14 11"])

# Hair variants
outer = [(4, 18)] + arc(12, 11.5, 8, 8.5, 180, 360) + [(20, 18)]
jaw = arc(12, 12, 4.6, 6.8, 0, 180)
fr = quad((16.6, 7.3), (10, 7.2), (7.4, 12.6))
add("HairA", [pts_str("L", outer), pts_str("L", jaw), pts_str("L", fr), pts_str("L", quad((16.6,7.3),(16.7,9.5),(16.6,12)))])
outer2 = [(3.5,18.5)] + quad((3.5,18.5),(4.6,17.2),(4.3,12))[1:] + arc(12, 11.8, 7.7, 8.3, 180, 360)[1:] + quad((19.7,12),(19.4,17.2),(20.5,18.5))[1:]
add("HairB", [pts_str("L", outer2), pts_str("L", jaw), pts_str("L", fr), pts_str("L", quad((16.6,7.3),(16.7,9.5),(16.6,12)))])
# short hair: head circle + hair cap with side fringe
head = arc(12, 12.5, 6.3, 7.5, -20, 200)
cap = [(5.4, 13.5)] + quad((5.4,13.5),(4.2,4.2),(12,4))[1:] + quad((12,4),(19.8,4.2),(18.6,13.5))[1:]
fr2 = quad((18.4, 10.5), (13, 10.8), (9, 6.6))
add("HairC", [pts_str("L", head), pts_str("L", cap), pts_str("L", [(5.6, 12.5)] + quad((5.6,12.5),(9,11),(10.5,6.5))[1:])])
# ponytail profile? no. bob with parted hair
outer4 = [(4,17.5)] + arc(12, 11.5, 8, 8.5, 180, 360) + [(20,17.5)]
add("HairD", [pts_str("L", outer4), pts_str("L", jaw), pts_str("L", quad((7.4,12.3),(8.2,7.4),(12,6.8))), pts_str("L", quad((12,6.8),(15.8,7.4),(16.6,12.3))), "L 12 3 12 6.8"])

# Cap variants
add("CapA", [pts_str("L", arc(10.5, 15, 8, 8, 180, 360)), "L 2.5 15 21.5 15", pts_str("L", quad((21.5, 15), (20.5, 17.8), (14, 16.7))), pts_str("L", quad((10.5, 7), (7, 9.5), (6.5, 15))), "D 10.5 6.3 1.1"])
add("CapB", [pts_str("L", arc(10, 15.5, 7.5, 8.5, 180, 360)), "L 2.5 15.5 13 15.5", pts_str("Z", fillet([(13,15.5,0),(21.5,15.5,1.0),(19.5,18.2,1.2),(13,17.2,0)], True)), pts_str("L", quad((10, 7), (6.5, 10), (6, 15.5)))])
json.dump({"icons": V}, open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "var.json"), "w"))
