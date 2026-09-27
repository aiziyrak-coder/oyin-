# NewWorld: real dunyo materiallari

Manba: **Poly Haven** — fotografik PBR materiallar va haqiqiy HDR osmon. Bu fayllar generativ AI bilan yaratilmagan. Ular rasmiy manbadan o'zgartirmasdan yuklangan; faqat loyiha ichidagi fayl nomi soddalashtirilgan.

Yuklangan sana: 2026-09-28. Barcha 13 faylning MD5 qiymati Poly Haven `/files/{id}` API javobi bilan solishtirildi va mos keldi. Jami: 56 722 406 bayt (54.09 MiB).

## Litsenziya va API

- Aktivlar: [Poly Haven CC0 litsenziyasi](https://polyhaven.com/license); [CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/).
- CC0 tijoriy foydalanish, o'zgartirish va qayta tarqatishga ruxsat beradi. Aktivlar uchun attribution majburiy emas; mualliflarni quyida saqladik.
- [Rasmiy API shartlari va hujjati](https://polyhaven.com/our-api) tekshirildi. So'rovlar `NewWorldAssetPreparation/1.0 (local CC0 asset integration)` User-Agent bilan yuborildi. Tayyor o'yin Poly Haven live API'siga bog'lanmaydi; aktivlar lokal asset sifatida ishlatiladi.

## Aktivlar

| Loyiha prefiksi | Asl aktiv | Muallif | Asl fizik tile |
| --- | --- | --- | --- |
| `Ground_` | [Grass Ground](https://polyhaven.com/a/grass_ground) | Charlotte Baglioni | taxminan 2.51 × 2.51 m |
| `Concrete_` | [Concrete Floor Worn 02](https://polyhaven.com/a/concrete_floor_worn_02) | Dimitrios Savva | 2 × 2 m |
| `Rock_` | [Rock 01](https://polyhaven.com/a/rock_01) | Rob Tuytel | 1.5 × 1.5 m |
| `Sky.hdr` | [Kloofendal 48d Partly Cloudy (Pure Sky)](https://polyhaven.com/a/kloofendal_48d_partly_cloudy_puresky) | Greg Zaal (asl HDRI), Jarod Guest (osmonni ajratish) | 360° panorama |

Har bir materialda 2048 × 2048 px to'rtta JPG bor: sRGB Diffuse, OpenGL Normal, linear Roughness va linear AO. Normal xarita rang rasmi sifatida emas, `NormalMap` sifatida import qilinadi. Roughness silliqlik emas: shader uchun smoothness kerak bo'lsa `1 - roughness` ishlatiladi. Metallic qiymat 0; bu materiallar metall emas. Tile fizik o'lchamga yaqin bo'lishi kerak, aks holda o't va tosh donalari kattalashib ketadi.

`Sky.hdr` haqiqiy Radiance `32-bit_rle_rgbe` formatida, 4096 × 2048 px. 8-bit JPG fon emas. API metama'lumoti: 21 EV va 5400 K white balance. Faylni o'qib o'lchangan eng yorqin piksel (2437, 479): quyosh balandligi taxminan 47.86°, equirectangular longitude 34.23° (-180°..180°). Bu longitude Unity skybox yaw'i bilan avtomatik bir xil emas; skybox aylanishi va directional light soyasi bir yo'nalishda moslanadi. Exposure 1 boshlang'ich tavsiya, keyingi tasvirga qarab tekshiriladi.

## Fayllar va tekshiruv qiymatlari

| Lokal fayl | Bayt | SHA-256 |
| --- | ---: | --- |
| `Ground_Diffuse.jpg` | 3787330 | `4272BDDC71A7FA659D1DEEB13A9DF8C42E68A5AC8CDE7523187E4A081E028258` |
| `Ground_Normal.jpg` | 5968024 | `EFFA95C06AF273C974476D9A74A234668A1C9292F60FFFB4FA40348D638EE7F3` |
| `Ground_Rough.jpg` | 1525000 | `AFAED95537E894B8054DB38C81712A6D9504B05B560274526621D91D5A95FBE7` |
| `Ground_AO.jpg` | 4064520 | `AE4739457175AB2ADD4421A8AE803C7EF5005C946F942828D2ADF7ABE9843850` |
| `Concrete_Diffuse.jpg` | 2832505 | `78B3A3431D2803508CBCB683195C31D74AF26D895CC17F708007F6B46C52B740` |
| `Concrete_Normal.jpg` | 4470431 | `1D301D3A42248A7EFDB4A5507D2A364847C418BDD56652BC36086EDD98579C95` |
| `Concrete_Rough.jpg` | 1015940 | `10285F502FD154D40EC31E79D3BC2D95C0AF807B46C48FDBAACBCE572A763A60` |
| `Concrete_AO.jpg` | 2807144 | `70CF27DC39823A90D95EF9BC3B5EE5286309BE57F8458F3494C99A19815299C2` |
| `Rock_Diffuse.jpg` | 3020379 | `FE9013368539B5604602E64BFA48BEF1A2A4C452FCB6894868FB556D0B14501C` |
| `Rock_Normal.jpg` | 2989093 | `70427A2C81F85C0E1EE4A147E273D028C3D0188BAD2583F2509D6DF9A3D55B89` |
| `Rock_Rough.jpg` | 1772336 | `FAD2E7353B9AF032A36D394D33B23154EC7BCE3371F9E9436CF318C5F863D64D` |
| `Rock_AO.jpg` | 1794847 | `A569590B9CA4B448BBAAC0CC2B6C4D52E3012D4E81B0C5793D0034A5A621D663` |
| `Sky.hdr` | 20674857 | `3061C00A16ECAE748E84FD6C44C04804F539C118BB66325DB858BAD87B11BF88` |

## To'g'ridan-to'g'ri asl fayl manzillari

- [Ground Diffuse](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/grass_ground/grass_ground_diff_2k.jpg)
- [Ground Normal GL](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/grass_ground/grass_ground_nor_gl_2k.jpg)
- [Ground Roughness](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/grass_ground/grass_ground_rough_2k.jpg)
- [Ground AO](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/grass_ground/grass_ground_ao_2k.jpg)
- [Concrete Diffuse](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/concrete_floor_worn_02/concrete_floor_worn_02_diff_2k.jpg)
- [Concrete Normal GL](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/concrete_floor_worn_02/concrete_floor_worn_02_nor_gl_2k.jpg)
- [Concrete Roughness](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/concrete_floor_worn_02/concrete_floor_worn_02_rough_2k.jpg)
- [Concrete AO](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/concrete_floor_worn_02/concrete_floor_worn_02_ao_2k.jpg)
- [Rock Diffuse](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/rock_01/rock_01_diff_2k.jpg)
- [Rock Normal GL](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/rock_01/rock_01_nor_gl_2k.jpg)
- [Rock Roughness](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/rock_01/rock_01_rough_2k.jpg)
- [Rock AO](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/rock_01/rock_01_ao_2k.jpg)
- [4K Radiance HDR osmon](https://dl.polyhaven.org/file/ph-assets/HDRIs/hdr/4k/kloofendal_48d_partly_cloudy_puresky_4k.hdr)

API fayl ro'yxatlari: [ground](https://api.polyhaven.com/files/grass_ground), [concrete](https://api.polyhaven.com/files/concrete_floor_worn_02), [rock](https://api.polyhaven.com/files/rock_01), [sky](https://api.polyhaven.com/files/kloofendal_48d_partly_cloudy_puresky).

## Qo'shimcha skanerlangan tosh

`ScannedRock/` ichidagi haqiqiy FBX tosh va uning o'ziga mos UV atlaslari: [Boulder 01 manbalari va import yo'riqnomasi](ScannedRock/SOURCES.md). Bu qo'shimcha 18 131 956 bayt yuqoridagi dastlabki 13 fayl jami hisobiga kiritilmagan.
