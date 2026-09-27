# CraDev o'yini: Claude uchun yo'riqnoma

Unity 6'da kompyuter (Windows, Steam) uchun yaratilayotgan multiplayer **virtual dunyo** (3D). Kompaniya: **CraDev**,
ikkinchi brend: **CDCGroup**. G'oya: real hayotdagi joylarni (do'konlar, o'quv markazlari, biznes, ko'ngilochar zona,
hamjamiyat) bosqichma-bosqich virtualga ko'chirish. Bosh menyu - shu dunyoga kirish joyi; zonalarning ichi keyin quriladi.

## Muloqot va uslub

- Foydalanuvchi bilan **o'zbek tilida** gaplashing. Kod izohlari, README va commit xabarlari ham o'zbekcha.
- O'yin matnlari **ikki tilda**: o'zbekcha (standart) va inglizcha. Hamma matn `Common/Scripts/Loc.cs` jadvalida,
  builder yozuvlarga `Localized(text, kalit)` bilan `LocalizedText` ulaydi; skriptlar `Loc.T(kalit)` ishlatadi va
  `Loc.Changed` bo'lganda o'z matnlarini yangilaydi. Yangi matn qo'shilsa - ikkala tilni ham yozing.
- Dizayn (foydalanuvchi bergan konsept rasm bo'yicha, **aynan shunday**): quyoshli futuristik shahar ustida yarim
  shaffof "shisha" panellar, ko'k gradientli asosiy tugma, ikonkali menyu, yonib turuvchi platforma. Shriftlar:
  Unbounded (sarlavha), Manrope (matn). Qisqa silliq harakatlar. Kosmos/galaktika uslubi kerak emas.
  **Qo'lyozma/qiyshiq shior yozuvlari kerak emas** (foydalanuvchi qarori: "Same You New World", "Haqiqiy hayot endi
  virtualda", qiyshiq iqtibos olib tashlangan - "originallikni buzadi").
- Avatar yaratish ekrani hozircha to'q studiyada (eski uslub); keyin yangi uslubga moslash mumkin.
- O'yin haqiqiy o'yindek alohida to'liq ekranli oynada ochiladi. Ortiqcha UI (debug yozuvlar, tugmalar) qo'shmang.

## Asosiy qoida: sahnalar koddan quriladi

`Assets/CraDev/Scenes/*.unity` fayllari **qo'lda tahrirlanmaydi**. Ularni
`Assets/CraDev/Editor/CraDevSceneBuilder.cs` noldan yaratadi (menyu **CraDev > Sahnalarni yaratish**).
Sahnaga biror narsa qo'shish yoki joyini o'zgartirish kerak bo'lsa: builder'ni o'zgartiring, keyin sahnalarni
qayta yarating. Yordamchi funksiyalar `UiBuild.cs` da (`CreateSliced`, `PlaceTopLeft`, `CreateLabel`,
private `[SerializeField]` maydonlarga yozish uchun `Set`/`SetArray`).

## Unity'ni buyruq qatoridan boshqarish (Windows)

```
powershell -NoProfile -ExecutionPolicy Bypass -File tools/unity.ps1 <buyruq>
```

(yoki `tools\unity.cmd <buyruq>`). Buyruqlar:

| Buyruq | Nima qiladi |
|---|---|
| `where` | qaysi Unity.exe topilganini va loyiha versiyasini ko'rsatadi |
| `check` | loyihani ochib, skriptlarni kompilyatsiya qiladi; `error CS...` qatorlarini chiqaradi |
| `scenes` | barcha sahnalarni qayta yaratadi (`CraDevBatch.CreateScenes`) |
| `build` | sahnalar + o'yinni `Builds/StandaloneWindows64/CraDev.exe` ga yig'adi |
| `run` | `build` + o'yinni alohida oynada ishga tushiradi |
| `playerlog` | o'yinning `Player.log` faylining oxirini ko'rsatadi (runtime xatolari shu yerda) |
| `open` | loyihani Unity tahrirlovchisida ochadi |

- Batchmode loyiha Unity oynasida **ochiq bo'lmaganda** ishlaydi (aks holda kod 2). Kerak bo'lsa foydalanuvchidan
  Unity'ni yopishni so'rang.
- Birinchi ochilish (`Library` yaratish) 5-15 daqiqa oladi: buyruqni uzun timeout bilan yoki fonda ishga tushiring.
- To'liq loglar: `Logs/batch-<buyruq>.log`. Chiqish kodi: 0 OK, 1 Unity xatosi, 2 Unity topilmadi yoki loyiha ochiq.
- Unity boshqa joyda o'rnatilgan bo'lsa: `UNITY_EXE` muhit o'zgaruvchisi yoki `-Unity <yo'l>`.
- Foydalanuvchi kompyuterida **Unity 6000.3.24f1 LTS** (Unity Hub orqali) o'rnatilgan; `ProjectVersion.txt` ham shu.
  Kompyuterda boshqa Unity 6 versiyasi bo'lsa, skript o'shani tanlaydi va Unity loyihani unga moslaydi: o'zgargan `ProjectVersion.txt`, Unity yaratgan
  `Packages/manifest.json`, `packages-lock.json` va `ProjectSettings/*.asset` fayllarini commit qiling.
  uGUI (`com.unity.ugui`) paketi kerak: u standart paketlar ichida bo'ladi.

## GitHub CI

`.github/workflows/ci.yml` har push'da: server testlari va `CraDevServer.exe` (`tools/ci/server-exe.sh`, Node SEA) → GameCI (`game-ci/unity-builder@v5`) bilan Unity'da
Windows va Linux build (`CraDevBatch.BuildGame`, sahnalar ham shu yerda yaratiladi) → Linux build'ni Xvfb'da
server bilan ishga tushirib, o'yinchi kabi o'tish va OCR bilan tekshirish (`tools/ci/smoke.sh`).

- Unity job'lari `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` secret'larisiz o'tkazib yuboriladi (README'da yo'riqnoma).
  Ularni hech qachon chatda so'ramang: foydalanuvchi o'zi GitHub Settings'ga kiritadi.
- Bulutdagi Claude sessiyasi CI loglarini GitHub MCP (`get_job_logs`) orqali o'qiydi. Artifact'larni (video, rasm)
  u yuklab ololmaydi, shuning uchun `smoke.sh` asosiy natijalarni (OCR matni, Player.log xatolari, PASS/FAIL) logga ham yozadi.
- Windows artifact'ida o'yin yonida `CraDevServer.exe` va `Play.cmd` (`tools/ci/Play.cmd`: server + o'yin) bo'ladi.
- `smoke.sh` dagi sichqoncha koordinatalari `BuildCharacterCreation` joylashuvidan olingan (1920x1080):
  forma o'zgarsa, ularni ham yangilang.

## O'yin oqimi

`Intro` (CraDev, 4.2 s) → `CDCGroup` (3.5 s) → `Loading` → yangi o'yinchi uchun `CharacterCreation`,
qaytgan o'yinchi uchun `MainMenu` (virtual dunyoga kirish joyi): menyu: Kirish (hali "tez orada"),
Personajni sozlash (`CharacterCreation` tahrirlash rejimida), Sozlamalar, Yordam, Chiqish; pastda zona kartalari
(bosilsa rasmdagi o'sha bino ustida belgi chiqadi). Bosh menyu profilni serverda tekshiradi (`GET /api/players/me`).
Profil PlayerPrefs'da (`PlayerProfile.cs`); uni o'chirish: menyu **CraDev > Test: saqlangan profilni o'chirish**.

**Loading ekrani faqat og'ir yuklashda** (o'yin ochilishi, keyin dunyoga kirish): `SceneLoader.Load`. Menyu ↔ personaj
sozlash kabi kichik o'tishlar ekran qorong'ilashib to'g'ridan-to'g'ri: `SceneLoader.Switch` (foydalanuvchi talabi).

Bosh menyu = **foydalanuvchining konsept rasmi** (`CraDevSceneBuilder.Lobby.cs`): fon - rasmning o'zi (yozuvlari va
qahramoni LaMa bilan o'chirilgan, Real-ESRGAN bilan 2560x1440 ga kattalashtirilgan: `MainMenu/Background/`, kartalar
rasmi `MainMenu/Zones/`); uning ustida rasmdagi platformada o'yinchining 3D qahramoni (kamera `SolveLobbyCamera` bilan
rasmdagi qahramon va platformaga sonli moslanadi, `LobbyCamera` ekran nisbatiga FOV'ni to'g'rilaydi). Hamma
interfeys elementining joyi, o'lchami, rangi `LobbyLayout.cs` da (rasm o'lchovlari, 1280x720 → UI x1.5). Nom yorlig'i
qahramon boshi ustida (`MainMenuScreen.PlaceNameplate`).

Skrinshot (fokusni olmaydi, foydalanuvchi ishiga xalaqit bermaydi):
`CraDev.exe -cradevShot <png> [-cradevScene MainMenu] [-cradevDelay 3] [-cradevPress <tugma nomi>] [-cradevQuit]`
(`Common/Scripts/DevCapture.cs`; sahna almashishlari `Player.log` ga yoziladi).

| Papka | Mazmuni |
|---|---|
| `Assets/CraDev/Common/` | `SplashSequence` (splash asosi), `SceneLoader`, `Anim` (easing, input, Esc), `GameSettings` (ekran, grafika, V-Sync, ovoz; o'yin ochilishi bilan qo'llanadi), `ModalWindow`/`ConfirmDialog`/`SettingsPanel` (oynalar), `UiSounds` (tugma ovozlari) |
| `Assets/CraDev/MainMenu/` | `MainMenuScreen`: bosh menyu (til, bildirishnoma, profil, zona kartalari, bosh ustidagi nom yorlig'i), `LobbyCamera`, `ShadowCatcher` shader (qahramon soyasi fon rasmiga), `Background/` va `Zones/` rasmlari |
| `Assets/CraDev/Intro/`, `CDCGroup/`, `Loading/` | splash va yuklash ekranlari |
| `Assets/CraDev/CharacterCreation/` | nickname + avatar tanlash ekrani: 3D studiyada qahramon turadi; `AvatarViewer` uni sichqoncha bilan aylantiradi, g'ildirakcha bilan yuziga yaqinlashtiradi |
| `Assets/CraDev/Face/` | o'yinchi yuzi: `FaceTracker` (MediaPipe BlazeFace + Face Mesh, 478 nuqta, `com.unity.ai.inference`), `FacePainter` (yuzni bosh teksturasiga chizadi), `FacePhoto` (fayl oynasi, EXIF, kamera kadri), `FaceStore` (yuz faqat shu kompyuterda saqlanadi) |
| `Assets/CraDev/Online/` | `GameApi` (server), `NicknameRules` (server bilan bir xil qoidalar), `PlayerProfile`, `RegistrationSession` |
| `Assets/CraDev/Avatars/Models/<ID>/` | 3D avatarlar: Microsoft Rocketbox (MIT, `LICENSE-Rocketbox.md`), `.fbx` + `Textures/` (.tga lar .jpg/.png ga siqilgan) |
| `Assets/CraDev/Avatars/Animations/` | idle animatsiyalar (Rocketbox, Generic `Bip01` skelet) va ularning Animator Controller'lari |
| `Assets/CraDev/Avatars/Cards/` | kartadagi rasmlar: builder 3D modeldan chizadi (qo'lda tahrirlanmaydi) |
| `Assets/CraDev/Avatars/FaceMaps.json` | yuz xaritalari: har avatar uchun 478 nuqtaning bosh teksturasidagi o'rni + uchburchaklar. Builder hisoblaydi (tekshiruv rasmlari `Logs/FaceMaps/`); grafikasiz CI shu fayldan o'qiydi |
| `Assets/CraDev/Avatars/Photos/` | eski 2D avatar rasmlari: endi ishlatilmaydi |
| `Assets/CraDev/Editor/` | `CraDevSceneBuilder` (sahnalar), `CraDevBatch` (batchmode), `CraDevArtImporter` (rasm import sozlamalari), `CraDevModelImporter` (Rocketbox modellari: material, LOD, animatsiya), `UiBuild` |
| `Server/` | Node.js o'yin serveri: nickname noyobligi, o'yinchi profillari |
| `Design/` | logolar, ikonkalar, avatar promptlari va ularni PNG/WAV ga aylantiruvchi skriptlar |

## Kod qoidalari

- UI: uGUI, Canvas Screen Space - Overlay, CanvasScaler 1920x1080 (Expand).
- 3D: Built-in render pipeline, Standard shader, Linear rang fazosi. Materiallar, pol, chiroqlar va taglik builder'da
  (`BuildStage`) yaratiladi; Rocketbox materiallari `CraDevModelImporter` da material nomidan (`m024_body` →
  `m024_body_color/normal`) quriladi. Import qoidasi o'zgarsa `GetVersion()` ni oshiring.
- Rasmlar 2x o'lchamda chizilgan (`UiBuild.ArtScale`). `CreateImage` `raycastTarget = false` qiladi:
  bosiladigan elementlarda uni `true` qiling.
- Input: eski Input Manager ham, yangi Input System ham ishlashi shart (`#if ENABLE_INPUT_SYSTEM` /
  `ENABLE_LEGACY_INPUT_MANAGER`, `Anim.AnyInputPressed`).
- Inspector'da sozlanadigan qiymatlar private `[SerializeField]`, builder ularni `UiBuild.Set` bilan yozadi.
- Yangi sahna qo'shilsa: builder'ga `Build...()` metodi va `AllScenes` ro'yxatiga qo'shing.

## Server

```
cd Server
npm start      # http://localhost:8080 (Node.js 22.13+, tashqi paketlar yo'q, SQLite: node:sqlite)
npm test
```

`CharacterCreation` nickname'ni shu serverda tekshiradi (manzil: `CharacterCreationDirector → Server Url`).
Server ishlamasa ekran "offline" holatini ko'rsatadi va qayta urinadi.

## Rasm va ovozlar

Hamma tayyor PNG/WAV'lar `Assets/` ga commit qilingan; qayta yaratish faqat manba o'zgarganda kerak:

- Logolar: `Design/CraDev|CDCGroup/build.sh` (Node.js + playwright, Python 3 + numpy + pillow).
- Ikonkalar: `Design/UI/build.sh`.
- 3D avatarlar: github.com/microsoft/Microsoft-Rocketbox `Assets/Avatars/Adults/<Nomi>/` dan `Export/<Nomi>.fbx` va
  `Textures/*.tga` (specular kerak emas) olinadi, teksturalar .jpg (rang, normal) va .png (`*_opacity_color`, alfa bilan)
  ga aylantiriladi. Avatar almashtirilsa: `CraDevSceneBuilder.AvatarList` dagi model nomi, keyin `scenes`.
- Eski 2D avatar rasmlari (`Design/Characters/`, `Avatars/Photos/`) endi ishlatilmaydi.

## Holat va keyingi qadamlar

1. Loyiha foydalanuvchi kompyuterida Unity 6000.3.24f1 bilan tekshirildi: `check` → `scenes` → `run` xatosiz,
   o'yin Intro → CDCGroup → Loading → CharacterCreation gacha to'liq ekranda ishlaydi (server bilan).
   Unity yaratgan `manifest.json` da uGUI yo'q edi: `com.unity.ugui` qo'lda qo'shildi. `.meta`, `Packages/`,
   `ProjectSettings/` va sahnalar commit qilingan.
2. Foydalanuvchi talabi: o'yin **sayt emas, haqiqiy o'yindek** ko'rinishi kerak. Shuning uchun avatar yaratish ekrani
   3D: studiyada realistik Rocketbox qahramoni turadi (idle animatsiya, aylantirish, yaqinlashtirish), forma ustida.
3. Avatarlar: erkaklar 4 ta (M1, M2, M3, M5), ayollar 5 ta (F1–F5). **M4 kerak emas** (foydalanuvchi qarori);
   kodlar o'zgarmaydi. Ro'yxat ikki joyda: `CraDevSceneBuilder.AvatarList` va `Server/src/nickname.js` (`AVATARS`).
4. Yuz: formadagi FACE bo'limi (Take photo - kamera oynasi, Upload photo - Windows fayl oynasi). Yuz 3D qahramon yuziga
   "teri" sifatida chiziladi (bosh shakli modelniki), rangi qahramon terisiga 70% moslashadi. Yuz serverga yuborilmaydi.
   Tekshirish: `Unity.exe -batchmode -quit -projectPath . -executeMethod CraDev.EditorTools.CraDevFaceTest.Run -facePhoto <rasm>`.
   Keyingi: yuzni serverga yuklash (multiplayer'da boshqalar ko'rishi), 3D yuz shakli.
5. O'yin turi: virtual dunyo (metaverse). Zonalar: Magazinlar, O'quv markazlari, Biznes markazi, Ko'ngilochar zona, Hamjamiyat
   (`CraDevSceneBuilder.LobbyZonesList`). Ularning ichi keyin quriladi. Dunyo ichida kamera **birinchi shaxs** bo'ladi.
   Avval qurilgan to'liq 3D shahar foydalanuvchiga yoqmadi ("umuman o'xshamabdi") va o'chirildi: bosh menyu konsept
   rasmning o'zidan quriladi (yuqorida).
6. Foydalanuvchi keyinga qoldirgan: pasport bilan ro'yxatdan o'tish (jins shundan olinadi, hozir
   `testGender`), yuzni serverga yuklash, Play'dan keyingi o'yin dunyosi, musiqa.
7. `tools/unity.ps1 run` o'yin serverini ham o'zi yoqadi (ishlamayotgan bo'lsa). Har bir sahnada Esc ishlaydi:
   ochiq oyna yopiladi, bosh menyu va yangi o'yinchida "Quit game?", tahrirlashda menyuga qaytish.

Git: ish `claude/gallant-dijkstra-r0opib` branch'ida. `Library/`, `Temp/`, `Logs/`, `Builds/` commit qilinmaydi.
