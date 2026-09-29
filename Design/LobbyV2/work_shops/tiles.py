from m import ink, IM, L
import numpy as np
tiles=[[118.8,187.6,197.0,226.9],[202.5,187.6,280.7,226.9],[285.6,187.6,364.0,226.9],[368.6,187.6,447.6,226.9],
       [118.8,231.5,197.0,270.3],[202.5,231.5,280.7,270.3],[285.6,231.5,364.0,270.3],[368.6,231.5,447.6,270.3]]
for i,t in enumerate(tiles):
    x0,y0,x1,y1=int(t[0])+4,int(t[1])+4,int(t[2])-3,int(t[3])-3
    r=ink(x0,y0,x1,y1,'dark')
    # fill color: median of bright pixels
    sub=IM[y0:y1,x0:x1].reshape(-1,3); l=L[y0:y1,x0:x1].reshape(-1)
    fill=np.median(sub[l>np.percentile(l,60)],axis=0)
    dark=np.median(sub[l<np.percentile(l,3)],axis=0)
    # saturated color
    print(i,'logo',r[0],'bg %.0f'%r[1],'fill #%02X%02X%02X'%tuple(int(v) for v in fill),'logo #%02X%02X%02X'%tuple(int(v) for v in dark))
