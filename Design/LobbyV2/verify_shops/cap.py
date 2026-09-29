import sys
from meas import ink
# ink box of a sub-region: x0 y0 x1 y1 (exact region, exp=0 uses border of region)
x0,y0,x1,y1=map(float,sys.argv[1:5]); mode=sys.argv[5] if len(sys.argv)>5 else 'bright'
print(ink([x0,y0,x1,y1],mode,exp=int(sys.argv[6]) if len(sys.argv)>6 else 1))
