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

/** Avatar kodi: erkaklar M1–M5, ayollar F1–F5. Avatar o'yinchi jinsiga mos bo'lishi shart. */
export function isValidAvatar(avatarId, gender) {
  if (typeof avatarId !== 'string' || !/^[MF][1-5]$/.test(avatarId)) return false;
  return (gender === 'male' && avatarId[0] === 'M') || (gender === 'female' && avatarId[0] === 'F');
}
