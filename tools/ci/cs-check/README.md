# C# kompilyatsiya tekshiruvi (Unity'siz, taxminiy)

`check.sh` loyihaning C# kodini Unity o'rnatmasdan va Unity litsenziyasisiz kompilyatsiya qilib ko'radi.
GitHub CI'da har push'da `C# kompilyatsiya` job'i shu skriptni ishga tushiradi (`.github/workflows/ci.yml`).

```
tools/ci/cs-check/check.sh            # repo ildizi avtomatik topiladi
tools/ci/cs-check/check.sh <repo>     # boshqa nusxani tekshirish
```

Kerak: .NET SDK 8 (`dotnet`), `curl`, `unzip` (yoki `python3`), birinchi ishga tushirishda internet.
Natija: `OK: ...` va chiqish kodi 0; xatoda 1 (kompilyatsiya xatolari ro'yxati) yoki 2 (muhit xatosi).

## Bu TAXMINIY tekshiruv

Haqiqiy Unity 6 kompilyatsiyasining o'rnini bosmaydi. Unity'da build (`tools\unity.cmd check` yoki CI'dagi
Unity job'lari) yagona ishonchli tekshiruv bo'lib qoladi. Farqlar:

- **Runtime kodi** (`Assets/CraDev`, `Editor` papkasisiz) Unity **2021.3** modullariga qarshi kompilyatsiya qilinadi
  (NuGet: `UnityEngine.Modules 2021.3.33`, `Unity3D.UnityEngine.UI 2020.3.21`), Unity 6 emas. Shuning uchun
  faqat Unity 6 da bor API (`Awaitable`, `Rigidbody.linearVelocity` va h.k.) bu yerda xato beradi: bunday API
  kerak bo'lsa, stubini `StubsUnity6.cs` ga yozing. Paket API'lari (`Unity.InferenceEngine`) `Stubs.cs` da
  qo'lda yozilgan stublar: yangi a'zo ishlatilsa, u yerga qo'shing.
- **Editor kodi** (`Assets/CraDev/Editor`) juda eski `UnityEditor.dll` (2018.1, NuGet `Unity3D.UnityEditor`) bilan
  kompilyatsiya qilinadi. Unda yo'q yangi API xatolari `edbase.txt` bazasida turadi va e'tiborga olinmaydi
  (qator raqamlari solishtirilmaydi). Faqat bazada yo'q **yangi** xatolar tekshiruvni yiqitadi.
- Define'lar: runtime uchun `ENABLE_LEGACY_INPUT_MANAGER`, `UNITY_STANDALONE_WIN`, `UNITY_6000_0_OR_NEWER`;
  `ENABLE_INPUT_SYSTEM` bo'laklari kompilyatsiya qilinmaydi.
- Shader, sahna, asset va Unity'ning o'z tekshiruvlari (serializatsiya, `asmdef`) umuman tekshirilmaydi.

Demak `OK` - "C# sintaksisi va asosiy turlar joyida" degani, "o'yin Unity'da yig'iladi" degani emas.

## DLL'lar

Unity DLL'lari repo'ga qo'shilmaydi. NuGet paketlari `dotnet restore` bilan odatdagidek yuklanadi.
`UnityEditor.dll` esa har ishga tushirishda keshdan olinadi, bo'lmasa
`https://api.nuget.org/v3-flatcontainer/unity3d.unityeditor/2018.1.6-f1/...nupkg` dan vaqtinchalik papkaga
yuklanadi va SHA-256 bilan tekshiriladi. Papkalarni muhit o'zgaruvchilari bilan o'zgartirish mumkin:
`CS_CHECK_WORK` (build papkasi, standart `/tmp/cradev-cs-check-<hash>`), `CS_CHECK_CACHE` (DLL keshi).

## Bazani yangilash

Editor kodiga eski `UnityEditor.dll` da yo'q, lekin Unity 6 da bor API qo'shilsa, tekshiruv uni yangi xato deb
ko'rsatadi. API haqiqatan Unity 6 da borligini tekshirib, bazani yangilang:

```
tools/ci/cs-check/check.sh --update-baseline
```

Bu joriy barcha Editor xatolarini `edbase.txt` ga yozadi, shuning uchun avval ro'yxatda haqiqiy xato yo'qligiga
ishonch hosil qiling va o'zgargan `edbase.txt` ni commit qiling.
