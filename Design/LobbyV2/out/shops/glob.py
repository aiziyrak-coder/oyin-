from lm import *
img=load(PAGE); m=loadm('mask.png')
H,W=m.shape
# V1 anisotropic whole-image
o=run512(img,m); r=img.copy(); r[m]=o[m]; save(r,'g_v1.png'); zoom(r,'g_v1_3x.png')
# V2: scale to width 512, reflect pad vertically to 512 (pad unmasked)
s=512/W; h2=round(H*s)
im2=np.asarray(Image.fromarray(img.astype(np.uint8)).resize((512,h2),Image.BICUBIC)).astype(np.float32)
m2=np.asarray(Image.fromarray((m*255).astype(np.uint8)).resize((512,h2),Image.BILINEAR))>10
pt=(512-h2)//2; pb=512-h2-pt
imp=np.pad(im2,((pt,pb),(0,0),(0,0)),mode='reflect'); mp=np.pad(m2,((pt,pb),(0,0)),mode='reflect')
x=imp.copy(); x[mp]=0
x=(x[...,::-1]/255.0).transpose(2,0,1)[None]
out=tools.lama().run(None,{'image':x.astype(np.float32),'mask':mp[None,None].astype(np.float32)})[0][0].transpose(1,2,0)[...,::-1]
out=np.clip(out,0,255).astype(np.uint8)[pt:pt+h2]
o2=np.asarray(Image.fromarray(out).resize((W,H),Image.BICUBIC)).astype(np.float32)
r=img.copy(); r[m]=o2[m]; save(r,'g_v2.png'); zoom(r,'g_v2_3x.png')
