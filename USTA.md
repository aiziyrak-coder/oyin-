# NewWorld

## Eng yangi — Share-Test tuzatildi (2026-09-30)

- [x] Do'st ZIPi uchun tasdiqlangan vaqtinchalik Cloudflare HTTPS; DNS tayyorligi kutiladi, xatolar yoziladi.
- [x] NoWait serverni yopmaydi; qayta bosish joriy ZIP/manzilni saqlaydi; alohida Stop-Share-Test tugmasi.
- [x] Mezbon/do'st bir serverga ulanadi; PS5/PS7 fingerprint bir xil. ZIP launcher eski muhit sozlamasidan ustun.
- [x] 136 paket testi; real ZIPdan internet olamiga kirish 59/59 + lobbyga qaytish. Paket va foydalanish ma'lumoti `HANDOFF.md` boshida.
- [ ] Haqiqiy boshqa kompyuter va mikrofonlar bilan sinov, doimiy server keyin.

## Eng yangi — umumiy shahar va shaxmat (2026-09-30)

- [x] Foydalanuvchi yangi so'rov bilan olamga o'tishni tasdiqladi; oldingi holat `af2c2d3`da saqlandi.
- [x] Tekis shahar infratuzilmasi, bo'sh bino joylari, belgilar va animatsiyali reklama monitorlari.
- [x] Kiriladigan shaxmat pavilioni: 10 haqiqiy multiplayer doska, kuzatuvchi, qonuniy yurishlar va qayta o'yin; tikish yo'q.
- [x] Random ko'cha spawn, umumiy sessiya va silliq masofadagi avatarlar; 18 m proximity voice kodi.
- [x] Fotografik osmon, 2K CC0 asfalt/marmar/yog'och. Manba va dalillar `HANDOFF.md`, `REMAINING-WORK.md` boshida.
- [x] Unity shahar sinovi ikki marta 75/75, real GUI e2-e4 va ikkinchi klient e7-e5, lobbyga qaytish; eski lobby regressiyasi 124/124.
- [x] Server 107/107; kechikkan HTTP body orqali vaqt/tezlik limitini chetlab o'tish yopildi. Dalillar `Logs/city-server-final-107-tests.log`, `Logs/city-stable2-player.log`.
- [ ] Ikki jismoniy qurilmada internet/nutq, uzoq muddatli barqarorlik, AEC, katta yuklama va mukammal locomotion hali tekshirilsin.
- [ ] Boshqa shahar binolari keyingi foydalanuvchi topshirig'i bilan bittadan qo'shiladi; tarixiy pre-world ochiq bandlar yo'qolmagan.

## Eng yangi — lobby konflikt auditi (2026-09-30)

- [x] Chat tez yopib-ochish, qoralama, tarix, bo'sh yuborish va huquq bekor bo'lishi tuzatildi.
- [x] Guruh formasi/kech javoblar va tugagan obunaning lobby taklif huquqi tuzatildi.
- [x] Ovoz roomId bilan bog'landi; lobby almashganda buferlar tozalanadi; qisman speaker-mute serverda ham mic'ni o'chiradi.
- [x] Server 85/85; Unity 1280x720 da 124/124; kamera xatosi tushunarli UI ga o'tishi tasdiqlandi.
- [x] LocalOnly ZIP va fallback port sinovi; bu ommaviy tarqatish yoki internet testi emas.
- [ ] Hamma pre-world imkoniyatlar hali bitmagan: aniq ochiq bandlar va dalillar `REMAINING-WORK.md` boshida. Olamga o'tilmadi.

## CDCoin va ijtimoiy lobby (2026-09-30)

- [x] Qaror: guruh tushumi egaga to'liq, komissiya yo'q; 1 CDCoin = 100 so'm.
- [x] Serverda balans, tarix, atomik guruh to'lovi va takror to'lovdan himoya; 81/81 test.
- [x] Lobbyda CDCoin +, paket narxlari va obuna tasdiqlash oynasi.
- [x] Qidiruv oldidagi ortiqcha tarmoq kutishi olib tashlandi; eski javoblar darhol bekor qilinadi.
- [ ] Click/Payme merchant + sandbox + ishonchli to'lov/qaytarish integratsiyasi. Hozir checkout xavfsiz yopiq.
- [ ] Olam multiplayeri, haqiqiy akkaunt tiklash, moderatsiya/report/block va production monitoring alohida ko'lam bilan rejalashtirilsin.

## Olam grafikasi — fotografik materiallar (2026-09-28)

- [x] Oldingi fizikasi ishlaydigan holat 5a566d0 commitda saqlandi; lobby dizayni o'zgarmadi.
- [x] 4096x2048 Radiance HDR osmon, suratga olingan 2048x2048 albedo/normal/roughness/AO materiallari. Poly Haven CC0 manbalari va hashlar World/Art/SOURCES.md da.
- [x] Skanerlangan Boulder 01, original UV atlas, LOD2 (16 530 uchburchak) bilan 15 tabiiy tosh. Yumshoq relyef va 52 minggacha ingichka o't; 6 scenery renderer, o'tda nozik shamol.
- [x] HDR + filmic rang ishlovi, chetlarni saqlaydigan kontakt soyasi, bir marta baked osmon reflection, quyosh osmondagi haqiqiy yo'nalishga mos. Yaqinda tuman yo'q.
- [x] Sinov konstruksiyalari o'lchami/colliderlari saqlandi; chiziladigan qirralarga kichik bevel qo'shildi. Material masshtabi haqiqiy metrga mos.
- [x] Panorama chokidagi oq chiziq topildi va tuzatildi: sky no-mips/Bilinear/aniso1/RepeatU/ClampV. 270 daraja kamera kadrida tasdiqlandi.
- [x] Logs/realism-build3.log Succeeded, shader xatosiz; realism-player3.log 89 checks/0 failures va lobbyga qaytish PASS. 1920x1080 RTX3060, 2.2 soniyalik namuna: 235.1 FPS, worst8.35ms, >33ms kadr yo'q. Bu uzoq benchmark yoki boshqa qurilmalar uchun kafolat emas.
- [x] Haqiqiy renderlar ko'rildi: Logs/Realism3/ (umumiy, skanlangan tosh, panorama choki, AO on/off, MSAA off, postfx off, sozlamalar, lobby). Bu hali mahalliy birinchi-shaxs fizik sinov maydoni, to'liq shahar emas.

## O'yin olami — tayyor sinov asosi

- [x] Lobby kirish -> Loading -> WorldSandbox, qaytish Esc menyusidan.
- [x] Oddiy yer/osmon, harakat uchun devor/zina/past shift/rampalar.
- [x] Birinchi shaxs WASD/mouse, Shift yugurish, Ctrl/C cho'kkalash, Space sakrash; silliq tezlanish va to'xtash.
- [x] Collider, gravity, step/slope, stand clearance, pitch clamp, fokus/pauza, respawn.
- [x] Chap-yuqori mini-xarita va olam uchun alohida Esc sozlamalari, UZ/EN.
- [x] Build hamda 63 runtime assertion xatosiz; server 57 test xatosiz. Zina ilinish bugi topildi va tuzatildi. Logs/World/ va Logs/world-player2.log.
- [ ] Olam ichidagi multiplayer harakat va ko'rinadigan tana animatsiyasi — bu mahalliy birinchi-shaxs fizik sinoviga kiritilmagan, keyingi alohida bosqich.
- Saqlash nuqtasi c7d51e3 (olamdan oldingi tayyor lobby).

## Kattaroq personaj va aralash joylashuv

- Qo'shimcha 0.68 scale bekor qilindi: avatar eski kattaroq scale=1 holatda, UI o'lchami o'zgarmadi.
- Mezbon markazda; 4 mehmon roomId bo'yicha aralashtirilgan xavfsiz yo'laklarda, turlicha chuqurlikda. Har bir yangilanishda joyi o'zgarmaydi. O'tirish/kreslo qaytarilmadi.
- Sinovga 200 xona uchun oraliq, deterministik joylashuv, turli chuqurlik hamda ekrandagi masofa va masshtab tekshiruvi qo'shildi.

## Oxirgi qaror — tik turgan party

- Foydalanuvchi o'tirishni bekor qildi: divan, ikki kreslo va SeatedIdle kodi olib tashlandi. Eski o'tirish rejalari tarixiy, qayta qo'shmang.
- 5 kishi uchun tik turish idle, nickname, online va 3 ustunchali ping qoldirildi. Remote avatarlar ham erkak/ayol idle controller oladi.
- Toast yuqori markazga ko'chirilgan. Party server 57/57 umumiy testdan o'tgan; alohida haqiqiy masofaviy qurilmalar bilan sinov hali o'tkazilmagan.
- Saqlash nuqtasi fd441e3. Bazaga profil/test do'stlari qo'shilmagan; zaxira Logs/pre-party-backup.db.
- Yakuniy tekshiruv: standing-final-build.log — Succeeded; standing-final-player.log — 9 avatar harakati, 5 personaj, kreslosiz sahna, ekran ichidagi nickname va yuqori toast: 0 xato. Logs/Party/five-standing.png test tasviri (TEST_* haqiqiy do'stlar emas). Oddiy o'yinda faqat haqiqiy party a'zolari ko'rinadi.

## Chap panel: pastgacha va yig‘iladigan

- Saqlash nuqtasi 90eeed4. 65% UI masshtabi saqlandi; chap panel yuqoridan 124 UI birlik, pastdan 24 birlik masofada stretch. Ro‘yxat bo‘yi moslashadi, profil/sozlama footer pastga bog‘langan.
- FriendsDrawerToggle chetdagi ‹/› tugma bilan 0.22 soniyada panelni chetga yig‘adi/ochadi. Yopiq panel inputni to‘smaydi, qidirish fokusi chiqariladi. O‘ngdagi do‘stlar amali yashirilgan panelni qayta ochadi.
- O‘rindiqlar bo‘yicha noaniqlik HAL QILINGAN: jami 5 kishi (host + 4 do‘st), o‘rtada 3 kishilik divan va ikki chetda bittadan kreslo. Nickname, online va o‘lchangan pingga asoslangan yashil/sariq/qizil 3 ustuncha so‘ralgan. Bu seating/invite/nameplate qismi hali bajarilmagan, qayta o‘rindiq sonini so‘ramang.
- Panel testlari: Logs/friends-drawer-build.log, Logs/friends-drawer-player.log, Logs/FriendsDrawer/.


## Ixcham UI va o‘tiradigan lobby — davom etayotgan talab

- Foydalanuvchi barcha UI va personajni 35% kichraytirishni so‘radi. Oldingi ma’qullangan holat: 78d7626.
- CanvasScaler referenceResolution = (1920,1080)/0.65; barcha UI bir xil 65% masshtab. O‘ng promo/kartalar/CTA pastga bog‘langan; sozlama oynasi markazlangan. Personajning kamera ichidagi balandligi ham 65%.
- Keyingi talab: do‘stlarni lobbiga chaqirish, divan/kreslolarda o‘tirish, tabiiy qo‘l/oyoq idle harakatlari. BU QISM HALI QO‘SHILMAGAN. Hozir faqat tik turish animatsiyalari mavjud; sofa/chair 3D model va party/invite API yo‘q.
- O‘rindiqlar soni uchun savol yuborilgan: 1 ta ikki kishilik divan + 2 kreslo yoki 5 ta ikki kishilik divan + 2 kreslo? Javob kelmaguncha joylashuvni taxmin qilib qurmaslik.
- Kichraytirish build/test: Logs/compact-lobby-build.log, Logs/compact-lobby-player.log, Logs/CompactLobby/.


## Reference sirtlarini tuzatish — joriy

- User old brown/flat cards and two-color button rejected. Backup: 3bd3b32 (tested time/weather system).
- New ReferenceSurface component + shared shader: charcoal vertical surfaces, soft rims, full landscape promo scrim, emerald center / golden edge primary, separate play orb. No GrabPass/blur/per-frame allocations.
- Explicit sRGB/linear conversion preserves authored dark shades in the project's linear renderer. First QA caught over-bright grey, corrected before delivery.
- Native filled play/users/compass glyphs; title bold 104; live friend/online/offline counts and footer portrait; 72px rows. No fabricated friends or level data.
- Built-in generated card art: Assets/CraDev/MainMenu/Pages/reference-world.png; prompt Design/LobbyV2/ReferenceSurface/PROMPTS.md.
- All time/weather behavior and archived pages preserved. -cradevReferencePreview is developer screenshot-only, restores local-clock mode after captures.
- Logs: reference-surface-build-final.log / reference-surface-player-final.log. Screenshots Logs/ReferenceSurfaceFinal/. Reference screenshot uses original sunset plate; live-clock.png shows restored actual clock.
- Final sampled-color pass: reference-surface-build-matched.log Succeeded; reference-matched-player.log 59 checks, 0 failures. Logs/ReferenceMatched contains comparison and restored live-clock captures. Primary midpoint adjusted from saturated emerald to reference grey-green, gold edge intensity reduced, native halo added.


## Qurilma soati va ob-havo — eng yangi qo'shimcha

- Saqlash nuqtasi: fa9c4d1. Oldingi gradient dizayn saqlangan.
- 9 ta yozuvsiz fon: Assets/CraDev/MainMenu/Resources/LobbyTimes/. Built-in image generation, promptlar Design/LobbyV2/TimeWeather/PROMPTS.md. Manba 1672×941; native 4K emas.
- Mahalliy DateTime.Now: 05–09 tong, 09–12 kunduz, 12–17 peshin, 17–20 shom, 20–24 kechqurun, 00–05 tun. Soat qayta tekshiriladi, dastur qayta ochilmasa ham almashadi.
- Sozlamalar → Ovoz va grafika → Lobbi ob-havosi: Soat bo'yicha / Bulutli / Yomg'irli / Qorli. Foydalanuvchi shu qo'lda tanlashni tasdiqladi. Real ob-havo/location API ishlatilmaydi.
- Fon animatsiyasi: osmonning yengil harakati, suv akslari, tungi miltillash, yomg'ir/qor. O'chirish sozlamasi mavjud. Effektlar faqat fon shaderida, UI yoki avatar ustida emas; GrabPass va blur yo'q.
- Asinxron yuklash, 2 soniya crossfade, eski resurs bo'shatiladi; to'qqiz rasm birga RAMda ushlab turilmaydi.
- Build: Logs/environment-build.log. Test/captures: -cradevEnvironmentTest, Logs/environment-player.log va Logs/Environment/.
- Yakuniy build environment-build-final.log Succeeded. environment-player-final.log: 1440 daqiqa va 4320 weather resolution, 9 fon, settings selector 4 holati, motion toggle — 0 failures. Piksel testi: off=0 o'zgarish, on=141 o'zgarish (focus true); 120-frame sample 239.7 FPS. Kunduz va sozlama skrinshotlari vizual ko'rildi. Logs/EnvironmentFinal/.


## Eng yangi dizayn — reference gradient

- Foydalanuvchining oxirgi rasmiga asoslangan iliq sunset penthouse, 492×896 chap panel, 650 px o'ng ustun, uchta ixcham karta, yashil–oltin kirish tugmasi. Bu qaror pastdagi ko'k/full-height dizayn qarorlarini almashtiradi.
- Oldingi holat: e361a80. Yashirilgan sahifalar, tungi fonlar va haqiqiy profil saqlangan. Offline filtr qo'shilgan; soxta do'stlar yoki level yo'q.
- Fon built-in image generation orqali yaratildi; prompt: Design/LobbyV2/ReferenceGradient/PROMPTS.md. Native 4K emas, manba o'lchami importerda saqlanadi.
- Native vertex gradient: GrabPass, real-time blur yoki per-frame material yaratish yo'q. Birinchi test 57/57. Yakuniy build logi: Logs/lobby-gradient-build-final.log; runtime: Logs/lobby-gradient-player-final.log.


## Amaldagi yo'nalish: bitta o'yin lobbysi

- [x] O'ng boshqaruvlar bitta 384 px ustunga tekislandi; profil/tadbir oralig'i 16 px, kirish pastki chetdan 64 px. Tiniq ko'k #2463EB yagona LobbyPalette orqali tarqatildi. Saqlash nuqtasi db71578.
- [x] Yangi tartib/rang: lobby-blue-build.log Succeeded; lobby-blue-player.log 57/57 tekshiruv, 0 failure; Logs/LobbyBlue/ skrinshotlarida home va sozlamalar ko'rildi. O'yin lobbyda ochiq.

- [x] Keyingi talab: do'stlar paneli chap chekkada, yuqoridan pastgacha to'liq; suzuvchi karta emas. Vertikal stretch anchor, ro'yxat ham ekran balandligiga moslashadi.
- [x] Chap panel tekshirildi: friends-dock-build.log Succeeded; friends-dock-player.log 54/54, 0 failure. Logs/FriendsDock/home.png vizual tekshirildi.

- [x] Oldingi ko'p sahifali holat e45788f commitda saqlandi.
- [x] Bosh ekran + uning ustidagi sozlamalar; header/nav/pastki sahifa doci yo'q.
- [x] Chap do'stlar paneli: qidirish, onlayn filtr, so'rovlar, tasdiq bilan o'chirish, yangilash. Boshqa sahifaga o'tmaydi.
- [x] O'ngda profil/boshqaruvlar/tadbir, pastda NewWorldga kirish.
- [x] Qolgan 8 sahifa va garderob o'chirilmagan, yo'llari yashirilgan. Keyingi joylashuvni foydalanuvchi aytadi. Tafsilot: LOBBY_ARCHIVE.md.
- [x] Yangi bitta lobby buildi Succeeded; 53/53 avtomatik tekshiruv, 0 failure. Home, qidiruv, sozlamalar va bildirishnomalar skrinshotlari ko'rildi. Profil/kiyim o'zgarmadi, arxivdagi 8 yo'l ochilmasligi tekshirildi. O'yin asosiy lobbyda ochiq.
- [x] Loglar: Logs/single-lobby-final-build.log, Logs/single-lobby-final-player.log. Skrinshotlar: Logs/SingleLobbyFinal/.
- [ ] Haqiqiy NewWorld gameplay sahnasini keyingi topshiriqda ulash (hozir mavjud emas; tugma yashirmasdan bildiradi).

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
## 2026-09-30 — qolgan ishlar navbati

Dolzarb batafsil ro'yxat: `REMAINING-WORK.md` (barcha ishlar bitgani yo'q).

- [x] Do'st/guruh yozishmasi serveri va lobby oynasi, guruhni tahrirlash/egalikni topshirish: 84 server testi o'tdi.
- [x] Mikrofon tanlash, V bilan gapirish, alohida ovozli chat balandligi; AEC yo'q.
- [x] Share-Test port va jarayon xavfsizligi; 8090 mahalliy test o'tdi.
- [x] Guruh havolasi parseri va tafsilotlarni ochish; Windows ro'yxatdan o'tkazish skripti tayyor, bajarilmagan.
- [x] Chat bo'sh holati haqiqiy 1366x768 da ko'rildi. Unity build o'tdi; yangi kichik-ekran masshtabida 110 lobby testi o'tdi.
- [x] Yakuniy 9 avatar / 5 kishilik dasturiy sinov: 0 xato. Kichik ekranda nom-ping yozuvlari ustma-ust tushmasligi tuzatildi; `Logs/RoadmapLayout` rasmlari ko'rildi.
- [ ] Ikki haqiqiy qurilmada chat/ovoz/guruhlar; kamera skaneri (video qurilma ochilmadi).
- [ ] Olam multiplayeri, o'z tanasi/animatsiyasi va masofaviy ovoz — keyingi asosiy yo'nalish.
- [ ] Haqiqiy to'lov, VPS, installer/yangilash, email/pasport, yuzni bo'lishish, 4K — qolgan; batafsil mezonlar alohida ro'yxatda.
