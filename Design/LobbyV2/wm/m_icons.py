from ink import *
import numpy as np
def white_box(x0,y0,x1,y1, lo=None, hi=None, thr=0.25):
    sub = IM[y0:y1,x0:x1]
    v = sub.min(axis=2)
    lo = np.quantile(v,0.5) if lo is None else lo
    hi = np.quantile(v,0.99) if hi is None else hi
    a = np.clip((v-lo)/(hi-lo),0,1)
    cA=a.max(0); rA=a.max(1)
    cs=np.where(cA>thr)[0]; rs=np.where(rA>thr)[0]
    L=cs[0]+1-min(1,cA[cs[0]]/0.9); R=cs[-1]+min(1,cA[cs[-1]]/0.9)
    T=rs[0]+1-min(1,rA[rs[0]]/0.9); B=rs[-1]+min(1,rA[rs[-1]]/0.9)
    return [round(x0+L,1),round(y0+T,1),round(x0+R,1),round(y0+B,1)], hexc(np.median(sub[a>0.85],0)), [round(float(x),1) for x in cA], [round(float(x),1) for x in rA]
I = {'mag':(240,155,266,181),'oquv':(467,116,494,139),'biz':(306,95,331,118),'kon':(523,193,549,216),'tur':(240,242,266,266),'tad':(387,288,413,312),'xiz':(552,304,579,327)}
for k,r in I.items():
    b,c,ca,ra = white_box(*r)
    print(k, b, c); print('  cols', ca); print('  rows', ra)
