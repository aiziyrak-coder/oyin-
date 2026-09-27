# Ishni boshqa AI yordamchida davom ettirish (Lynxos lobby v2)

## Yangilangan holat — 2026-09-27, Codex

**Quyidagi eski handoffdan farqli ravishda loyiha endi kompilyatsiya bo'ladi va Lobby V2 buildi ishlaydi.**
Eski StandaloneWindows64 buildini ochmang: yangisi `Builds/LobbyV2/CraDev.exe`.
`Play-LobbyV2.cmd` server bilan birga ochadi. `powershell -File tools/lobby.ps1 -Build` yangi build yaratadi.

- Yangi builder: `Assets/CraDev/Editor/CraDevSceneBuilder.LobbyV2.cs`. 8 asosiy sahifa + Top-lar + Ko'ngilochar zona.
- Sahifa funksiyalari: `MainMenu/Scripts/LobbyContent*.cs`, tugmalar: `LobbyCommand.cs`.
- Home/world/wardrobe/shops/business fonlari tozalangan; education/friends hero rasmlarida matn va UI yo'q.
- Garderob kartalari ishlayotgan avatardan 3D chiziladi; rang, mato, soch, uslub, saqlangan obrazlar va saqlamasdan chiqish dialogi bor.
- Kiyim faqat server tasdiqlagach saqlangan hisoblanadi; xato bo'lsa draft qoladi.
- Do'st qidirish, so'rov/qabul qilish/o'chirish, tadbirlar, reyting, profil va sozlamalar API'ga ulangan.
- Serverning eski jarayoni yangilandi. Migratsiya oldi SQLite zaxirasi: `Logs/cradev-before-lobby-v2-20260927.db`.
- Tekshiruv: server 56/56; `-cradevAllPages -cradevUiSmoke` sahifalar, raycast, navigatsiya, kategoriyalar, 3D kartalar, preview va discard'ni sinaydi.
- Kadrlar: `Logs/LobbyV2/`; build log: `Logs/lobby-v2-build.log`; runtime log: `Logs/lobby-v2-player.log`.
- Grafik holat xatosi tuzatildi: FacePainter va OutfitPainter RenderTexture.active ni avvalgi holatiga qaytaradi.

Oldindan keyinga qoldirilganlar hali tayyor deb ko'rsatilmaydi: dunyo/interyerlar, chat/guruhlar, email/parol va aksessuar modellari.
`Design/LobbyV2` manba fayllarini o'chirmang va Git'ga qo'shmang. Kerakli runtime rasmlar `Assets/CraDev/MainMenu/Pages` da.
Hozirgi qisqa checklist: `USTA.md`. Quyida Claude Code'dan olingan boshlang'ich handoff tarix uchun saqlangan.

Sana: 2026-09-27. Branch: `claude/gallant-dijkstra-r0opib`. Oxirgi commit: `34a48b0` (ishlaydigan eski bosh menyu).
GitHub'ga push qilinmagan 6 ta commit bor.

## 1. Hozirgi holat — ENG MUHIM

Kod **yarim yo'lda to'xtatilgan: loyiha hozir kompilyatsiya bo'lmaydi.** Sababi: eski bosh menyu builderi
`Assets/CraDev/Editor/CraDevSceneBuilder.Lobby.cs` eski `LobbyLayout` API'ga tayanadi, `LobbyLayout.cs` esa yangi formatda
qayta yaratilgan. Yangi lobby builderi hali yozilmagan.

Ishlaydigan o'yinni qaytarish kerak bo'lsa (yangi ishlar yo'qolmaydi):

```
git stash -u        # yangi ishni vaqtincha chetga olish
git stash pop       # qaytarish
```

## 2. Foydalanuvchi talabi (nima qilinishi kerak)

`Design/LobbyV2/concept_pages.webp` rasmidagi 8 sahifani **aynan shunday** qilish (Call of Duty / PUBG uslubi,
yuqorida navigatsiya, chapda ustun menyu EMAS):
Bosh sahifa, Dunyo xaritasi, Garderob, Magazinlar, O'quv markazlari, Biznes markazi, Do'stlar va muloqot, Sozlamalar
(+ Top-lar va Ko'ngilochar zona — rasmi yo'q, shu uslubda). Dunyo nomi: **Lynxos**. Hammasi "haqiqiy" ishlashi kerak.
Qo'shimcha qoidalar: qiyshiq/qo'lyozma shior yozuvlari yo'q; o'ngdagi globus yo'q; Loading ekrani faqat o'yin
ochilishida; real brend logotiplari (Zara, Nike...) ishlatilmaydi — o'ylab topilgan brendlar.
Loyiha qoidalari: `CLAUDE.md` (o'zbekcha, sahnalar faqat builder kodi orqali quriladi, ikki til — Loc.cs).

## 3. Tayyor qismlar

**Server** (`Server/`, `npm test` — 56/56 o'tadi): profil maydonlari (publicId, outfit, country, showOnline,
allowRequests), `POST /api/presence`, `GET /api/stats`, qidiruv, tavsiyalar, do'stlar (request/accept/remove),
`GET /api/leaderboard`, `GET /api/events` (`src/events.js`). API ro'yxati `src/app.js` boshida.
Eslatma: 8080 portda eski server ishlab turgan bo'lishi mumkin — qayta ishga tushiring.

**Garderob (ishlaydi, sinalgan):**
- `CraDevSceneBuilder.Outfit.cs` — har avatar uchun kiyim niqoblari (`Textures/*_outfit.png`, `*_hair.png`,
  `Avatars/OutfitMaps.json`). Sinov: `Unity.exe -batchmode -quit -projectPath . -executeMethod CraDev.EditorTools.CraDevBatch.BakeOutfits`
  → `Logs/Outfit/test_*.png`.
- `Wardrobe/Shaders/OutfitRecolor.shader`, `Wardrobe/Scripts/Outfit.cs`, `WardrobeCatalog.cs` (buyumlar, ranglar,
  obrazlar), `OutfitPainter.cs`; `AvatarViewer.SetOutfit(...)` ulangan.

**Klient kodi (yozilgan, hali kompilyatsiyada sinalmagan):**
- `Online/Scripts/GameApi.cs` (yangi server API), `PlayerProfile.cs`, `Presence.cs`.
- `MainMenu/Scripts/MainMenuScreen.cs` (sahifalar rejissyori), `LobbyPage.cs`, `LobbyStage.cs` (kamera/yorug'lik
  holatlari), `LobbyCamera.cs`, `LobbyPrefs.cs`; `Common/Scripts/SelectList.cs`, `SwitchToggle.cs`;
  `MainMenu/Shaders/ShadowCatcher.shader` (soya + kontakt soya).
- `Common/Scripts/Loc.cs` — BARCHA sahifalar matnlari ikki tilda tayyor (nav.*, home.*, world.*, wardrobe.*, shops.*,
  education.*, business.*, fun.*, friends.*, top.*, settings.*, country.*).
- `Editor/CraDevSceneBuilder.Icons.cs` — ikonkalarni `Editor/LobbyIcons.json` dan chizadi (JSON hali yo'q, pastga qarang).
- `Editor/LobbyLayout.cs` — 8 sahifaning o'lchovlari (generator: `Design/LobbyV2/gen_layout2.py`).

**Dizayn materiallari** — `Design/LobbyV2/`:
- `pages/<sahifa>.png` — sahifalar kesimi; `spec/<sahifa>.verified.json` — har elementning joyi, rangi, shrifti
  (wardrobe hali tekshirilmagan: `spec/wardrobe.json`).
- `tools.py` — LaMa bilan UI'ni o'chirish va ESRGAN bilan kattalashtirish (modellar: `lama.onnx`, `esrgan_x4v3.onnx` —
  `%TEMP%\claude\D--Game1\4f1a5cc1-...\scratchpad\ref\` da; yo'qolgan bo'lsa huggingface.co/opencv/inpainting_lama).
- `icons/part_*.json` + `icons/icon_render.py` — ikonkalar (4 guruh tayyor, `misc` va birlashtirish qolgan).
- `out/` — fonlarni tozalash chala qolgan (tayyor `*_4x.png` fonlar YO'Q).

## 4. Qolgan ishlar (tartib bilan)

1. Ikonkalar: `icons/part_*.json` ni birlashtirib `misc` guruhini qo'shish → `Assets/CraDev/Editor/LobbyIcons.json`.
2. Fonlar: har sahifa uchun UI va qahramonni o'chirib (`tools.py mask/inpaint/upscale`, `spec/*.json` dagi
   `remove:true`), `Assets/CraDev/MainMenu/Pages/` ga qo'yish. Umumiy to'q fon (Sozlamalar/Do'stlar/Ta'lim uchun).
3. **Yangi lobby builderi** (`CraDevSceneBuilder.Lobby.cs` ni to'liq qayta yozish): fon kamerasi + 2 ta RawImage
   (almashish), 3D sahna (`LobbyStage`, Bosh sahifa va Garderob uchun kamera holati — `LobbyLayout.Home.CharacterX/HeadY/FeetY/HorizonY`
   bo'yicha hisoblanadi), umumiy navigatsiya (Home o'lchovlari bo'yicha), profil menyusi, toast, dialog,
   har sahifa alohida partial faylda. Joylashuv: `s = min(1920/W, 1080/H)`, element `anchor` bo'yicha chetga bog'lanadi.
4. Sahifa skriptlari: HomePage, WorldMapPage (filtr, qidiruv, zoom, onlayn soni), WardrobePage (+ runtime rasmchalar),
   ZonePage (magazin/ta'lim/biznes/ko'ngilochar), FriendsPage, LeaderboardPage, SettingsPage.
5. `CharacterCreationScreen` tahrirlashda `viewer.SetOutfit(Outfit.FromJson(PlayerProfile.Outfit))`.
6. Eski `SettingsPanel`/`BuildSettingsPanel` kerak bo'lmasa o'chirish; `tools/ci/smoke.sh` ni yangilash.
7. `tools/unity.ps1 build`, har sahifa skrinshoti:
   `CraDev.exe -cradevShot out.png -cradevDelay 3 [-cradevPress <tugma nomi>] -cradevQuit`, konsept bilan solishtirish.
8. `CLAUDE.md` ni yangilash, commit (push faqat foydalanuvchi so'rasa).

## 5. Boshqa AI'ga beriladigan tayyor matn (nusxa oling)

> Men Unity 6000.3.24f1 loyihasida ishlayapman: `D:\Game1\CraDev` (branch `claude/gallant-dijkstra-r0opib`).
> Avval `CLAUDE.md` va `HANDOFF.md` ni to'liq o'qing — ularda loyiha qoidalari, hozirgi holat va qolgan ishlar bor.
> Loyiha hozir kompilyatsiya bo'lmaydi: bosh menyu builderi yangi dizaynga qayta yozilayotgan edi.
> Vazifa: `Design/LobbyV2/concept_pages.webp` dagi 8 sahifali lobby'ni aynan rasmdagidek qurib tugatish,
> `HANDOFF.md` 4-bo'limdagi tartibda. Sahnalarni qo'lda tahrirlamang — faqat `Assets/CraDev/Editor/` dagi builder
> kodi orqali. Har qadamdan keyin `powershell -NoProfile -ExecutionPolicy Bypass -File tools/unity.ps1 check`
> bilan kompilyatsiyani tekshiring. Men bilan o'zbek tilida gaplashing.
