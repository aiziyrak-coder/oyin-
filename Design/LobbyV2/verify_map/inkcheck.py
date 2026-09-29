import json, sys, numpy as np
from PIL import Image
IM=np.asarray(Image.open(r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\map.png").convert("RGB")).astype(float)
L=IM@[0.299,0.587,0.114]
spec=json.load(open(sys.argv[1],encoding='utf-8'))
kinds=sys.argv[2].split(',') if len(sys.argv)>2 else ['text','icon']
M=float(sys.argv[3]) if len(sys.argv)>3 else 2.5
def edges(prof, thr):
    idx=np.where(prof>=thr)[0]
    if len(idx)==0: return None
    i0,i1=idx[0],idx[-1]
    # subpixel: fraction of pixel covered ~ linear interp
    lo = i0 - (0 if i0==0 else (prof[i0]-thr)/max(1e-6,prof[i0]-prof[i0-1]))  # crossing position (pixel centers)
    hi = i1 + (0 if i1==len(prof)-1 else (prof[i1]-thr)/max(1e-6,prof[i1]-prof[i1+1]))
    return lo+0.5-0.0, hi+0.5  # convert center coords to edge coords approx
for e in spec['elements']:
    if e['kind'] not in kinds: continue
    b=e['box']
    x0=int(np.floor(b[0]-M)); y0=int(np.floor(b[1]-M)); x1=int(np.ceil(b[2]+M)); y1=int(np.ceil(b[3]+M))
    c=L[y0:y1,x0:x1]
    ring=np.concatenate([c[0],c[-1],c[:,0],c[:,-1]])
    bg=np.median(ring); pk=np.percentile(c,99.5)
    thr=bg+0.5*(pk-bg)
    ex=edges(c.max(0),thr); ey=edges(c.max(1),thr)
    if ex is None: print(e['id'],'none'); continue
    got=[x0+ex[0]-0.5+0.5-0.5, y0+ey[0]-0.5, x0+ex[1]-0.5, y0+ey[1]-0.5]
    # (edges() returns crossing+0.5 => boundary; subtract .5 then treat crossing at pixel center as boundary)
    got=[x0+ex[0]-0.5, y0+ey[0]-0.5, x0+ex[1]-0.5, y0+ey[1]-0.5]
    d=[g-s for g,s in zip(got,b)]
    flag='  <<<' if max(abs(v) for v in d)>0.6 else ''
    print(f"{e['id']:28s} spec {[round(v,1) for v in b]} ink {[round(v,1) for v in got]} d {[round(v,1) for v in d]} bg{bg:.0f} pk{pk:.0f}{flag}")
