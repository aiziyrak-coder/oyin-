# Lynxos Lobby V2

## Fon sifatini yaxshilash — davom etmoqda

- [x] Asl fonlar tekshirildi: kengligi 1449–2060 px, 4K emas.
- [x] Bosh sahifa uchun tiniqroq namuna yaratildi: Design/LobbyV2/quality-preview/home-detail-preview.png. Natija 1672x941; o'yinga o'rnatilmadi, 4K deb hisoblanmaydi.
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
