import numpy as np
from meas import L
def cr(p, lo, hi):
    m=(lo+hi)/2
    for i in range(len(p)-1):
        a,b=p[i],p[i+1]
        if (a-m)*(b-m)<=0 and a!=b: return i+0.5+(m-a)/(b-a)
    return None
print('search TL (left edge per row)')
for y in range(43,52):
    p=L[y,160:175]; c=cr(p,np.median(L[y,160:163]),100); print(y, None if c is None else round(160+c,2))
print('search BL')
for y in range(58,68):
    p=L[y,160:175]; c=cr(p,np.median(L[y,160:163]),100); print(y, None if c is None else round(160+c,2))
print('filter TR (right edge per row), from outside')
for y in range(42,52):
    p=L[y,455:472][::-1]; c=cr(p,np.median(L[y,469:472]),np.median(L[48:60,455:460])); print(y, None if c is None else round(472-c,2))
print('filter TL')
for y in range(42,52):
    p=L[y,370:385]; c=cr(p,np.median(L[y,370:373]),np.median(L[48:60,379:382])); print(y, None if c is None else round(370+c,2))
