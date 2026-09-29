import sys
import numpy as np
from meas import IM, L
# usage: px.py row Y X0 X1  | col X Y0 Y1 | rgb row/col same
mode=sys.argv[1]; a=list(map(float,sys.argv[2:5]))
if mode in('row','rrow'):
    y=int(a[0]); xs=range(int(a[1]),int(a[2]))
    print(' '.join(f"{x}:{'%02X%02X%02X'%tuple(IM[y,x].astype(int))}" if mode=='rrow' else f"{x}:{L[y,x]:.0f}" for x in xs))
else:
    x=int(a[0]); ys=range(int(a[1]),int(a[2]))
    print(' '.join(f"{y}:{'%02X%02X%02X'%tuple(IM[y,x].astype(int))}" if mode=='rcol' else f"{y}:{L[y,x]:.0f}" for y in ys))
