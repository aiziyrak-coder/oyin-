# NewWorld: bitta o'yin lobbysi

## Foydalanuvchining amaldagi qarori — 2026-09-28

Bu sayt emas, o'yin. Asosiy ekran yagona lobby bo'ladi. Header, sahifa navigatsiyasi va pastdagi bo'limlar doci kerak emas. Sozlamalar lobby ustida ochiladi va yopiladi; lobby sahifasi, fon va avatar o'zgarmaydi.

- Chap: do'stlar paneli ekran chekkasiga yopishgan, yuqoridan pastgacha to'liq balandlikda (alohida suzuvchi karta emas). Yuqorida qidirish/qo'shish, onlayn filtri, boshqarish/o'chirish, so'rovlar, yangilash ikonkalari. Ro'yxat qolgan balandlikni egallaydi. NewWorld yozuvi paneldan o'ngga surilgan.
- Markaz: mavjud 3D avatar va tasdiqlangan tungi fon.
- O'ng: profil, sozlamalar/til/chiqish, hozirgi tadbir.
- O'ng past: **NewWorldga kirish**. O'yin dunyosi hali yaratilmagan/ulanmagan; `MainMenuScreen.gameplayScene` bo'sh. Tugma xarita sahifasini ochmaydi, hali ulanmaganini ochiq bildiradi. Haqiqiy sahna keyingi topshiriqda ulanadi.

## O'chirilmagan, keyin ishlatiladigan qismlar

Quyidagi sahifalar, kodlar va rasmlar saqlangan, lekin foydalanuvchiga hozir ko'rinmaydi va `Show(id)` orqali ochilmaydi:

1. world — dunyo xaritasi
2. wardrobe — garderob (barcha 9 avatar, kiyim/mato/rang/soch, preview, saqlash)
3. shops — magazinlar
4. education — o'quv markazlari
5. business — biznes markazi
6. friends — eski alohida do'stlar/tadbirlar sahifasi
7. top — reyting
8. entertainment — ko'ngilochar zona

Ularni avtomatik qaytarmang. Foydalanuvchi keyin qayerga joylashni aytadi. Garderobga sozlamalarda ham o'tish yo'q. Foydalanuvchining saqlangan kiyimi avatarda qoladi.

## Tiklash uchun joylar

- Saqlash nuqtasi **e45788f**: oldingi tekshirilgan NewWorld ko'p sahifali tekis dizayni. Push qilinmagan.
- `CraDevSceneBuilder.LobbyV2.cs`: eski `V2Home`, `V2Navigation`, `V2Page` funksiyalari saqlangan. Barcha 10 LobbyPage sahnada mavjud.
- Hozirgi ko'rinish: `CraDevSceneBuilder.GameLobby.cs`; `BuildMainMenu` `V2GameHome` va `V2GameControls`ni ishlatadi, `singleWindow=true`.
- `MainMenuScreen.Show` arxiv yo'llarini to'sadi. `SetSettings` asosiy ekranni almashtirmasdan overlayni boshqaradi.
- Yangi chap panel: `LobbyFriendsPanel.cs`, haqiqiy GameApi bilan ulangan. O'chirish tasdiq so'raydi. Avtomatik test do'stlikni yaratmaydi/o'chirmaydi.
- `MainMenu/Pages/Night` barcha ma'qullangan fonlar; o'zgartirilmagan.
- `DevSingleLobbySmoke` / `-cradevSingleLobbySmoke`: bitta ekran, yashirin yo'llar, do'st qidirish/filtr, o'chirish tasdig'i, sozlamalar va profilni tekshiradi.

Eski `-cradevAllPages` / `-cradevUiSmoke` ko'p sahifali rejim sinovlari tarix uchun saqlangan. Ular singleWindow rejimida ishlatish uchun emas.

## Yakuniy tekshiruv

Build Succeeded; 53 checks / 0 failures. Qidiruv, onlayn filtr, so'rovlar, o'chirishdan oldingi tasdiq, sozlamalar overlayi/yopish, yashirin 8 sahifaga o'tishning taqiqlanishi, tarjima va profil saqlanishi tekshirildi. Do'st nomining chizilishi alohida tekshiruvga qo'shildi. Hech bir do'st test orqali o'chirilmadi yoki qo'shilmadi.
Loglar: `Logs/single-lobby-final-build.log`, `Logs/single-lobby-final-player.log`. Skrinshotlar: `Logs/SingleLobbyFinal/`. Yangi nusxa `Builds/LobbyV2/CraDev.exe`; o'yin lobbyda ochiq.
