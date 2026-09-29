from ink import *
pins = {
 'mag': (235,268,336,151,185),
 'oquv': (464,496,582,111,144),
 'biz': (303,333,410.5,90,122),
 'kon': (520,551,647,188,220.5),
 'tur': (236,268,337,237,271),
 'tad': (383,415,480,282,318),
 'xiz': (549,581,644,298,333),
}
for k,(cx0,cx1,bx1,y0,y1) in pins.items():
    cy = int((y0+y1)/2)
    print('==', k)
    print(' L cols y=%d from %d:'%(cy,cx0-5), prof_cols(cx0-5,cy-1,cx0+5,cy+2))
    print(' R cols y=%d from %d:'%(cy,int(bx1)-6), prof_cols(int(bx1)-6,cy-1,int(bx1)+5,cy+2))
    xm = int(bx1)-6
    print(' T/B rows x=%d from %d:'%(xm,int(y0)-5), prof_rows(xm-1,int(y0)-5,xm+1,int(y1)+5))
    xc = int((cx0+cx1)/2)
    print(' circle rows x=%d from %d:'%(xc,int(y0)-5), prof_rows(xc-1,int(y0)-5,xc+1,int(y1)+9))
