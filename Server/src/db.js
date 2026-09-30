import { randomInt } from 'node:crypto';
import { DatabaseSync } from 'node:sqlite';
import { createGroupStore } from './groups.js';
import { createWalletStore } from './wallet.js';
import { createChatStore } from './chat.js';

/** Oxirgi heartbeat'dan keyin o'yinchi shuncha vaqt onlayn hisoblanadi. */
export const ONLINE_WINDOW_MS = 90_000;
/** Bitta heartbeat onlayn vaqtga ko'pi bilan shuncha soniya qo'shadi. */
export const MAX_HEARTBEAT_SECONDS = 60;
/**
 * Kutilayotgan do'stlik so'rovlari: o'yinchi bir vaqtda ko'pi bilan shuncha chiquvchi so'rov ushlab tura oladi,
 * GET /api/friends ham kiruvchi va chiquvchi ro'yxatlarda ko'pi bilan shunchasini qaytaradi.
 */
export const MAX_PENDING_REQUESTS = 100;

const PUBLIC_ID_MIN = 100_000;
const PUBLIC_ID_MAX = 999_999;

// Keyin qo'shilgan ustunlar: yangi bazada CREATE TABLE bilan, eski bazada ALTER TABLE bilan yaratiladi.
// NOT NULL ustunlarning DEFAULT qiymati eski qatorlarni ham to'ldiradi.
const ADDED_COLUMNS = [
  ['avatar_id', "TEXT NOT NULL DEFAULT ''"],
  ['public_id', 'INTEGER'],
  ['outfit', "TEXT NOT NULL DEFAULT ''"],
  ['country', "TEXT NOT NULL DEFAULT 'UZ'"],
  ['show_online', 'INTEGER NOT NULL DEFAULT 1'],
  ['allow_requests', 'INTEGER NOT NULL DEFAULT 1'],
  ['last_seen', 'TEXT'],
  ['online_seconds', 'INTEGER NOT NULL DEFAULT 0'],
];

// O'yinchining o'zi ko'radigan profili (GET /api/players/me)
const PROFILE_COLUMNS = `
  id, public_id AS publicId, nickname, gender, avatar_id AS avatarId, outfit, country,
  show_online AS showOnline, allow_requests AS allowRequests, created_at AS createdAt, online_seconds AS onlineSeconds`;

// Boshqa o'yinchining ommaviy ko'rinishi. Onlayn: :cutoff dan keyin ko'ringan va buni yashirmagan.
const SUMMARY_COLUMNS = `
  p.nickname, p.public_id AS publicId, p.avatar_id AS avatarId, p.gender,
  COALESCE(p.show_online = 1 AND p.last_seen >= :cutoff, 0) AS online`;

// Ommaviy ko'rinish va chaqiruvchiga (:me) nisbatan do'stlik holati.
// Juftlik noyob, shuning uchun JOIN har bir o'yinchi uchun ko'pi bilan bitta qator beradi.
const SUMMARY_SELECT = `
  SELECT ${SUMMARY_COLUMNS},
         CASE WHEN f.status = 'accepted' THEN 'friends'
              WHEN f.requester_id = :me THEN 'outgoing'
              WHEN f.requester_id IS NOT NULL THEN 'incoming'
              ELSE 'none' END AS friendship
  FROM players p
  LEFT JOIN friendships f
    ON (f.requester_id = :me AND f.addressee_id = p.id) OR (f.addressee_id = :me AND f.requester_id = p.id)`;

// Boshqalar uchun tartiblashda ishlatiladigan last_seen: onlayn holatini yashirgan o'yinchida NULL,
// aks holda uning ro'yxatdagi o'rni ham yaqinda kirganini oshkor qilardi.
const VISIBLE_LAST_SEEN = '(CASE WHEN show_online = 1 THEN last_seen END)';

/**
 * O'yinchilar bazasi (SQLite). nickname_key ustunidagi UNIQUE cheklov bir xil nickname'ni
 * ikki marta olishning oldini oladi, hatto so'rovlar bir vaqtda kelsa ham.
 * Vaqtlar ISO UTC satr ko'rinishida saqlanadi: ularni matn sifatida taqqoslash vaqt tartibini beradi.
 * @param {string} path - fayl yo'li yoki ':memory:'. Standart joy yo'q: u config.js resolveDbPath() da aniqlanadi
 *                        (ilgari joriy papkaga nisbatan edi va har bir build papkasi o'z bazasini ochardi).
 */
export function openDatabase(path) {
  const db = new DatabaseSync(path);
  db.exec(`
    PRAGMA journal_mode = WAL;
    PRAGMA foreign_keys = ON;
    CREATE TABLE IF NOT EXISTS players (
      id            TEXT PRIMARY KEY,
      nickname      TEXT NOT NULL,
      nickname_key  TEXT NOT NULL UNIQUE,
      gender        TEXT NOT NULL CHECK (gender IN ('male', 'female')),
      token_hash    TEXT NOT NULL,
      created_at    TEXT NOT NULL,
      ${ADDED_COLUMNS.map(([name, type]) => `${name} ${type}`).join(',\n      ')}
    );
  `);

  // Eski bazalarga yetishmayotgan ustunlarni qo'shamiz
  const columns = db.prepare('PRAGMA table_info(players)').all().map(c => c.name);
  for (const [name, type] of ADDED_COLUMNS) {
    if (!columns.includes(name)) db.exec(`ALTER TABLE players ADD COLUMN ${name} ${type}`);
  }

  db.exec(`
    CREATE UNIQUE INDEX IF NOT EXISTS players_public_id ON players (public_id);
    CREATE INDEX IF NOT EXISTS players_token_hash ON players (token_hash);
    CREATE INDEX IF NOT EXISTS players_last_seen ON players (last_seen);
    CREATE INDEX IF NOT EXISTS players_visible_last_seen ON players (${VISIBLE_LAST_SEEN} DESC, created_at DESC);
    CREATE INDEX IF NOT EXISTS players_online_seconds ON players (online_seconds DESC, created_at);

    CREATE TABLE IF NOT EXISTS friendships (
      requester_id  TEXT NOT NULL REFERENCES players (id),
      addressee_id  TEXT NOT NULL REFERENCES players (id),
      status        TEXT NOT NULL CHECK (status IN ('pending', 'accepted')),
      created_at    TEXT NOT NULL,
      PRIMARY KEY (requester_id, addressee_id),
      CHECK (requester_id <> addressee_id)
    );
    -- Juftlik yo'nalishidan qat'i nazar noyob: A->B bor bo'lsa, B->A yozuvini yaratib bo'lmaydi
    CREATE UNIQUE INDEX IF NOT EXISTS friendships_pair
      ON friendships (min(requester_id, addressee_id), max(requester_id, addressee_id));
    CREATE INDEX IF NOT EXISTS friendships_addressee ON friendships (addressee_id);
  `);

  const publicIdTaken = db.prepare('SELECT 1 FROM players WHERE public_id = ?');
  const setPublicId = db.prepare('UPDATE players SET public_id = ? WHERE id = ?');

  /** Hali hech kimga berilmagan tasodifiy 6 xonali raqam. */
  function freePublicId() {
    for (let attempt = 0; attempt < 1000; attempt++) {
      const publicId = randomInt(PUBLIC_ID_MIN, PUBLIC_ID_MAX + 1);
      if (publicIdTaken.get(publicId) === undefined) return publicId;
    }
    throw new Error("Bo'sh publicId topilmadi");
  }

  // publicId'siz eski o'yinchilarga raqam beramiz
  const withoutPublicId = db.prepare('SELECT id FROM players WHERE public_id IS NULL').all();
  if (withoutPublicId.length > 0) {
    transaction(db, () => {
      for (const { id } of withoutPublicId) setPublicId.run(freePublicId(), id);
    });
  }

  const findByKey = db.prepare('SELECT id, allow_requests AS allowRequests FROM players WHERE nickname_key = ?');
  const insert = db.prepare(`
    INSERT INTO players (id, public_id, nickname, nickname_key, gender, avatar_id, token_hash, created_at)
    VALUES (?, ?, ?, ?, ?, ?, ?, ?)`);
  const findByToken = db.prepare(`SELECT ${PROFILE_COLUMNS} FROM players WHERE token_hash = ?`);
  const findById = db.prepare(`SELECT ${PROFILE_COLUMNS} FROM players WHERE id = ?`);
  // null - o'zgarmaydigan maydon
  const updateProfile = db.prepare(`
    UPDATE players SET
      avatar_id      = COALESCE(:avatarId, avatar_id),
      outfit         = COALESCE(:outfit, outfit),
      country        = COALESCE(:country, country),
      show_online    = COALESCE(:showOnline, show_online),
      allow_requests = COALESCE(:allowRequests, allow_requests)
    WHERE id = :id`);
  const getLastSeen = db.prepare('SELECT last_seen AS lastSeen FROM players WHERE id = ?');
  const setPresence = db.prepare('UPDATE players SET last_seen = ?, online_seconds = online_seconds + ? WHERE id = ?');
  const countPlayers = db.prepare('SELECT COUNT(*) AS n FROM players');
  const countOnline = db.prepare('SELECT COUNT(*) AS n FROM players WHERE last_seen >= ?'); // indeks oralig'i

  // Qidiruv ikki bosqichda, ikkalasi ham nickname_key indeksi bo'ylab va :limit ta topilganda to'xtaydi:
  // avval shu bilan boshlanadiganlar (indeks oralig'i), keyin ichida bor boshqalari. Kalitlar ASCII,
  // shuning uchun :query bilan boshlanadigan har bir kalit [:query, :query || U+FFFF) oralig'ida.
  const prefixKeys = db.prepare(`
    SELECT nickname_key AS key FROM players
    WHERE nickname_key >= :query AND nickname_key < :query || char(65535) AND instr(nickname_key, :query) = 1
    ORDER BY nickname_key
    LIMIT :limit`);
  const infixKeys = db.prepare(`
    SELECT nickname_key AS key FROM players
    WHERE instr(nickname_key, :query) > 1
    ORDER BY nickname_key
    LIMIT :limit`);
  const summaryByKey = db.prepare(`${SUMMARY_SELECT} WHERE p.nickname_key = :key AND p.id <> :me`);
  // Ommaviy raqamli ID bo'yicha aniq qidiruv (chaqiruvchining o'zi chiqmaydi)
  const summaryByPublicId = db.prepare(`${SUMMARY_SELECT} WHERE p.public_id = :publicId AND p.id <> :me`);
  const friendsOf = db.prepare(`${SUMMARY_SELECT}
    WHERE f.status = 'accepted'
    ORDER BY online DESC, p.nickname_key`);
  const incomingOf = db.prepare(`
    SELECT ${SUMMARY_COLUMNS}, 'incoming' AS friendship
    FROM friendships f JOIN players p ON p.id = f.requester_id
    WHERE f.addressee_id = :me AND f.status = 'pending'
    ORDER BY online DESC, p.nickname_key
    LIMIT :limit`);
  const outgoingOf = db.prepare(`
    SELECT ${SUMMARY_COLUMNS}, 'outgoing' AS friendship
    FROM friendships f JOIN players p ON p.id = f.addressee_id
    WHERE f.requester_id = :me AND f.status = 'pending'
    ORDER BY online DESC, p.nickname_key
    LIMIT :limit`);
  const countOutgoing = db.prepare(
    "SELECT COUNT(*) AS n FROM friendships WHERE requester_id = ? AND status = 'pending'");

  const findRelation = db.prepare(`
    SELECT requester_id AS requesterId, status FROM friendships
    WHERE (requester_id = :a AND addressee_id = :b) OR (requester_id = :b AND addressee_id = :a)`);
  const insertRequest = db.prepare(
    "INSERT INTO friendships (requester_id, addressee_id, status, created_at) VALUES (?, ?, 'pending', ?)");
  const acceptRequest = db.prepare(
    "UPDATE friendships SET status = 'accepted' WHERE requester_id = ? AND addressee_id = ? AND status = 'pending'");
  const deleteRelation = db.prepare(`
    DELETE FROM friendships
    WHERE (requester_id = :a AND addressee_id = :b) OR (requester_id = :b AND addressee_id = :a)`);

  // Onlayn holatini yashirganlar reytingda yo'q: ochiq ro'yxatdagi daqiqalari o'sishi ular hozir
  // o'yinda ekanini ko'rsatib qo'yardi.
  const topOnline = db.prepare(`
    SELECT nickname, avatar_id AS avatarId, gender, online_seconds AS onlineSeconds FROM players
    WHERE show_online = 1
    ORDER BY online_seconds DESC, created_at ASC
    LIMIT ?`);

  const cutoff = now => new Date(now.getTime() - ONLINE_WINDOW_MS).toISOString();
  const summaries = (statement, params) => statement.all(params).map(toSummary);

  const groups = createGroupStore(db, cutoff);
  const wallet = createWalletStore(db);
  const chat = createChatStore(db);

  return {
    /** Guruhlar jadvallari va so'rovlari (groups.js). */
    groups,
    wallet,
    chat,
    /** Token xeshi bo'yicha o'yinchi profili (topilmasa undefined). */
    findPlayerByTokenHash(tokenHash) {
      return toProfile(findByToken.get(tokenHash));
    },
    /** id bo'yicha o'yinchi profili (topilmasa undefined). */
    getPlayer(id) {
      return toProfile(findById.get(id));
    },
    /** Nickname kaliti bo'yicha { id, allowRequests } (topilmasa undefined). */
    findPlayerByNicknameKey(key) {
      const row = findByKey.get(key);
      return row && { id: row.id, allowRequests: row.allowRequests === 1 };
    },
    isNicknameTaken(key) {
      return findByKey.get(key) !== undefined;
    },
    /** @returns {number | null} berilgan publicId; null: nickname band (UNIQUE cheklov ishladi) */
    insertPlayer(player) {
      const publicId = freePublicId();
      try {
        insert.run(player.id, publicId, player.nickname, player.nicknameKey, player.gender, player.avatarId,
          player.tokenHash, player.createdAt);
        return publicId;
      } catch (err) {
        if (String(err.message).includes('UNIQUE constraint failed: players.nickname_key')) return null;
        throw err;
      }
    },
    /**
     * Profilning berilgan maydonlarini yangilaydi (qiymatlar oldindan tekshirilgan bo'lishi kerak).
     * @param {{ avatarId?: string, outfit?: string, country?: string, showOnline?: boolean,
     *          allowRequests?: boolean }} changes
     */
    updatePlayer(id, changes) {
      const flag = value => (typeof value === 'boolean' ? Number(value) : null);
      return updateProfile.run({
        id,
        avatarId: changes.avatarId ?? null,
        outfit: changes.outfit ?? null,
        country: changes.country ?? null,
        showOnline: flag(changes.showOnline),
        allowRequests: flag(changes.allowRequests),
      }).changes > 0;
    },
    /**
     * Heartbeat: last_seen = now. Oldingi heartbeat onlayn oynasi ichida bo'lsa, oradagi vaqt
     * (ko'pi bilan MAX_HEARTBEAT_SECONDS) onlayn vaqtga qo'shiladi. Oradan o'tgan butun soniya chegaralari
     * sanaladi: soniya qoldig'i keyingi heartbeat'ga o'tadi, shuning uchun tez-tez yuborilgan heartbeat'lar
     * haqiqatda o'tgan vaqtdan ko'p bermaydi (yaxlitlash har 0.5 soniyada 1 soniya berardi).
     */
    touchPresence(id, now) {
      const previous = getLastSeen.get(id)?.lastSeen;
      const previousMs = previous ? Date.parse(previous) : NaN;
      const elapsedMs = now.getTime() - previousMs;
      const seconds = elapsedMs >= 0 && elapsedMs <= ONLINE_WINDOW_MS
        ? Math.min(Math.floor(now.getTime() / 1000) - Math.floor(previousMs / 1000), MAX_HEARTBEAT_SECONDS)
        : 0;
      setPresence.run(now.toISOString(), seconds, id);
    },
    /** { online, players }: onlayn oynasida ko'ringanlar va jami o'yinchilar. */
    stats(now) {
      return { online: countOnline.get(cutoff(now)).n, players: countPlayers.get().n };
    },
    /** Nickname kalitida `query` bor o'yinchilar: avval shu bilan boshlanadiganlar, keyin qolganlari. */
    searchPlayers(me, query, now, limit = 20) {
      // Chaqiruvchining o'zi ham topilishi mumkin: bitta ortiq olib, keyin uni chiqarib tashlaymiz
      const keys = prefixKeys.all({ query, limit: limit + 1 }).map(row => row.key);
      if (keys.length <= limit) {
        keys.push(...infixKeys.all({ query, limit: limit + 1 - keys.length }).map(row => row.key));
      }
      const params = { me, cutoff: cutoff(now) };
      return keys
        .map(key => summaryByKey.get({ ...params, key }))
        .filter(row => row !== undefined)
        .slice(0, limit)
        .map(toSummary);
    },
    /** Ommaviy ID (6 xonali) bo'yicha: [summary] yoki [] (tavsiya etilganlar ro'yxati olib tashlangan). */
    findByPublicId(me, publicId, now) {
      const row = summaryByPublicId.get({ me, publicId, cutoff: cutoff(now) });
      return row ? [toSummary(row)] : [];
    },
    /**
     * { friends, incoming, outgoing }: har biri avval onlaynlar, keyin nickname bo'yicha.
     * Kiruvchi va chiquvchi so'rovlar ko'pi bilan `pendingLimit` ta.
     */
    listFriends(me, now, pendingLimit = MAX_PENDING_REQUESTS) {
      const params = { me, cutoff: cutoff(now) };
      return {
        friends: summaries(friendsOf, params),
        incoming: summaries(incomingOf, { ...params, limit: pendingLimit }),
        outgoing: summaries(outgoingOf, { ...params, limit: pendingLimit }),
      };
    },
    /** `me` yuborgan va hali javob berilmagan so'rovlar soni. */
    pendingOutgoingCount(me) {
      return countOutgoing.get(me).n;
    },
    /** `me` nuqtai nazaridan munosabat: 'none' | 'friends' | 'outgoing' | 'incoming'. */
    friendship(me, other) {
      const row = findRelation.get({ a: me, b: other });
      if (!row) return 'none';
      if (row.status === 'accepted') return 'friends';
      return row.requesterId === me ? 'outgoing' : 'incoming';
    },
    addFriendRequest(from, to, now) {
      insertRequest.run(from, to, now.toISOString());
    },
    /** @returns {boolean} false: from -> to kutilayotgan so'rov yo'q */
    acceptFriendRequest(from, to) {
      return acceptRequest.run(from, to).changes > 0;
    },
    /** Ikki o'yinchi orasidagi har qanday munosabatni o'chiradi (rad etish, bekor qilish, do'stlikdan chiqarish). */
    removeFriendship(a, b) {
      return deleteRelation.run({ a, b }).changes > 0;
    },
    /** Eng ko'p onlayn bo'lganlar (yashirganlarsiz): [{ rank, nickname, avatarId, gender, minutes }]. */
    leaderboard(limit) {
      return topOnline.all(limit).map((row, index) => ({
        rank: index + 1,
        nickname: row.nickname,
        avatarId: row.avatarId,
        gender: row.gender,
        minutes: Math.floor(row.onlineSeconds / 60),
      }));
    },
    close() {
      db.close();
    },
  };
}

function transaction(db, work) {
  db.exec('BEGIN');
  try {
    work();
    db.exec('COMMIT');
  } catch (err) {
    db.exec('ROLLBACK');
    throw err;
  }
}

function toProfile(row) {
  if (!row) return undefined;
  return {
    id: row.id,
    publicId: row.publicId,
    nickname: row.nickname,
    gender: row.gender,
    avatarId: row.avatarId,
    outfit: row.outfit,
    country: row.country,
    showOnline: row.showOnline === 1,
    allowRequests: row.allowRequests === 1,
    createdAt: row.createdAt,
    onlineSeconds: row.onlineSeconds,
  };
}

function toSummary(row) {
  return {
    nickname: row.nickname,
    publicId: row.publicId,
    avatarId: row.avatarId,
    gender: row.gender,
    online: row.online === 1,
    friendship: row.friendship,
  };
}
