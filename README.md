# CraDev — o'yin loyihasi

Unity'da kompyuter uchun (Steam) yaratilayotgan 3D o'yin. O'yinni bosqichma-bosqich quryapmiz.

**Hozirgi bosqich — 2: o'yin boshidagi sahnalar.** O'yin ochilganda ketma-ket:

1. **CraDev intro**: kompaniya logosi va "A NEW ERA OF GAMING" yozuvi.
2. **CDCGroup**: ikkinchi brendning kumush logosi.
3. **Loading**: keyingi sahnani fonda yuklaydigan ekran (katta foiz raqami va ingichka chiziq).

Uslub zamonaviy va minimal: bir xil qora fon, tekis ranglar, aniq shriftlar (Unbounded, Manrope),
qisqa va silliq harakatlar. Uchqun, nur yoki "kosmik" effektlar yo'q.

| CraDev | CDCGroup |
|---|---|
| ![CraDev intro](Design/CraDev/preview.png) | ![CDCGroup](Design/CDCGroup/preview.png) |

## Loyihani ochish

1. Loyihani kompyuterga yuklab oling:
   - GitHub Desktop yoki `git clone https://github.com/aiziyrak-coder/oyin-.git`, so'ng
     `claude/gallant-dijkstra-r0opib` branch'iga o'ting;
   - yoki GitHub'da shu branch'ni tanlab, **Code → Download ZIP**.
2. **Unity Hub → Projects → Add → Add project from disk** va loyiha papkasini tanlang
   (ichida `Assets` va `ProjectSettings` papkalari bor papka).
3. Agar Hub "Editor version not installed" desa, ro'yxatdan kompyuteringizdagi
   Unity 6 versiyasini tanlang va versiya almashtirishni tasdiqlang.
4. Birinchi ochilish bir necha daqiqa davom etadi, chunki Unity `Library` papkasini yaratadi.
5. Yuqori menyudan **CraDev → Sahnalarni yaratish (Intro, CDCGroup, Loading)** ni bosing.
   Uchala sahna `Assets/CraDev/Scenes/` ga saqlanadi va Build Settings'da shu tartibda
   birinchi o'rinlarga qo'yiladi. Keyin Intro sahnasi ochiladi.
6. Tezkor tekshirish uchun **Play ▶** tugmasini bosing (Game oynasida 16:9 yoki 1920x1080 ni tanlang).
7. Haqiqiy o'yindek alohida oynada ko'rish uchun **CraDev → O'yinni alohida oynada ishga tushirish
   (Build and Run)** ni bosing. O'yin `Builds/` papkasiga yig'iladi va to'liq ekranda ochiladi:
   ekranda faqat CraDev intro, CDCGroup va Loading chiqadi. Yopish: Alt+F4.

Loyihani avval 1-bosqichda ochgan bo'lsangiz ham, yangilangandan keyin shu menyuni bir marta
bosing: u sahnalarni yangi skriptlar bilan qayta yaratadi.

## O'yin oynasi sozlamalari

Menyudagi buyruqlar Player sozlamalarini avtomatik o'rnatadi:

- **"Made with Unity" ekrani o'chiriladi**: o'yin darhol CraDev intro'si bilan boshlanadi.
  Unity 6'da bu bepul (Personal) litsenziyada ham ishlaydi.
- **To'liq ekranli oyna** (Fullscreen Window), monitorning o'z o'lchamida.
- Kompaniya nomi "CraDev". O'yin nomi tanlanguncha oyna sarlavhasi ham "CraDev" bo'ladi.

## Sahnalar qanday ishlaydi

### 1. CraDev intro (4.2 s)

| Vaqt (s) | Nima bo'ladi |
|---|---|
| 0.25 – 0.85 | Ko'k belgi ekran markazida paydo bo'ladi, ichidagi oq shakl biroz keyin chiqadi ("pop" ovozi) |
| 0.95 – 1.80 | Belgi chapga suriladi, uning ortidan "CraDev" yozuvi chiqib keladi (yengil "vish" va akkord) |
| 1.75 – 2.45 | Ostida "A NEW ERA OF GAMING" paydo bo'ladi |
| 3.6 – 4.2 | Qorong'ilashish, so'ng CDCGroup |

### 2. CDCGroup (3.5 s)

| Vaqt (s) | Nima bo'ladi |
|---|---|
| 0.2 – 0.9 | Ingichka kumush chiziq markazdan ikki tomonga cho'ziladi |
| 0.5 – 1.25 | "CDC" chiziq ortidan yuqoriga ko'tariladi (yumshoq past ton) |
| 0.65 – 1.40 | "GROUP" chiziq ortidan pastga tushadi |
| 2.9 – 3.5 | Qorong'ilashish, so'ng Loading |

### 3. Loading

- Keyingi sahnani (`MainMenu`) fonda yuklaydi va foizni katta raqam hamda ingichka chiziq bilan ko'rsatadi. Yuklash juda tez tugasa ham
  ekran kamida 3 soniya ko'rinib turadi. 100% ga yetgach qorong'ilashib, sahna ochiladi.
- `MainMenu` hali yo'q, shuning uchun hozircha jarayon namoyish uchun to'ladi va "READY" bo'lib
  to'xtaydi. Console'da shu haqda xabar chiqadi. Menyu keyingi bosqichda qo'shiladi.
- Keyinchalik istalgan sahnani Loading orqali ochish mumkin: `SceneLoader.Load("Level01");`

### Umumiy

- Splash ekranlarini istalgan tugma, sichqoncha yoki geympad tugmasi o'tkazib yuboradi. Loading o'tkazib yuborilmaydi.
- Barcha vaqt va effekt qiymatlarini sahnadagi **IntroDirector**, **CDCGroupDirector** va
  **LoadingDirector** obyektlarining Inspector'idan o'zgartirish mumkin.
- Sahnalar to'liq UI (Canvas, Screen Space - Overlay) orqali chiziladi, shuning uchun
  Built-in, URP va HDRP render pipeline'larining barchasida bir xil ishlaydi.
- Eski Input Manager ham, yangi Input System ham qo'llab-quvvatlanadi.

## Fayllar tuzilmasi

```
Assets/CraDev/
  Common/Scripts/   SplashSequence.cs (splash asosi), SceneLoader.cs, Anim.cs
  Common/Fonts/     Manrope (Loading yozuvlari uchun)
  Intro/            CraDev intro: Art, Audio, Scripts/IntroSequence.cs
  CDCGroup/         CDCGroup splash: Art, Audio, Scripts/CdcGroupSplash.cs
  Loading/          Loading ekrani: Scripts/LoadingScreen.cs
  Editor/           CraDevSceneBuilder.cs (sahna quruvchi menyu), UiBuild.cs, CraDevArtImporter.cs
  Scenes/           Intro, CDCGroup, Loading (menyu orqali yaratiladi)
Design/
  CraDev/, CDCGroup/             logo manbalari va qayta yaratish skriptlari (build.sh)
  fonts/                         shriftlar va litsenziyalari
  tools/                         umumiy yordamchi skriptlar
```

## Logolarni o'zgartirish

Logolar `Design/CraDev/compose.html` va `Design/CDCGroup/compose.html` da (HTML/SVG) chizilgan.
O'zgartirgandan keyin shu papkadagi `build.sh` ni ishga tushiring. U PNG qatlamlarni va ovozni
qayta yaratadi (Node.js + playwright, Python 3 + numpy + pillow kerak). Keyin Unity'da
**CraDev → Sahnalarni yaratish** ni qayta bosing.

Shriftlar: Unbounded va Manrope, ikkalasi ham SIL Open Font License asosida
(`Design/fonts/OFL-*.txt`), o'yinda bepul ishlatish mumkin.
