import sys
from PIL import Image
P = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\pages\friends.png"
im = Image.open(P).convert("RGB")
boxes = [tuple(map(int, b.split(","))) for b in sys.argv[2:]]
s = 16
tiles = [im.crop(b).resize(((b[2]-b[0])*s, (b[3]-b[1])*s), Image.LANCZOS) for b in boxes]
W = sum(t.width for t in tiles) + 10 * len(tiles); H = max(t.height for t in tiles)
out = Image.new("RGB", (W, H), (255, 0, 255)); x = 0
for t in tiles: out.paste(t, (x, 0)); x += t.width + 10
out.save(sys.argv[1])
