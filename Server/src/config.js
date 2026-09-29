import { existsSync, mkdirSync, renameSync, rmSync, statSync } from 'node:fs';
import { homedir } from 'node:os';
import path from 'node:path';
import { DatabaseSync } from 'node:sqlite';

/**
 * Server sozlamalari (muhit o'zgaruvchilari):
 *   PORT        - port (standart 8080)
 *   HOST        - qaysi manzilda tinglaydi. Standart: faqat shu kompyuter (127.0.0.1 va ::1), shunda o'yin yonidagi
 *                 server tarmoqdan ko'rinmaydi va Windows brandmauer oynasi chiqmaydi. Umumiy server: HOST=0.0.0.0
 *   DB_PATH     - baza fayli. Standart: foydalanuvchi papkasi (defaultDbPath), server qaysi papkadan ishga
 *                 tushirilganiga bog'liq emas
 *   TRUST_PROXY - server teskari proksi (HTTPS) ortida bo'lsa: ishonchli proksilar soni (1 yoki "true").
 *                 Shunda so'rovlar cheklovi proksi manzili emas, X-Forwarded-For'dagi o'yinchi manzili bo'yicha
 */
export function serverConfig(env = process.env) {
  const host = (env.HOST ?? '').trim();
  return {
    port: parsePort(env.PORT),
    // Birinchisi asosiy; qolganlari (IPv6 loopback) kompyuterda IPv6 bo'lmasa o'tkazib yuboriladi
    hosts: host ? [host] : ['127.0.0.1', '::1'],
    trustProxy: parseTrustProxy(env.TRUST_PROXY),
  };
}

function parsePort(value) {
  const port = Number(value ?? 8080);
  if (!Number.isInteger(port) || port < 0 || port > 65535) throw new Error(`PORT noto'g'ri: ${value}`);
  return port;
}

/** TRUST_PROXY: "1", "2", ... (proksilar soni) yoki "true"/"yes" (bitta); boshqa hamma narsa - 0 (ishonilmaydi). */
export function parseTrustProxy(value) {
  const text = String(value ?? '').trim().toLowerCase();
  if (/^\d+$/.test(text)) return Math.min(Number(text), 10);
  return text === 'true' || text === 'yes' || text === 'on' ? 1 : 0;
}

/**
 * Bazaning standart joyi - o'yinchining ma'lumotlar papkasi. Hamma build papkalari va dev skriptlari bitta bazani
 * ishlatadi (token esa PlayerPrefs'da, u ham hamma build'lar uchun bitta).
 *   Windows: %LOCALAPPDATA%\CraDev\server\cradev.db
 *   boshqalar: $XDG_DATA_HOME/cradev/cradev.db (standart ~/.local/share/cradev/cradev.db)
 */
export function defaultDbPath({ env = process.env, platform = process.platform, home = homedir() } = {}) {
  if (platform === 'win32') {
    const base = env.LOCALAPPDATA || path.win32.join(home, 'AppData', 'Local');
    return path.win32.join(base, 'CraDev', 'server', 'cradev.db');
  }
  const base = env.XDG_DATA_HOME || path.posix.join(home, '.local', 'share');
  return path.posix.join(base, 'cradev', 'cradev.db');
}

/**
 * Eski versiyalar bazani ishga tushirilgan papkaga nisbatan "data/cradev.db" da saqlagan: Play.cmd - o'yin papkasi
 * (CraDevServer.exe yonida), lobby.ps1 va unity.ps1 - Server/ papkasi.
 * @param script - process.argv[1] (node src/server.js bo'lsa Server/data ham tekshiriladi)
 */
export function legacyDbPaths({ cwd = process.cwd(), execPath = process.execPath, script = process.argv[1] } = {}) {
  const list = [path.resolve(cwd, 'data', 'cradev.db'), path.join(path.dirname(execPath), 'data', 'cradev.db')];
  if (script && /\.[cm]?js$/i.test(script)) list.push(path.resolve(path.dirname(script), '..', 'data', 'cradev.db'));
  return [...new Set(list)];
}

/**
 * Qaysi baza ochilishini aniqlaydi. DB_PATH berilgan bo'lsa - aynan u (CI va testlar), aks holda standart joy.
 * Standart joyda baza hali yo'q, lekin eski joyda bor bo'lsa, u ko'chiriladi: profillar (va o'yinchi tokenlari)
 * saqlanib qoladi. Eski fayl o'zgartirilmaydi (zaxira).
 * @returns {{ path: string, fromEnv: boolean, migratedFrom: string | null, otherLegacy: string[] }}
 */
export function resolveDbPath({ env = process.env, platform, home, legacy = legacyDbPaths() } = {}) {
  const custom = (env.DB_PATH ?? '').trim();
  if (custom) {
    return { path: custom === ':memory:' ? custom : path.resolve(custom), fromEnv: true, migratedFrom: null, otherLegacy: [] };
  }
  const target = defaultDbPath({ env, platform, home });
  mkdirSync(path.dirname(target), { recursive: true });
  return { path: target, fromEnv: false, ...migrateLegacyDatabase(target, legacy) };
}

/**
 * `target` yo'q bo'lsa, eski bazalardan eng oxirgi o'zgartirilganini unga ko'chiradi. VACUUM INTO WAL faylidagi
 * (server majburan to'xtatilganda asosiy faylga yozilmay qolgan) ma'lumotlarni ham oladi va yaxlit nusxa beradi.
 * Nusxa avval vaqtinchalik faylga yoziladi: jarayon yarmida to'xtasa, keyingi safar qayta uriniladi.
 * Ko'chirib bo'lmasa xato tashlanadi: bo'sh baza bilan ishga tushish hamma profillarni "yo'qotgan" bo'lardi.
 */
export function migrateLegacyDatabase(target, candidates) {
  if (existsSync(target)) return { migratedFrom: null, otherLegacy: [] };
  const found = [...new Set(candidates.map(p => path.resolve(p)))]
    .filter(p => p !== path.resolve(target) && isFile(p))
    .sort((a, b) => lastModified(b) - lastModified(a));
  if (found.length === 0) return { migratedFrom: null, otherLegacy: [] };

  const [source, ...others] = found;
  const temp = `${target}.migrating`;
  rmSync(temp, { force: true });
  let db;
  try {
    db = new DatabaseSync(source);
    db.prepare('VACUUM INTO ?').run(temp);
  } catch (err) {
    rmSync(temp, { force: true });
    throw new Error(`Eski bazani ko'chirib bo'lmadi (${source} -> ${target}): ${err.message}`);
  } finally {
    db?.close();
  }
  renameSync(temp, target);
  return { migratedFrom: source, otherLegacy: others };
}

function isFile(file) {
  try {
    return statSync(file).isFile();
  } catch {
    return false;
  }
}

/** Faylning (va uning WAL faylining) oxirgi o'zgarish vaqti. */
function lastModified(file) {
  let time = 0;
  for (const part of [file, `${file}-wal`]) {
    try {
      time = Math.max(time, statSync(part).mtimeMs);
    } catch {
      // WAL fayli bo'lmasligi mumkin
    }
  }
  return time;
}
