import numpy as np
from meas import IM
B=IM[...,2]-IM[...,0]  # blueness
def cr(p, lo, hi):
    m=(lo+hi)/2
    for i in range(len(p)-1):
        a,b=p[i],p[i+1]
        if (a-m)*(b-m)<=0 and a!=b: return i+0.5+(m-a)/(b-a)
    return None
print('item0 TL left edge per row (blueness)')
for y in range(88,96):
    p=B[y,8:20]; c=cr(p,np.median(B[95:102,8:10]),np.median(B[95:102,16:20])); print(y, None if c is None else round(8+c,2))
print('item0 BL')
for y in range(102,110):
    p=B[y,8:20]; c=cr(p,np.median(B[95:102,8:10]),np.median(B[95:102,16:20])); print(y, None if c is None else round(8+c,2))
print('item0 TR right edge per row')
for y in range(88,96):
    p=B[y,95:108][::-1]; c=cr(p,np.median(B[95:102,105:108]),np.median(B[95:102,95:98])); print(y, None if c is None else round(108-c,2))
