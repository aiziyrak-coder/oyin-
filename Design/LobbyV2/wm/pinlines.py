from ink import *
pins = {  # cx, cy, bubble right approx, bubble x for vertical probe
 'mag': (253,168,336,318),
 'oquv': (480,128,582,560),
 'kon': (537,205,647,625),
 'tur': (253,254,337,318),
 'tad': (400,300,480,462),
 'xiz': (566,316,644,625),
}
def seq(pts): return ' '.join('%d:%s'%(k,px(*xy)) for k,xy in pts)
for k,(cx,cy,br,bx) in pins.items():
    print('=====',k)
    print('Vcirc', seq([(y,(cx,y)) for y in range(cy-24,cy+26)]))
    print('Hleft', seq([(x,(x,cy)) for x in range(cx-23,cx-12)]))
    print('Vbub', seq([(y,(bx,y)) for y in range(cy-22,cy-12)]), '|', seq([(y,(bx,y)) for y in range(cy+12,cy+24)]))
    print('Hright', seq([(x,(x,cy)) for x in range(br-6,br+6)]))
