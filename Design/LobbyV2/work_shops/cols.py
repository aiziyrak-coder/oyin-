import numpy as np, math
from m import IM, L
def core(box, mode='bright', thr=0.85, pad=1):
    x0=max(0,int(math.floor(box[0]))-pad); y0=max(0,int(math.floor(box[1]))-pad)
    x1=int(math.ceil(box[2]))+pad; y1=int(math.ceil(box[3]))+pad
    l=L[y0:y1,x0:x1]; c=IM[y0:y1,x0:x1]
    border=np.concatenate([l[0,:],l[-1,:],l[:,0],l[:,-1]]); bg=np.median(border)
    fg=np.percentile(l,99.5) if mode=='bright' else np.percentile(l,0.5)
    cov=np.clip((l-bg)/(fg-bg),0,1)
    sel=cov>=thr
    med=np.median(c[sel],axis=0)
    mx=c[sel].max(axis=0)
    return '#%02X%02X%02X'%tuple(int(round(v)) for v in med), int(sel.sum())
items={
 'brand_text':[33.8,15.1,71.0,24.3],'brand_icon_glyph':[15.6,13.6,26.3,25.5],
 'tab_home':[92.9,16.3,129.8,22.5],'tab_world':[150.4,16.5,171.9,23.9],'tab_friends':[192.8,16.5,217.3,22.4],
 'tab_top':[238.4,16.4,262.1,23.4],'tab_settings':[281.9,16.2,316.8,22.6],
 'globe':[335.3,14.1,346.8,26.0],'uz':[350.0,17.3,358.0,22.7],'lang_chev':[361.0,19.0,365.1,21.1],
 'bell':[375.8,14.1,385.9,25.7],'pname':[417.3,18.4,450.0,23.1],'pchev':[455.1,19.0,460.0,21.9],
 'title':[14.6,45.7,100.2,61.8],'subtitle':[14.0,66.9,112.2,74.0],
 'search_icon':[173.1,50.3,182.5,59.8],'placeholder':[190.0,52.3,283.2,59.0],
 'filter_icon':[382.8,50.3,392.0,59.9],'filter_text':[398.0,52.1,425.7,58.0],'filter_caret':[441.9,54.0,444.5,56.1],'filter_chev':[453.2,53.2,458.3,56.3],
 'sign':[234.3,117.3,316.8,131.7],'link':[370.6,285.8,433.3,292.0],'link_arrow':[440.2,286.0,446.9,291.9],
}
side_icons=[[22.1,93.2,32.9,103.8],[22.2,117.0,33.0,127.0],[22.2,141.8,32.7,151.9],[22.4,166.3,32.9,175.0],[22.5,191.0,32.6,200.9],[23.0,215.7,32.3,225.8],[22.6,240.9,32.7,250.4],[22.6,265.2,32.6,276.0]]
side_labels=[[43.0,95.1,70.0,101.4],[43.0,119.0,68.8,126.2],[43.0,143.3,79.3,149.5],[42.8,168.1,85.0,174.0],[43.0,192.9,79.2,200.1],[43.0,217.6,70.1,223.8],[43.0,242.2,60.9,249.4],[43.0,267.8,67.6,274.8]]
for k,v in items.items(): print(k, core(v), core(v,thr=0.7))
for i,v in enumerate(side_icons): print('icon',i, core(v))
for i,v in enumerate(side_labels): print('label',i, core(v), core(v,thr=0.7))
print('---- p90 of core')
def p90(box, pad=1):
    x0=max(0,int(math.floor(box[0]))-pad); y0=max(0,int(math.floor(box[1]))-pad)
    x1=int(math.ceil(box[2]))+pad; y1=int(math.ceil(box[3]))+pad
    l=L[y0:y1,x0:x1].reshape(-1); c=IM[y0:y1,x0:x1].reshape(-1,3)
    order=np.argsort(l); top=c[order[int(len(order)*0.97):]]
    return '#%02X%02X%02X'%tuple(int(round(v)) for v in np.median(top,axis=0))
for k,v in items.items(): print(k, p90(v))
for i,v in enumerate(side_icons): print('icon',i, p90(v))
for i,v in enumerate(side_labels): print('label',i, p90(v))
