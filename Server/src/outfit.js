// O'yinchi kiyimi: "" (avatarning asl kiyimi) yoki Unity'dagi Outfit JSON'i (Wardrobe/Scripts/Outfit.cs, JsonUtility):
// { top, topColor, bottom, bottomColor, shoes, shoesColor, hair, hairColor }. Buyum - WardrobeCatalog id'si
// (masalan "top_denim"), rang - "RRGGBB"; har bir maydon ixtiyoriy, bo'sh satr - shu qism o'zgartirilmagan.

export const MAX_OUTFIT_LENGTH = 512;

const ITEM = /^([a-z][a-z0-9_]{0,31})?$/;
const COLOR = /^([0-9A-Fa-f]{6})?$/;
const FIELDS = new Map(['top', 'bottom', 'shoes', 'hair'].flatMap(slot => [[slot, ITEM], [`${slot}Color`, COLOR]]));

/**
 * Kiyim guruhdagi boshqa o'yinchilarga ham yuboriladi, shuning uchun faqat ma'lum maydonlar va ularning
 * qiymatlari qabul qilinadi (noma'lum kalit, satr bo'lmagan qiymat, uzun matn - yo'q).
 */
export function isValidOutfit(value) {
  if (typeof value !== 'string' || value.length > MAX_OUTFIT_LENGTH) return false;
  if (value === '') return true;
  let parsed;
  try {
    parsed = JSON.parse(value);
  } catch {
    return false;
  }
  if (parsed === null || typeof parsed !== 'object' || Array.isArray(parsed)) return false;
  return Object.entries(parsed).every(([key, item]) => FIELDS.has(key) && typeof item === 'string' && FIELDS.get(key).test(item));
}

/** Bazadagi kiyim boshqalarga yuborilishidan oldin: tekshiruv joriy qilinishidan oldin yozilgan noto'g'risi - "". */
export function publicOutfit(value) {
  return isValidOutfit(value) ? value : '';
}
