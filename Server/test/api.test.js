import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { after, before, describe, test } from 'node:test';
import { createApp } from '../src/app.js';
import { openDatabase } from '../src/db.js';
import { isValidAvatar, validateNickname } from '../src/nickname.js';

describe('nickname qoidalari', () => {
  test('to\'g\'ri nickname\'lar', () => {
    for (const name of ['Ali', 'dark_knight', 'Player2026', 'abcdefghijklmnop']) {
      assert.equal(validateNickname(name).ok, true, name);
    }
  });

  test('noto\'g\'ri nickname\'lar', () => {
    for (const name of ['', 'ab', 'abcdefghijklmnopq', '1player', '_ali', 'ali baba', 'Алишер', 'ali!']) {
      assert.equal(validateNickname(name).ok, false, name);
    }
  });

  test('band qilingan nomlar', () => {
    assert.deepEqual(validateNickname('Admin').reason, 'reserved');
  });

  test('avatar jinsga mos bo\'lishi kerak', () => {
    assert.equal(isValidAvatar('M3', 'male'), true);
    assert.equal(isValidAvatar('F5', 'female'), true);
    assert.equal(isValidAvatar('F1', 'male'), false);
    assert.equal(isValidAvatar('M6', 'male'), false);
    assert.equal(isValidAvatar('M4', 'male'), false); // M4 olib tashlangan
    assert.equal(isValidAvatar('M5', 'male'), true);
    assert.equal(isValidAvatar('M1', 'toString'), false);
    assert.equal(isValidAvatar(undefined, 'male'), false);
  });
});

describe('API', () => {
  let server, base, db;

  before(async () => {
    db = openDatabase(':memory:');
    server = createServer(createApp(db, { rateLimits: { check: 1000, create: 1000 } }));
    await new Promise(resolve => server.listen(0, resolve));
    base = `http://127.0.0.1:${server.address().port}`;
  });

  after(() => {
    server.close();
    db.close();
  });

  const check = name => fetch(`${base}/api/nicknames/availability?name=${encodeURIComponent(name)}`).then(r => r.json());
  const create = body => fetch(`${base}/api/players`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body),
  });

  test('bo\'sh nickname mavjud', async () => {
    assert.deepEqual(await check('Shadow'), { name: 'Shadow', available: true });
  });

  test('o\'yinchi yaratiladi va nickname band bo\'ladi (katta-kichik harfdan qat\'i nazar)', async () => {
    const res = await create({ nickname: 'Shadow', gender: 'male', avatarId: 'M3' });
    assert.equal(res.status, 201);
    const player = await res.json();
    assert.equal(player.nickname, 'Shadow');
    assert.equal(player.gender, 'male');
    assert.equal(player.avatarId, 'M3');
    assert.match(player.token, /^[0-9a-f]{64}$/);

    const again = await check('shadow');
    assert.equal(again.available, false);
    assert.equal(again.reason, 'taken');

    const dup = await create({ nickname: 'SHADOW', gender: 'female', avatarId: 'F2' });
    assert.equal(dup.status, 409);
    assert.equal((await dup.json()).error, 'nickname_taken');
  });

  test('bir vaqtda kelgan so\'rovlardan faqat bittasi nickname\'ni oladi', async () => {
    const results = await Promise.all(Array.from({ length: 8 }, (_, i) =>
      create({ nickname: 'Racer', gender: i % 2 ? 'male' : 'female', avatarId: i % 2 ? 'M1' : 'F1' }).then(r => r.status)));
    assert.equal(results.filter(s => s === 201).length, 1);
    assert.equal(results.filter(s => s === 409).length, 7);
  });

  test('noto\'g\'ri ma\'lumotlar rad etiladi', async () => {
    assert.equal((await create({ nickname: 'ab', gender: 'male', avatarId: 'M1' })).status, 400);
    assert.equal((await create({ nickname: 'Valid_Name', gender: 'other', avatarId: 'M1' })).status, 400);
    const wrongAvatar = await create({ nickname: 'Valid_Name', gender: 'male', avatarId: 'F1' });
    assert.equal(wrongAvatar.status, 400);
    assert.equal((await wrongAvatar.json()).error, 'invalid_avatar');
    const bad = await fetch(`${base}/api/players`, { method: 'POST', body: '{not json' });
    assert.equal(bad.status, 400);
    assert.equal((await check('admin')).reason, 'reserved');
    assert.equal((await check('1abc')).reason, 'invalid');
  });

  test('o\'yinchi o\'z profilini token bilan oladi va avatarini o\'zgartiradi', async () => {
    const created = await (await create({ nickname: 'Returner', gender: 'female', avatarId: 'F1' })).json();
    const me = (method, token, body) => fetch(`${base}/api/players/me`, {
      method,
      headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
      body: body && JSON.stringify(body),
    });

    const got = await me('GET', created.token);
    assert.equal(got.status, 200);
    const profile = await got.json();
    assert.equal(profile.nickname, 'Returner');
    assert.equal(profile.avatarId, 'F1');
    assert.equal(profile.token, undefined); // token qaytarilmaydi

    const changed = await me('PATCH', created.token, { avatarId: 'F4' });
    assert.equal(changed.status, 200);
    assert.equal((await changed.json()).avatarId, 'F4');
    assert.equal((await (await me('GET', created.token)).json()).avatarId, 'F4');

    // Jinsga mos bo'lmagan avatar, noto'g'ri va yo'q token
    assert.equal((await me('PATCH', created.token, { avatarId: 'M1' })).status, 400);
    assert.equal((await me('GET', 'a'.repeat(64))).status, 401);
    assert.equal((await fetch(`${base}/api/players/me`)).status, 401);
  });

  test('so\'rovlar cheklovi', async () => {
    const limited = createServer(createApp(openDatabase(':memory:'), { rateLimits: { check: 2, create: 1 } }));
    await new Promise(resolve => limited.listen(0, resolve));
    const url = `http://127.0.0.1:${limited.address().port}/api/nicknames/availability?name=Test1`;
    const statuses = [];
    for (let i = 0; i < 3; i++) statuses.push((await fetch(url)).status);
    limited.close();
    assert.deepEqual(statuses, [200, 200, 429]);
  });
});
