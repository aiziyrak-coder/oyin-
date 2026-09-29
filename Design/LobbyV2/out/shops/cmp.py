import sys
from PIL import Image, ImageDraw
# usage: cmp.py out.png x0 y0 x1 y1 scale img1 img2 ...
out=sys.argv[1]; x0,y0,x1,y1=map(int,sys.argv[2:6]); s=int(sys.argv[6]); ims=sys.argv[7:]
tiles=[Image.open(p).convert('RGB').crop((x0,y0,x1,y1)).resize(((x1-x0)*s,(y1-y0)*s),Image.LANCZOS) for p in ims]
w=tiles[0].width; h=tiles[0].height
horiz = w < h*1.3
if horiz:
    c=Image.new('RGB',(w*len(tiles)+6*(len(tiles)-1),h+14),(255,255,255))
    for i,t in enumerate(tiles):
        c.paste(t,(i*(w+6),14)); ImageDraw.Draw(c).text((i*(w+6)+2,1),ims[i],fill=(0,0,0))
else:
    c=Image.new('RGB',(w,(h+14)*len(tiles)),(255,255,255))
    for i,t in enumerate(tiles):
        c.paste(t,(0,i*(h+14)+14)); ImageDraw.Draw(c).text((2,i*(h+14)+1),ims[i],fill=(0,0,0))
c.save(out); print(out,c.size)
