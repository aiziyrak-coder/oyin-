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

## Shahar yo'li uchun asfalt — 2026-09-30

[Asphalt 02](https://polyhaven.com/a/asphalt_02), muallif **Rob Tuytel**, Poly Haven **CC0**. Asl fizik tile **3 × 3 metr**, fotografik mayda tosh donalari va tabiiy yoriqlar. 2048 × 2048 px to'rtta JPG rasmiy [fayllar API](https://api.polyhaven.com/files/asphalt_02) orqali o'zgartirmasdan yuklandi; [aktiv ma'lumoti](https://api.polyhaven.com/info/asphalt_02) bilan muallif, o'lcham va nom tasdiqlandi. API so'rovlari yuqoridagi `NewWorldAssetPreparation/1.0 (local CC0 asset integration)` User-Agent bilan bajarildi. Tayyor o'yin API'dan tekstura yuklamaydi.

Diffuse — sRGB; Normal — OpenGL normal-map; Rough va AO — linear. UV tile'ni 3 m atrofida qo'llang; asfalt metall emas. Bu qo'shimcha **12 289 570 bayt** dastlabki 13 fayl jami hisobiga kiritilmagan. Yuklangan fayllarning bayt hajmi va MD5'i API bilan mos, SHA-256 quyida. Rang xaritasi ko'z bilan ham tekshirildi; aktivni kattalashtirish yoki sun'iy detal qo'shish qilinmadi.

| Lokal fayl | Bayt | Asl MD5 | SHA-256 |
| --- | ---: | --- | --- |
| `Asphalt_Diffuse.jpg` | 3075676 | `336AF399FD98A39AB986D8B3BF73B4FF` | `28F5BA8690553F192C0E5E1A5FF40F34765B4B93F4CC7059B1A3E9C795B6C28C` |
| `Asphalt_Normal.jpg` | 4943950 | `77EBD1CC0B020CCAA1C6B58F103D1F75` | `FFE49DB71A0FD34C1E259625DF66A302A237597D6EF655307987EC4E45BC6F21` |
| `Asphalt_Rough.jpg` | 2230457 | `6FE669AB38640EF2009D6A28D1AD5EE9` | `B9A516B61B7040A9245AC206642D5F767AD9589A0169C4A1668221429E8A998C` |
| `Asphalt_AO.jpg` | 2039487 | `2BF61D77E68DA004EEA168B88C4C31B5` | `5FE1737CBBCD55F2FEC3632D6170762E5733F24BBC7A7816AFB22A23C710B11F` |

- [Asphalt Diffuse](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/asphalt_02/asphalt_02_diff_2k.jpg)
- [Asphalt Normal GL](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/asphalt_02/asphalt_02_nor_gl_2k.jpg)
- [Asphalt Roughness](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/asphalt_02/asphalt_02_rough_2k.jpg)
- [Asphalt AO](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/asphalt_02/asphalt_02_ao_2k.jpg)

## Shaxmat zali: marmar va yong'oq yog'ochi — 2026-09-30

- `CityFloor_`: [Marble 01](https://polyhaven.com/a/marble_01), **Rob Tuytel**, fizik tile **1.5 × 1.5 m**. Och krem-bej tabiiy marmar plitalari, mayda tomirlar va choklar; silliq ichki pol uchun. [Ma'lumot](https://api.polyhaven.com/info/marble_01), [fayllar](https://api.polyhaven.com/files/marble_01).
- `CityWood_`: [Black Walnut Veneer 02](https://polyhaven.com/a/black_walnut_veneer_02), **Jenelle van Heerden**, fizik tile **1 × 1 m**. Tabiiy yong'oq yog'ochining ingichka tolalari; shaxmat stoli va mebel uchun. Diffuse asl tabiiy jigarrang; to'q lak ko'rinishi materialning tint/smoothness sozlamalari bilan berilishi mumkin, manba fayl qayta bo'yalmagan. [Ma'lumot](https://api.polyhaven.com/info/black_walnut_veneer_02), [fayllar](https://api.polyhaven.com/files/black_walnut_veneer_02).

Ikkalasi ham rasmiy **Poly Haven CC0** aktivlaridir, generativ rasm emas. Har birida to'rtta **2048 × 2048** JPG: sRGB Diffuse, OpenGL NormalMap, linear Rough va AO. Ikki material ham metall emas. API yuqoridagi User-Agent bilan ishlatildi. Barcha sakkiz faylning MD5'i va bayt hajmi rasmiy API javobiga mos keldi; Diffuse xaritalari ko'z bilan tekshirildi. O'zgartirmasdan yuklangan jami **12 446 593 bayt**, dastlabki aktivlar jami hisobiga kiritilmagan.

| Lokal fayl | Bayt | Asl MD5 | SHA-256 |
| --- | ---: | --- | --- |
| `CityFloor_Diffuse.jpg` | 1269188 | `4EEEFEA16242CECB3B429AC0C8F88740` | `D403786171716F86718BDD67EBA923D4FB6125C0636BACEF0E6A21DD5D623A48` |
| `CityFloor_Normal.jpg` | 409581 | `F25EFB0B61EC7AC183B3B0F4D032ED17` | `D5E17CCB2913ADBF28FCB781FDDF0AA711259DDEA6C1C918442F9A3589AA4660` |
| `CityFloor_Rough.jpg` | 281388 | `C4CF0375D84277C6020BF230823EFDCC` | `1B970E033856C93EE7390D947DA5340E101B489FC8C1463354EA9E6655CE039A` |
| `CityFloor_AO.jpg` | 999921 | `1DE878F3C292637E3A101D81BFBFCA69` | `E0D535B591430F1EBFD8FFA38086949BB074AE470F6FEC7F3DCD55F594F52D4E` |
| `CityWood_Diffuse.jpg` | 2365710 | `E4EF13D171747D6C877BEC563B67D389` | `AC4CD64753A55684EFE4CCAF6F81B465E237A8A885FFFB95377CC371924C0F88` |
| `CityWood_Normal.jpg` | 1877332 | `F2AF23CE4886EDCEF6B1E7226183D445` | `125A46A42525157CB2CF277B712C9933A76518D5A729B43CAAFCE1AEBD96BBBB` |
| `CityWood_Rough.jpg` | 2670532 | `C1C2F3BFC6942B516499500C60200A9A` | `172832F6FA42E539E35B04F321C067B61962DA7AEBE106FCD7934B84AA5DC204` |
| `CityWood_AO.jpg` | 2572941 | `160F4F0A609808257F3715DD3D854105` | `FE27B499DC80CB22BF3B940C5E8937BE0F4ACE699ACFAD28E876C4453934E534` |

- [CityFloor Diffuse](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/marble_01/marble_01_diff_2k.jpg)
- [CityFloor Normal GL](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/marble_01/marble_01_nor_gl_2k.jpg)
- [CityFloor Roughness](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/marble_01/marble_01_rough_2k.jpg)
- [CityFloor AO](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/marble_01/marble_01_ao_2k.jpg)
- [CityWood Diffuse](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/black_walnut_veneer_02/black_walnut_veneer_02_diff_2k.jpg)
- [CityWood Normal GL](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/black_walnut_veneer_02/black_walnut_veneer_02_nor_gl_2k.jpg)
- [CityWood Roughness](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/black_walnut_veneer_02/black_walnut_veneer_02_rough_2k.jpg)
- [CityWood AO](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/black_walnut_veneer_02/black_walnut_veneer_02_ao_2k.jpg)
