// Nickname qoidalari. Unity'dagi NicknameRules.cs bilan bir xil bo'lishi kerak.

export const MIN_LENGTH = 3;
export const MAX_LENGTH = 16;

// Harf bilan boshlanadi, keyin harf, raqam yoki "_"
const PATTERN = /^[A-Za-z][A-Za-z0-9_]*$/;

// O'yinchilar ololmaydigan nomlar (katta-kichik harf farq qilmaydi)
const RESERVED = new Set([
  'admin', 'administrator', 'moderator', 'support', 'system', 'server',
  'cradev', 'cdcgroup', 'gamemaster', 'gm', 'null', 'undefined',
]);

/** Taqqoslash uchun kalit: "Ali" va "ali" bitta nickname hisoblanadi. */
export function nicknameKey(nickname) {
  return nickname.toLowerCase();
}

/**
 * Nickname'ni tekshiradi.
 * @returns {{ ok: true, nickname: string } | { ok: false, reason: 'invalid' | 'reserved', message: string }}
 */
export function validateNickname(raw) {
  if (typeof raw !== 'string') return { ok: false, reason: 'invalid', message: 'Nickname is required.' };
  const nickname = raw.trim();
  if (nickname.length < MIN_LENGTH || nickname.length > MAX_LENGTH) {
    return { ok: false, reason: 'invalid', message: `Use ${MIN_LENGTH}–${MAX_LENGTH} characters.` };
  }
  if (!PATTERN.test(nickname)) {
    return { ok: false, reason: 'invalid', message: 'Start with a letter. Use only letters, numbers and _.' };
  }
  if (RESERVED.has(nicknameKey(nickname))) {
    return { ok: false, reason: 'reserved', message: 'This nickname is reserved.' };
  }
  return { ok: true, nickname };
}

export const GENDERS = ['male', 'female'];

/**
 * O'yindagi avatarlar (CraDevSceneBuilder.AvatarList bilan bir xil).
 * Erkaklar 4 ta: M4 olib tashlangan, qolganlarining kodi o'zgarmagan.
 */
export const AVATARS = {
  male: ['M1', 'M2', 'M3', 'M5'],
  female: ['F1', 'F2', 'F3', 'F4', 'F5'],
};

/** Avatar mavjud va o'yinchi jinsiga mos bo'lishi shart. */
export function isValidAvatar(avatarId, gender) {
  return Object.hasOwn(AVATARS, gender) && AVATARS[gender].includes(avatarId);
}
