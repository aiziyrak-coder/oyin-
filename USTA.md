# NewWorld Lobby

## Silliq, ixcham NewWorld dizayni — 2026-09-28

- [x] Foydalanuvchi talabi: Liquid Glass olib tashlandi; ma'qullangan tungi fonlar o'zgarmadi.
- [x] Platforma NewWorld deb nomlandi; haqiqiy foydalanuvchi nomlari va saqlangan profil o'zgartirilmadi.
- [x] Saqlash nuqtasi 2b9a3d8; katta Design manbalari Git'ga qo'shilmadi, push yo'q.
- [x] Tekis panellar, 8 px radius, 52–56 px asosiy tugmalar, kichikroq Manrope yozuvlari; 10 sahifa, menyu, dialog va switchlar bir uslubda.
- [x] Garderob previewlari GPUda alohida kichik teksturada navbat bilan chiziladi; CPU readback yo'q, asosiy avatar yuzini qayta bo'yamaydi.
- [x] Statik sahifa UI keshi, o'zgarmagan outfitni qayta hisoblamaslik, tez navigatsiyada pending sahifani bekor qilish.
- [x] UI 39/39; 9 avatarning 4 slotidan surat olindi, materiallar/outfit va profil saqlanishi tekshirildi.
- [x] Dastlabki garderob uzilishi 82.99 ms -> 22.06 ms; qayta kirish 10.85 ms (shu qurilmada, 1920x1080). Runtime exception yo'q.
- [ ] Kartalarning yakuniy kosmetik tuzatishidan so'ng NewWorld build va screenshot tekshiruvi.

Quyidagi Liquid Glass va Lynxos platforma nomi haqidagi yozuvlar tarixiy; yangi talab ularning o'rnini bosadi.

## Tungi Liquid Glass — 2026-09-28

- [x] Foydalanuvchi yo'nalishi: tungi shahar/interyerlar; eski yorqin fonlar butunlay yangisiga almashtirildi.
- [x] Oldingi holat lokal commitda saqlandi: 8671410; push qilinmadi.
- [x] Yangi 8 ta raster fayl: Assets/CraDev/MainMenu/Pages/Night/. Oldingi rasmlar o'chirilmagan.
- [x] 10 lobby sahifasi: umumiy shisha shader, yumaloq navigatsiya va yon menyu, kartalar, qidiruv, profil, rasmlar niqobi, toast va dialog.
- [x] Sozlamalarda yumaloq switchlar; hover/press holatlari; ko'k neon o'rniga sokin slate ranglar; tungi avatarga mos yorug'lik.
- [x] Unity build Succeeded. Birinchi to'liq UI tekshiruvi: 33/33.
- [x] Yakuniy UI tekshiruvi: 35/35, 0 failure; 10 sahifa + switchlar + dialog skrinshotlari ko'rildi. Shader xatosi va runtime exception yo'q.
- [x] 1920x1080 da 120 kadrli qisqa namuna: 239.7 FPS / 4.17 ms (shu qurilmadagi namuna, universal kafolat emas). O'yin ochiq qoldirildi.

UI native iOS emas: Apple Liquid Glass tamoyillaridan ilhomlangan Unity desktop ko'rinishi. Rasm generatsiyasi built-in image_gen; native 4K emas (asosiy fonlar 1672x941). Promptlar: Design/LobbyV2/Night/PROMPTS.md.

## Fon sifatini yaxshilash — davom etmoqda

- [x] Asl fonlar tekshirildi: kengligi 1449–2060 px, 4K emas.
- [x] Bosh sahifaning tiniqroq 1672x941 nusxasi endi o'yinga o'rnatildi; bu 4K emas.
- [x] 7 ta asosiy fon/hero yangilandi, education-cards atlasidagi 4 ta bino rasmi qayta tiklandi. Fonlardagi brend, logo va shiorlar olib tashlandi.
- [x] Sun'iy do'kon brendlari o'rniga ikki tildagi umumiy mahsulot turlari qo'yildi. Ta'lim/ko'ngilochar hero nomlari ham umumiylashtirildi. O'yinning navigatsiyadagi o'z nomi saqlandi.
- [x] Yangi build Succeeded; 10 sahifa skrinshoti, UI 33/33 sinov o'tdi (ikki tildagi brendsiz katalog va qidiruv ham). O'yin bosh sahifada ochiq. Logs/LobbyArtRefresh, Logs/lobby-art-refresh-build.log, Logs/lobby-art-refresh-player.log.
- [x] Rasm yo'llari, haqiqiy o'lchamlar va promptlar: Design/LobbyV2/quality-preview/ART-REFRESH.md. Built-in image_gen; CLI/API ishlatilmadi.
- [x] Unity desktop importi: siqilmagan RGBA32, 4096 limit, asl o'lcham saqlanishini avtomatik tekshirish.
- [x] Import o'zgarishi tekshirildi: Lobby build Succeeded; 9 ta tekstura asl o'lchamda RGBA32; 10 sahifa skrinshoti; UI test 0 failures. O'yin ochiq qoldirildi. Loglar: Logs/lobby-quality-build.log va Logs/lobby-quality-player.log.
- [ ] Haqiqiy 3840x2160 fonlar: aniq o'lchamli API/CLI yo'li uchun foydalanuvchi roziligi kerak; built-in so'ralgan o'lchamni bermadi.
- [ ] Yangi 4K fonlarni o'yinda tekshirish.

- [x] Mavjud yarim qolgan ishlar lokal Git saqlash nuqtasiga yozildi (dizayn manbalari kiritilmadi).
- [x] Hozirgi kompilyatsiya to'sig'i aniqlandi: eski lobby builder yangi `LobbyLayout` API bilan mos emas.
- [x] Yangi lobby builderi: 8 asosiy sahifa + Top-lar va Ko'ngilochar zona.
- [x] Ikonkalar, tozalangan home/map/wardrobe/shops/business fonlari va education/friends hero rasmlari.
- [x] Sahifa funksiyalari: API, kiyim/rang/mato preview, saqlash, do'stlar, tadbirlar, reyting, profil va sozlamalar.
- [x] Unity build: Succeeded; 10 ta sahifa skrinshoti; UI smoke: 0 failure.

Muhim qarorlar: sahnalar faqat `Assets/CraDev/Editor/` builder kodi orqali yaratiladi; barcha matnlar `Loc.cs` orqali o'zbekcha va inglizcha beriladi; `Design/LobbyV2` katta manba papkasi Git commitiga kiritilmaydi.

Yangi o'yin: `Play-LobbyV2.cmd` (serverni ham tekshiradi). Qayta build: `powershell -File tools/lobby.ps1 -Build`.
Build alohida `Builds/LobbyV2` da; eski `Builds/StandaloneWindows64` bilan adashtirmang.
Server qayta ishga tushirildi. Migratsiyadan oldingi baza: `Logs/cradev-before-lobby-v2-20260927.db`.
Server sinovlari: 56/56. Yakuniy UI smoke: 0 failure. Runtime logda exception, yo'q tarjima, qora kadr va RenderTexture.active ogohlantirishi yo'q.
Yakuniy build ishga tushirilib, tekshiruvdan keyin bosh sahifada foydalanuvchi uchun ochiq qoldirildi.
Oldindan keyinga qoldirilgan: haqiqiy dunyo/interyerlar, chat/guruhlar, email/parol, aksessuar modellari. Ularning tugmalari rostgo'y “tez orada” xabarini ko'rsatadi.
