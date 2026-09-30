import { parseGroupCode } from './groups.js';

export function createChatStore(sql) {
  sql.exec(`CREATE TABLE IF NOT EXISTS chat_messages (
    id INTEGER PRIMARY KEY AUTOINCREMENT, channel TEXT NOT NULL,
    author_id TEXT NOT NULL REFERENCES players(id), client_id TEXT NOT NULL,
    text TEXT NOT NULL, created_at TEXT NOT NULL, UNIQUE(author_id, client_id)
  ); CREATE INDEX IF NOT EXISTS chat_channel ON chat_messages(channel, id);`);
  const target = sql.prepare('SELECT id FROM players WHERE public_id = ?');
  const group = sql.prepare(`SELECT g.id, m.joined_at FROM social_groups g JOIN group_members m ON m.group_id=g.id
    WHERE g.code=? AND m.player_id=? AND (m.expires_at IS NULL OR m.expires_at > ?)`);
  const existing = sql.prepare('SELECT id, channel, text FROM chat_messages WHERE author_id=? AND client_id=?');
  const insert = sql.prepare('INSERT INTO chat_messages(channel, author_id, client_id, text, created_at) VALUES (?,?,?,?,?)');
  const columns = 'm.id, p.nickname, p.public_id AS publicId, m.text, m.created_at AS createdAt';
  const latest = sql.prepare(`SELECT ${columns} FROM chat_messages m JOIN players p ON p.id=m.author_id
    WHERE m.channel=? AND m.created_at >= ? AND m.id < ? ORDER BY m.id DESC LIMIT 51`);
  const newer = sql.prepare(`SELECT ${columns} FROM chat_messages m JOIN players p ON p.id=m.author_id
    WHERE m.channel=? AND m.created_at >= ? AND m.id > ? ORDER BY m.id LIMIT 51`);
  return {
    target(id) { return target.get(id)?.id; },
    group(code, player, now) { return group.get(code, player, now.toISOString()); },
    existing(player, key) { return existing.get(player, key); },
    send(channel, player, key, text, now) { return Number(insert.run(channel, player, key, text, now.toISOString()).lastInsertRowid); },
    read(channel, since, after, before) {
      const rows = after > 0 ? newer.all(channel, since, after) : latest.all(channel, since, before || Number.MAX_SAFE_INTEGER);
      const hasMore = rows.length > 50;
      const items = rows.slice(0, 50); if (!after) items.reverse();
      return { items, hasMore };
    },
  };
}

export function chatRoutes(db) {
  function access(player, kind, target, now) {
    if (kind === 'direct') {
      if (typeof target !== 'string' || !/^\d{6}$/.test(target)) return null;
      const other = db.chat.target(Number(target));
      if (!other || other === player.id || db.friendship(player.id, other) !== 'friends') return null;
      return { channel: 'd:' + [player.id, other].sort().join(':'), since: '' };
    }
    if (kind === 'group') {
      const code = parseGroupCode(target);
      const group = code && db.chat.group(code, player.id, now);
      if (group) return { channel: 'g:' + group.id, since: group.joined_at };
    }
    return null;
  }
  return {
    'GET /api/chat/messages': ({ player, now, url }) => {
      const scope = access(player, url.searchParams.get('kind'), url.searchParams.get('target'), now);
      if (!scope) return [403, { error: 'chat_forbidden' }];
      const after = Number(url.searchParams.get('after') ?? 0), before = Number(url.searchParams.get('before') ?? 0);
      if (![after,before].every(n => Number.isSafeInteger(n) && n >= 0) || (after && before)) return [400, { error: 'invalid_cursor' }];
      return [200, db.chat.read(scope.channel, scope.since, after, before)];
    },
    'POST /api/chat/send': ({ player, now }, body) => {
      if (!body) return [400, { error: 'bad_json' }];
      const scope = access(player, body.kind, body.target, now);
      if (!scope) return [403, { error: 'chat_forbidden' }];
      if (typeof body.text !== 'string' || !body.text.trim() || body.text.length > 1000 || /[\u0000-\u0008\u000b-\u001f\u007f]/.test(body.text))
        return [400, { error: 'invalid_message' }];
      if (typeof body.clientId !== 'string' || !/^[a-zA-Z0-9_-]{16,80}$/.test(body.clientId)) return [400, { error: 'invalid_message_id' }];
      const text = body.text.trim();
      const prior = db.chat.existing(player.id, body.clientId);
      if (prior) return prior.channel === scope.channel && prior.text === text
        ? [200, { id: prior.id }] : [409, { error: 'message_conflict' }];
      return [201, { id: db.chat.send(scope.channel, player.id, body.clientId, text, now) }];
    },
  };
}
