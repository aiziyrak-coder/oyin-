# NewWorld — qolgan ishlar va qabul mezonlari

2026-09-30. Bu ro'yxat bajarilganlik da'vosi emas. Foydalanuvchi barcha ishlarni davom ettirishga ruxsat bergan; mayda dizayn savollari bilan to'xtatmang. Tashqi hisob, to'lov yoki biometrik xizmatni soxtalashtirmang.

## Eng yangi: Share-Test orqali haqiqiy internet ZIP tekshiruvi

- [x] Cloudflare uchun foydalanuvchi ruxsati olindi; public HTTPS /health va paket o'yinidan ulanish tekshirildi. Yangi domen DNS tayyorligi uchun 120 soniyalik qayta urinish, tushunarli xato/transkript.
- [x] NoWait/oyna yopilganda server saqlanadi; `Stop-Share-Test.cmd` faqat o'zi egalik qiladigan jarayonni yopadi. Launcher serveri bo'lsa, bir xil olam ishlatiladi va Stop uni o'chirmaydi.
- [x] PS5/PS7 fingerprint farqi va PS5 build-stamp JSON metama'lumoti xatosi tuzatildi. Qayta bosishda URL/tunnel PID/ZIP vaqti o'zgarmasligi tekshirildi.
- [x] 136/136 paket sinovi; real ZIP 186 fayl/entry, zarur Unity fayllari, server.txt va launcher bor; DB/.env/PDB/log/DoNotShip yo'q. ZIPdan ochilgan o'yin `Logs/share-public-client.log`da 59/59 + lobbyga qaytish PASS.
- [ ] Do'stning boshqa jismoniy kompyuterida internet va ikki mikrofon sinovi hali kerak. Doimiy VPS/installer/avtoyangilash bandlari bajarilmagan. Oldingi "ommaviy tunnel sinovi yo'q" jumlalari pastdagi tarixiy auditga tegishli.

## Eng yangi: shahar infratuzilmasi va umumiy shaxmat olami

Foydalanuvchining yangi so'rovi bilan olamga o'tildi; quyidagi lobby-audit bo'limi tarixiy. Hali ochiq hisob/to'lov/hardware ishlarini tayyor deb belgilamang.

- [x] Tekis 300x300 m hudud; yo'l, belgi, zebra, piyoda yo'lagi, chiroqlar, bino uchastkalari va 4 animatsiyali reklama monitori.
- [x] Ko'chada random spawn, autentifikatsiyali umumiy sessiya, yaqin o'yinchi avatari, nomi va silliqlangan harakati. Hudud va gorizontal tezlik serverda cheklangan; serverda to'liq collider/fizika simulyatsiyasi YO'Q.
- [x] Alohida kiriladigan shaxmat pavilioni, 10 doska, 20 stul, 320 3D dona; E interaksiyasi, ikki tomon, kuzatuvchi, haqiqiy yurish qoidalari va qayta o'yin.
- [x] 18 m ovoz filtrining server/klient kodi, mikrofonning aniq rozilik bilan yoqilishi, uzilish/mute paytida eski audio tozalanishi. Bu haqiqiy nutq sifati sinovi degani emas.
- [x] Fotografik HDR osmon va litsenziyasi toza asfalt/marmar/yog'och xaritalari; manbalari `World/Art/SOURCES.md`.
- [ ] Ikki haqiqiy kompyuterda internet, mikrofon, paket yo'qotilishi va uzoq muddatli multiplayer sinovi; AEC, individual block/report/mute.
- [ ] O'z tanasi va to'liq sifatli 9-model locomotion; hozir boshqa o'yinchi protsedurali animatsiya bilan ko'rinadi, o'zi birinchi shaxs kamerasi.
- [ ] 128 o'yinchi yuklama sinovi, production transport/anti-cheat va server colliderlari. Hozirgi HTTP prototipni cheksiz MMO yoki professional voice deb atamang.
- [ ] Shaharni keyinchalik bittadan binolar/zonalar bilan to'ldirish. Joriy scope bo'yicha boshqa binolar ataylab qo'yilmagan; eski lobby sahifalari arxivda saqlangan.

Tekshirish dalillari:

- `Logs/city-stable-build.log`: Unity 6000.3.24f1 `BuildWorld` Succeeded, `Builds/LobbyV2/CraDev.exe`.
- `Logs/city-server-final-107-tests.log`: 107/107 server testi. Qo'shimcha mustaqil audit kechikkan HTTP body vaqt hisobini orqaga qaytarib tezlik cheklovini buzishini aniqladi; world/chess/voice POST vaqti body o'qilgach olinadi, world vaqti monoton. Kechikkan body/sessiya regressiya sinovlari qo'shildi.
- `Logs/city-stable-player.log`: 75/75 va lobbyga qaytish PASS. Real Unity klienti + alohida HTTP test klienti: oq GUI o'rni, e2-e4, qora e7-e5, navbat va tasdiqlangan chiqish. Bu ikki jismoniy kompyuter testi emas.
- `Logs/city-stable2-player.log`: yangilangan server bilan takroran 75/75 va lobbyga qaytish PASS; o'sha DX12 sozlamalari, avvalgi kutilmagan yopilish qaytalanmadi. Barcha yangi renderlar `Logs/CityStable2/`; haqiqiy ko'cha, monitor, pavilion, ichkari, multiplayer va e2-e4/e7-e5 doskasi.
- `Logs/city-lobby-regression.log`: avvalgi lobby 1280x720 da 124/124; foydalanuvchi profili saqlangan. Kamera yo'q holati tushunarli xatoga o'tdi; haqiqiy webcam ishladi degani emas.
- `Logs/CityStable/city-chess-playing.png`: birinchi qayta sinovning ko'rib tekshirilgan shaxmat renderi. Birinchi uch tashqi surat papka tayyor emasligi uchun saqlanmagan; `CityStable2`da bu takrorlanmadi.
- 1366x768 RTX3060 da 160 chizilgan kadrli qisqa namuna: 198.7 FPS, eng sekin 8.35 ms, 33 ms dan sekin kadr yo'q. Bu uzoq benchmark emas. Oldingi yashirin oyna sinovidagi qora rasmlar va 2184 FPS yaroqsiz dalil, ishlatmang.
- `Logs/city-verified-player.log` bir marta Unity native culling-job `c0000005` bilan yopildi. `Logs/city-crash-symbolicated.log` matching UnityPlayer PDB bilan `ujob_find_dependency_chain -> SyncPreparedFences -> CullScene`ni ko'rsatadi; GPU drayveri sababligi isbotlanmagan. Har kadr `TextMesh.characterSize` o'zgartirish o'rniga doimiy geometry + transform scale qilindi, caption faqat o'zgarganda yoziladi. Keyingi ikkita to'liq sinov o'tdi; asl engine sababi aniqlangan/tubdan tuzatilgan deb yozmang.

Tarmoq testlari faqat haqiqiy bazaning izchil mahalliy klonida; Logs/DB/EXE fayllari Gitga qo'shilmaydi. Yangi `-cradevCitySmoke` ishlating: eski `-cradevWorldSmoke` olib tashlangan zina/rampa maydonini kutadi va bu shahar uchun mos emas.

## Tarixiy: olamdan oldingi lobby auditi

Saqlash nuqtasi `4488ecd`, push yo'q. Olam funksiyalariga o'tilmadi.

Tuzatilgan aniq xatolar:

- Chat tez yopib-ochilganda eski javoblar yangi oynaga tushishi: yopishda so'rov avlodi darhol bekor qilinadi.
- Chat qoralamasi kontakt almashganda yoki oyna yopilganda yo'qolishi: har suhbatga alohida xotiradagi qoralama. Dastur yopilganda saqlanmaydi; diskka shaxsiy matn yozilmaydi. Noaniq yuborish javobida takror yuborish kaliti saqlanadi.
- Eski tarixning polling sabab qayta sakrashi: eski tarixda jonli yangilanish to'xtaydi, so'nggi xabarlarga qaytish tugmasi bor. Dastlab ochilganda eng yangi xabarlar ko'rinadi. Bo'sh xabar yuborilmaydi.
- Chat huquqi bekor bo'lsa ekrandagi tarix tozalanadi va yuborish yopiladi. Dinamik sarlavhani tarjima komponenti ustidan yozmaydi.
- Guruh formasi pending yuklashdan qayta chizilishi, oldingi xato forma ustida qolishi, saqlash paytida matn o'zgarishi va eski operatsiya javobining boshqa guruhni almashtirishi tuzatildi.
- Kechikkan ovoz boshqa lobbyga yuborilishi: har bo'lak yozilgan roomId bilan bog'lanadi, server noto'g'ri/eski roomId ni rad etadi. Lobby o'zgarganda ovoz buferlari/oqimlari tozalanadi. Mute qilganda kutayotgan bo'laklar tashlanadi.
- Faqat speakerOn=false yuborilganda server micOn=true bo'lib qolishi tuzatildi.
- Obunasi tugagan guruhdoshning taklif yuborishi/qabul qilishi: guruh sahifasi ochilmasa ham muddat har ruxsat tekshiruvida hisoblanadi.
- Guruh havolasi sozlama yoki avatar studiyasi ustidan ochilmaydi; parser oxirgi yangi qator kabi ortiqcha belgilarni ham rad etadi.

Dalillar: server 85/85; Unity 1280x720 da 124/124 (`Logs/lobby-audit-final-player.log`), build `Logs/lobby-audit-final-build.log` Succeeded. Guruh/chat holat testlari haqiqiy foydalanuvchilarga xabar, guruh yoki to'lov yaratmagan. Kamera 5 soniyalik kutishdan keyin tushunarli xatoga o'tishi tekshirildi, ammo haqiqiy skaner ishladi degani emas. Windows Camera/Image qurilmalari ro'yxati bo'sh, joriy foydalanuvchining webcam ruxsati Allow; bu drayver/hardware muammosini to'liq tashxislash o'rnini bosmaydi.

Share-Test mahalliy ZIP sinovi o'tdi: `Builds/NewWorld-test-20260930-110652-ef147a.zip`, 355876690 bayt; exe/UnityPlayer/server.txt bor, .db/.env/.pdb/.log yo'q. Test 8090 serveri yopildi, 8080 asosiy server ishladi. Bu ZIP lokal 127.0.0.1 manzilli SINOV NUSXASI: do'stlarga tarqatmang; eng oxirgi mayda tuzatishlardan oldin yig'ilgan. Ommaviy tunnel tekshirilmagan.

Muhim: ovoz protokoli endi roomId talab qiladi; eski mijozlar serverga ovoz yubora olmaydi. Tarqatishda server va barcha o'yinchilarning buildlari birga yangilansin.

Hali "hech qanday kamchilik yo'q" deb bo'lmaydi: ikki qurilma/internet sinovi, haqiqiy kamera/yuz natijasi, AEC, email/parol va akkaunt tiklash, yuzni bo'lishish, 4K, Click/Payme, VPS va installer/yangilash quyidagi ro'yxatda ochiq turibdi. Ular tayyor deb belgilansin uchun real dalil kerak.

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

## Olam — keyingi sifat va kengaytirish ishlari

- [x] Umumiy multiplayer sessiya, kirish/chiqish/qayta ulanish va boshlang'ich harakat cheklovlari; Unity + HTTP klientida tekshirildi. Ikki jismoniy qurilma va server fizikasi hali kerak.
- [ ] O'z avatari/tanasi, yurish/yugurish/cho'kkalash/burilish animatsiyalari, kamera to'qnashuvi; 9 modelda sinov.
- [x] Tarmoqdagi avatarlarni silliq ko'rsatish va sessiya uzilganda tozalash. Internet/paket yo'qotishidagi kengroq sinov ochiq.
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
