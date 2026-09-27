# Lynxos Lobby V2

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
