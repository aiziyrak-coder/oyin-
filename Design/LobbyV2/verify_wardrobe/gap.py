from m import *
import numpy as np, json, sys
s=json.load(open(sys.argv[1],encoding='utf-8'))
P=np.array(s['removePolygons'][0]['points'])
def polyx(y, lo, hi):
    best=[]
    n=len(P)
    for i in range(n):
        (x0,y0),(x1,y1)=P[i],P[(i+1)%n]
        if min(y0,y1)<=y<=max(y0,y1) and y0!=y1:
            x=x0+(x1-x0)*(y-y0)/(y1-y0)
            if lo<=x<=hi: best.append(round(x,1))
    return best
for y in range(200,286,2):
    row=L[y,176:214]
    bright=np.percentile(row,85); dark=np.percentile(row,8); mid=(bright+dark)/2
    idx=np.where(row>mid)[0]
    if len(idx)==0: print(y,'none'); continue
    l=idx[0]; r=idx[-1]
    le=176+l-(row[l]-mid)/(row[l]-row[l-1]+1e-6)+0.5 if l>0 else float('nan')
    re=176+r+(row[r]-mid)/(row[r]-row[r+1]+1e-6)+0.5 if r<len(row)-1 else float('nan')
    print(y,'gap %.1f..%.1f'%(le,re),'poly',polyx(y,176,214),'d%.0f b%.0f'%(dark,bright))
