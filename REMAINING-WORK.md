# NewWorld — qolgan ishlar va qabul mezonlari

2026-09-30. Bu ro'yxat bajarilganlik da'vosi emas. Foydalanuvchi barcha ishlarni davom ettirishga ruxsat bergan; mayda dizayn savollari bilan to'xtatmang. Tashqi hisob, to'lov yoki biometrik xizmatni soxtalashtirmang.

## Shu ishda qo'shilgan

- Do'st va guruh yozishmasi: serverda ruxsat tekshiruvi, takroriy yuborishdan himoya, oldingi xabarlar; lobby ichidagi bitta modal oyna. Haqiqiy foydalanuvchilarga sinov xabari yuborilmagan.
- Guruh egasi nom/tavsif/narx/davrni o'zgartirishi va egalikni faol a'zoga topshirishi mumkin. Eski ega admin bo'ladi; keyingi to'lov yangi egaga tushadi. Oldingi obuna muddati saqlanadi.
- Mikrofon qurilmasini tanlash, alohida ovozli chat balandligi va V bilan bosib gapirish. Karnayni yoqish endi mikrofonni avtomatik yoqmaydi. Aks-sado tozalash hali YO'Q.
- `newworld://group/XXXXXX` qat'iy tekshiriladi va guruh tafsilotlarini ochadi; avtomatik a'zolik/to'lov yo'q. `tools/register-protocol.ps1` tayyor, Windows ro'yxatiga hali o'rnatilmagan. Bir nusxali dasturga havolani uzatish hali yo'q.
- Share-Test bo'sh portni tanlaydi, begona serverni o'chirmaydi, har tarqatish alohida ZIP bo'ladi. Mahalliy fallback port tekshirildi; ommaviy tunnel/ZIP to'liq sinovi hali kerak.

## Birinchi navbat: Unity va ikki qurilma tekshiruvi

- [ ] Ikki haqiqiy akkaunt/qurilmada do'st qidirish, yozishma, guruh tahriri va egalik almashishi.
- [ ] Kameradan yuz skaneri: shu kompyuterda video qurilma ochilmadi. Testni o'tgan deb belgilamang. Rasm yuklash va barcha avatar yuz chetlari uchun rozilik bilan sinov materiallari kerak.
- [ ] Barcha 9 avatarda salomlashish qo'l/ko'krak mosligi, M/V va mic/speaker ikonkalari, ikki qurilmada nutq sifati.
- [x] 1366x768 va 5 kishilik lobby: 9 avatar animatsiyasi, joylashuv, studio ochib-yopish; 0 xato. Nom-ping yorliqlari zich joyda navbatma-navbat balandlikda. Boshqa nisbatdagi ekranlar hali alohida tekshirilsin.
- [ ] Haqiqiy internet orqali tunnel/ZIP va Windows havolasi.

## Olam — keyingi asosiy ish

- [ ] Server boshqaradigan multiplayer sessiya, kirish/chiqish/qayta ulanish, harakat cheklovlari; ikki alohida klientda tekshiruv.
- [ ] O'z avatari/tanasi, yurish/yugurish/cho'kkalash/burilish animatsiyalari, kamera to'qnashuvi; 9 modelda sinov.
- [ ] Tarmoqdagi avatarlarni silliq ko'rsatish, noto'g'ri/eskirgan koordinatalar va uzilishlarni to'g'ri qayta ishlash.
- [ ] Masofaga qarab ovozli chat, ovoz o'chirish/bloklash, mikrofon ruxsati, AEC uchun mos audio yechim. Lobbydagi HTTP ovozni professional past kechikishli yechim deb atamang.
- [ ] Olam zonalari: sinov maydonini buzmasdan shahar, do'kon, ta'lim, biznes, ko'ngilochar joylar. Arxiv lobby kodini o'chirmang; xizmatlarga olamdagi nuqtalardan kirish.
- [ ] Mualliflik huquqi toza musiqa va muhit ovozlari; alohida balandlik kanallari.

## Profil va iqtisod

- [ ] Email/parol: serverda mustahkam parol xeshi, sessiyani bekor qilish, urinish limiti, email tasdig'i va tiklash. Mavjud lokal profilni yo'qotmasdan ko'chirish.
- [ ] Pasport orqali tekshiruv: rasmiy provayder, maxfiylik/rozilik, yosh cheklovlari va saqlash siyosatisiz pasport yig'mang. Jinsni taxmin qilmang.
- [ ] Yuzni bo'lishish: alohida rozilik, cheklangan hajm/o'lcham, tekshirilgan format, kirish huquqi, o'chirish/keshni yangilash. Mahalliy yuzni avtomatik serverga yubormang.
- [ ] Click/Payme: merchant ma'lumotlari, imzolangan callback, takroriy so'rov/cancel/refund va sandbox sinovlari. Hozir checkout 503 bilan yopiq; klientdan balans to'ldirish yo'li ochilmasin.
- [ ] Qaror o'zgarmaydi: 1 CDCoin = 100 so'm; guruh egasiga 100%, komissiya 0%; pul yechish yo'q.

## Tarqatish va sifat

- [ ] Doimiy server: hosting hisobi va hudud tanlovi, HTTPS, avtomatik zaxira/tiklash sinovi, monitoring. VPS mavjudligi/past ping kafolatini uydirmang.
- [ ] Installer va imzolangan yangilanish: versiya, fayl xeshi/imzo, rollback, buzilgan yuklashdan himoya, profilni saqlash. Hali yo'q.
- [ ] Haqiqiy 4K fonlar: mavjud 9 fayl 1672x941. Unity importi 4096 limit bo'lishi manbaning 4K ekanini anglatmaydi. Avvalgi built-in generatsiya ham shu o'lchamni bergan; tasdiqlangan kompozitsiyani yo'qotmang.
- [ ] Kengroq xavfsizlik tekshiruvi: chat saqlash/o'chirish siyosati, shikoyat/bloklash, spam cheklovlari; serverni ochiq tarqatishdan oldin tekshirish.

## Tekshiruv dalillari

- Server: 84/84, 0 xato (chat huquqlari/paginatsiya/idempotency, guruh boshqaruvi, narx o'zgarsa eski obuna saqlanishi va yangi egaga keyingi tushum bilan).
- Lobby: 1366x768 da 110 dasturiy tekshiruv, 0 xato (`Logs/roadmap-final-player.log`). Chat raycast/modal/input, qat'iy havola parseri va mikrofon tanlash boshqaruvlari ham kiritilgan.
- Yakuniy build: `Logs/roadmap-layout-build.log` Succeeded. 5 kishilik yakuniy test: `Logs/roadmap-layout-player.log`, failures=0; ko'rilgan render `Logs/RoadmapLayout/five-standing.png`. Dastlab sinovdagi real avatarni profil yangilanishi almashtirgan; animatsiya testi izolyatsiyalangan replikaga o'tkazildi. Fiks ekran foizi o'rniga proyeksiyadagi avatar tanasi markazlari va dunyodagi masofa tekshiriladi. Bu haqiqiy 5 qurilmali tarmoq testi emas.
- Olamning mavjud mahalliy fizikasi: 101 dasturiy tekshiruv o'tgan (`Logs/audit-world-player.log`); bu multiplayer sinovi emas.
- Unity dasturiy lobby sinovlari va renderlar `Logs/roadmap-*`, `Logs/RoadmapLobby`, `Logs/ChatVerify` ichida. Kamera video qurilmasi xatosi bor; hardware sinovi deb yozmang.
- Saqlash nuqtasi: `70569bd`; baza zaxirasi `Logs/pre-roadmap-20260930.db`. Logs ichidagi baza va profil ma'lumotlarini Git/GitHubga qo'shmang.
