from PIL import Image
im = Image.open("pages.webp").convert("RGB")
boxes = {
 "p1_home": (0, 0, 767, 415),
 "p2_map": (776, 0, 1536, 415),
 "p3_wardrobe": (0, 421, 521, 732),
 "p4_shops": (526, 421, 1020, 732),
 "p5_edu": (1022, 421, 1536, 732),
 "p6_business": (0, 739, 520, 1024),
 "p7_friends": (526, 739, 1020, 1024),
 "p8_settings": (1030, 739, 1536, 1024),
}
for k, b in boxes.items():
    c = im.crop(b)
    c.save(k + ".png")
    c.resize((c.width * 2, c.height * 2), Image.LANCZOS).save(k + "_2x.png")
    print(k, c.size)
