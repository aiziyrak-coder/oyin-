"""avatars.json dan prompts.md (3D modelchi va rasm generatori uchun) va avatars.html (nusxalash tugmali sahifa) ni yaratadi."""
import json, os

here = os.path.dirname(os.path.abspath(__file__))
data = json.load(open(os.path.join(here, "avatars.json"), encoding="utf-8"))
lines = [
    "# CraDev avatarlari: rasm promptlari",
    "",
    "9 ta asosiy avatar (4 erkak, 5 ayol), yuzsiz: o'yinchining yuzi keyin skaner qilinib qo'yiladi.",
    "Har bir prompt 3 tomondan ko'rinishli (old, yon, orqa) reference rasm beradi: 3D modelchi shu rasm bo'yicha ishlaydi.",
    "Teri rangi o'rtacha neytral: o'yinda u o'yinchi yuzining rangiga avtomatik moslanadi.",
    "",
    "**Negative prompt (Stable Diffusion, Leonardo, Flux):**",
    "",
    "```", data["negative"], "```",
    "",
    f"**Midjourney uchun prompt oxiriga:** `{data['midjourney']}`",
    "",
]
for gender, title in (("male", "Erkaklar"), ("female", "Ayollar")):
    lines += [f"## {title}", ""]
    for a in (a for a in data["avatars"] if a["gender"] == gender):
        prompt = data["template"].replace("{subject}", a["subject"]).replace("{hair}", a["hair"]).replace("{outfit}", a["outfit"])
        lines += [
            f"### {a['id']} · {a['name']}",
            "",
            f"- Yosh: {a['age']}, bo'y: {a['height']} sm, qomat: {a['build']}",
            f"- Soch: {a['hairUz']}",
            f"- Kiyim: {a['outfitUz']}",
            "",
            "```", prompt, "```", "",
        ]
open(os.path.join(here, "prompts.md"), "w", encoding="utf-8").write("\n".join(lines))
page = open(os.path.join(here, "page.template.html"), encoding="utf-8").read()
page = page.replace("__DATA__", json.dumps(data, ensure_ascii=False))
open(os.path.join(here, "avatars.html"), "w", encoding="utf-8").write(page)
print("prompts.md va avatars.html tayyor")
