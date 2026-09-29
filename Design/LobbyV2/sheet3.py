from PIL import Image, ImageDraw
ids = ["M2","M3","M5","F1","F2","F3","F4","F5"]
rows = []
for i in ids:
    im = Image.open(rf"D:\Game1\CraDev\Logs\Outfit\test_{i}.png").convert("RGB")
    im = im.resize((im.width * 2 // 5, im.height * 2 // 5), Image.LANCZOS)
    ImageDraw.Draw(im).text((4, 4), i, fill=(255, 255, 0))
    rows.append(im)
W = rows[0].width; H = rows[0].height
sheet = Image.new("RGB", (W * 2, H * 4), (20, 20, 20))
for k, r in enumerate(rows):
    sheet.paste(r, ((k % 2) * W, (k // 2) * H))
sheet.save("outfit_tests.png"); print(sheet.size)
