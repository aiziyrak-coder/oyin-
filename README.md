# CraDev — o'yin loyihasi

Unity'da kompyuter uchun (Steam) yaratilayotgan 3D o'yin. O'yinni bosqichma-bosqich quryapmiz.

**Hozirgi bosqich — 2: o'yin boshidagi sahnalar.** O'yin ochilganda ketma-ket:

1. **CraDev intro**: kompaniya logosi va "A NEW ERA OF GAMING" yozuvi (animatsiya va ovoz bilan).
2. **CDCGroup**: ikkinchi brendning kumush logosi.
3. **Loading**: keyingi sahnani fonda yuklaydigan ekran (foiz, progress chizig'i, aylanuvchi yoylar).

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
6. **Play ▶** tugmasini bosing. Game oynasida 16:9 yoki 1920x1080 o'lchamini tanlang.

Loyihani avval 1-bosqichda ochgan bo'lsangiz ham, yangilangandan keyin shu menyuni bir marta
bosing: u sahnalarni yangi skriptlar bilan qayta yaratadi.

## Unity splash ekranini o'chirish

O'yin boshida "Made with Unity" chiqmasdan, darhol CraDev intro'si ko'rinishi uchun:
**Edit → Project Settings → Player → Splash Image → Show Splash Screen** belgisini olib tashlang.
Unity 6'da buni bepul (Personal) litsenziyada ham qilish mumkin.

## Sahnalar qanday ishlaydi

### 1. CraDev intro (6.6 s)

| Vaqt (s) | Nima bo'ladi |
|---|---|
| 0.0 – 0.8 | Qora ekrandan qorong'i fon ochiladi |
| 0.55 – 1.05 | Emblema paydo bo'ladi, 1.05 da ovozdagi zarba bilan nur chaqnaydi |
| 1.45 – 2.45 | "CraDev" yozuvi markazdan ikki tomonga ochiladi |
| 2.45 – 3.25 | Yozuv ustidan nur o'tadi (ovozda jiringlash) |
| 2.75 – 3.85 | Ajratuvchi chiziq va "A NEW ERA OF GAMING" |
| 5.6 – 6.6 | Qorong'ilashish, so'ng CDCGroup |

### 2. CDCGroup (4.8 s)

| Vaqt (s) | Nima bo'ladi |
|---|---|
| 0.0 – 0.6 | Qora ekrandan ochiladi |
| 0.35 – 1.30 | Kumush emblema (C ichida D, uning ichida C) aylanib joyiga tushadi, ovozda yumshoq zarba |
| 1.05 – 1.85 | "CDC GROUP" yozuvi chapdan o'ngga ochiladi |
| 1.95 – 2.70 | Butun logo ustidan kumush yaltirash o'tadi (metall "shiing", chapdan o'ngga) |
| 2.05 – 2.95 | Logo ostida ingichka chiziq cho'ziladi |
| 4.0 – 4.8 | Qorong'ilashish, so'ng Loading |

### 3. Loading

- Keyingi sahnani (`MainMenu`) fonda yuklaydi va foizni ko'rsatadi. Yuklash juda tez tugasa ham
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
  Common/Scripts/   SplashSequence.cs (splash asosi), SceneLoader.cs, UiParticles.cs, Anim.cs
  Common/Fonts/     Rajdhani (Loading yozuvlari uchun)
  Intro/            CraDev intro: Art, Audio, Scripts/IntroSequence.cs
  CDCGroup/         CDCGroup splash: Art, Audio, Scripts/CdcGroupSplash.cs
  Loading/          Loading ekrani: Art, Scripts/LoadingScreen.cs
  Editor/           CraDevSceneBuilder.cs (sahna quruvchi menyu), UiBuild.cs, CraDevArtImporter.cs
  Scenes/           Intro, CDCGroup, Loading (menyu orqali yaratiladi)
Design/
  CraDev/, CDCGroup/, Loading/   logo manbalari va qayta yaratish skriptlari (build.sh)
  fonts/                         shriftlar va litsenziyalari
  tools/                         umumiy yordamchi skriptlar
```

## Logolarni o'zgartirish

Logolar `Design/CraDev/compose.html` va `Design/CDCGroup/compose.html` da (HTML/SVG) chizilgan.
O'zgartirgandan keyin shu papkadagi `build.sh` ni ishga tushiring. U PNG qatlamlarni va ovozni
qayta yaratadi (Node.js + playwright, Python 3 + numpy + pillow kerak). Keyin Unity'da
**CraDev → Sahnalarni yaratish** ni qayta bosing.

Shriftlar: Orbitron, Rajdhani va Michroma, barchasi SIL Open Font License asosida
(`Design/fonts/OFL-*.txt`), o'yinda bepul ishlatish mumkin.
