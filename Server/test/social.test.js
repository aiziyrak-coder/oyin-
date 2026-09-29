import assert from 'node:assert/strict';
import { createHash, randomBytes, randomUUID } from 'node:crypto';
import { mkdtempSync, rmSync } from 'node:fs';
import { createServer } from 'node:http';
import { connect } from 'node:net';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { DatabaseSync } from 'node:sqlite';
import { after, before, describe, test } from 'node:test';
import { createApp } from '../src/app.js';
import { MAX_PENDING_REQUESTS, openDatabase } from '../src/db.js';
import { upcomingEvents, WEEKLY_SCHEDULE, ZONES } from '../src/events.js';

const HIGH_LIMITS = { check: 100_000, create: 100_000, social: 100_000, search: 100_000, presence: 100_000 };
// 2026-09-27 yakshanba, Toshkentda 17:00
const START = Date.parse('2026-09-27T12:00:00.000Z');

const PROFILE_KEYS = ['id', 'publicId', 'nickname', 'gender', 'avatarId', 'outfit', 'country', 'showOnline',
  'allowRequests', 'createdAt', 'onlineSeconds'];
const SUMMARY_KEYS = ['nickname', 'avatarId', 'gender', 'online', 'friendship'];

/**
 * Xotiradagi (yoki berilgan) baza ustida server. Vaqt `clock` orqali boshqariladi.
 * call(metod, yo'l, { token, body, raw }) -> { status, headers, body }
 */
async function startApi({ db = openDatabase(':memory:'), rateLimits = HIGH_LIMITS, trustProxy = 0 } = {}) {
  const clock = { ms: START };
  const server = createServer(createApp(db, { rateLimits, trustProxy, now: () => new Date(clock.ms) }));
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const base = `http://127.0.0.1:${server.address().port}`;

  async function call(method, path, { token, body, raw } = {}) {
    const headers = {};
    if (token) headers.Authorization = `Bearer ${token}`;
    if (body !== undefined || raw !== undefined) headers['Content-Type'] = 'application/json';
    const payload = raw ?? (body === undefined ? undefined : JSON.stringify(body));
    const res = await fetch(base + path, { method, headers, body: payload });
    const text = await res.text();
    return { status: res.status, headers: res.headers, body: text ? JSON.parse(text) : undefined };
  }

  return {
    db,
    clock,
    call,
    port: server.address().port,
    /** Soatni `seconds` soniya oldinga suradi. */
    tick(seconds) {
      clock.ms += seconds * 1000;
    },
    /** O'yinchi yaratadi: { id, publicId, nickname, gender, avatarId, token, createdAt }. */
    async register(nickname, gender = 'male') {
      const res = await call('POST', '/api/players', { body: { nickname, gender, avatarId: gender === 'male' ? 'M1' : 'F1' } });
      assert.equal(res.status, 201, `${nickname}: ${JSON.stringify(res.body)}`);
      return res.body;
    },
    me: token => call('GET', '/api/players/me', { token }),
    patch: (token, body) => call('PATCH', '/api/players/me', { token, body }),
    heartbeat: token => call('POST', '/api/presence', { token }),
    friends: token => call('GET', '/api/friends', { token }),
    friend: (action, token, nickname) => call('POST', `/api/friends/${action}`, { token, body: { nickname } }),
    search: (token, q) => call('GET', `/api/players/search?q=${encodeURIComponent(q)}`, { token }),
    suggested: token => call('GET', '/api/players/suggested', { token }),
    async close() {
      server.closeAllConnections();
      await new Promise(resolve => server.close(resolve));
      db.close();
    },
  };
}

const names = list => list.map(p => p.nickname);

/** Xom HTTP so'rov yuboradi va javobni matn sifatida qaytaradi ('' - javob kelmadi). */
function rawRequest(port, requestLine) {
  return new Promise(resolve => {
    let data = '';
    const socket = connect(port, '127.0.0.1', () => {
      socket.write(`${requestLine}\r\nHost: localhost\r\nConnection: close\r\n\r\n`);
    });
    socket.setEncoding('utf8');
    socket.setTimeout(2000, () => socket.destroy());
    socket.on('data', chunk => { data += chunk; });
    socket.on('error', () => {});
    socket.on('close', () => resolve(data));
  });
}
const pick = (object, keys) => Object.fromEntries(keys.map(key => [key, object[key]]));
const SETTINGS = ['avatarId', 'outfit', 'country', 'showOnline', 'allowRequests'];

describe('o\'yinchi profili: yangi maydonlar', () => {
  let api;
  before(async () => { api = await startApi(); });
  after(() => api.close());

  test('yangi o\'yinchi publicId oladi, profil standart qiymatlar bilan qaytadi', async () => {
    const created = await api.register('Profiler', 'female');
    assert.deepEqual(Object.keys(created), ['id', 'publicId', 'nickname', 'gender', 'avatarId', 'token', 'createdAt']);
    assert.ok(Number.isInteger(created.publicId) && created.publicId >= 100000 && created.publicId <= 999999);
    assert.match(created.token, /^[0-9a-f]{64}$/);
    assert.equal(created.createdAt, new Date(START).toISOString());

    const me = await api.me(created.token);
    assert.equal(me.status, 200);
    assert.deepEqual(me.body, {
      id: created.id,
      publicId: created.publicId,
      nickname: 'Profiler',
      gender: 'female',
      avatarId: 'F1',
      outfit: '',
      country: 'UZ',
      showOnline: true,
      allowRequests: true,
      createdAt: created.createdAt,
      onlineSeconds: 0,
    });
    assert.deepEqual(Object.keys(me.body), PROFILE_KEYS);
  });

  test('publicId har bir o\'yinchida har xil', async () => {
    const ids = [];
    for (let i = 0; i < 40; i++) ids.push((await api.register(`Unique${i}`)).publicId);
    assert.equal(new Set(ids).size, ids.length);
    assert.ok(ids.every(id => id >= 100000 && id <= 999999));
  });

  test('PATCH istalgan maydonni alohida yoki birga o\'zgartiradi', async () => {
    const { token } = await api.register('Patcher', 'female');
    const outfit = JSON.stringify({ top: 'top_denim', topColor: '3E5F8F', shoes: 'shoes_white', shoesColor: 'EDEDEA' });

    let res = await api.patch(token, { outfit });
    assert.equal(res.status, 200);
    assert.equal(res.body.outfit, outfit);
    assert.deepEqual(Object.keys(res.body), PROFILE_KEYS);

    res = await api.patch(token, { country: 'KZ', showOnline: false, allowRequests: false, avatarId: 'F3' });
    assert.equal(res.status, 200);
    assert.equal(res.body.country, 'KZ');
    assert.equal(res.body.showOnline, false);
    assert.equal(res.body.allowRequests, false);
    assert.equal(res.body.avatarId, 'F3');
    assert.equal(res.body.outfit, outfit); // yuborilmagan maydon o'zgarmaydi

    res = await api.patch(token, { showOnline: true, outfit: '' });
    assert.equal(res.body.showOnline, true);
    assert.equal(res.body.allowRequests, false);
    assert.equal(res.body.outfit, '');

    assert.deepEqual(pick((await api.me(token)).body, SETTINGS),
      { avatarId: 'F3', outfit: '', country: 'KZ', showOnline: true, allowRequests: false });
  });

  test('noto\'g\'ri qiymat 400 qaytaradi va hech narsa yozilmaydi', async () => {
    const { token } = await api.register('Validator');
    const cases = [
      [{ avatarId: 'F1' }, 'invalid_avatar'],
      [{ avatarId: 'M4' }, 'invalid_avatar'],
      [{ outfit: '[]' }, 'invalid_outfit'],
      [{ outfit: 'null' }, 'invalid_outfit'],
      [{ outfit: '"hat"' }, 'invalid_outfit'],
      [{ outfit: '42' }, 'invalid_outfit'],
      [{ outfit: '{broken' }, 'invalid_outfit'],
      [{ outfit: { hat: 1 } }, 'invalid_outfit'],
      [{ outfit: null }, 'invalid_outfit'],
      [{ outfit: `{"top":"top_denim"${' '.repeat(494)}}` }, 'invalid_outfit'], // 513 belgi
      [{ outfit: '{"hat":"red"}' }, 'invalid_outfit'], // noma'lum kalit
      [{ outfit: '{"top":"top_denim","evil":[1,2,3]}' }, 'invalid_outfit'],
      [{ outfit: '{"__proto__":"x"}' }, 'invalid_outfit'],
      [{ outfit: '{"top":1}' }, 'invalid_outfit'], // satr emas
      [{ outfit: '{"top":{"id":"top_denim"}}' }, 'invalid_outfit'],
      [{ outfit: '{"top":null}' }, 'invalid_outfit'],
      [{ outfit: '{"top":"Top Denim"}' }, 'invalid_outfit'], // buyum id'si emas
      [{ outfit: `{"top":"t${'o'.repeat(32)}"}` }, 'invalid_outfit'], // 33 belgi
      [{ outfit: '{"topColor":"blue"}' }, 'invalid_outfit'], // rang RRGGBB emas
      [{ outfit: '{"topColor":"#3E5F8F"}' }, 'invalid_outfit'],
      [{ outfit: '{"hairColor":"12345"}' }, 'invalid_outfit'],
      [{ country: 'uz' }, 'invalid_country'],
      [{ country: 'UZB' }, 'invalid_country'],
      [{ country: 'U1' }, 'invalid_country'],
      [{ country: 7 }, 'invalid_country'],
      [{ showOnline: 'false' }, 'invalid_showOnline'],
      [{ showOnline: 0 }, 'invalid_showOnline'],
      [{ allowRequests: 1 }, 'invalid_allowRequests'],
      [{ allowRequests: null }, 'invalid_allowRequests'],
    ];
    for (const [body, error] of cases) {
      // To'g'ri maydonlar ham birga yuboriladi: ular ham yozilmasligi kerak
      const res = await api.patch(token, { country: 'TR', showOnline: false, avatarId: 'M2', ...body });
      assert.equal(res.status, 400, JSON.stringify(body));
      assert.deepEqual(res.body, { error }, JSON.stringify(body));
    }
    assert.deepEqual(pick((await api.me(token)).body, SETTINGS),
      { avatarId: 'M1', outfit: '', country: 'UZ', showOnline: true, allowRequests: true });
  });

  test('outfit: Unity Outfit JSON\'i (ko\'pi bilan 512 belgi) yoki bo\'sh satr', async () => {
    const { token } = await api.register('Tailor');
    // JsonUtility.ToJson(Outfit): hamma 8 maydon, tanlanmagan qism bo'sh satr
    const full = JSON.stringify({ top: 'top_leather_black', topColor: '18181B', bottom: 'bottom_jeans_black',
      bottomColor: '25282e', shoes: '', shoesColor: '', hair: 'hair_platinum', hairColor: 'DCD2BE' });
    let res = await api.patch(token, { outfit: full });
    assert.equal(res.status, 200);
    assert.equal(res.body.outfit, full);

    const longest = `{"top":"top_denim"${' '.repeat(493)}}`;
    assert.equal(longest.length, 512);
    res = await api.patch(token, { outfit: longest });
    assert.equal(res.status, 200);
    assert.equal(res.body.outfit, longest);
    res = await api.patch(token, { outfit: `{"top":"t${'o'.repeat(31)}","hairColor":""}` }); // 32 belgilik id
    assert.equal(res.status, 200);
    res = await api.patch(token, { outfit: '{}' });
    assert.equal(res.body.outfit, '{}');
    res = await api.patch(token, { outfit: '' });
    assert.equal(res.body.outfit, '');
  });

  test('maxfiylik: faqat yuborilgan bayroq o\'zgaradi, ketma-ket so\'rovlar bir-birini bekor qilmaydi', async () => {
    const { token } = await api.register('Private');
    let res = await api.patch(token, { showOnline: false });
    assert.deepEqual([res.status, res.body.showOnline, res.body.allowRequests], [200, false, true]);
    res = await api.patch(token, { allowRequests: false });
    assert.deepEqual([res.body.showOnline, res.body.allowRequests], [false, false]);

    // Ikki kalit deyarli bir vaqtda bosildi: har biri faqat o'zini yuboradi, ikkalasi ham saqlanadi
    await Promise.all([api.patch(token, { showOnline: true }), api.patch(token, { allowRequests: true })]);
    assert.deepEqual(pick((await api.me(token)).body, ['showOnline', 'allowRequests']), { showOnline: true, allowRequests: true });

    // Eski mijozlar ikkalasini birga yuboradi: bu ham ishlaydi
    res = await api.patch(token, { showOnline: false, allowRequests: true });
    assert.deepEqual([res.status, res.body.showOnline, res.body.allowRequests], [200, false, true]);
  });

  test('noma\'lum maydonlar e\'tiborsiz, ma\'lum maydon bo\'lmasa nothing_to_update', async () => {
    const { token, id } = await api.register('Ignorer');
    let res = await api.patch(token, { country: 'DE', nickname: 'Hacker', id: 'x', onlineSeconds: 999, token: 'x' });
    assert.equal(res.status, 200);
    assert.equal(res.body.country, 'DE');
    assert.equal(res.body.nickname, 'Ignorer');
    assert.equal(res.body.id, id);
    assert.equal(res.body.onlineSeconds, 0);

    for (const body of [{}, { nickname: 'Other' }, { lastSeen: 'now' }]) {
      res = await api.patch(token, body);
      assert.equal(res.status, 400);
      assert.deepEqual(res.body, { error: 'nothing_to_update' });
    }
    res = await api.call('PATCH', '/api/players/me', { token, raw: '{oops' });
    assert.deepEqual([res.status, res.body], [400, { error: 'bad_json' }]);
    res = await api.call('PATCH', '/api/players/me', { token, raw: '[1]' });
    assert.deepEqual([res.status, res.body], [400, { error: 'bad_json' }]);
  });

  test('profil tokensiz ochilmaydi va maxfiy maydonlarni qaytarmaydi', async () => {
    const { token } = await api.register('Secretive');
    const me = (await api.me(token)).body;
    for (const key of ['token', 'tokenHash', 'token_hash', 'lastSeen', 'nicknameKey']) assert.equal(me[key], undefined, key);

    assert.deepEqual((await api.patch('b'.repeat(64), { country: 'US' })).body, { error: 'unauthorized' });
    assert.equal((await api.patch(undefined, { country: 'US' })).status, 401);
    assert.equal((await api.call('PATCH', '/api/players/me', { token: 'not-a-token', body: { country: 'US' } })).status, 401);
  });
});

describe('onlayn holat va statistika', () => {
  let api;
  before(async () => { api = await startApi(); });
  after(() => api.close());

  test('heartbeat onlayn vaqtni hisoblaydi', async () => {
    const { token } = await api.register('Pulse');
    const seconds = async () => (await api.me(token)).body.onlineSeconds;

    let res = await api.heartbeat(token);
    assert.equal(res.status, 200);
    assert.deepEqual(res.body, { online: 1, players: 1 });
    assert.equal(await seconds(), 0); // birinchi heartbeat vaqt qo'shmaydi

    api.tick(30);
    await api.heartbeat(token);
    assert.equal(await seconds(), 30);

    api.tick(45);
    await api.heartbeat(token);
    assert.equal(await seconds(), 75);

    api.tick(80); // oyna ichida, lekin 60 soniyadan ko'p qo'shilmaydi
    await api.heartbeat(token);
    assert.equal(await seconds(), 135);

    api.tick(90); // aynan 90 soniya: hali onlayn
    await api.heartbeat(token);
    assert.equal(await seconds(), 195);

    api.tick(91); // tanaffus: o'yinchi oflayn bo'lgan
    await api.heartbeat(token);
    assert.equal(await seconds(), 195);

    api.tick(29.6); // faqat to'liq soniyalar: 29
    await api.heartbeat(token);
    assert.equal(await seconds(), 224);

    api.tick(0.4); // qoldiq yo'qolmaydi: 29.6 + 0.4 = 30 soniya
    await api.heartbeat(token);
    assert.equal(await seconds(), 225);
  });

  test('onlayn soni oxirgi 90 soniyada ko\'ringanlar; /api/stats ochiq', async () => {
    api.tick(91); // Pulse endi oflayn
    const a = await api.register('OnlineA');
    const b = await api.register('OnlineB');
    await api.register('NeverOnline');

    assert.deepEqual((await api.call('GET', '/api/stats')).body, { online: 0, players: 4 });

    await api.heartbeat(a.token);
    api.tick(60);
    assert.deepEqual((await api.heartbeat(b.token)).body, { online: 2, players: 4 });

    api.tick(30); // A 90 soniya oldin ko'ringan: hali onlayn
    assert.deepEqual((await api.call('GET', '/api/stats')).body, { online: 2, players: 4 });

    api.tick(1);
    const stats = await api.call('GET', '/api/stats');
    assert.equal(stats.status, 200);
    assert.deepEqual(stats.body, { online: 1, players: 4 });

    // showOnline=false bo'lsa ham umumiy sonda hisoblanadi
    await api.patch(a.token, { showOnline: false });
    assert.deepEqual((await api.heartbeat(a.token)).body, { online: 2, players: 4 });
  });

  test('heartbeat token talab qiladi, tanasi ixtiyoriy', async () => {
    assert.deepEqual((await api.call('POST', '/api/presence')).body, { error: 'unauthorized' });
    assert.equal((await api.heartbeat('c'.repeat(64))).status, 401);

    const { token } = await api.register('BodyOptional');
    assert.equal((await api.call('POST', '/api/presence', { token, body: { scene: 'MainMenu' } })).status, 200);
    assert.equal((await api.call('POST', '/api/presence', { token, raw: 'not json at all' })).status, 200);
  });
});

describe('onlayn vaqt hisobi', () => {
  let api;
  before(async () => { api = await startApi(); });
  after(() => api.close());

  test('tez-tez heartbeat haqiqiy vaqtdan ko\'p onlayn vaqt bermaydi', async () => {
    // Har 0.5 soniyada (standart cheklov 120/daqiqa shunga ruxsat beradi) 60 soniya davomida
    for (const [gap, name] of [[0.5, 'Spammer'], [0.6, 'Spammer2'], [0.999, 'Spammer3']]) {
      const { token } = await api.register(name);
      await api.heartbeat(token);
      const steps = Math.round(60 / gap);
      for (let i = 0; i < steps; i++) {
        api.tick(gap);
        await api.heartbeat(token);
      }
      const elapsed = steps * gap;
      const { onlineSeconds } = (await api.me(token)).body;
      assert.ok(onlineSeconds <= elapsed, `${gap}: ${onlineSeconds} > ${elapsed}`);
      assert.ok(onlineSeconds >= Math.floor(elapsed) - 1, `${gap}: ${onlineSeconds}`);
      api.tick(91);
    }
  });

  test('tartibsiz oraliqlar: jami to\'liq soniyalar yig\'indisiga teng', async () => {
    const { token, id } = await api.register('Jitter');
    api.tick(0.25); // soniya boshidan emas
    await api.heartbeat(token);
    const start = api.clock.ms;
    let seed = 7;
    for (let i = 0; i < 300; i++) {
      seed = (seed * 48271) % 2147483647;
      api.tick((seed % 44) + 0.5 + (seed % 400) / 1000); // 0.5 .. 44.9 soniya, kasr qismi 0.5 dan katta
      api.db.touchPresence(id, new Date(api.clock.ms));
    }
    const expected = Math.floor(api.clock.ms / 1000) - Math.floor(start / 1000);
    assert.equal((await api.me(token)).body.onlineSeconds, expected);
    // Butun sessiya davomida haqiqiy vaqtdan farq 1 soniyadan kam (har heartbeat'da emas)
    assert.ok(Math.abs(expected - (api.clock.ms - start) / 1000) < 1);
    api.tick(91);
  });
});

describe('qidiruv va tavsiyalar', () => {
  let api, seeker;
  before(async () => {
    api = await startApi();
    seeker = await api.register('Alfa_Ali');
    for (const name of ['Zed', 'alisher', 'Kali_Ali', 'Alibek', 'Bali', 'Nobody', 'Bekzod']) await api.register(name, 'male');
  });
  after(() => api.close());

  test('katta-kichik harfsiz: avval boshlanishi mos, keyin ichida bor; chaqiruvchi chiqmaydi', async () => {
    const res = await api.search(seeker.token, 'ALI');
    assert.equal(res.status, 200);
    assert.deepEqual(names(res.body), ['Alibek', 'alisher', 'Bali', 'Kali_Ali']);
    for (const summary of res.body) assert.deepEqual(Object.keys(summary), SUMMARY_KEYS);
    assert.deepEqual(res.body[0], { nickname: 'Alibek', avatarId: 'M1', gender: 'male', online: false, friendship: 'none' });

    assert.deepEqual(names((await api.search(seeker.token, '   ali  ')).body), ['Alibek', 'alisher', 'Bali', 'Kali_Ali']);
    assert.deepEqual(names((await api.search(seeker.token, 'BEK')).body), ['Bekzod', 'Alibek']); // alifbodan qat'i nazar
    assert.deepEqual(names((await api.search(seeker.token, 'zed')).body), ['Zed']);
    assert.deepEqual((await api.search(seeker.token, 'xyz')).body, []);
  });

  test('"_" va "%" oddiy belgi sifatida qidiriladi', async () => {
    assert.deepEqual(names((await api.search(seeker.token, '_')).body), ['Kali_Ali']);
    assert.deepEqual((await api.search(seeker.token, '%')).body, []);
    assert.deepEqual((await api.search(seeker.token, 'a%i')).body, []);
  });

  test('so\'rov 1..64 belgi; nickname\'dan uzuni xato emas, bo\'sh natija', async () => {
    for (const q of ['', '   ', 'a'.repeat(65)]) {
      const res = await api.search(seeker.token, q);
      assert.deepEqual([res.status, res.body], [400, { error: 'invalid_query' }], JSON.stringify(q));
    }
    const missing = await api.call('GET', '/api/players/search', { token: seeker.token });
    assert.deepEqual([missing.status, missing.body], [400, { error: 'invalid_query' }]);
    // O'yindagi qidiruv maydoni 40 belgigacha yozadi: 17..64 belgi "Server xatosi" emas, shunchaki hech kim
    for (const q of ['a'.repeat(24), 'kali_ali_and_more_', 'x'.repeat(40), ` ${'a'.repeat(64)} `]) {
      const res = await api.search(seeker.token, q);
      assert.deepEqual([res.status, res.body], [200, []], JSON.stringify(q));
    }
    assert.deepEqual(names((await api.search(seeker.token, 'Kali_Ali')).body), ['Kali_Ali']); // 8 belgi
  });

  test('ko\'pi bilan 20 ta natija', async () => {
    for (let i = 1; i <= 25; i++) await api.register(`Robo${String(i).padStart(2, '0')}`);
    const res = await api.search(seeker.token, 'robo');
    assert.equal(res.body.length, 20);
    assert.equal(res.body[0].nickname, 'Robo01');
    assert.equal(res.body[19].nickname, 'Robo20');
  });

  test('natijada onlayn holat va do\'stlik ko\'rinadi', async () => {
    const zed = await api.register('Zedd');
    await api.heartbeat(zed.token);
    let [found] = (await api.search(seeker.token, 'zedd')).body;
    assert.equal(found.online, true);

    await api.patch(zed.token, { showOnline: false });
    [found] = (await api.search(seeker.token, 'zedd')).body;
    assert.equal(found.online, false); // yashirgan

    await api.patch(zed.token, { showOnline: true });
    api.tick(91);
    [found] = (await api.search(seeker.token, 'zedd')).body;
    assert.equal(found.online, false); // uzoq vaqt heartbeat yo'q

    await api.friend('request', seeker.token, 'Zedd');
    [found] = (await api.search(seeker.token, 'zedd')).body;
    assert.equal(found.friendship, 'outgoing');
    const [back] = (await api.search(zed.token, 'alfa')).body;
    assert.equal(back.friendship, 'incoming');
  });

  test('qidiruv token talab qiladi', async () => {
    assert.equal((await api.search(undefined, 'ali')).status, 401);
    assert.equal((await api.suggested(undefined)).status, 401);
  });

  test('chaqiruvchining o\'zi mos kelsa ham 20 ta boshqa o\'yinchi qaytadi', async () => {
    const others = [];
    for (let i = 1; i <= 20; i++) others.push((await api.register(`Alfa_B${String(i).padStart(2, '0')}`)).nickname);
    // 'alfa' bilan chaqiruvchi (Alfa_Ali) ham boshlanadi va tartibda birinchi
    assert.deepEqual(names((await api.search(seeker.token, 'alfa')).body), others);
    // Ichida bor (boshlanishida emas) bosqichida ham
    assert.deepEqual(names((await api.search(seeker.token, 'lfa_')).body), others);
    // Boshlanishi mos kelganlar kam bo'lsa, qolgani ichida bor bilan to'ldiriladi
    const mixed = names((await api.search(seeker.token, 'b0')).body);
    assert.deepEqual(mixed, others.slice(0, 9));
    assert.deepEqual(names((await api.search(seeker.token, 'b')).body).slice(0, 3), ['Bali', 'Bekzod', 'Alfa_B01']);
  });
});

describe('tavsiya etilgan o\'yinchilar', () => {
  let api;
  before(async () => { api = await startApi(); });
  after(() => api.close());

  test('aloqasi yo\'qlar, avval yaqinda ko\'ringanlar, keyin yangilar; ko\'pi bilan 10 ta', async () => {
    const me = await api.register('Center');
    const players = {};
    for (let i = 1; i <= 14; i++) {
      api.tick(1);
      players[i] = await api.register(`Player${String(i).padStart(2, '0')}`);
    }
    // Munosabatlar: 1 - do'st, 2 - chiquvchi so'rov, 3 - kiruvchi so'rov
    await api.friend('request', me.token, 'Player01');
    await api.friend('accept', players[1].token, 'Center');
    await api.friend('request', me.token, 'Player02');
    await api.friend('request', players[3].token, 'Center');

    await api.heartbeat(players[5].token);
    api.tick(10);
    await api.heartbeat(players[7].token);
    api.tick(10);
    await api.heartbeat(players[4].token);
    await api.heartbeat(me.token);
    api.tick(500); // hech kim onlayn emas, lekin tartib last_seen bo'yicha qoladi

    const res = await api.suggested(me.token);
    assert.equal(res.status, 200);
    assert.deepEqual(names(res.body), [
      'Player04', 'Player07', 'Player05',
      'Player14', 'Player13', 'Player12', 'Player11', 'Player10', 'Player09', 'Player08',
    ]);
    for (const summary of res.body) {
      assert.deepEqual(Object.keys(summary), SUMMARY_KEYS);
      assert.equal(summary.friendship, 'none');
      assert.equal(summary.online, false);
    }

    // Munosabat o'chirilgach o'yinchi yana tavsiya qilinadi
    await api.friend('remove', me.token, 'Player01');
    await api.heartbeat(players[1].token);
    const again = await api.suggested(me.token);
    assert.equal(again.body[0].nickname, 'Player01');
    assert.equal(again.body[0].online, true);
  });

  test('onlayn holatini yashirgan o\'yinchining o\'rni oxirgi kirishiga bog\'liq emas', async () => {
    const own = await startApi();
    try {
      const players = {};
      for (const name of ['Oldest', 'Hidden', 'Visible', 'Newest', 'Spy']) {
        players[name] = await own.register(name);
        own.tick(1);
      }
      await own.patch(players.Hidden.token, { showOnline: false });
      const spy = players.Spy.token;

      await own.heartbeat(players.Visible.token);
      const before = (await own.suggested(spy)).body;
      // Yashirgan o'yinchi "hech ko'rinmagan"lar qatorida, ro'yxatdan o'tgan vaqti bo'yicha
      assert.deepEqual(names(before), ['Visible', 'Newest', 'Hidden', 'Oldest']);
      assert.deepEqual(before.map(p => p.online), [true, false, false, false]);

      // U hozir kirdi: ro'yxat umuman o'zgarmaydi
      own.tick(5);
      await own.heartbeat(players.Hidden.token);
      assert.deepEqual((await own.suggested(spy)).body, before);

      // Ko'rinadigan o'yinchi oflayn bo'lsa ham undan yuqoriga chiqmaydi
      own.tick(120);
      await own.heartbeat(players.Hidden.token);
      const later = (await own.suggested(spy)).body;
      assert.deepEqual(names(later), ['Visible', 'Newest', 'Hidden', 'Oldest']);
      assert.ok(later.every(p => p.online === false));

      // Yashirish o'chirilsa oddiy tartib qaytadi
      await own.patch(players.Hidden.token, { showOnline: true });
      const shown = (await own.suggested(spy)).body;
      assert.deepEqual(names(shown), ['Hidden', 'Visible', 'Newest', 'Oldest']);
      assert.equal(shown[0].online, true);
    } finally {
      await own.close();
    }
  });
});

describe('do\'stlar', () => {
  let api, ali, bek, cora, dana, eva;
  before(async () => {
    api = await startApi();
    ali = await api.register('Ali');
    bek = await api.register('Bek');
    cora = await api.register('Cora', 'female');
    dana = await api.register('Dana', 'female');
    eva = await api.register('Eva', 'female');
  });
  after(() => api.close());

  const lists = async token => {
    const res = await api.friends(token);
    assert.equal(res.status, 200);
    return { friends: names(res.body.friends), incoming: names(res.body.incoming), outgoing: names(res.body.outgoing) };
  };

  test('noto\'g\'ri so\'rovlar', async () => {
    assert.deepEqual((await api.friend('request', ali.token, 'Ghost')).body, { error: 'not_found' });
    assert.equal((await api.friend('request', ali.token, 'Ghost')).status, 404);
    const self = await api.friend('request', ali.token, ' ALI ');
    assert.deepEqual([self.status, self.body], [400, { error: 'self' }]);
    const noName = await api.call('POST', '/api/friends/request', { token: ali.token, body: { nickname: 42 } });
    assert.deepEqual([noName.status, noName.body], [400, { error: 'invalid_nickname' }]);
    const badJson = await api.call('POST', '/api/friends/request', { token: ali.token, raw: 'nope' });
    assert.deepEqual([badJson.status, badJson.body], [400, { error: 'bad_json' }]);

    for (const action of ['request', 'accept', 'remove']) {
      assert.equal((await api.friend(action, undefined, 'Bek')).status, 401, action);
      const wrong = await api.call('POST', `/api/friends/${action}`, { token: ali.token, raw: '[]' });
      assert.deepEqual([wrong.status, wrong.body], [400, { error: 'bad_json' }], action);
    }
    // Noma'lum nickname: accept uchun kiruvchi so'rov yo'q, remove uchun munosabat baribir yo'q
    const ghostAccept = await api.friend('accept', ali.token, 'Ghost');
    assert.deepEqual([ghostAccept.status, ghostAccept.body], [404, { error: 'no_request' }]);
    const ghostRemove = await api.friend('remove', ali.token, 'Ghost');
    assert.deepEqual([ghostRemove.status, ghostRemove.body], [200, { friendship: 'none' }]);
    // O'zini: accept - no_request, remove - 'none'
    assert.deepEqual((await api.friend('accept', ali.token, 'ali')).body, { error: 'no_request' });
    assert.deepEqual((await api.friend('remove', ali.token, ' ALI ')).body, { friendship: 'none' });
    assert.equal((await api.friends(undefined)).status, 401);
  });

  test('so\'rov yuboriladi, qayta yuborish o\'zgartirmaydi', async () => {
    const res = await api.friend('request', ali.token, '  bEK ');
    assert.deepEqual([res.status, res.body], [200, { friendship: 'outgoing' }]);
    assert.deepEqual((await api.friend('request', ali.token, 'Bek')).body, { friendship: 'outgoing' });

    assert.deepEqual(await lists(ali.token), { friends: [], incoming: [], outgoing: ['Bek'] });
    assert.deepEqual(await lists(bek.token), { friends: [], incoming: ['Ali'], outgoing: [] });

    const { body } = await api.friends(bek.token);
    assert.deepEqual(body.incoming[0],
      { nickname: 'Ali', avatarId: 'M1', gender: 'male', online: false, friendship: 'incoming' });
    assert.deepEqual((await api.friends(ali.token)).body.outgoing[0].friendship, 'outgoing');
  });

  test('qarshi so\'rov avtomatik qabul qilinadi, do\'stlar holati saqlanadi', async () => {
    const res = await api.friend('request', bek.token, 'ali');
    assert.deepEqual([res.status, res.body], [200, { friendship: 'friends' }]);
    assert.deepEqual(await lists(ali.token), { friends: ['Bek'], incoming: [], outgoing: [] });
    assert.deepEqual(await lists(bek.token), { friends: ['Ali'], incoming: [], outgoing: [] });
    assert.deepEqual((await api.friend('request', ali.token, 'Bek')).body, { friendship: 'friends' });
    assert.deepEqual((await api.friend('request', bek.token, 'Ali')).body, { friendship: 'friends' });
    assert.equal((await api.friends(ali.token)).body.friends[0].friendship, 'friends');
  });

  test('accept faqat kiruvchi so\'rovni qabul qiladi', async () => {
    await api.friend('request', cora.token, 'Ali');
    const res = await api.friend('accept', ali.token, 'CORA');
    assert.deepEqual([res.status, res.body], [200, { friendship: 'friends' }]);
    assert.deepEqual((await lists(cora.token)).friends, ['Ali']);

    // So'rov yo'q, chiquvchi so'rov, allaqachon do'st, o'zini
    const none = await api.friend('accept', ali.token, 'Eva');
    assert.deepEqual([none.status, none.body], [404, { error: 'no_request' }]);
    await api.friend('request', ali.token, 'Eva');
    assert.deepEqual((await api.friend('accept', ali.token, 'Eva')).body, { error: 'no_request' });
    assert.deepEqual((await api.friend('accept', ali.token, 'Cora')).body, { error: 'no_request' });
    const self = await api.friend('accept', ali.token, 'Ali');
    assert.deepEqual([self.status, self.body], [404, { error: 'no_request' }]);

    // Eva qabul qilsa ikkalasi do'st
    assert.deepEqual((await api.friend('accept', eva.token, 'Ali')).body, { friendship: 'friends' });
  });

  test('allowRequests=false yangi so\'rovlarni to\'xtatadi', async () => {
    await api.patch(dana.token, { allowRequests: false });
    const res = await api.friend('request', ali.token, 'Dana');
    assert.deepEqual([res.status, res.body], [403, { error: 'requests_disabled' }]);
    assert.deepEqual((await lists(dana.token)).incoming, []);

    // Dana o'zi so'rov yubora oladi va u qabul qilinadi
    assert.deepEqual((await api.friend('request', dana.token, 'Ali')).body, { friendship: 'outgoing' });
    assert.deepEqual((await api.friend('request', ali.token, 'Dana')).body, { friendship: 'friends' });
    // Allaqachon do'st bo'lsa, sozlama munosabatni buzmaydi
    assert.deepEqual((await api.friend('request', ali.token, 'Dana')).body, { friendship: 'friends' });
  });

  test('ro\'yxat: avval onlaynlar, keyin nickname bo\'yicha', async () => {
    assert.deepEqual((await lists(ali.token)).friends, ['Bek', 'Cora', 'Dana', 'Eva']);
    await api.heartbeat(eva.token);
    await api.heartbeat(cora.token);
    await api.heartbeat(bek.token);
    await api.patch(bek.token, { showOnline: false }); // yashirgan o'yinchi oflayn ko'rinadi
    const { body } = await api.friends(ali.token);
    assert.deepEqual(names(body.friends), ['Cora', 'Eva', 'Bek', 'Dana']);
    assert.deepEqual(body.friends.map(f => f.online), [true, true, false, false]);
    for (const summary of [...body.friends, ...body.incoming, ...body.outgoing]) {
      assert.deepEqual(Object.keys(summary), SUMMARY_KEYS);
    }
  });

  test('remove: rad etish, bekor qilish va do\'stlikdan chiqarish', async () => {
    const zara = await api.register('Zara', 'female');
    const yan = await api.register('Yan');

    // Rad etish (kiruvchi so'rov)
    await api.friend('request', zara.token, 'Yan');
    let res = await api.friend('remove', yan.token, 'Zara');
    assert.deepEqual([res.status, res.body], [200, { friendship: 'none' }]);
    assert.deepEqual(await lists(zara.token), { friends: [], incoming: [], outgoing: [] });

    // Bekor qilish (chiquvchi so'rov)
    await api.friend('request', zara.token, 'Yan');
    assert.deepEqual((await api.friend('remove', zara.token, 'yan')).body, { friendship: 'none' });
    assert.deepEqual(await lists(yan.token), { friends: [], incoming: [], outgoing: [] });

    // Do'stlikdan chiqarish
    await api.friend('request', zara.token, 'Yan');
    await api.friend('accept', yan.token, 'Zara');
    assert.deepEqual((await api.friend('remove', yan.token, 'Zara')).body, { friendship: 'none' });
    assert.deepEqual(await lists(zara.token), { friends: [], incoming: [], outgoing: [] });

    // Munosabat bo'lmasa ham xato emas
    assert.deepEqual((await api.friend('remove', yan.token, 'Zara')).body, { friendship: 'none' });
    // Olib tashlangandan keyin yangidan so'rov yuborish mumkin
    assert.deepEqual((await api.friend('request', yan.token, 'Zara')).body, { friendship: 'outgoing' });
  });

  test('juftlik yo\'nalishidan qat\'i nazar bazada bitta', async () => {
    const one = await api.register('PairOne');
    const two = await api.register('PairTwo');
    api.db.addFriendRequest(one.id, two.id, new Date(START));
    assert.throws(() => api.db.addFriendRequest(two.id, one.id, new Date(START)), /UNIQUE/);
    assert.throws(() => api.db.addFriendRequest(one.id, two.id, new Date(START)), /UNIQUE/);
    assert.equal(api.db.friendship(one.id, two.id), 'outgoing');
    assert.equal(api.db.friendship(two.id, one.id), 'incoming');
  });
});

describe('kutilayotgan so\'rovlar chegarasi', () => {
  let api, spammer, victim, targets;
  before(async () => {
    api = await startApi();
    spammer = await api.register('Spammer');
    victim = await api.register('Victim', 'female');
    targets = [];
    for (let i = 0; i <= MAX_PENDING_REQUESTS; i++) targets.push(await api.register(`Target${String(i).padStart(3, '0')}`));
  });
  after(() => api.close());

  test('bitta o\'yinchi ko\'pi bilan MAX_PENDING_REQUESTS ta chiquvchi so\'rov ushlab turadi', async () => {
    for (let i = 0; i < MAX_PENDING_REQUESTS; i++) {
      assert.deepEqual((await api.friend('request', spammer.token, targets[i].nickname)).body, { friendship: 'outgoing' });
    }
    const last = targets[MAX_PENDING_REQUESTS].nickname;
    const over = await api.friend('request', spammer.token, last);
    assert.deepEqual([over.status, over.body], [403, { error: 'too_many_pending' }]);

    // Mavjud munosabatlar va kiruvchi so'rovni qabul qilish chegaraga bog'liq emas
    assert.deepEqual((await api.friend('request', spammer.token, targets[0].nickname)).body, { friendship: 'outgoing' });
    await api.friend('request', victim.token, 'Spammer');
    assert.deepEqual((await api.friend('request', spammer.token, 'Victim')).body, { friendship: 'friends' });

    // Bittasi bekor qilinsa yana yuborish mumkin
    await api.friend('remove', spammer.token, targets[0].nickname);
    assert.deepEqual((await api.friend('request', spammer.token, last)).body, { friendship: 'outgoing' });
    assert.deepEqual((await api.friend('request', spammer.token, targets[0].nickname)).body, { error: 'too_many_pending' });

    const lists = (await api.friends(spammer.token)).body;
    assert.deepEqual(names(lists.friends), ['Victim']);
    assert.equal(lists.outgoing.length, MAX_PENDING_REQUESTS);
  });

  test('ko\'p kiruvchi so\'rov: ro\'yxatda ko\'pi bilan MAX_PENDING_REQUESTS ta', async () => {
    for (const target of targets) {
      assert.deepEqual((await api.friend('request', target.token, 'Victim')).body, { friendship: 'outgoing' });
    }
    await api.heartbeat(targets[MAX_PENDING_REQUESTS].token); // onlayn: tartibda birinchi
    const lists = (await api.friends(victim.token)).body;
    assert.equal(lists.incoming.length, MAX_PENDING_REQUESTS);
    assert.deepEqual(names(lists.incoming),
      [targets[MAX_PENDING_REQUESTS], ...targets.slice(0, MAX_PENDING_REQUESTS - 1)].map(t => t.nickname));
    assert.deepEqual(names(lists.friends), ['Spammer']);

    // Rad etilgach navbatdagilar ko'rinadi
    await api.friend('remove', victim.token, targets[0].nickname);
    const next = (await api.friends(victim.token)).body.incoming;
    assert.equal(next.length, MAX_PENDING_REQUESTS);
    assert.equal(next.at(-1).nickname, targets[MAX_PENDING_REQUESTS - 1].nickname);
  });
});

describe('reyting', () => {
  let api;
  before(async () => { api = await startApi(); });
  after(() => api.close());

  /** O'yinchiga `seconds` soniya onlayn vaqt beradi (heartbeat'lar orqali). */
  function giveOnline(id, seconds) {
    let at = START - 10 * 24 * 3600_000;
    api.db.touchPresence(id, new Date(at));
    while (seconds > 0) {
      const step = Math.min(seconds, 60);
      at += step * 1000;
      api.db.touchPresence(id, new Date(at));
      seconds -= step;
    }
  }

  test('onlayn vaqt bo\'yicha, teng bo\'lsa oldin ro\'yxatdan o\'tgan yuqorida', async () => {
    const players = {};
    for (const name of ['Early', 'Late', 'Top', 'Zero', 'Mid']) {
      players[name] = await api.register(name);
      api.tick(1);
    }
    giveOnline(players.Top.id, 1000); // 16 daqiqa 40 soniya
    giveOnline(players.Early.id, 179); // 2 daqiqa 59 soniya
    giveOnline(players.Late.id, 179);
    giveOnline(players.Mid.id, 600);

    const res = await api.call('GET', '/api/leaderboard');
    assert.equal(res.status, 200);
    assert.deepEqual(res.body, [
      { rank: 1, nickname: 'Top', avatarId: 'M1', gender: 'male', minutes: 16 },
      { rank: 2, nickname: 'Mid', avatarId: 'M1', gender: 'male', minutes: 10 },
      { rank: 3, nickname: 'Early', avatarId: 'M1', gender: 'male', minutes: 2 },
      { rank: 4, nickname: 'Late', avatarId: 'M1', gender: 'male', minutes: 2 },
      { rank: 5, nickname: 'Zero', avatarId: 'M1', gender: 'male', minutes: 0 },
    ]);
  });

  test('limit: standart 20, ko\'pi bilan 50', async () => {
    for (let i = 0; i < 55; i++) await api.register(`Racer${i}`);
    const count = async query => (await api.call('GET', `/api/leaderboard${query}`)).body.length;
    assert.equal(await count(''), 20);
    assert.equal(await count('?limit=5'), 5);
    assert.equal(await count('?limit=50'), 50);
    assert.equal(await count('?limit=100'), 50);
    assert.equal(await count('?limit=0'), 1);
    assert.equal(await count('?limit=abc'), 20);
    assert.equal(await count('?limit=-3'), 20);
    const ranks = (await api.call('GET', '/api/leaderboard?limit=50')).body.map(r => r.rank);
    assert.deepEqual(ranks, Array.from({ length: 50 }, (_, i) => i + 1));
  });

  test('onlayn holatini yashirganlar reytingda yo\'q: ochiq reytingdan kirgani sezilmaydi', async () => {
    const own = await startApi();
    try {
      const shy = await own.register('Shy');
      const open = await own.register('Open', 'female');
      for (let i = 0; i < 3; i++) {
        await own.heartbeat(shy.token);
        await own.heartbeat(open.token);
        own.tick(30);
      }
      await own.patch(shy.token, { showOnline: false });
      const board = (await own.call('GET', '/api/leaderboard')).body;
      assert.deepEqual(board, [{ rank: 1, nickname: 'Open', avatarId: 'F1', gender: 'female', minutes: 1 }]);

      // Shy o'ynashda davom etadi: o'z vaqti hisoblanadi, ochiq reyting esa o'zgarmaydi
      for (let i = 0; i < 4; i++) {
        await own.heartbeat(shy.token);
        own.tick(30);
      }
      assert.equal((await own.me(shy.token)).body.onlineSeconds, 180);
      assert.deepEqual((await own.call('GET', '/api/leaderboard')).body, board);

      await own.patch(shy.token, { showOnline: true });
      assert.deepEqual((await own.call('GET', '/api/leaderboard')).body, [
        { rank: 1, nickname: 'Shy', avatarId: 'M1', gender: 'male', minutes: 3 },
        { rank: 2, nickname: 'Open', avatarId: 'F1', gender: 'female', minutes: 1 },
      ]);
    } finally {
      await own.close();
    }
  });
});

describe('tadbirlar', () => {
  test('keyingi 7 kun: davom etayotganlari ham, Toshkent vaqti UTC\'ga o\'giriladi', () => {
    const events = upcomingEvents(new Date(START)); // yakshanba 17:00 (Toshkent)
    assert.deepEqual(events.map(e => e.id), [
      'new-friends-day-2026-09-27', // 16:00-18:00, hozir davom etmoqda
      'concert-2026-09-27',
      'concert-2026-09-28',
      'it-academy-2026-09-29',
      'concert-2026-09-29',
      'concert-2026-09-30',
      'it-academy-2026-10-01',
      'concert-2026-10-01',
      'startup-meetup-2026-10-02',
      'concert-2026-10-02',
      'weekend-sale-2026-10-03',
      'concert-2026-10-03',
      'new-friends-day-2026-10-04', // 7 kun ichida boshlanadi (keyingi konsert esa yo'q)
    ]);

    const byId = Object.fromEntries(events.map(e => [e.id, e]));
    assert.deepEqual(byId['it-academy-2026-09-29'], {
      id: 'it-academy-2026-09-29',
      zone: 'education',
      title: { uz: 'IT Academy: ochiq dars', en: 'IT Academy: open lesson' },
      place: { uz: "O'quv markazlari", en: 'Education centers' },
      startsAt: '2026-09-29T13:00:00.000Z',
      endsAt: '2026-09-29T14:30:00.000Z',
    });
    assert.deepEqual(byId['concert-2026-09-27'].title, { uz: 'Virtual konsert', en: 'Virtual Concert' });
    assert.deepEqual(byId['concert-2026-09-27'].place, { uz: "Ko'ngilochar zona", en: 'Entertainment zone' });
    assert.equal(byId['concert-2026-09-27'].startsAt, '2026-09-27T15:00:00.000Z');
    assert.equal(byId['concert-2026-09-27'].endsAt, '2026-09-27T17:00:00.000Z');
    assert.deepEqual(byId['startup-meetup-2026-10-02'].title, { uz: 'Startaplar uchrashuvi', en: 'Startup meetup' });
    assert.equal(byId['startup-meetup-2026-10-02'].startsAt, '2026-10-02T14:00:00.000Z');
    assert.deepEqual(byId['weekend-sale-2026-10-03'].title, { uz: 'Dam olish kuni savdosi', en: 'Weekend sale' });
    assert.equal(byId['weekend-sale-2026-10-03'].startsAt, '2026-10-03T07:00:00.000Z');
    assert.equal(byId['weekend-sale-2026-10-03'].endsAt, '2026-10-03T13:00:00.000Z');
    assert.deepEqual(byId['new-friends-day-2026-09-27'].title, { uz: "Yangi do'stlar kuni", en: 'New friends day' });
    assert.equal(byId['new-friends-day-2026-09-27'].zone, 'community');

    for (const event of events) {
      assert.deepEqual(Object.keys(event), ['id', 'zone', 'title', 'place', 'startsAt', 'endsAt']);
      assert.ok(ZONES.includes(event.zone), event.zone);
      assert.ok(Date.parse(event.endsAt) > START);
      assert.ok(Date.parse(event.startsAt) < START + 7 * 24 * 3600_000);
    }
    const starts = events.map(e => e.startsAt);
    assert.deepEqual(starts, [...starts].sort());
  });

  test('tugagan tadbir chiqmaydi', () => {
    const justEnded = upcomingEvents(new Date('2026-09-27T13:00:00.000Z')); // 18:00, "Yangi do'stlar kuni" tugadi
    assert.equal(justEnded[0].id, 'concert-2026-09-27');
    const almost = upcomingEvents(new Date('2026-09-27T12:59:59.000Z'));
    assert.equal(almost[0].id, 'new-friends-day-2026-09-27');
  });

  test('Toshkent yarim tunidan keyin sana va hafta kuni to\'g\'ri', () => {
    // UTC bo'yicha hali dushanba 19:30, Toshkentda esa seshanba 00:30
    const events = upcomingEvents(new Date('2026-09-28T19:30:00.000Z'));
    assert.equal(events[0].id, 'it-academy-2026-09-29');
    assert.equal(events[0].startsAt, '2026-09-29T13:00:00.000Z');
  });

  test('yarim tundan o\'tadigan tadbir va 20 ta chegarasi', () => {
    const night = [{
      id: 'night', zone: 'entertainment', days: [0], start: '23:00', end: '01:00',
      title: { uz: 'Tun', en: 'Night' }, place: { uz: 'Zona', en: 'Zone' },
    }];
    // Dushanba 00:30 (Toshkent): yakshanba kechasi boshlangan tadbir davom etmoqda
    const [ongoing] = upcomingEvents(new Date('2026-09-27T19:30:00.000Z'), { schedule: night });
    assert.equal(ongoing.id, 'night-2026-09-27');
    assert.equal(ongoing.startsAt, '2026-09-27T18:00:00.000Z');
    assert.equal(ongoing.endsAt, '2026-09-27T20:00:00.000Z');

    const busy = ['a', 'b', 'c', 'd'].map((id, i) => ({ ...WEEKLY_SCHEDULE[0], id, start: `0${i + 1}:00`, end: `0${i + 1}:30` }));
    assert.equal(upcomingEvents(new Date(START), { schedule: busy }).length, 20);
  });

  test('GET /api/events ochiq va joriy vaqtdan foydalanadi', async () => {
    const api = await startApi();
    try {
      let res = await api.call('GET', '/api/events');
      assert.equal(res.status, 200);
      assert.deepEqual(res.body, upcomingEvents(new Date(START)));
      api.tick(3600 * 24);
      res = await api.call('GET', '/api/events');
      assert.equal(res.body[0].id, 'concert-2026-09-28');
      assert.ok(res.body.length <= 20);
    } finally {
      await api.close();
    }
  });
});

describe('so\'rovlar cheklovi va CORS', () => {
  test('ijtimoiy so\'rovlar umumiy cheklovda, qidiruv alohida', async () => {
    const api = await startApi({ rateLimits: { check: 1000, create: 1000, social: 3, search: 2, presence: 1000 } });
    try {
      const { token } = await api.register('Limited');
      await api.register('Other');
      assert.equal((await api.friends(token)).status, 200);
      assert.equal((await api.suggested(token)).status, 200);
      assert.equal((await api.search(token, 'oth')).status, 200);
      assert.equal((await api.search(token, 'oth')).status, 200);
      assert.deepEqual((await api.search(token, 'oth')).body, { error: 'too_many_requests' });
      assert.equal((await api.friend('request', token, 'Other')).status, 200);
      assert.equal((await api.friend('remove', token, 'Other')).status, 429);
      assert.equal((await api.friends(token)).status, 429);
      // Profil va heartbeat boshqa cheklovda
      assert.equal((await api.me(token)).status, 200);
      assert.equal((await api.heartbeat(token)).status, 200);
    } finally {
      await api.close();
    }
  });

  test('heartbeat yaratish cheklovini sarflamaydi', async () => {
    const api = await startApi({ rateLimits: { check: 1000, create: 1, presence: 3 } });
    try {
      const { token } = await api.register('Beating');
      for (let i = 0; i < 3; i++) assert.equal((await api.heartbeat(token)).status, 200);
      assert.equal((await api.heartbeat(token)).status, 429); // o'z cheklovi bor
      const second = await api.call('POST', '/api/players', { body: { nickname: 'Second', gender: 'male', avatarId: 'M1' } });
      assert.equal(second.status, 429);
    } finally {
      await api.close();
    }
  });

  test('standart cheklovlar: social 120 va search 60 IP bo\'yicha, me va presence o\'yinchi bo\'yicha', async () => {
    const api = await startApi({ rateLimits: {} });
    try {
      // Ikki o'yinchi bitta IP ortida (kompyuter klubi, NAT)
      const a = await api.register('LimitA');
      const b = await api.register('LimitB');

      // social: friends, suggested va friends/* ikkala o'yinchi uchun bitta hisobda
      for (let i = 0; i < 120; i++) {
        const res = i % 2 ? await api.friends(a.token) : await api.suggested(b.token);
        assert.equal(res.status, 200, `social ${i}`);
      }
      assert.deepEqual((await api.friend('request', b.token, 'LimitA')).body, { error: 'too_many_requests' });
      assert.equal((await api.friends(a.token)).status, 429);

      // search alohida: 60
      for (let i = 0; i < 60; i++) assert.equal((await api.search(i % 2 ? a.token : b.token, 'lim')).status, 200, `search ${i}`);
      assert.equal((await api.search(b.token, 'lim')).status, 429);

      // heartbeat: har bir o'yinchiga 120, bir IP dagi boshqa o'yinchiga ta'sir qilmaydi
      for (let i = 0; i < 120; i++) assert.equal((await api.heartbeat(a.token)).status, 200, `presence ${i}`);
      assert.equal((await api.heartbeat(a.token)).status, 429);
      assert.equal((await api.heartbeat(b.token)).status, 200);

      // profil: har bir o'yinchiga 60 (GET va PATCH birga)
      for (let i = 0; i < 60; i++) {
        const res = i % 2 ? await api.me(a.token) : await api.patch(a.token, { country: 'UZ' });
        assert.equal(res.status, 200, `me ${i}`);
      }
      assert.equal((await api.me(a.token)).status, 429);
      assert.equal((await api.patch(a.token, { country: 'UZ' })).status, 429);
      assert.equal((await api.me(b.token)).status, 200);
    } finally {
      await api.close();
    }
  });

  test('TRUST_PROXY: cheklov X-Forwarded-For\'dagi o\'yinchi manzili bo\'yicha, aks holda sarlavha e\'tiborsiz', async () => {
    const check = (api, forwarded) => fetch(`http://127.0.0.1:${api.port}/api/nicknames/availability?name=Proxied`,
      { headers: forwarded === undefined ? {} : { 'X-Forwarded-For': forwarded } }).then(res => res.status);

    const proxied = await startApi({ rateLimits: { ...HIGH_LIMITS, check: 2 }, trustProxy: 1 });
    try {
      // Proksi ortidagi har bir o'yinchi o'z cheklovida (proksi manzili hammaga umumiy emas)
      const first = [];
      for (let i = 0; i < 3; i++) first.push(await check(proxied, '203.0.113.5'));
      assert.deepEqual(first, [200, 200, 429]);
      assert.equal(await check(proxied, '198.51.100.7'), 200);
      // Chapdagi yozuvlarni mijozning o'zi yozishi mumkin: proksi qo'shgan o'ngdagisi hisoblanadi
      assert.equal(await check(proxied, 'spoofed-1, 203.0.113.5'), 429);
      assert.equal(await check(proxied, ' spoofed-2 ,203.0.113.5 '), 429);
      // Sarlavha yo'q: ulanish manzili
      assert.equal(await check(proxied), 200);
    } finally {
      await proxied.close();
    }

    const twoProxies = await startApi({ rateLimits: { ...HIGH_LIMITS, check: 1 }, trustProxy: 2 });
    try {
      assert.equal(await check(twoProxies, 'fake, 203.0.113.9, 10.0.0.2'), 200);
      assert.equal(await check(twoProxies, 'other, 203.0.113.9, 10.0.0.3'), 429); // o'sha o'yinchi, boshqa ichki proksi
      assert.equal(await check(twoProxies, '203.0.113.10'), 200); // yozuv kam: eng chapdagisi
    } finally {
      await twoProxies.close();
    }

    // Proksiga ishonilmasa sarlavha orqali cheklovni aylanib o'tib bo'lmaydi
    const direct = await startApi({ rateLimits: { ...HIGH_LIMITS, check: 2 } });
    try {
      const statuses = [];
      for (const forwarded of ['1.1.1.1', '2.2.2.2', '3.3.3.3']) statuses.push(await check(direct, forwarded));
      assert.deepEqual(statuses, [200, 200, 429]);
    } finally {
      await direct.close();
    }
  });

  test('buzilgan so\'rov manzili 400 qaytaradi va serverni to\'xtatmaydi', async () => {
    const api = await startApi();
    try {
      for (const target of ['http://a:99999/api/stats', 'http://%zz/', 'http://[::1/api/stats']) {
        const response = await rawRequest(api.port, `GET ${target} HTTP/1.1`);
        assert.match(response, /^HTTP\/1\.1 400 /, target);
      }
      const response = await rawRequest(api.port, 'GET http://a:99999/ HTTP/1.1');
      assert.deepEqual(JSON.parse(response.split('\r\n\r\n')[1]), { error: 'bad_request' });
      assert.match(response, /access-control-allow-origin: \*/i);
      // Oddiy absolyut manzil ishlayveradi
      assert.match(await rawRequest(api.port, 'GET http://localhost/health HTTP/1.1'), /^HTTP\/1\.1 200 /);
      assert.deepEqual((await api.call('GET', '/health')).body, { ok: true });
    } finally {
      await api.close();
    }
  });

  test('CORS usullari va noma\'lum yo\'llar', async () => {
    const api = await startApi();
    try {
      const options = await api.call('OPTIONS', '/api/friends');
      assert.equal(options.status, 204);
      assert.equal(options.headers.get('access-control-allow-methods'), 'GET, POST, PATCH, OPTIONS');
      assert.equal((await api.call('GET', '/api/stats')).headers.get('access-control-allow-origin'), '*');
      assert.equal((await api.call('DELETE', '/api/friends')).status, 404);
      assert.equal((await api.call('GET', '/api/friends/request')).status, 404);
      assert.deepEqual((await api.call('GET', '/api/nope')).body, { error: 'not_found' });
    } finally {
      await api.close();
    }
  });
});

describe('profilni tiklash (POST /api/players/restore)', () => {
  let api;
  before(async () => { api = await startApi(); });
  after(() => api.close());

  const restore = (body, target = api) => target.call('POST', '/api/players/restore', { body });
  /** O'yin PlayerPrefs'da saqlagan profil (server bazasi yangidan boshlangan). */
  const saved = (overrides = {}) => ({
    id: randomUUID(), token: randomBytes(32).toString('hex'), nickname: 'Returner', gender: 'female', avatarId: 'F3',
    ...overrides,
  });

  test('mijozdagi profil qayta yaratiladi, token ishlaydi, takror so\'rov o\'sha profilni qaytaradi', async () => {
    const profile = saved({
      nickname: 'Comeback', outfit: '{"hair":"hair_pink","hairColor":"D96C9C"}', country: 'KZ', showOnline: false,
      allowRequests: true,
    });
    const res = await restore(profile);
    assert.equal(res.status, 201);
    assert.deepEqual(Object.keys(res.body), PROFILE_KEYS);
    assert.equal(res.body.token, undefined);
    assert.deepEqual(pick(res.body, ['id', 'nickname', 'gender', ...SETTINGS]), {
      id: profile.id, nickname: 'Comeback', gender: 'female', avatarId: 'F3', outfit: profile.outfit, country: 'KZ',
      showOnline: false, allowRequests: true,
    });
    assert.ok(res.body.publicId >= 100000 && res.body.publicId <= 999999);

    const me = await api.me(profile.token);
    assert.deepEqual([me.status, me.body.id, me.body.nickname], [200, profile.id, 'Comeback']);
    assert.equal((await api.patch(profile.token, { avatarId: 'F5' })).status, 200);

    // Javob yetib kelmay qayta yuborilgan so'rov
    const again = await restore({ ...profile, token: profile.token.toUpperCase() });
    assert.deepEqual([again.status, again.body.id, again.body.avatarId], [200, profile.id, 'F5']);
    assert.equal((await api.call('GET', '/api/nicknames/availability?name=COMEBACK')).body.reason, 'taken');
  });

  test('band nickname, id yoki token: 409 va hech narsa yozilmaydi', async () => {
    const owner = await api.register('Taken_Name');
    let res = await restore(saved({ nickname: 'TAKEN_NAME' }));
    assert.deepEqual([res.status, res.body.error], [409, 'nickname_taken']);
    res = await restore(saved({ id: owner.id, nickname: 'Other_Name' })); // boshqa o'yinchining id'si
    assert.deepEqual([res.status, res.body], [409, { error: 'conflict' }]);
    res = await restore(saved({ token: owner.token, nickname: 'Third_Name' })); // boshqa o'yinchining tokeni
    assert.deepEqual([res.status, res.body], [409, { error: 'conflict' }]);

    const me = (await api.me(owner.token)).body;
    assert.deepEqual([me.id, me.nickname, me.gender], [owner.id, 'Taken_Name', 'male']);
    for (const name of ['Other_Name', 'Third_Name']) {
      assert.equal((await api.call('GET', `/api/nicknames/availability?name=${name}`)).body.available, true, name);
    }
  });

  test('noto\'g\'ri ma\'lumot 400; avatar va sozlamalar yumshoq tekshiriladi', async () => {
    const cases = [
      [{ token: 'abc' }, 'invalid_token'],
      [{ token: undefined }, 'invalid_token'],
      [{ token: 'g'.repeat(64) }, 'invalid_token'],
      [{ id: 'old-0' }, 'invalid_id'],
      [{ id: 42 }, 'invalid_id'],
      [{ nickname: 'ab' }, 'invalid_nickname'],
      [{ nickname: 'admin' }, 'invalid_nickname'],
      [{ nickname: undefined }, 'invalid_nickname'],
      [{ gender: 'other' }, 'invalid_gender'],
    ];
    for (const [override, error] of cases) {
      const res = await restore(saved({ nickname: 'Valid_Restore', ...override }));
      assert.deepEqual([res.status, res.body.error], [400, error], JSON.stringify(override));
    }
    const broken = await api.call('POST', '/api/players/restore', { raw: '{oops' });
    assert.deepEqual([broken.status, broken.body], [400, { error: 'bad_json' }]);

    // Avatar bo'sh (eng eski profil) yoki jinsga mos emas: jinsning birinchi avatari; noto'g'ri sozlamalar e'tiborsiz
    const res = await restore(saved({
      nickname: 'Valid_Restore', gender: 'male', avatarId: '', outfit: '{"hat":1}', country: 'uzb', showOnline: 'no',
    }));
    assert.equal(res.status, 201);
    assert.deepEqual(pick(res.body, SETTINGS),
      { avatarId: 'M1', outfit: '', country: 'UZ', showOnline: true, allowRequests: true });
    assert.equal((await restore(saved({ nickname: 'Cross_Gender', gender: 'female', avatarId: 'M2' }))).body.avatarId, 'F1');
  });

  test('tiklash yaratish cheklovidan foydalanadi', async () => {
    const limited = await startApi({ rateLimits: { ...HIGH_LIMITS, create: 2 } });
    try {
      assert.equal((await restore(saved({ nickname: 'Limit_One' }), limited)).status, 201);
      await limited.register('Limit_Two');
      assert.deepEqual((await restore(saved({ nickname: 'Limit_Three' }), limited)).body, { error: 'too_many_requests' });
    } finally {
      await limited.close();
    }
  });
});

describe('eski bazani ko\'chirish', () => {
  let dir;
  before(() => { dir = mkdtempSync(join(tmpdir(), 'cradev-migrate-')); });
  after(() => rmSync(dir, { recursive: true, force: true }));

  const sha256 = text => createHash('sha256').update(text).digest('hex');

  /** Eski sxemali baza yaratadi va o'yinchilar tokenlarini qaytaradi. */
  function createOldDatabase(path, { withAvatar }) {
    const old = new DatabaseSync(path);
    old.exec(`
      PRAGMA journal_mode = WAL;
      CREATE TABLE players (
        id            TEXT PRIMARY KEY,
        nickname      TEXT NOT NULL,
        nickname_key  TEXT NOT NULL UNIQUE,
        gender        TEXT NOT NULL CHECK (gender IN ('male', 'female')),
        ${withAvatar ? "avatar_id     TEXT NOT NULL DEFAULT ''," : ''}
        token_hash    TEXT NOT NULL,
        created_at    TEXT NOT NULL
      );
    `);
    const tokens = {};
    const columns = ['id', 'nickname', 'nickname_key', 'gender', ...(withAvatar ? ['avatar_id'] : []), 'token_hash', 'created_at'];
    const insert = old.prepare(`INSERT INTO players (${columns.join(', ')}) VALUES (${columns.map(() => '?').join(', ')})`);
    ['Veteran', 'OldTimer', 'Pioneer'].forEach((nickname, i) => {
      const token = randomBytes(32).toString('hex');
      tokens[nickname] = token;
      const values = [`old-${i}`, nickname, nickname.toLowerCase(), i === 1 ? 'female' : 'male'];
      if (withAvatar) values.push(i === 1 ? 'F2' : 'M3');
      insert.run(...values, sha256(token), `2026-01-0${i + 1}T10:00:00.000Z`);
    });
    old.close();
    return tokens;
  }

  const columnsOf = path => {
    const raw = new DatabaseSync(path);
    try {
      return raw.prepare('PRAGMA table_info(players)').all().map(c => c.name);
    } finally {
      raw.close();
    }
  };

  test('avatar_id bor eski baza: ustunlar qo\'shiladi, publicId beriladi, tokenlar ishlaydi', async () => {
    const path = join(dir, 'previous.db');
    const tokens = createOldDatabase(path, { withAvatar: true });

    const api = await startApi({ db: openDatabase(path) });
    let publicIds;
    try {
      const veteran = await api.me(tokens.Veteran);
      assert.equal(veteran.status, 200);
      assert.deepEqual(Object.keys(veteran.body), PROFILE_KEYS);
      assert.deepEqual({ ...veteran.body, publicId: 0 }, {
        id: 'old-0', publicId: 0, nickname: 'Veteran', gender: 'male', avatarId: 'M3', outfit: '', country: 'UZ',
        showOnline: true, allowRequests: true, createdAt: '2026-01-01T10:00:00.000Z', onlineSeconds: 0,
      });

      publicIds = [];
      for (const token of Object.values(tokens)) publicIds.push((await api.me(token)).body.publicId);
      assert.ok(publicIds.every(id => Number.isInteger(id) && id >= 100000 && id <= 999999));
      assert.equal(new Set(publicIds).size, 3);
      assert.equal((await api.me(tokens.OldTimer)).body.avatarId, 'F2');

      // Yangi funksiyalar eski o'yinchilar bilan ishlaydi
      assert.equal((await api.patch(tokens.Pioneer, { country: 'KG', outfit: '{"hair":"hair_blond","hairColor":"C9A46A"}' })).status, 200);
      assert.equal((await api.heartbeat(tokens.Pioneer)).status, 200);
      assert.deepEqual((await api.friend('request', tokens.Veteran, 'oldtimer')).body, { friendship: 'outgoing' });
      assert.deepEqual((await api.friend('accept', tokens.OldTimer, 'Veteran')).body, { friendship: 'friends' });
      const newcomer = await api.register('Newcomer');
      assert.ok(!publicIds.includes(newcomer.publicId));
      const taken = await api.call('POST', '/api/players', { body: { nickname: 'VETERAN', gender: 'male', avatarId: 'M1' } });
      assert.equal(taken.status, 409);
    } finally {
      await api.close();
    }

    for (const column of ['public_id', 'outfit', 'country', 'show_online', 'allow_requests', 'last_seen', 'online_seconds']) {
      assert.ok(columnsOf(path).includes(column), column);
    }
    // Har bir token bilan kelgan so'rov (heartbeat har 30 soniyada) butun jadvalni ko'rib chiqmasligi kerak
    const raw = new DatabaseSync(path);
    try {
      const plan = sql => raw.prepare(`EXPLAIN QUERY PLAN ${sql}`).all().map(row => row.detail).join(' | ');
      assert.match(plan('SELECT id FROM players WHERE token_hash = ?'), /USING (COVERING )?INDEX players_token_hash/);
      assert.match(plan('SELECT COUNT(*) FROM players WHERE last_seen >= ?'), /USING (COVERING )?INDEX players_last_seen/);
    } finally {
      raw.close();
    }

    // Qayta ochilganda hech narsa o'zgarmaydi
    const reopened = await startApi({ db: openDatabase(path) });
    try {
      const again = [];
      for (const token of Object.values(tokens)) again.push((await reopened.me(token)).body.publicId);
      assert.deepEqual(again, publicIds);
      const pioneer = (await reopened.me(tokens.Pioneer)).body;
      assert.equal(pioneer.country, 'KG');
      assert.equal(pioneer.outfit, '{"hair":"hair_blond","hairColor":"C9A46A"}');
      assert.deepEqual(names((await reopened.friends(tokens.Veteran)).body.friends), ['OldTimer']);
    } finally {
      await reopened.close();
    }
  });

  test('avatar_id ham yo\'q eng eski baza', async () => {
    const path = join(dir, 'oldest.db');
    const tokens = createOldDatabase(path, { withAvatar: false });
    const api = await startApi({ db: openDatabase(path) });
    try {
      const me = (await api.me(tokens.OldTimer)).body;
      assert.equal(me.avatarId, '');
      assert.equal(me.country, 'UZ');
      assert.equal(me.showOnline, true);
      assert.ok(me.publicId >= 100000 && me.publicId <= 999999);
      assert.equal((await api.patch(tokens.OldTimer, { avatarId: 'F4' })).body.avatarId, 'F4');
      assert.deepEqual((await api.call('GET', '/api/stats')).body, { online: 0, players: 3 });
    } finally {
      await api.close();
    }
    assert.ok(columnsOf(path).includes('avatar_id'));
  });
});
