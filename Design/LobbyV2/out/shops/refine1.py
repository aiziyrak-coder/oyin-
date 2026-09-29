from lm import *
img=load(PAGE); m=loadm('mask.png'); H,W=m.shape
g=load('g_v2.png')
base=img.copy(); base[m]=g[m]
R=lambda x0,y0,x1,y1: rect(m.shape,x0,y0,x1,y1)
def win(cx,cy,size):
    size=int(min(size,H,W)); x0=int(np.clip(cx-size//2,0,W-size)); y0=int(np.clip(cy-size//2,0,H-size)); return (x0,y0,x0+size,y0+size)
out=base.copy()
# nav items (x>=85, y<34), per component
navm=m&R(85,4,W-4,34)
for (x0,y0,x1,y1) in tools.components(navm):
    cm=navm&R(x0,y0,x1+1,y1+1)
    size=max(140,2.4*max(x1-x0+1,y1-y0+1))
    out=fill(out,cm,win((x0+x1)//2,(y0+y1)//2,size))
save(out,'r_nav.png')
# search + filter
sm=m&R(150,36,W-4,74)
for (x0,y0,x1,y1) in tools.components(sm):
    cm=sm&R(x0,y0,x1+1,y1+1)
    size=max(160,2.2*max(x1-x0+1,y1-y0+1))
    print('search comp',x0,y0,x1,y1,win((x0+x1)//2,(y0+y1)//2,size))
    out=fill(out,cm,win((x0+x1)//2,(y0+y1)//2,size))
save(out,'r_search.png')
# view-all
vm=m&R(360,280,460,300)
for (x0,y0,x1,y1) in tools.components(vm):
    cm=vm&R(x0,y0,x1+1,y1+1)
    out=fill(out,cm,win((x0+x1)//2,(y0+y1)//2,160))
save(out,'r_view.png')
