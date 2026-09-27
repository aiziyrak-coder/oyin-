# CraDev o'yini: Claude uchun yo'riqnoma

Unity 6'da kompyuter (Windows, Steam) uchun yaratilayotgan multiplayer 3D o'yin. Kompaniya: **CraDev**,
ikkinchi brend: **CDCGroup**. O'yin konsepsiyasi hali yo'q: foydalanuvchi bilan bosqichma-bosqich quryapmiz.

## Muloqot va uslub

- Foydalanuvchi bilan **o'zbek tilida** gaplashing. Kod izohlari, README va commit xabarlari ham o'zbekcha.
- O'yin ichidagi barcha matnlar (UI) **inglizcha**.
- Dizayn: zamonaviy, tekis, minimal. Qora fon (`#0B0B0C`), Unbounded va Manrope shriftlari, qisqa silliq harakatlar.
  "Galaktik", kosmik, uchqun va nur effektlari **kerak emas**: foydalanuvchi ularni rad etgan.
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
- `ProjectSettings/ProjectVersion.txt` dagi versiya (`6000.0.23f1`) taxminiy. Kompyuterda boshqa Unity 6 versiyasi
  bo'lsa, skript o'shani tanlaydi va Unity loyihani unga moslaydi: o'zgargan `ProjectVersion.txt`, Unity yaratgan
  `Packages/manifest.json`, `packages-lock.json` va `ProjectSettings/*.asset` fayllarini commit qiling.
  uGUI (`com.unity.ugui`) paketi kerak: u standart paketlar ichida bo'ladi.

## GitHub CI

`.github/workflows/ci.yml` har push'da: server testlari → GameCI (`game-ci/unity-builder`) bilan Unity'da
Windows va Linux build (`CraDevBatch.BuildGame`, sahnalar ham shu yerda yaratiladi) → Linux build'ni Xvfb'da
server bilan ishga tushirib, o'yinchi kabi o'tish va OCR bilan tekshirish (`tools/ci/smoke.sh`).

- Unity job'lari `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` secret'larisiz o'tkazib yuboriladi (README'da yo'riqnoma).
  Ularni hech qachon chatda so'ramang: foydalanuvchi o'zi GitHub Settings'ga kiritadi.
- Bulutdagi Claude sessiyasi CI loglarini GitHub MCP (`get_job_logs`) orqali o'qiydi. Artifact'larni (video, rasm)
  u yuklab ololmaydi, shuning uchun `smoke.sh` asosiy natijalarni (OCR matni, Player.log xatolari, PASS/FAIL) logga ham yozadi.
- `smoke.sh` dagi sichqoncha koordinatalari `BuildCharacterCreation` joylashuvidan olingan (1920x1080):
  forma o'zgarsa, ularni ham yangilang.

## O'yin oqimi

`Intro` (CraDev, 4.2 s) → `CDCGroup` (3.5 s) → `Loading` → yangi o'yinchi uchun `CharacterCreation`,
qaytgan o'yinchi uchun `MainMenu` (hali yo'q, Loading "READY" da to'xtaydi). Profil PlayerPrefs'da
(`PlayerProfile.cs`); uni o'chirish: menyu **CraDev > Test: saqlangan profilni o'chirish**.

| Papka | Mazmuni |
|---|---|
| `Assets/CraDev/Common/` | `SplashSequence` (splash asosi: vaqt o'qi, fade, ovoz, o'tkazib yuborish), `SceneLoader`, `Anim` (easing, input) |
| `Assets/CraDev/Intro/`, `CDCGroup/`, `Loading/` | splash va yuklash ekranlari |
| `Assets/CraDev/CharacterCreation/` | nickname + avatar tanlash ekrani; `AvatarViewer` qahramonni sichqoncha bilan 360° aylantiradi |
| `Assets/CraDev/Online/` | `GameApi` (server), `NicknameRules` (server bilan bir xil qoidalar), `PlayerProfile`, `RegistrationSession` |
| `Assets/CraDev/Avatars/Photos/` | avatarlarning fonsiz rasmlari: `<ID>_Front/Side/Back[/FrontQuarter/BackQuarter].png` |
| `Assets/CraDev/Editor/` | `CraDevSceneBuilder` (sahnalar), `CraDevBatch` (batchmode), `CraDevArtImporter` (rasm import sozlamalari), `UiBuild` |
| `Server/` | Node.js o'yin serveri: nickname noyobligi, o'yinchi profillari |
| `Design/` | logolar, ikonkalar, avatar promptlari va ularni PNG/WAV ga aylantiruvchi skriptlar |

## Kod qoidalari

- UI: uGUI, Canvas Screen Space - Overlay, CanvasScaler 1920x1080 (Expand). Render pipeline'ga bog'liq emas.
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
- Avatarlar: `Design/Characters/references/<ID>.png` (foydalanuvchi ChatGPT'da `prompts.md` bo'yicha yaratgan) →
  `python3 Design/Characters/process_references.py <ID>` (rembg bilan fonni olib tashlaydi) → `scenes`.

## Holat va keyingi qadamlar

1. **Kod hali haqiqiy Unity'da ishga tushirilmagan**: u bulutda faqat Unity DLL'lariga qarshi kompilyatsiya
   qilib tekshirilgan. Birinchi ish: `check` → `scenes` → `run`, chiqqan xatolarni tuzatish va natijani
   foydalanuvchiga ko'rsatish (Play rejimida yoki `run` bilan).
2. Avatarlar: erkaklar 4 ta (M1, M2, M3, M5), ayollar 5 ta (F1–F5). **M4 kerak emas** (foydalanuvchi qarori);
   kodlar o'zgarmaydi. Ro'yxat ikki joyda: `CraDevSceneBuilder.AvatarList` va `Server/src/nickname.js` (`AVATARS`).
3. Aylanish silliqroq bo'lishi uchun 45° rasmlar (`Design/Characters/prompts_quarter.md`) qo'shish mumkin.
4. Foydalanuvchi keyinga qoldirgan: pasport bilan ro'yxatdan o'tish (jins shundan olinadi, hozir
   `testGender`), yuzni skaner qilish, realistik 3D modellar, `MainMenu`.

Git: ish `claude/gallant-dijkstra-r0opib` branch'ida. `Library/`, `Temp/`, `Logs/`, `Builds/` commit qilinmaydi.
