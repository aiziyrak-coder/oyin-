from m import *
import numpy as np, json
s=json.load(open('work.json',encoding='utf-8'))
E={e['id']:e for e in s['elements']}
cols=[(279.3,331.0),(336.8,388.0),(393.7,445.0),(450.0,502.0)]
rows=[(89.0,142.0),(149.0,202.0),(208.0,259.0)]
for i in range(12):
    x0,x1=cols[i%4]; y0,y1=rows[i//4]
    X0,X1,Y0,Y1=int(x0)+3,int(x1)-3,int(y0)+3,int(y1)-3
    sub=IM[Y0:Y1,X0:X1]
    # bg per row from left/right margins (3px each)
    marg=np.concatenate([sub[:,0:3],sub[:,-3:]],1)
    bg=np.median(marg,1)[:,None,:]
    d=np.abs(sub-bg).max(2)
    m=d>14
    # remove badge zone
    b=E[f'wardrobe.tile_{i}_badge']['box']
    m[int(b[1])-1-Y0:int(b[3])+2-Y0, int(b[0])-1-X0:int(b[2])+2-X0]=False
    # column/row counts
    cc=m.sum(0); rc=m.sum(1)
    cs=np.where(cc>=2)[0]; rs=np.where(rc>=2)[0]
    bb=[X0+cs[0],Y0+rs[0],X0+cs[-1]+1,Y0+rs[-1]+1]
    print(i,'spec',E[f'wardrobe.tile_{i}_image']['box'],'auto',bb, 'bgTop',hexc(bg[2,0]),'bgBot',hexc(bg[-3,0]))
