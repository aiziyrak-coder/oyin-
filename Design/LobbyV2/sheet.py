from PIL import Image, ImageDraw
import sys
ids = ["M1","M2","M3","M5","F1","F2","F3","F4","F5"]
tiles = []
for i in ids:
    im = Image.open(rf"D:\Game1\CraDev\Logs\Outfit\{i}_labels.png").convert("RGB").resize((800, 400), Image.LANCZOS)
    d = ImageDraw.Draw(im); d.text((6, 4), i, fill=(255,255,0))
    tiles.append(im)
sheet = Image.new("RGB", (1600, 400 * 5), (20, 20, 20))
for k, t in enumerate(tiles):
    sheet.paste(t, ((k % 2) * 800, (k // 2) * 400))
sheet.save("outfit_sheet.png")
