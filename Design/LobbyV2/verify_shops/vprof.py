import sys, numpy as np
from meas import IM, L
x0,x1,y0,y1=map(int,sys.argv[1:5]); axis=sys.argv[5] if len(sys.argv)>5 else 'v'
if axis=='v':
    p=L[y0:y1,x0:x1].mean(1); print(' '.join(f'{y0+i}:{v:.1f}' for i,v in enumerate(p)))
else:
    p=L[y0:y1,x0:x1].mean(0); print(' '.join(f'{x0+i}:{v:.1f}' for i,v in enumerate(p)))
