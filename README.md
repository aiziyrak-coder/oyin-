# Humo Osmon Qoʻriqchisi

Brauzerda ishlaydigan vertikal kosmik oʻyin. Humo qushi qanotidagi kema bilan
osmonni dushmanlar toʻlqinidan himoya qiling. Har 5-bosqichda Ajdar (boss) keladi.

## Ishga tushirish

Hech qanday oʻrnatish kerak emas: `index.html` faylini brauzerda oching.
Yoki lokal server orqali:

```bash
python3 -m http.server 8000
# keyin http://localhost:8000 manziliga kiring
```

## Boshqaruv

| Amal              | Klaviatura              | Telefon                 |
|-------------------|-------------------------|-------------------------|
| Harakat           | ← ↑ → ↓ yoki W A S D    | Barmoq bilan suring     |
| Otish             | avtomatik               | avtomatik               |
| Boshlash / davom  | Space yoki Enter        | Ekranga bosing          |
| Pauza             | P yoki Esc              | ⏸ tugmasi               |
| Ovoz              | M                       | 🔊 tugmasi              |

## Dushmanlar

- **Qizil uchburchak** — oddiy, 100 ochko. 4-bosqichdan boshlab ba'zilari oʻq otadi.
- **Yashil romb** — ilonizi harakatlanadi, 2 zarbaga chidaydi, 150 ochko.
- **Binafsha olti burchak** — sekin, 5 zarbaga chidaydi, oʻq otadi, 300 ochko.
- **Ajdar** — har 5-bosqichda keladigan boss, uch xil hujum turi bor.

## Sovgʻalar

- **Uch oʻq** (sariq) — 10 soniya davomida uchta yoʻnalishda otadi.
- **Qalqon** (koʻk) — 12 soniya yoki bitta zarbagacha himoya qiladi.
- **Qoʻshimcha jon** (pushti) — +1 jon (maksimum 5 ta; toʻla boʻlsa +1 000 ochko).

Har bir bosqich tugaganda `bosqich × 250` bonus beriladi. Rekord brauzerda saqlanadi.

## Fayllar

- `index.html` — sahifa tuzilmasi
- `style.css` — dizayn
- `game.js` — butun oʻyin mantiqi (Canvas 2D + WebAudio, kutubxonalarsiz)
