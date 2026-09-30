import { createHash, randomBytes, randomUUID } from 'node:crypto';
import { MAX_PENDING_REQUESTS } from './db.js';
import { createParties } from './party.js';
import { groupRoutes } from './groups.js';
import { walletRoutes } from './wallet.js';
import { chatRoutes } from './chat.js';
import { createWorld } from './world.js';
import { createChess } from './chess.js';
import { upcomingEvents } from './events.js';
import { AVATARS, GENDERS, isValidAvatar, MAX_LENGTH, nicknameKey, validateNickname } from './nickname.js';
import { isValidOutfit } from './outfit.js';

const MAX_BODY_BYTES = 4 * 1024;
// Ovoz bo'lagi (base64) uchun kattaroq chegara
const VOICE_BODY_BYTES = 20 * 1024;
// Qidiruv maydoni o'yinda 40 belgigacha yozadi: undan uzunlari ham xato emas, nickname'dan (16) uzun so'rov
// shunchaki hech kimni topmaydi
const MAX_QUERY_LENGTH = 64;
const LEADERBOARD_DEFAULT = 20;
const LEADERBOARD_MAX = 50;

// Daqiqasiga (windowMs) ruxsat etilgan so'rovlar. social: do'stlar va tavsiyalar uchun umumiy.
// Odatda bir IP uchun; profil (me) va heartbeat (presence) esa har bir o'yinchi uchun alohida hisoblanadi,
// chunki bitta IP ortida (kompyuter klubi, NAT, proksi) ko'p o'yinchi bo'lishi mumkin.
// Server teskari proksi ortida bo'lsa trustProxy (TRUST_PROXY) beriladi: IP X-Forwarded-For'dan olinadi.
export const DEFAULT_RATE_LIMITS = { check: 60, create: 10, social: 120, search: 60, presence: 120 };
// Nickname qidiruvi uchun eng kam belgi (ID raqam bilan aniq qidiriladi)
export const MIN_NICKNAME_QUERY = 2;

/**
 * HTTP so'rovlarini qayta ishlovchi funksiyani yaratadi.
 * (A) - "Authorization: Bearer <token>" kerak, aks holda 401 { error: 'unauthorized' }.
 * Cheklovdan oshsa 429 { error: 'too_many_requests' }, buzilgan so'rov manzili 400 { error: 'bad_request' }.
 *
 * GET   /health                                  -> { ok: true }
 * GET   /api/nicknames/availability?name=<nick>  -> { name, available, reason?, message? }
 * POST  /api/players { nickname, gender, avatarId }
 *                          -> 201 { id, publicId, nickname, gender, avatarId, token, createdAt }
 *                             409 { error: 'nickname_taken' }
 * POST  /api/players/restore { id, token, nickname, gender, avatarId, outfit?, country?, showOnline?, allowRequests? }
 *                          -> 201 profil (mijozdagi profil shu serverda qayta yaratildi) | 200 (allaqachon bor)
 *                             409 nickname_taken | conflict (id yoki token boshqa o'yinchida)
 *                             400 bad_json | invalid_token | invalid_id | invalid_nickname | invalid_gender
 * GET   /api/players/me (A) -> { id, publicId, nickname, gender, avatarId, outfit, country, showOnline, allowRequests,
 *                                createdAt, onlineSeconds }
 * PATCH /api/players/me (A) { avatarId?, outfit?, country?, showOnline?, allowRequests? } -> yangilangan o'yinchi
 *                             400 invalid_avatar | invalid_outfit | invalid_country | invalid_showOnline |
 *                                 invalid_allowRequests | nothing_to_update
 * POST  /api/presence (A)   -> { online, players }  (mijoz har 30 soniyada yuboradigan heartbeat)
 * GET   /api/stats          -> { online, players }
 * GET   /api/ping             -> { ok, t } (bazaga tegmaydi: mijoz ping'ni shu bilan o'lchaydi)
 * GET   /api/players/search?q=<matn> (A) -> [summary] (ko'pi bilan 20) | 400 invalid_query (1..64 belgi)
 *                                           q = 6 xonali ID ("123456", "#123456", "ID 123456") - aniq o'yinchi;
 *                                           aks holda nickname (avval boshi, keyin ichi), kamida 2 belgi:
 *                                           400 query_too_short. Hamma o'yinchilar ro'yxati (tavsiyalar) yo'q.
 * Guruhlar (A): groups.js dagi groupRoutes izohi.
 * GET   /api/friends (A)                 -> { friends: [summary], incoming: [summary], outgoing: [summary] }
 *                                           (incoming va outgoing ko'pi bilan MAX_PENDING_REQUESTS ta)
 * POST  /api/friends/request (A) { nickname } -> { friendship } | 404 not_found | 400 self | 403 requests_disabled
 *                                              | 403 too_many_pending (chiquvchi so'rovlar MAX_PENDING_REQUESTS ta)
 * POST  /api/friends/accept  (A) { nickname } -> { friendship: 'friends' } | 404 no_request
 * POST  /api/friends/remove  (A) { nickname } -> { friendship: 'none' } (munosabat yoki o'yinchi bo'lmasa ham)
 *       friends/* tanasi noto'g'ri bo'lsa: 400 bad_json | invalid_nickname
 * GET   /api/leaderboard?limit=N -> [{ rank, nickname, avatarId, gender, minutes }] (N: 20, ko'pi bilan 50;
 *                                   onlayn holatini yashirgan o'yinchilar ko'rsatilmaydi)
 * GET   /api/events              -> [{ id, zone, title: { uz, en }, place: { uz, en }, startsAt, endsAt }]
 *
 * summary = { nickname, publicId, avatarId, gender, online, friendship: 'none' | 'friends' | 'outgoing' | 'incoming' }
 * outfit  = "" yoki Unity Outfit JSON'i: { top, topColor, bottom, bottomColor, shoes, shoesColor, hair, hairColor }
 *           (hammasi ixtiyoriy satr: buyum - garderob id'si, rang - RRGGBB yoki ""; ko'pi bilan 512 belgi): outfit.js
 *
 * `now` - joriy vaqt manbai (testlar uchun almashtiriladi).
 * `trustProxy` - ishonchli teskari proksilar soni (0: X-Forwarded-For e'tiborsiz, so'rov manzili ishlatiladi).
 */
export function createApp(db, { rateLimits = {}, windowMs = 60_000, now = () => new Date(), trustProxy = 0 } = {}) {
  const limits = { ...DEFAULT_RATE_LIMITS, ...rateLimits };
  const limiter = createRateLimiter(windowMs);
  const party = createParties(db);
  const world = createWorld(db);
  limits.party = rateLimits.party ?? 120;
  // Ovoz: ~5 yuborish + ~5 o'qish soniyasiga (har o'yinchiga alohida)
  limits.voice = rateLimits.voice ?? 1200;
  // Ping: har ~2 s (IP bo'yicha; bitta NAT ortida bir necha o'yinchi bo'lishi mumkin)
  limits.ping = rateLimits.ping ?? 600;
  // Guruhlar: o'qish va amallar o'yinchi bo'yicha; yaratish alohida, sekinroq
  limits.groups = rateLimits.groups ?? 240;
  limits.groupCreate = rateLimits.groupCreate ?? 10;
  limits.chatRead = rateLimits.chatRead ?? 120;
  limits.chatSend = rateLimits.chatSend ?? 30;
  limits.world = rateLimits.world ?? 900;
  limits.worldJoin = rateLimits.worldJoin ?? 20;
  limits.worldVoice = rateLimits.worldVoice ?? 1200;
  limits.chessRead = rateLimits.chessRead ?? 600;
  limits.chessWrite = rateLimits.chessWrite ?? 120;

  // "METOD /yo'l" -> { limit: rateLimits kaliti, bucket?: cheklovchi kaliti (standart: limit), auth?,
  //                    perPlayer?: cheklov IP emas, o'yinchi bo'yicha (auth kerak), run }
  // run(ctx) [status, body] qaytaradi.
  const routes = new Map(Object.entries({
    'GET /health': { run: () => [200, { ok: true }] },
    'GET /api/ping': { limit: 'ping', run: ({ req }) => { req.resume(); return [200, { ok: true, t: Date.now() }]; } },
    'GET /api/nicknames/availability': { limit: 'check', run: checkAvailability },
    'POST /api/players': { limit: 'create', run: createPlayer },
    'POST /api/players/restore': { limit: 'create', run: restorePlayer },
    'GET /api/players/me': {
      limit: 'check', bucket: 'me', auth: true, perPlayer: true, run: ({ player }) => [200, player],
    },
    'PATCH /api/players/me': { limit: 'check', bucket: 'me', auth: true, perPlayer: true, run: updateMe },
    'POST /api/presence': { limit: 'presence', auth: true, perPlayer: true, run: heartbeat },
    'GET /api/stats': { limit: 'check', bucket: 'public', run: ctx => [200, db.stats(ctx.now)] },
    'GET /api/players/search': { limit: 'search', auth: true, run: searchPlayers },
    'GET /api/friends': { limit: 'social', auth: true, run: ctx => [200, db.listFriends(ctx.player.id, ctx.now)] },
    'POST /api/friends/request': { limit: 'social', auth: true, run: requestFriend },
    'POST /api/friends/accept': { limit: 'social', auth: true, run: acceptFriend },
    'POST /api/friends/remove': { limit: 'social', auth: true, run: removeFriend },
    'GET /api/leaderboard': { limit: 'check', bucket: 'public', run: leaderboard },
    'GET /api/events': { limit: 'check', bucket: 'public', run: ctx => [200, upcomingEvents(ctx.now)] },
  }));

  for (const kind of ['heartbeat', 'invite', 'accept', 'decline', 'leave']) {
    routes.set('POST /api/party/' + kind, { limit: 'party', auth: true, perPlayer: true,
      run: async ctx => party(kind, ctx.player, await readJson(ctx.req), ctx.now) });
  }

  for (const [key, run] of Object.entries({ ...groupRoutes(db), ...walletRoutes(db) })) {
    const post = key.startsWith('POST ');
    routes.set(key, { limit: key.endsWith('/create') ? 'groupCreate' : 'groups', auth: true, perPlayer: true,
      run: async ctx => run(ctx, post ? await readJson(ctx.req) : undefined) });
  }

  routes.set('POST /api/party/voice', { limit: 'voice', auth: true, perPlayer: true,
    run: async ctx => party.postVoice(ctx.player, await readJson(ctx.req, VOICE_BODY_BYTES), ctx.now) });
  for (const [key, run] of Object.entries(chatRoutes(db))) {
    const post = key.startsWith('POST ');
    routes.set(key, { limit: post ? 'chatSend' : 'chatRead', auth: true, perPlayer: true,
      run: async ctx => run(ctx, post ? await readJson(ctx.req) : undefined) });
  }
  routes.set('GET /api/party/voice', { limit: 'voice', bucket: 'voice-read', auth: true, perPlayer: true,
    run: ctx => party.readVoice(ctx.player, Number(ctx.url.searchParams.get('since') ?? -1), ctx.now) });

  for (const [key, run] of Object.entries(world.routes)) {
    routes.set(key, { limit: key.endsWith('/join') ? 'worldJoin' : 'world', auth: true, perPlayer: true,
      run: async ctx => {
        const body = await readJson(ctx.req);
        return run({ ...ctx, now: now() }, body);
      } });
  }
  routes.set('POST /api/world/voice', { limit: 'worldVoice', auth: true, perPlayer: true,
    run: async ctx => {
      const body = await readJson(ctx.req, VOICE_BODY_BYTES);
      return world.postVoice(ctx.player, body, now());
    } });
  routes.set('GET /api/world/voice', { limit: 'worldVoice', bucket: 'worldVoice-read', auth: true, perPlayer: true,
    run: ctx => world.readVoice(ctx.player, ctx.url, ctx.now) });
  for (const [key, run] of Object.entries(createChess(world))) {
    const post = key.startsWith('POST ');
    routes.set(key, { limit: post ? 'chessWrite' : 'chessRead', auth: true, perPlayer: true,
      run: async ctx => {
        if (!post) return run(ctx);
        const body = await readJson(ctx.req);
        // Validate sessions/seats at execution time, never at the arrival time of an unfinished body.
        return run({ ...ctx, now: now() }, body);
      } });
  }

  function checkAvailability({ url }) {
    const name = url.searchParams.get('name') ?? '';
    const check = validateNickname(name);
    if (!check.ok) return [200, { name, available: false, reason: check.reason, message: check.message }];
    const taken = db.isNicknameTaken(nicknameKey(check.nickname));
    return [200, taken
      ? { name: check.nickname, available: false, reason: 'taken', message: 'This nickname is already taken.' }
      : { name: check.nickname, available: true }];
  }

  async function createPlayer({ req, now }) {
    const body = await readJson(req);
    if (body === null) return [400, { error: 'bad_json' }];

    const check = validateNickname(body.nickname);
    if (!check.ok) return [400, { error: 'invalid_nickname', reason: check.reason, message: check.message }];
    if (!GENDERS.includes(body.gender)) return [400, { error: 'invalid_gender' }];
    if (!isValidAvatar(body.avatarId, body.gender)) return [400, { error: 'invalid_avatar' }];

    // Token keyinchalik o'yinchini tanish uchun; bazada faqat uning xeshi saqlanadi
    const token = randomBytes(32).toString('hex');
    const player = {
      id: randomUUID(),
      nickname: check.nickname,
      nicknameKey: nicknameKey(check.nickname),
      gender: body.gender,
      avatarId: body.avatarId,
      tokenHash: sha256(token),
      createdAt: now.toISOString(),
    };
    const publicId = db.insertPlayer(player);
    if (publicId === null) return [409, { error: 'nickname_taken', message: 'This nickname is already taken.' }];
    return [201, {
      id: player.id, publicId, nickname: player.nickname, gender: player.gender, avatarId: player.avatarId, token,
      createdAt: player.createdAt,
    }];
  }

  /**
   * Mijozda (PlayerPrefs) saqlangan profilni shu serverda qayta yaratadi: baza yangidan boshlangan yoki o'yin boshqa
   * serverga ulangan bo'lsa, o'yinchi qahramonini yo'qotmaydi (do'stlar esa har bir serverda o'zi). Token mijozda,
   * bazaga faqat uning xeshi yoziladi. id, token yoki nickname boshqa o'yinchida bo'lsa - 409.
   * Ixtiyoriy sozlamalardan noto'g'rilari e'tiborsiz qoldiriladi (tiklash ular sabab to'xtamaydi).
   */
  async function restorePlayer({ req, now }) {
    const body = await readJson(req);
    if (body === null) return [400, { error: 'bad_json' }];
    if (typeof body.token !== 'string' || !TOKEN_PATTERN.test(body.token)) return [400, { error: 'invalid_token' }];
    if (typeof body.id !== 'string' || !UUID_PATTERN.test(body.id)) return [400, { error: 'invalid_id' }];

    const tokenHash = sha256(body.token.toLowerCase());
    const existing = db.findPlayerByTokenHash(tokenHash);
    if (existing) return existing.id === body.id ? [200, existing] : [409, { error: 'conflict' }];
    if (db.getPlayer(body.id)) return [409, { error: 'conflict' }];

    const check = validateNickname(body.nickname);
    if (!check.ok) return [400, { error: 'invalid_nickname', reason: check.reason, message: check.message }];
    if (!GENDERS.includes(body.gender)) return [400, { error: 'invalid_gender' }];
    const player = {
      id: body.id,
      nickname: check.nickname,
      nicknameKey: nicknameKey(check.nickname),
      gender: body.gender,
      // Eng eski profillarda avatar yo'q: jinsning birinchi avatari
      avatarId: isValidAvatar(body.avatarId, body.gender) ? body.avatarId : AVATARS[body.gender][0],
      tokenHash,
      createdAt: now.toISOString(),
    };
    if (db.insertPlayer(player) === null) return [409, { error: 'nickname_taken', message: 'This nickname is already taken.' }];

    const settings = {};
    for (const [field, rule] of Object.entries(PROFILE_FIELDS)) {
      if (field !== 'avatarId' && Object.hasOwn(body, field) && rule.valid(body[field], player)) settings[field] = body[field];
    }
    if (Object.keys(settings).length > 0) db.updatePlayer(player.id, settings);
    return [201, db.getPlayer(player.id)];
  }

  /**
   * Faqat yuborilgan ma'lum maydonlar o'zgaradi (masalan faqat { showOnline }: allowRequests o'z holicha qoladi,
   * shuning uchun ikki maxfiylik sozlamasi alohida so'rovlarda bir-birini bekor qilmaydi); bittasi noto'g'ri bo'lsa
   * hech narsa yozilmaydi.
   */
  async function updateMe({ req, player }) {
    const body = await readJson(req);
    if (body === null) return [400, { error: 'bad_json' }];

    const changes = {};
    for (const [field, rule] of Object.entries(PROFILE_FIELDS)) {
      if (!Object.hasOwn(body, field)) continue;
      if (!rule.valid(body[field], player)) return [400, { error: rule.error }];
      changes[field] = body[field];
    }
    if (Object.keys(changes).length === 0) return [400, { error: 'nothing_to_update' }];

    db.updatePlayer(player.id, changes);
    return [200, db.getPlayer(player.id)];
  }

  function heartbeat({ req, player, now }) {
    req.resume(); // tana ixtiyoriy va ishlatilmaydi
    db.touchPresence(player.id, now);
    return [200, db.stats(now)];
  }

  function searchPlayers({ url, player, now }) {
    const query = (url.searchParams.get('q') ?? '').trim();
    if (query.length < 1 || query.length > MAX_QUERY_LENGTH) return [400, { error: 'invalid_query' }];
    const id = /^(?:#|id[:\s#]*)?(\d{6})$/i.exec(query);
    if (id) return [200, db.findByPublicId(player.id, Number(id[1]), now)];
    if (query.length < MIN_NICKNAME_QUERY) return [400, { error: 'query_too_short' }];
    if (query.length > MAX_LENGTH) return [200, []]; // hech bir nickname bunchalik uzun emas
    return [200, db.searchPlayers(player.id, nicknameKey(query), now)];
  }

  /**
   * Mavjud munosabat birinchi tekshiriladi: do'stlar yoki so'rov allaqachon yuborilgan bo'lsa o'zgarmaydi,
   * nishon chaqiruvchiga so'rov yuborgan bo'lsa u qabul qilinadi. requests_disabled faqat yangi so'rovga tegishli.
   */
  async function requestFriend(ctx) {
    const { target, error } = await readTarget(ctx);
    if (error) return error;
    if (!target) return [404, { error: 'not_found' }];
    if (target.id === ctx.player.id) return [400, { error: 'self' }];

    const relation = db.friendship(ctx.player.id, target.id);
    if (relation === 'friends' || relation === 'outgoing') return [200, { friendship: relation }];
    if (relation === 'incoming') {
      db.acceptFriendRequest(target.id, ctx.player.id);
      return [200, { friendship: 'friends' }];
    }
    if (!target.allowRequests) return [403, { error: 'requests_disabled' }];
    if (db.pendingOutgoingCount(ctx.player.id) >= MAX_PENDING_REQUESTS) return [403, { error: 'too_many_pending' }];
    db.addFriendRequest(ctx.player.id, target.id, ctx.now);
    return [200, { friendship: 'outgoing' }];
  }

  /** Faqat kiruvchi kutilayotgan so'rov qabul qilinadi; boshqa har qanday holatda (o'zi, noma'lum nickname) 404. */
  async function acceptFriend(ctx) {
    const { target, error } = await readTarget(ctx);
    if (error) return error;
    if (!target || !db.acceptFriendRequest(target.id, ctx.player.id)) return [404, { error: 'no_request' }];
    return [200, { friendship: 'friends' }];
  }

  /** Har qanday munosabatni o'chiradi; o'yinchi topilmasa yoki o'zi bo'lsa ham munosabat baribir 'none'. */
  async function removeFriend(ctx) {
    const { target, error } = await readTarget(ctx);
    if (error) return error;
    if (target) db.removeFriendship(ctx.player.id, target.id);
    return [200, { friendship: 'none' }];
  }

  /** Tanadagi { nickname } bo'yicha o'yinchi: { target } (topilmasa undefined) yoki { error: [status, body] }. */
  async function readTarget({ req }) {
    const body = await readJson(req);
    if (body === null) return { error: [400, { error: 'bad_json' }] };
    if (typeof body.nickname !== 'string') return { error: [400, { error: 'invalid_nickname' }] };
    return { target: db.findPlayerByNicknameKey(nicknameKey(body.nickname.trim())) };
  }

  function leaderboard({ url }) {
    const raw = url.searchParams.get('limit');
    const limit = raw !== null && /^\d+$/.test(raw)
      ? Math.min(Math.max(Number(raw), 1), LEADERBOARD_MAX)
      : LEADERBOARD_DEFAULT;
    return [200, db.leaderboard(limit)];
  }

  /**
   * So'rov kelgan manzil. Proksiga ishonilsa - X-Forwarded-For'ning o'ngdan trustProxy-chi yozuvi: uni oxirgi ishonchli
   * proksi qo'shgan (chapdagilarini mijozning o'zi yozib yuborishi mumkin). Sarlavha bo'lmasa - ulanish manzili.
   */
  function clientAddress(req) {
    if (trustProxy > 0) {
      const hops = String(req.headers['x-forwarded-for'] ?? '').split(',').map(hop => hop.trim()).filter(Boolean);
      if (hops.length > 0) return hops[Math.max(0, hops.length - trustProxy)].slice(0, 64);
    }
    return req.socket.remoteAddress ?? 'unknown';
  }

  /** Bitta so'rovni bajaradi va [status, body] qaytaradi. */
  async function dispatch(req) {
    let url;
    try {
      url = new URL(req.url, 'http://localhost');
    } catch {
      // HTTP tahlilchisi o'tkazib yuboradigan, lekin URL bo'lmagan manzil (masalan "GET http://a:99999/")
      return [400, { error: 'bad_request' }];
    }

    const route = routes.get(`${req.method} ${url.pathname}`);
    if (!route) return [404, { error: 'not_found' }];

    const bucket = route.bucket ?? route.limit;
    // IP bo'yicha cheklov token tekshirilishidan oldin, o'yinchi bo'yicha cheklov esa undan keyin
    if (route.limit && !route.perPlayer && !limiter.allow(`${bucket}:${clientAddress(req)}`, limits[route.limit])) {
      return [429, { error: 'too_many_requests' }];
    }

    const ctx = { req, url, now: now() };
    if (route.auth) {
      const token = bearerToken(req);
      ctx.player = token ? db.findPlayerByTokenHash(sha256(token)) : undefined;
      if (!ctx.player) return [401, { error: 'unauthorized' }];
    }
    if (route.perPlayer && !limiter.allow(`${bucket}:player:${ctx.player.id}`, limits[route.limit])) {
      return [429, { error: 'too_many_requests' }];
    }

    return route.run(ctx);
  }

  // Hech qachon rad etilgan promise qaytarmaydi: aks holda Node jarayonni to'xtatardi
  return async function handle(req, res) {
    try {
      setCors(res);
      if (req.method === 'OPTIONS') return send(res, 204);
      const [status, body] = await dispatch(req);
      send(res, status, body);
    } catch (err) {
      console.error(err);
      if (res.headersSent) res.destroy();
      else send(res, 500, { error: 'server_error' });
    }
  };
}

// PATCH /api/players/me da o'zgartiriladigan maydonlar: tekshiruv va xato kodi
const PROFILE_FIELDS = {
  avatarId: { error: 'invalid_avatar', valid: (value, player) => isValidAvatar(value, player.gender) },
  outfit: { error: 'invalid_outfit', valid: isValidOutfit },
  country: { error: 'invalid_country', valid: value => typeof value === 'string' && /^[A-Z]{2}$/.test(value) },
  showOnline: { error: 'invalid_showOnline', valid: value => typeof value === 'boolean' },
  allowRequests: { error: 'invalid_allowRequests', valid: value => typeof value === 'boolean' },
};

const TOKEN_PATTERN = /^[0-9a-f]{64}$/i;
const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function sha256(text) {
  return createHash('sha256').update(text).digest('hex');
}

function setCors(res) {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, PATCH, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type, Authorization');
}

/** "Authorization: Bearer <token>" sarlavhasidan token (64 ta hex belgi). */
function bearerToken(req) {
  const match = /^Bearer ([0-9a-f]{64})$/i.exec(req.headers.authorization ?? '');
  return match ? match[1].toLowerCase() : null;
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
async function readJson(req, maxBytes = MAX_BODY_BYTES) {
  let size = 0;
  const chunks = [];
  for await (const chunk of req) {
    size += chunk.length;
    if (size > maxBytes) return null;
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
