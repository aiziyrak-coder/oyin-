# Yangi chatda davom ettirish uchun (CraDev / NewWorld)

Yangi chatga shu matnni bering: **"`DAVOM.md`, `CLAUDE.md` va `HANDOFF.md` ni o'qib chiq, ishni davom ettiramiz."**

## Loyiha
- Unity 6 (6000.3.24f1), Windows uchun multiplayer virtual dunyo o'yini. Kompaniya CraDev, platforma nomi NewWorld.
- GitHub: `aiziyrak-coder/oyin-`, branch **`claude/trusting-archimedes-aq2wdl`**.
- Foydalanuvchi kompyuterida papka: `D:\Game1\CraDev`. Foydalanuvchi bilan **o'zbek tilida** gaplashing.
- Qoidalar `CLAUDE.md` da: sahnalar faqat builder kodi (`Assets/CraDev/Editor/`) orqali quriladi, `.unity` qo'lda
  tahrirlanmaydi; barcha matn ikki tilda (`Loc.cs` + `Loc.<Bo'lim>.cs` partial jadvallar, `Register`).

## Foydalanuvchi qanday ishga tushiradi (Windows, ODDIY PowerShell, Administrator emas)
```
cd D:\Game1\CraDev
git pull
powershell -NoProfile -ExecutionPolicy Bypass -File tools\lobby.ps1 -Build   # barcha sahnalar + build
```
- Keyin o'ynash: `Play-LobbyV2.cmd`. Do'stlar bilan internet orqali sinash: `Share-Test.cmd`
  (server + Cloudflare tunnel + `Builds\CraDev-test.zip` - do'stlarga yuboriladi; oyna ochiq turishi kerak).
- Eski server yopilmasa launcher bo'sh portni (8090+) tanlaydi. Administrator oynasidan yoqilgan server faqat
  kompyuter qayta yoqilganda yopiladi.
- Xato bo'lsa: `Logs\lobby-v2-build.log` yoki `%USERPROFILE%\AppData\LocalLow\CraDev\CraDev\Player.log` oxiri.

## Bulutdagi Claude uchun tekshiruv (Unity yo'q)
- C# kompilyatsiya: `tools/ci/cs-check/check.sh` (Unity 2021 API + stublar, taxminiy; dotnet 8 kerak).
- Server: `cd Server && npm test` (hozir 80/80).

## Oxirgi sessiyada qilingan (hammasi push qilingan, lekin Unity'da hali TO'LIQ SINALMAGAN)
1. Lobbyda Avatar studiyasi: avatar almashtirish, kamera bilan jonli yuz skaneri, rasm yuklash
   (`LobbyAvatarStudio.cs`, `Face/Scripts/FaceScanner.cs`, `FaceScanView.cs`, builder `CraDevSceneBuilder.AvatarStudio.cs`).
2. Sozlamalar qayta qurilgan (`LobbyContent.Settings*.cs`, `GameSettings.cs`, `QualitySettings.asset`).
3. Lobby tuzatishlari, olam Esc menyusi, server bazasi doimiy joyda (`%LOCALAPPDATA%\CraDev\server`), `-server <url>` / `server.txt`.
4. Lobby ovozli chati (HTTP orqali, `LobbyVoice.cs`), mikrofon/karnay tugmalari (karnay o'chsa mikrofon ham o'chadi),
   nom ustida ikonkalar, mehmon kelganda salom animatsiyasi + "Assalomu alaykum" (`LobbyGreeting.cs`), "Lobbydan chiqish".
5. Ping alohida `/api/ping` bilan, ID bo'yicha qidiruv (hamma o'yinchilar ro'yxati olib tashlangan),
   Guruhlar - Telegram kabi: ochiq/yopiq/pullik, kod `NW-XXXXXX` (`LobbyGroupsPanel.cs`, `Server/src/groups.js`).
6. Launcherlar: `tools/common.ps1`, `tools/lobby.ps1`, `tools/share.ps1`; CI da C# tekshiruvi.

## Foydalanuvchi aytgan, hali hal qilinmagan
- Avatar yaratishda eski kamera oynasi chiqqan edi -> build barcha sahnalarni qayta yaratadigan qilindi; qayta tekshirish kerak.
- Do'st qidirish maydoni ishlamagan -> mustahkamlandi, sababi aniq topilmagan; tekshirish kerak.
- Foydalanuvchi papkada O'ZI ko'p o'zgarish qilgan va ularni push qilishi kerak edi
  (`git add -A; git commit; git pull; git push`). Yangi chat avval `git log` bilan ular kelganini tekshirsin.

## Qilinmagan ishlar (tavsiya etilgan tartib)
1. Foydalanuvchi sinovidan chiqqan xatolarni tuzatish.
2. Doimiy server (VPS) - tunnel o'rniga.
3. Olamda birga yurish (multiplayer harakat) + olamda o'z avatari va yurish animatsiyasi.
4. Olamda ovozli chat (yaqindagilar eshitiladi).
5. Yozma chat (do'stlar, guruhlar); guruhni tahrirlash, egalikni o'tkazish.
6. Haqiqiy to'lov (Click/Payme) pullik guruhlar uchun; yuzni serverga yuklash (boshqalar ko'rishi).
7. Olam zonalari (magazin, o'quv markazi, biznes, ko'ngilochar), installer va avto-yangilanish, musiqa,
   pasport/email ro'yxatdan o'tish.

## Foydalanuvchi qarorlari (buzmang)
- Liquid Glass uslubi rad etilgan; tekis 8px panellar, ko'k #2463EB. O'tirish/divan bekor qilingan, 5 kishi tik turadi.
- Soxta do'st/online/level ko'rsatilmaydi. `Pages/Night/` fonlari o'zgartirilmaydi. Arxiv sahifalar o'chirilmaydi.
- `Design/LobbyV2` Git'da turibdi - o'chirmang (pull qilganda foydalanuvchi diskidan o'chib ketadi).
