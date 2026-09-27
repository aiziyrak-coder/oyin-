# Ishni boshqa AI yordamchida davom ettirish (NewWorld lobby)

## SO‘NGGI: FULL-HEIGHT COLLAPSIBLE FRIENDS

Left friends panel now stretches from top offset124 to bottom24 at preserved 65% scale. Footer bottom anchored, scroll viewport expands. LobbyFriendsDrawer slide toggle (‹/›, 0.22sec, no raycasts when hidden), social actions reopen it. Backup90eeed4; logs friends-drawer-build/player, screenshots Logs/FriendsDrawer.
SEATING CLARIFICATION RESOLVED: host + 4 invited friends = FIVE total. Center THREE-seat sofa (host center), TWO single armchairs at sides. Above each: nickname, online, three-bar connection-quality icon green/yellow/red based on measured ping. Seating, party invites and nameplates are still TODO; old pending-count clarification below is superseded. Do not ask chair count again.


## ENG YANGI TALAB: 35% IXCHAM + O‘TIRADIGAN PARTY LOBBY

35% shrink implemented in builder: UI CanvasScaler reference size /0.65, right action stack bottom anchored, settings centered, avatar camera framing 65%. Backup 78d7626. User also requests seated multiplayer lobby with natural idle motion. Seating/invites are NOT implemented yet. Async clarification pending: one two-person sofa + two armchairs (4 seats), or five two-person sofas + two armchairs (12 seats)? Current assets have only standing generic Rocketbox idle animations, no seat furniture; server currently has friends but no party/invite routes. Continue after resolving chair count; preserve real profiles, backup DB before changes, test invite permissions/capacity/leave/disconnect and multiple clients. No fake friends accepted by tests. Shrink logs compact-lobby-build.log / compact-lobby-player.log and Logs/CompactLobby/.


## CURRENT VISUAL CORRECTION

User rejected brown flat cards / flat mint-gold CTA. ReferenceSurface shader + component replaces those with charcoal gradients and thin rims, landscape promo and emerald/gold play CTA with separate orb. Shader authored colors are sRGB and explicitly converted for Linear renderer. Do not remove that conversion (otherwise cards become light grey). Landscape art in Pages/reference-world.png, prompt Design/LobbyV2/ReferenceSurface/PROMPTS.md, built-in mode. Backup 3bd3b32. Dynamic clock/weather and all archived pages remain intact. No fake friend records or level. Logs reference-surface-build-final.log / reference-surface-player-final.log; captures ReferenceSurfaceFinal. -cradevReferencePreview temporarily displays original sunset for comparison and restores real clock after capture.


## ENG YANGI: DYNAMIC TIME / WEATHER

LobbyEnvironment mahalliy qurilma soatidan 6 kun qismi tanlaydi; soatni o'zgartirmaydi. Sozlamalar ovoz/grafika bo'limida weather selector va motion toggle bor, PlayerPrefs'da saqlanadi. User explicitly chose manual weather selection. Clear=clock, cloudy/rain/snow=manual override. 9 image files: Assets/CraDev/MainMenu/Resources/LobbyTimes; prompt manifest Design/LobbyV2/TimeWeather/PROMPTS.md. Shader CraDev/LobbyAtmosphere animates sky/water/weather without GrabPass. Async Resources loading + 2s crossfade releases old image. Backup commit fa9c4d1. Run -cradevEnvironmentTest with -cradevShot for all 9 screenshots and clock tests. Existing -cradevSingleLobbySmoke remains available. All archived pages preserved, world gameplay still unconnected.


## OXIRGI USTUVOR DIZAYN: YASHIL–OLTIN REFERENCE

Foydalanuvchi oxirgi reference rasmga o'tishni so'radi: ko'k rangdan voz kechildi, iliq sunset penthouse, chap inset friends panel, o'ngda NewWorld sarlavha/promo/uch karta/gradient kirish. Quyidagi eski ko'k va full-height qarorlar tarixiy. Saqlash nuqtasi e361a80. Yangi fon Assets/CraDev/MainMenu/Pages/sunset-home.png; prompt Design/LobbyV2/ReferenceGradient/PROMPTS.md. All ten original pages remain, archived routes blocked. Gameplay remains unconnected. Tests: Logs/lobby-gradient-player-final.log; screenshots: Logs/LobbyGradientFinal/. Do not fabricate reference friends, online counts or level.


## ENG YANGI QAROR: BITTA O'YIN LOBBYSI

Oxirgi bezak tuzatishi: o'ng bloklar bitta 384 px kenglikdagi ustunda, profil/tadbir orasida 16 px, pastki kirish tugmasi 64 px chet masofasida. Ko'k rang xira slate emas, LobbyPalette.Accent = #2463EB; tugmalar/tanlangan holatlar/switchlarda bir xil. Chapdagi full-height panel va fonlar o'zgarmagan. Saqlash nuqtasi db71578; tekshiruv Logs/lobby-blue-player.log: 57 checks, 0 failures. Skrinshotlar Logs/LobbyBlue/.

Foydalanuvchi ko'p sahifali sayt ko'rinishini bekor qildi. Faqat asosiy lobby va uning ustida ochiladigan sozlamalar qoladi. Header, nav va pastki sahifa doci olib tashlandi. Chapda bir panelda do'stlar/onlayn/qidirish/so'rov/o'chirish; o'ngda profil va boshqa zarur boshqaruvlar; o'ng pastda NewWorldga kirish tugmasi.
**Hech bir eski sahifani o'chirmang yoki avtomatik qaytarmang.** Garderob ham vaqtincha yashirilgan. Foydalanuvchi keyin ularni qayerga joylashni aytadi. Barcha tafsilot va tiklash nuqtalari: `LOBBY_ARCHIVE.md`. Oldingi holat commit: e45788f.
Yangi builder: `CraDevSceneBuilder.GameLobby.cs`; chap panel: `LobbyFriendsPanel.cs`; test: `-cradevSingleLobbySmoke`. Kirish uchun haqiqiy gameplay sahnasi hali mavjud emas; tugma buni aniq bildiradi, eski xaritaga olib o'tmaydi.
Tekshirildi: `Logs/single-lobby-final-build.log` Succeeded; `Logs/single-lobby-final-player.log` 53 checks, 0 failures, runtime exception yo'q. `Logs/SingleLobbyFinal/` home-final.png, settings.png, notifications.png, friends-search.png. O'yin asosiy lobbyda ochiq qoldirildi. Tungi rasmlar o'zgarmagan. Do'stlar panelidagi eski Lynxos yozuvi haqiqiy o'yinchi nomi, platforma brendi emas.

## Hozirgi talab va holat — NewWorld, 2026-09-28

Foydalanuvchi Liquid Glass uslubini RAD ETDI. Tungi fonlarni yoqtirdi: `Pages/Night/` fayllarini o'zgartirmang. Platforma nomi **NewWorld**; Lynxos haqiqiy o'yinchi nickname'i bo'lishi mumkin, profil/DB nomlarini almashtirmang. Quyidagi Liquid Glass bo'limlari faqat tarix.

Hozirgi UI: tekis, 8 px radiusli standart UI Image panellari; 52–56 px asosiy tugmalar, Manrope Medium/SemiBold, kichikroq sarlavhalar va yozuvlar. Hech bir faol panelda GlassSurface yoki GrabPass materiali yo'q. Eski shader va material aktiv emas; `FlatDialog` standart UI orqali quriladi. Runtime product/company identifikatorlari profil saqlash manzilini buzmaslik uchun o'zgartirilmagan.

Qotish tuzatishlari: garderob kartalari CPU ReadPixels bilan birdaniga chizilmaydi. 192x192 RenderTexture, alohida 512 px yordamchi kiyim teksturalari, kadriga bittadan navbat va avatar/buyum keshi ishlaydi. Asosiy avatar sifati pasaymagan; yuz/kiyim materiallari previewdan keyin aynan tiklanadi. O'zgarmagan outfit qayta bo'yalmaydi; oddiy kiyim tanlash yuzni qayta hisoblamaydi. Statik sahifalar qayta ochilganda UI saqlanadi. Tez bosib qaytishda pending sahifa bekor qilinadi.

Sinovlar: `Logs/lobby-flat-final-build.log` Succeeded; `Logs/lobby-flat-final-player.log` UI 39/39, 9 avatar x 4 slot preview tekshiruvi muvaffaqiyatli, profil o'zgarmagan. Birinchi garderob transition worst frame: oldin 82.99 ms (`lobby-baseline-player.log`), keyin 22.06 ms; qayta ochish 10.85 ms. Bu shu qurilmadagi 1920x1080 qisqa o'lchov, universal FPS kafolati emas.

Yakuniy kosmetik tartib (ko'ngilochar kartalarda takroriy matn o'rniga ikonka, ta'lim kartasi cheti) `Logs/newworld-build.log` va `Logs/newworld-player.log` bilan qayta tekshiriladi. Skrinshotlar `Logs/NewWorld/`. Test bayroqlari: `-cradevTransitionTest -cradevAllPages -cradevUiSmoke -cradevAvatarPreviews -cradevUiDetails`. Avatar testi faqat ko'rinadigan obyektlarni vaqtincha almashtiradi, profil/serverga yozmaydi va avvalgi avatarni qaytaradi.

O'yin: `Play-LobbyV2.cmd` yoki `Builds/LobbyV2/CraDev.exe`. Saqlash nuqtasi: **2b9a3d8** (tasdiqlangan tungi fonlar, eski shisha UI). Design/LobbyV2 Git'ga qo'shilmagan, push qilinmagan.

## Eng so'nggi dizayn — 2026-09-28

Foydalanuvchi eski yorqin dizaynni rad etdi, tungi shahar/interyerlar va butun lobby uchun iOS 27 Liquid Glass yo'nalishini tanladi. Hozir 10 sahifada yangi umumiy shisha dizayn ishlaydi. `GlassSurface.cs` vertexlarda o'lcham, radius va hover holatini uzatadi; `LobbyGlass.shader` bitta named GrabPass bilan fonni yumshatib, mayin shisha chetlarini chizadi. Material `MainMenu/Resources/LobbyGlass.mat`da.
Yangi fonlar `Assets/CraDev/MainMenu/Pages/Night/` ichida; V2Texture avval shu papkani tanlaydi. Eski yorqin fayllarni qayta ulab yubormang. Fonlar yangidan yaratilgan, shunchaki rang filtri emas. O'lchamlari 4K emas, bu foydalanuvchiga aytilgan. Promptlar `Design/LobbyV2/Night/PROMPTS.md`.
Sahnalar avvalgidek builderdan yaratiladi. `CraDevSceneBuilder.Glass.cs` material va dialogni quradi. Saqlash nuqtasi: 8671410. Build/log: `Logs/lobby-night-build.log`; skrinshotlar `Logs/LobbyNight/`; runtime log `Logs/lobby-night-player.log`.
Yakuniy tekshiruv: build Succeeded; UI 35/35; 12 ta skrinshot (10 sahifa, switchlar, dialog); shader xatosi/runtime exception yo'q. `-cradevGlassDetails` qo'shimcha skrinshot va qisqa FPS namunasini beradi; oxirida home sahifaga qaytadi. O'yin ochiq qoldirilgan.

## Yangilangan holat — 2026-09-27, Codex

Fon sifati bo'yicha keyingi yangilanish: 7 ta fon/hero va o'quv kartalari yangilangan. Bino fonlaridagi brend yozuvlari/logolari olib tashlangan, katalogda sun'iy brendlar emas umumiy mahsulot turlari bor. O'yinning o'z Lynxos nomi navigatsiyada saqlangan.
Bu native 4K emas: ko'p fonlar 1672x941, education hero 2060x763, kartalar atlasi 1586x992. Unity ularni siqilmagan RGBA32, asl o'lchamda yuklaydi. Aniq 4K uchun API/CLI roziligi hali olinmagan.
Yangi tekshiruv: build Succeeded, UI 33/33. Skrinshotlar `Logs/LobbyArtRefresh/`; loglar `Logs/lobby-art-refresh-build.log`, `Logs/lobby-art-refresh-player.log`. Prompt va fayl ro'yxati `Design/LobbyV2/quality-preview/ART-REFRESH.md`.

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
