import { DatabaseSync } from 'node:sqlite';

/**
 * O'yinchilar bazasi (SQLite). nickname_key ustunidagi UNIQUE cheklov bir xil nickname'ni
 * ikki marta olishning oldini oladi, hatto so'rovlar bir vaqtda kelsa ham.
 */
export function openDatabase(path = 'data/cradev.db') {
  const db = new DatabaseSync(path);
  db.exec(`
    PRAGMA journal_mode = WAL;
    CREATE TABLE IF NOT EXISTS players (
      id            TEXT PRIMARY KEY,
      nickname      TEXT NOT NULL,
      nickname_key  TEXT NOT NULL UNIQUE,
      gender        TEXT NOT NULL CHECK (gender IN ('male', 'female')),
      avatar_id     TEXT NOT NULL DEFAULT '',
      token_hash    TEXT NOT NULL,
      created_at    TEXT NOT NULL
    );
  `);

  // Eski bazalarga yangi ustunni qo'shamiz
  const columns = db.prepare('PRAGMA table_info(players)').all().map(c => c.name);
  if (!columns.includes('avatar_id')) db.exec("ALTER TABLE players ADD COLUMN avatar_id TEXT NOT NULL DEFAULT ''");

  const findByKey = db.prepare('SELECT id FROM players WHERE nickname_key = ?');
  const insert = db.prepare(
    'INSERT INTO players (id, nickname, nickname_key, gender, avatar_id, token_hash, created_at) VALUES (?, ?, ?, ?, ?, ?, ?)');

  return {
    isNicknameTaken(key) {
      return findByKey.get(key) !== undefined;
    },
    /** @returns {boolean} false: nickname band (UNIQUE cheklov ishladi) */
    insertPlayer(player) {
      try {
        insert.run(player.id, player.nickname, player.nicknameKey, player.gender, player.avatarId, player.tokenHash, player.createdAt);
        return true;
      } catch (err) {
        if (String(err.message).includes('UNIQUE constraint failed')) return false;
        throw err;
      }
    },
    close() {
      db.close();
    },
  };
}
