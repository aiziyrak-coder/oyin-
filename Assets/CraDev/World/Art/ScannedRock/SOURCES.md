# Skanerlangan Boulder 01

[Boulder 01 — Poly Haven](https://polyhaven.com/a/boulder_01), muallif **Rico Cilliers**. Litsenziya: [CC0 1.0](https://polyhaven.com/license). 2026-09-28 kuni rasmiy API orqali olingan. Manba fayllar o'zgartirilmagan, faqat lokal nomlari soddalashtirilgan.

Aktiv Poly Haven'ning `verdant_trail` to'plamiga kiradi. [To'plamning rasmiy maqolasi](https://blog.polyhaven.com/verdant-trail/) tabiatda fotografik skanerlash orqali to'plangan toshlarni tasvirlaydi. Bu loyiha ichidagi prosedural sharga tekstura yopishtirish emas: asl skanerlangan shakl va shu shaklga mos UV atlas ishlatiladi.

## Import

- `Boulder.fbx`: Unity'ning native FBX importi; tashqi GLTF paketi kerak emas. Normallar va UV saqlansin, tangentlar import yoki MikkTSpace orqali hisoblansin.
- Fizik o'lchami taxminan 1.273 × 1.831 × 1.004 m; API o'qlari Blender tartibida. Unity importidan keyin renderer bounds orqali o'lcham va pastki nuqtani tekshiring.
- `Boulder_Diffuse.jpg`: sRGB; qolgan xaritalar linear. `Boulder_Normal.jpg`: OpenGL normal, `NormalMap` sifatida import qilinadi.
- Barcha xaritalar 2048 × 2048 px. Ular tile qilinadigan tosh materiallari emas, ushbu meshning UV atlaslari. `CraDev/WorldUVPBR` va tiling 1 × 1 ishlatilsin; world-space triplanar `WorldPBR` bu modelga mos emas.
- FBX'ning asl material havolalari normal/roughness EXR nomlariga ishora qiladi. Importdan so'ng quyidagi JPG xaritalarini materialga aniq ulang; EXR fayllari kerak emas va yuklanmagan.
- Roughness uchun smoothness = `1 - roughness`, metallic = 0. AO o'z xaritasidan olinadi.

## LOD va o'lcham

FBX 7400 binary strukturasi o'qib tekshirildi. To'rtta meshdagi triangulyatsiyalangan indekslar: **66 122**, **33 060**, **16 530**, **8 264** uchburchak. Model tugunlari `boulder_01_LOD0`..`boulder_01_LOD3`. API'dagi 123 976 uchburchak — to'rtta LOD yig'indisi, bitta eng yuqori mesh emas.

Ko'p nusxa uchun 16 530 uchburchakli mesh (LOD2) tavsiya qilinadi; juda yaqin ko'rinish uchun 33 060 (LOD1) qo'llash mumkin. Bir joyda barcha LOD'larni bir vaqtda chizish kerak emas. Pastki bounds yerga botirilmasdan moslanadi, tartibsiz aylantirish tabiiy turfalik beradi.

## Manbalar va tekshiruv

Barcha 5 fayl API MD5 bilan tekshirildi: mos. Jami **18 131 956 bayt** (17.29 MiB).

| Fayl | Bayt | SHA-256 |
| --- | ---: | --- |
| `Boulder.fbx` | 6482860 | `B1125CD1E08A425FA89BC62B96279B40E9A921618E12BFBBE0D575CB78F53C95` |
| `Boulder_Diffuse.jpg` | 3672483 | `90BBAA17C1FE0254D2B0B6E5148264FA834953A72724D566F14AF029EEBE3CA3` |
| `Boulder_Normal.jpg` | 4649207 | `5174B318712AC7725CBCB55D422607F9C66D7F8D035C5412AB897B5939E2DB6B` |
| `Boulder_Rough.jpg` | 1825938 | `D32E0EDCB01D7248138F118638A9C73AC1A612E3F84A464FCBFFFE899EE205C9` |
| `Boulder_AO.jpg` | 1501468 | `FDD41EAB852D005CC3772CD0F9F69000ABF2CF6C973C7E3C2BAB5AECEC8623B6` |

- [Asl FBX](https://dl.polyhaven.org/file/ph-assets/Models/fbx/2k/boulder_01/boulder_01_2k.fbx)
- [Asl Diffuse](https://dl.polyhaven.org/file/ph-assets/Models/jpg/2k/boulder_01/boulder_01_diff_2k.jpg)
- [Asl Normal GL](https://dl.polyhaven.org/file/ph-assets/Models/jpg/2k/boulder_01/boulder_01_nor_gl_2k.jpg)
- [Asl Roughness](https://dl.polyhaven.org/file/ph-assets/Models/jpg/2k/boulder_01/boulder_01_rough_2k.jpg)
- [Asl AO](https://dl.polyhaven.org/file/ph-assets/Models/jpg/2k/boulder_01/boulder_01_ao_2k.jpg)
- [API info](https://api.polyhaven.com/info/boulder_01) va [fayl ro'yxati](https://api.polyhaven.com/files/boulder_01).
