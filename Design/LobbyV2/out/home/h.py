import sys
from PIL import Image
# usage: h.py up in out scale [x0 y0 x1 y1]
a=sys.argv
if a[1]=='up':
    im=Image.open(a[2]).convert('RGB')
    s=float(a[4])
    if len(a)>5:
        x0,y0,x1,y1=map(int,a[5:9]); im=im.crop((x0,y0,x1,y1))
    im.resize((int(im.width*s),int(im.height*s)),Image.LANCZOS).save(a[3])
