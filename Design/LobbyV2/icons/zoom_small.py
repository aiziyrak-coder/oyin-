import json, sys
sys.path.insert(0, 'v2/icons')
from icon_render import render
from PIL import Image
data = json.load(open(sys.argv[1], encoding='utf-8-sig'))
icons = data['icons']
names = sys.argv[3].split(',') if len(sys.argv) > 3 else None
if names: icons = [i for i in icons if i['name'] in names]
S = 5  # upscale
cell = 24 * S + 12
cell2 = 32 * S // 2 * 0 + 16 * S + 12
W = len(icons)
bg = Image.new('RGB', (W * (24*S + 12) , 24*S*2 + 30 + 12), (22, 30, 44))
for k, ic in enumerate(icons):
    x = k * (24*S + 12) + 6
    im24 = render(ic, 24).resize((24*S, 24*S), Image.NEAREST)
    bg.paste(im24, (x, 6), im24)
    im12 = render(ic, 16).resize((16*S, 16*S), Image.NEAREST)
    bg.paste(im12, (x, 24*S + 18), im12)
bg.save(sys.argv[2])
print(bg.size)
