import numpy as np, sys
from meas import L
def cr(p, lo, hi):
    m=(lo+hi)/2
    for i in range(len(p)-1):
        a,b=p[i],p[i+1]
        if (a-m)*(b-m)<=0 and a!=b: return i+0.5+(m-a)/(b-a)
    return None
# tile 0 top-left corner, tile 3 top-right, tile 4 bottom-left
for y in range(187,196):
    p=L[y,114:130]; print('TL y',y, round(114+cr(p,60,248),2) if cr(p,60,248) is not None else None)
for y in range(262,272):
    p=L[y,114:130]; print('BL y',y, round(114+cr(p,60,248),2) if cr(p,60,248) is not None else None)
for y in range(187,196):
    p=L[y,436:452][::-1]; c=cr(p,115,248); print('TR y',y, round(452-c,2) if c is not None else None)
