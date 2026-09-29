import sys
from ink import *
# usage: line.py h y x0 x1 | v x y0 y1
m=sys.argv[1]; a,b,c=map(int,sys.argv[2:5])
out=[]
if m=='h':
    for x in range(b,c): out.append('%d:%s'%(x,px(x,a)))
else:
    for y in range(b,c): out.append('%d:%s'%(y,px(a,y)))
print(' '.join(out))
