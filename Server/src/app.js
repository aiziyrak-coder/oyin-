import { createHash, randomBytes, randomUUID } from 'node:crypto';
import { GENDERS, isValidAvatar, nicknameKey, validateNickname } from './nickname.js';

const MAX_BODY_BYTES = 4 * 1024;

/**
 * HTTP so'rovlarini qayta ishlovchi funksiyani yaratadi.
 *
 * GET  /health                                  -> { ok: true }
 * GET  /api/nicknames/availability?name=<nick>  -> { name, available, reason?, message? }
 * POST /api/players { nickname, gender, avatarId } -> 201 { id, nickname, gender, avatarId, token, createdAt }
 *                                                  409 { error: 'nickname_taken' }
 */
export function createApp(db, { rateLimits = { check: 60, create: 10 }, windowMs = 60_000 } = {}) {
  const limiter = createRateLimiter(windowMs);

  return async function handle(req, res) {
    setCors(res);
    if (req.method === 'OPTIONS') return send(res, 204);

    const url = new URL(req.url, 'http://localhost');
    const ip = req.socket.remoteAddress ?? 'unknown';

    try {
      if (req.method === 'GET' && url.pathname === '/health') {
        return send(res, 200, { ok: true });
      }

      if (req.method === 'GET' && url.pathname === '/api/nicknames/availability') {
        if (!limiter.allow(`check:${ip}`, rateLimits.check)) return send(res, 429, { error: 'too_many_requests' });
        const name = url.searchParams.get('name') ?? '';
        const check = validateNickname(name);
        if (!check.ok) return send(res, 200, { name, available: false, reason: check.reason, message: check.message });
        const taken = db.isNicknameTaken(nicknameKey(check.nickname));
        return send(res, 200, taken
          ? { name: check.nickname, available: false, reason: 'taken', message: 'This nickname is already taken.' }
          : { name: check.nickname, available: true });
      }

      if (req.method === 'POST' && url.pathname === '/api/players') {
        if (!limiter.allow(`create:${ip}`, rateLimits.create)) return send(res, 429, { error: 'too_many_requests' });
        const body = await readJson(req);
        if (body === null) return send(res, 400, { error: 'bad_json' });

        const check = validateNickname(body.nickname);
        if (!check.ok) return send(res, 400, { error: 'invalid_nickname', reason: check.reason, message: check.message });
        if (!GENDERS.includes(body.gender)) return send(res, 400, { error: 'invalid_gender' });
        if (!isValidAvatar(body.avatarId, body.gender)) return send(res, 400, { error: 'invalid_avatar' });

        // Token keyinchalik o'yinchini tanish uchun; bazada faqat uning xeshi saqlanadi
        const token = randomBytes(32).toString('hex');
        const player = {
          id: randomUUID(),
          nickname: check.nickname,
          nicknameKey: nicknameKey(check.nickname),
          gender: body.gender,
          avatarId: body.avatarId,
          tokenHash: createHash('sha256').update(token).digest('hex'),
          createdAt: new Date().toISOString(),
        };
        if (!db.insertPlayer(player)) {
          return send(res, 409, { error: 'nickname_taken', message: 'This nickname is already taken.' });
        }
        return send(res, 201, {
          id: player.id, nickname: player.nickname, gender: player.gender, avatarId: player.avatarId, token, createdAt: player.createdAt,
        });
      }

      return send(res, 404, { error: 'not_found' });
    } catch (err) {
      console.error(err);
      return send(res, 500, { error: 'server_error' });
    }
  };
}

function setCors(res) {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
}

function send(res, status, body) {
  if (body === undefined) {
    res.writeHead(status);
    return res.end();
  }
  const json = JSON.stringify(body);
  res.writeHead(status, { 'Content-Type': 'application/json; charset=utf-8', 'Content-Length': Buffer.byteLength(json) });
  res.end(json);
}

/** JSON tanani o'qiydi. Noto'g'ri yoki juda katta bo'lsa null qaytaradi. */
async function readJson(req) {
  let size = 0;
  const chunks = [];
  for await (const chunk of req) {
    size += chunk.length;
    if (size > MAX_BODY_BYTES) return null;
    chunks.push(chunk);
  }
  try {
    const value = JSON.parse(Buffer.concat(chunks).toString('utf8'));
    return value && typeof value === 'object' && !Array.isArray(value) ? value : null;
  } catch {
    return null;
  }
}

/** Oddiy xotiradagi cheklovchi: har bir kalit uchun oynada ko'pi bilan `limit` ta so'rov. */
function createRateLimiter(windowMs) {
  const hits = new Map();
  return {
    allow(key, limit) {
      const now = Date.now();
      const entry = hits.get(key);
      if (!entry || now - entry.start >= windowMs) {
        hits.set(key, { start: now, count: 1 });
        if (hits.size > 10_000) {
          for (const [k, v] of hits) if (now - v.start >= windowMs) hits.delete(k);
        }
        return true;
      }
      entry.count += 1;
      return entry.count <= limit;
    },
  };
}
