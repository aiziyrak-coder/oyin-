import numpy as np
from meas import IM
# per row: color diff between x=108 and x=111 (panel edge contrast)
for y in range(0,309,3):
    a=IM[y,106:109].mean(0); b=IM[y,111:114].mean(0)
    print(y, int(np.abs(a-b).sum()), '%02X%02X%02X'%tuple(a.astype(int)), '%02X%02X%02X'%tuple(b.astype(int)))
