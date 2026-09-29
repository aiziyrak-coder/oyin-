import numpy as np
from meas import L
def edge(p, off, rising=True):
    # p: profile across an edge; find 0.5 crossing between lo and hi estimated from ends
    lo=np.median(p[:2]) if rising else np.median(p[-2:]); hi=np.median(p[-2:]) if rising else np.median(p[:2])
    for i in range(len(p)-1):
        a,b=p[i],p[i+1]
        m=(lo+hi)/2
        if (a-m)*(b-m)<=0 and a!=b:
            return off+i+0.5+(m-a)/(b-a), lo, hi
    return None,lo,hi
cols=[(118.8,197.0),(202.5,280.7),(285.6,364.0),(368.6,447.6)]
rows=[(187.6,226.9),(231.5,270.3)]
for r,(ty,by) in enumerate(rows):
    for c,(lx,rx) in enumerate(cols):
        ym0,ym1=int(ty)+8,int(by)-8
        xm0,xm1=int(lx)+10,int(rx)-10
        # left edge: horizontal profile averaged over mid rows
        x0=int(lx)-4; p=L[ym0:ym1, x0:x0+9].mean(0); le=edge(p,x0,True)
        x0=int(rx)-4; p=L[ym0:ym1, x0:x0+9].mean(0); re=edge(p,x0,False)
        y0=int(ty)-4; p=L[y0:y0+9, xm0:xm1].mean(1); te=edge(p,y0,True)
        y0=int(by)-4; p=L[y0:y0+9, xm0:xm1].mean(1); be=edge(p,y0,False)
        print(r,c,'L %.2f R %.2f T %.2f B %.2f'%(le[0],re[0],te[0],be[0]), 'spec',lx,ty,rx,by)
