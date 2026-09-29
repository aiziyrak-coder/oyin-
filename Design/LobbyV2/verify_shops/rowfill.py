import numpy as np
from meas import IM
rows=[(109.8,133.5),(134.5,157.8),(158.8,182.6),(183.5,207.7),(208.7,232.4),(233.4,257.2),(258.2,282.4)]
A=[];Bv=[]
pairs=[]
for t,b in rows:
    ys=slice(int(t)+4,int(b)-3)
    for xin,xout in [((15,19),(6,10)),((96,101),(104,108)),((86,92),(104,108))]:
        rin=IM[ys,xin[0]:xin[1]].reshape(-1,3).mean(0)
        rout=IM[ys,xout[0]:xout[1]].reshape(-1,3).mean(0)
        pairs.append((rin,rout))
pairs=np.array(pairs)  # N x 2 x 3
# model: rin = a*F + (1-a)*rout  -> linear in rout: rin = c + k*rout, k=1-a, c=a*F
for ch in range(3):
    x=pairs[:,1,ch]; y=pairs[:,0,ch]
    k,c=np.polyfit(x,y,1)
    a=1-k; F=c/a
    print('ch',ch,'alpha %.2f F %.1f'%(a,F))
print(np.round(pairs[:6]).astype(int))
