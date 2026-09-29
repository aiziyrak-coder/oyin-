from PIL import Image, ImageDraw
ids = ["M1","M2","M3","M5","F1","F2","F3","F4","F5"]
for kind in ["hair", "body"]:
    sheet = Image.new("RGB", (1500, 250 * 5), (20, 20, 20))
    for k, i in enumerate(ids):
        im = Image.open(rf"D:\Game1\CraDev\Logs\Outfit\{i}_{kind}.png").convert("RGB").resize((500, 250), Image.LANCZOS)
        ImageDraw.Draw(im).text((6, 4), i, fill=(255,255,0))
        sheet.paste(im, ((k % 3) * 500, (k // 3) * 250))
    sheet.save(f"outfit_{kind}.png")
