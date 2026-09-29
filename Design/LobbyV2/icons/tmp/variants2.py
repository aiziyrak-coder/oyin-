import sys, os, json, math
exec(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "gen_wardrobe.py"), encoding="utf-8").read().split("icons = []")[0])
V = []
def add(name, paths, stroke=1.5): V.append({"name": name, "stroke": stroke, "paths": paths})
def sh(pts, dx=0, dy=0): return [(x+dx, y+dy) for x, y in pts]

# caps
crown = cubic((2.5, 15), (2.5, 9.5), (6.5, 6.5), (10.5, 6.5)) + cubic((10.5, 6.5), (15, 6.5), (17.5, 10), (17.5, 15))[1:]
seam = quad((10.5, 6.5), (7.5, 9), (7.2, 15))
add("CapC", [pts_str("L", crown), "L 2.5 15 21.5 15", pts_str("L", quad((21.5, 15), (20.8, 17.6), (14.5, 16.8))), pts_str("L", seam)])
crown2 = arc(10, 15, 7.5, 8.5, 180, 360)
add("CapD", [pts_str("L", crown2), pts_str("L", fillet([(2.5,15,0),(21.5,15,1.2),(20.3,17,1.0),(15,17,0)], False)), pts_str("L", quad((10, 6.5), (6.8, 9), (6.5, 15)))])
crown3 = cubic((3, 15), (3, 9), (7, 6.5), (11, 6.5)) + cubic((11, 6.5), (15.5, 6.5), (18, 9.5), (18, 15))[1:]
brim = [(3,15)] + quad((18, 15), (21.8, 15.2), (21.5, 16.8))[0:1] + quad((18, 15), (21.8, 15.2), (21.5, 16.8))[1:] + quad((21.5,16.8),(19,18),(14,16.5))[1:]
add("CapE", [pts_str("L", crown3), pts_str("L", brim), pts_str("L", quad((11, 6.5), (8, 9), (7.8, 15)))])

# shoes: C shape, shifted up 1, laces perpendicular
o2 = sh(fillet([(2.5,19.5,1.2),(2.5,8,1.2),(7,8,1.0),(9,11,1.0),(14,12,1.5),(19,13,2.5),(21.5,16.3,1.5),(21.5,19.5,1.2)], True), 0, -1)
add("ShoeE", [pts_str("Z", o2), "L 2.5 15.3 21.5 15.3", "L 10.6 12.6 11.7 10.3", "L 13.6 13 14.5 10.8"])
o3 = sh(fillet([(2.5,19.5,1.2),(2.5,8,1.2),(6.5,8,1.0),(8.5,10.8,1.0),(14,11.8,1.5),(19,13,2.5),(21.5,16.3,1.5),(21.5,19.5,1.2)], True), 0, -1)
add("ShoeF", [pts_str("Z", o3), "L 2.5 15.3 21.5 15.3", "L 10.5 12.8 11.3 10.4", "L 13.5 13.1 14.3 10.8"])
add("ShoeG", [pts_str("Z", o3), "L 2.5 15.3 21.5 15.3", "L 10.8 10.3 9.3 13", "L 13.8 10.8 12.6 13.4", "L 3 11 7 11"])
# hair B refined, shifted
jaw = arc(12, 12, 4.6, 6.8, 0, 180)
fr = quad((16.6, 7.3), (10, 7.2), (7.4, 12.6))
outer2 = [(3.5,18.5)] + quad((3.5,18.5),(4.6,17.2),(4.3,12))[1:] + arc(12, 11.8, 7.7, 8.3, 180, 360)[1:] + quad((19.7,12),(19.4,17.2),(20.5,18.5))[1:]
add("HairB", [pts_str("L", outer2), pts_str("L", jaw), pts_str("L", fr), pts_str("L", quad((16.6,7.3),(16.7,9.5),(16.6,12)))])
# hair with neck/shoulders? variant: bob hair with shoulders line
jaw2 = arc(12, 10.5, 4.3, 6, 0, 180)
outer5 = [(4,15.5)] + quad((4,15.5),(4.8,14.3),(4.6,10.5))[1:] + arc(12, 10.3, 7.4, 7.8, 180, 360)[1:] + quad((19.4,10.5),(19.2,14.3),(20,15.5))[1:]
fr5 = quad((16.4, 6.2), (10.2, 6), (7.7, 11))
add("HairE", [pts_str("L", outer5), pts_str("L", jaw2), pts_str("L", fr5), pts_str("L", quad((16.4,6.2),(16.5,8.3),(16.3,10.5))), pts_str("L", arc(12, 23.5, 7.5, 4.5, 200, 340))])
json.dump({"icons": V}, open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "var.json"), "w"))
