from lm import *
img=load(PAGE); m=loadm('mask.png'); H,W=m.shape
g=load('g_v2.png')
# side strip mask = mask pixels with x<118 (panel + header + subtitle + left frame)
side=m.copy(); side[:,118:]=False
# connected: the nav brand etc are inside side. Keep other mask regions filled from g (context)
base=img.copy(); base[m]=g[m]
# A: two square windows 200x200, stacked, mask=side
a=fill(base, side, (0,0,200,200), paste=side&rect(m.shape,0,0,W,150))
a=fill(a, side&rect(m.shape,0,150,W,H), (0,109,200,309))
save(a,'l_A.png')
# B: 250 windows
b=fill(base, side, (0,0,250,250), paste=side&rect(m.shape,0,0,W,160))
b=fill(b, side&rect(m.shape,0,160,W,H), (0,59,250,309))
save(b,'l_B.png')
# C: ring: inner band x>=60 first (window 0..309 full height), then rest
inner=side&rect(m.shape,60,0,W,H)
c=fill(base, side, (0,0,309,309), paste=inner)
c=fill(c, side&rect(m.shape,0,0,60,H), (0,0,200,200), paste=side&rect(m.shape,0,0,60,150))
c=fill(c, side&rect(m.shape,0,150,60,H), (0,109,200,309))
save(c,'l_C.png')
