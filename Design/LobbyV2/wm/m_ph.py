from ink import *
import numpy as np
def colbg_ink(x0,y0,x1,y1,thr=0.3,delta=None):
    v = LUM[y0:y1,x0:x1]
    bg = np.median(v,axis=0,keepdims=True)
    d = v-bg
    fg = np.quantile(d,0.995) if delta is None else delta
    a = np.clip(d/fg,0,1)
    cA=a.max(0); rA=a.max(1)
    cs=np.where(cA>thr)[0]; rs=np.where(rA>thr)[0]
    L=cs[0]+1-min(1,cA[cs[0]]/0.9); R=cs[-1]+min(1,cA[cs[-1]]/0.9)
    T=rs[0]+1-min(1,rA[rs[0]]/0.9); B=rs[-1]+min(1,rA[rs[-1]]/0.9)
    sub=IM[y0:y1,x0:x1]
    return [round(x0+L,1),round(y0+T,1),round(x0+R,1),round(y0+B,1)], hexc(np.median(sub[a>0.8],0)), [round(float(x),2) for x in rA]
print(colbg_ink(560,61,672,89))
print('J', colbg_ink(563,61,571,89))
