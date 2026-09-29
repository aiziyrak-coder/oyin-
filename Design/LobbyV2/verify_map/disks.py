import json, numpy as np
from PIL import Image
IM=np.asarray(Image.open(r"..\pages\map.png").convert("RGB")).astype(float)
spec=json.load(open('work.json',encoding='utf-8'))
E={e['id']:e for e in spec['elements']}
def hx(h): h=h.lstrip('#'); return np.array([int(h[i:i+2],16) for i in (0,2,4)],float)
for n in range(7):
    c=E[f'map.pin_{n}_circle']; b=c['box']; fill=hx(c['fill'])
    x0,y0,x1,y1=int(b[0])-3,int(b[1])-3,int(b[2])+3,int(b[3])+3
    W=IM[y0:y1,x0:x1]
    d=np.sqrt(((W-fill)**2).sum(2))
    m=d<45
    # boundary points: per row leftmost; per column top & bottom ; (right side may merge? disk colour differs from bubble so ok)
    pts=[]
    for i in range(m.shape[0]):
        xs=np.where(m[i])[0]
        if len(xs)>2: pts+= [(xs[0]-0.0+x0, i+0.5+y0),(xs[-1]+1.0+x0, i+0.5+y0)]
    for j in range(m.shape[1]):
        ys=np.where(m[:,j])[0]
        if len(ys)>2: pts+= [(j+0.5+x0, ys[0]+y0),(j+0.5+x0, ys[-1]+1.0+y0)]
    P=np.array(pts)
    # algebraic circle fit
    A=np.c_[2*P[:,0],2*P[:,1],np.ones(len(P))]; bb=(P**2).sum(1)
    cx,cy,k=np.linalg.lstsq(A,bb,rcond=None)[0]; r=np.sqrt(k+cx*cx+cy*cy)
    res=np.abs(np.sqrt(((P-[cx,cy])**2).sum(1))-r)
    # refit w/o outliers
    keep=res<1.5
    A2=A[keep]; b2=bb[keep]
    cx,cy,k=np.linalg.lstsq(A2,b2,rcond=None)[0]; r=np.sqrt(k+cx*cx+cy*cy)
    print(f"pin{n} disk centre ({cx:.1f},{cy:.1f}) r_disk {r:.2f}  spec circle centre ({(b[0]+b[2])/2:.1f},{(b[1]+b[3])/2:.1f}) R {(b[2]-b[0])/2:.1f}  outlier frac {1-keep.mean():.2f}")
