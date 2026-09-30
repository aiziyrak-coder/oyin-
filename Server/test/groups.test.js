import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { after, before, describe, test } from 'node:test';
import { createApp } from '../src/app.js';
import { openDatabase } from '../src/db.js';
import { GROUP_LIMITS, parseGroupCode } from '../src/groups.js';

const DAY = 86_400_000;

async function startApi(rateLimits = { create: 10_000, groups: 10_000, groupCreate: 10_000, party: 10_000, social: 10_000, search: 10_000 }) {
  const db = openDatabase(':memory:');
  const clock = { ms: Date.parse('2026-09-29T10:00:00Z') };
  const server = createServer(createApp(db, { rateLimits, now: () => new Date(clock.ms) }));
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const base = `http://127.0.0.1:${server.address().port}`;
  async function call(method, path, player, body) {
    const headers = player ? { Authorization: `Bearer ${player.token}` } : {};
    if (body !== undefined) headers['Content-Type'] = 'application/json';
    const res = await fetch(base + path, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) });
    const text = await res.text();
    return { status: res.status, body: text ? JSON.parse(text) : undefined };
  }
  return {
    db, clock, call,
    async register(nickname) {
      const res = await call('POST', '/api/players', null, { nickname, gender: 'male', avatarId: 'M1' });
      assert.equal(res.status, 201);
      return res.body;
    },
    post: (path, player, body) => call('POST', '/api/groups/' + path, player, body),
    get: (path, player) => call('GET', '/api/groups/' + path, player),
    async close() {
      server.closeAllConnections();
      await new Promise(resolve => server.close(resolve));
      db.close();
    },
  };
}

test('guruh kodi: NW-, havola va kichik harf qabul qilinadi', () => {
  assert.equal(parseGroupCode('NW-AB23CD'), 'AB23CD');
  assert.equal(parseGroupCode(' nw-ab23cd '), 'AB23CD');
  assert.equal(parseGroupCode('newworld://group/AB23CD'), 'AB23CD');
  assert.equal(parseGroupCode('AB23CD'), 'AB23CD');
  for (const bad of ['', 'AB23C', 'AB23CD1', 'AB0OCD', 'NW-AB 3CD', 42, null]) assert.equal(parseGroupCode(bad), null, String(bad));
});

describe('guruhlar', () => {
  let api, owner, bob, cara, dan;
  before(async () => {
    api = await startApi();
    owner = await api.register('GroupOwner');
    bob = await api.register('Bobby');
    cara = await api.register('Cara');
    dan = await api.register('Danny');
  });
  after(() => api.close());

  test('yaratish: tekshiruv va egasi a\'zo', async () => {
    for (const [body, error] of [
      [{ name: 'ab', kind: 'free' }, 'invalid_name'],
      [{ name: 'x'.repeat(41), kind: 'free' }, 'invalid_name'],
      [{ name: 'Bad\u0001name', kind: 'free' }, 'invalid_name'],
      [{ name: 'Good name', kind: 'secret' }, 'invalid_kind'],
      [{ name: 'Good name', kind: 'free', description: 'x'.repeat(301) }, 'invalid_description'],
      [{ name: 'Paid club', kind: 'paid', price: 0, currency: 'UZS', period: 'month' }, 'invalid_price'],
      [{ name: 'Paid club', kind: 'paid', price: 1.5, currency: 'UZS', period: 'month' }, 'invalid_price'],
      [{ name: 'Paid club', kind: 'paid', price: 1000, currency: 'EUR', period: 'month' }, 'invalid_currency'],
      [{ name: 'Paid club', kind: 'paid', price: 1000, currency: 'CDCoin', period: 'day' }, 'invalid_period'],
    ]) {
      const res = await api.post('create', owner, body);
      assert.deepEqual([res.status, res.body], [400, { error }], JSON.stringify(body));
    }
    assert.equal((await api.post('create', null, { name: 'Anon group', kind: 'free' })).status, 401);

    const res = await api.post('create', owner, { name: '  Toshkent   o\'yinchilari ', description: 'Salom', kind: 'free' });
    assert.equal(res.status, 201);
    assert.equal(res.body.name, "Toshkent o'yinchilari");
    assert.match(res.body.code, /^[A-Z2-9]{6}$/);
    assert.equal(res.body.role, 'owner');
    assert.equal(res.body.memberCount, 1);
    assert.deepEqual(res.body.members.map(m => [m.nickname, m.role, m.friendship]), [['GroupOwner', 'owner', 'self']]);
    assert.match(String(res.body.members[0].publicId), /^\d{6}$/);
  });

  test('ochiq guruh: qidiruv, qo\'shilish, a\'zolar, chiqish', async () => {
    const { body: group } = await api.post('create', owner, { name: 'Futbol klubi', kind: 'free' });
    const found = (await api.get('search?q=futb', bob)).body;
    assert.deepEqual(found.map(g => g.name), ['Futbol klubi']);
    assert.equal(found[0].role, '');
    assert.deepEqual((await api.get('search?q=f', bob)).body, { error: 'query_too_short' });
    assert.deepEqual((await api.get(`search?q=NW-${group.code}`, bob)).body.map(g => g.code), [group.code]);

    // A'zo bo'lmagan a'zolar ro'yxatini ko'rmaydi
    assert.deepEqual((await api.get(`detail?code=${group.code}`, bob)).body.members, []);
    const joined = await api.post('join', bob, { code: `newworld://group/${group.code}` });
    assert.equal(joined.body.status, 'joined');
    assert.deepEqual(joined.body.group.members.map(m => m.nickname), ['GroupOwner', 'Bobby']);
    assert.equal((await api.post('join', bob, { code: group.code })).body.status, 'member');

    const mine = (await api.get('mine', bob)).body;
    assert.deepEqual(mine.groups.map(g => [g.name, g.role]), [['Futbol klubi', 'member']]);

    assert.deepEqual((await api.post('leave', owner, { code: group.code })).body, { error: 'owner_cannot_leave' });
    assert.deepEqual((await api.post('leave', bob, { code: group.code })).body, { ok: true });
    assert.deepEqual((await api.post('leave', bob, { code: group.code })).body, { error: 'not_member' });
    assert.deepEqual((await api.post('join', bob, { code: 'ZZZZZZ' })).body, { error: 'group_not_found' });
    assert.deepEqual((await api.post('join', bob, { code: 'bad' })).body, { error: 'invalid_code' });
  });

  test('yopiq guruh: qidiruvda yo\'q, kod bilan so\'rov, admin tasdiqlaydi yoki rad etadi', async () => {
    const { body: group } = await api.post('create', owner, { name: 'Maxfiy jamoa', kind: 'private' });
    assert.deepEqual((await api.get('search?q=maxfiy', bob)).body, []);
    assert.equal((await api.get(`search?q=${group.code}`, bob)).body.length, 1); // kodni bilgan topadi

    const request = await api.post('join', bob, { code: group.code });
    assert.equal(request.body.status, 'requested');
    assert.equal(request.body.group.requested, true);
    assert.equal(request.body.group.pending, 0); // so'rovlar soni oddiy foydalanuvchiga ko'rinmaydi
    assert.deepEqual((await api.get('mine', bob)).body.requests.map(g => g.code), [group.code]);
    await api.post('join', cara, { code: group.code });

    // Oddiy a'zo tasdiqlay olmaydi
    assert.deepEqual((await api.post('approve', cara, { code: group.code, nickname: 'Bobby' })).body, { error: 'admin_only' });
    const detail = (await api.get(`detail?code=${group.code}`, owner)).body;
    assert.deepEqual(detail.requests.map(r => r.nickname), ['Bobby', 'Cara']);
    assert.equal(detail.pending, 2);

    const approved = await api.post('approve', owner, { code: group.code, nickname: 'Bobby' });
    assert.deepEqual(approved.body.members.map(m => m.nickname), ['GroupOwner', 'Bobby']);
    assert.equal(approved.body.members[1].expiresAt, '');
    assert.deepEqual((await api.post('reject', owner, { code: group.code, nickname: 'Cara' })).body.requests, []);
    assert.deepEqual((await api.post('approve', owner, { code: group.code, nickname: 'Cara' })).body, { error: 'no_request' });

    // Rol: admin boshqa a'zoni tasdiqlaydi va chiqaradi, lekin adminni emas
    assert.deepEqual((await api.post('role', bob, { code: group.code, nickname: 'Bobby', role: 'admin' })).body, { error: 'owner_only' });
    assert.equal((await api.post('role', owner, { code: group.code, nickname: 'Bobby', role: 'admin' })).status, 200);
    await api.post('join', dan, { code: group.code });
    assert.equal((await api.post('approve', bob, { code: group.code, nickname: 'Danny' })).status, 200);
    assert.deepEqual((await api.post('remove', bob, { code: group.code, nickname: 'GroupOwner' })).body, { error: 'cannot_remove' });
    assert.equal((await api.post('remove', bob, { code: group.code, nickname: 'Danny' })).status, 200);
    assert.equal((await api.post('remove', owner, { code: group.code, nickname: 'Bobby' })).status, 200);

    // So'rovni bekor qilish
    await api.post('join', cara, { code: group.code });
    assert.equal((await api.post('cancel', cara, { code: group.code })).body.requested, false);

    // O'chirish faqat egasiga
    assert.deepEqual((await api.post('delete', cara, { code: group.code })).body, { error: 'owner_only' });
    assert.deepEqual((await api.post('delete', owner, { code: group.code })).body, { ok: true });
    assert.equal((await api.get(`detail?code=${group.code}`, owner)).status, 404);
  });

  test('paid membership: owner receives full CDCoin payment, retries cannot double charge, membership expires', async () => {
    const { body: group } = await api.post('create', owner,
      { name: 'Premium kurs', kind: 'paid', price: 500, currency: 'CDCoin', period: 'month', description: 'Oylik' });
    const [found] = (await api.get('search?q=premium', cara)).body;
    assert.deepEqual([found.kind, found.price, found.currency, found.period], ['paid', 500, 'CDCoin', 'month']);
    const body = { code: group.code, expectedPrice: 500, paymentKey: 'subscription-test-0001' };
    assert.equal((await api.post('join', cara, body)).body.error, 'insufficient_coins');
    assert.equal((await api.post('approve', owner, { code: group.code, nickname: 'Cara' })).body.error, 'payment_required');
    api.db.wallet.creditVerified(cara.id, 'test:receipt:0001', 1000, new Date(api.clock.ms));
    const sub = await api.post('join', cara, body);
    assert.equal(sub.body.status, 'joined');
    await api.post('join', cara, body);
    assert.equal(api.db.wallet.balance(cara.id), 500);
    assert.equal(api.db.wallet.balance(owner.id), 500);
    const cara1 = sub.body.group.members.find(m => m.nickname === 'Cara');
    assert.equal(cara1.expiresAt, new Date(api.clock.ms + 30 * DAY).toISOString());
    assert.equal((await api.get(`detail?code=${group.code}`, cara)).body.expiresAt, cara1.expiresAt);

    api.clock.ms += 31 * DAY;
    const after = (await api.get(`detail?code=${group.code}`, owner)).body;
    assert.deepEqual(after.members.map(m => m.nickname), ['GroupOwner']);
    assert.equal((await api.get(`detail?code=${group.code}`, cara)).body.role, '');
    assert.equal((await api.post('join', cara, body)).body.status, 'already_processed');
    assert.equal((await api.get(`detail?code=${group.code}`, cara)).body.role, '');
    assert.equal(api.db.wallet.balance(cara.id), 500);
  });

  test('guruhdoshni lobbyga chaqirish mumkin (do\'st bo\'lmasa ham)', async () => {
    const stranger = await api.register('Stranger');
    assert.equal((await api.call('POST', '/api/party/invite', owner, { nickname: 'Stranger' })).status, 403);
    const { body: group } = await api.post('create', owner, { name: 'Lobby do\'stlari', kind: 'free' });
    await api.post('join', stranger, { code: group.code });
    assert.equal((await api.call('POST', '/api/party/invite', owner, { nickname: 'Stranger' })).status, 200);
    const invite = (await api.call('POST', '/api/party/heartbeat', stranger, { pingMs: 10 })).body.invitations[0];
    assert.equal((await api.call('POST', '/api/party/accept', stranger, { invitationId: invite.id })).status, 200);
  });

  test('group editing and ownership transfer preserve members and enforce owner permissions', async () => {
    const {body:g}=await api.post('create',owner,{name:'Editable club',kind:'free'});
    await api.post('join',bob,{code:g.code});
    const edit={code:g.code,name:'Renamed club',description:'Updated',kind:'free'};
    assert.equal((await api.post('edit',bob,edit)).status,403);
    assert.equal((await api.post('edit',owner,edit)).body.name,'Renamed club');
    assert.equal((await api.post('transfer',bob,{code:g.code,nickname:owner.nickname})).status,403);
    assert.equal((await api.post('transfer',owner,{code:g.code,nickname:dan.nickname})).status,400);
    const transfer=await api.post('transfer',owner,{code:g.code,nickname:bob.nickname});
    assert.equal(transfer.status,200);assert.equal(transfer.body.role,'admin');assert.equal(transfer.body.owner,bob.nickname);
    assert.equal(transfer.body.members.filter(m=>m.role==='owner').length,1);
    assert.equal((await api.post('edit',owner,edit)).status,403);
    assert.equal((await api.post('edit',bob,edit)).status,200);
    assert.equal((await api.post('leave',bob,{code:g.code})).body.error,'owner_cannot_leave');
    assert.equal((await api.post('delete',owner,{code:g.code})).status,403);
  });

  test('paid group edits preserve expiry and future payments follow the new owner', async () => {
    const {body:g}=await api.post('create',owner,{name:'Transferred paid club',kind:'paid',price:100,currency:'CDCoin',period:'month'});
    api.db.wallet.creditVerified(bob.id,'test:transfer:bob',100,new Date(api.clock.ms));
    api.db.wallet.creditVerified(cara.id,'test:transfer:cara',500,new Date(api.clock.ms));
    await api.post('join',bob,{code:g.code,expectedPrice:100,paymentKey:'transfer-bob-0001'});
    const joined=await api.post('join',cara,{code:g.code,expectedPrice:100,paymentKey:'transfer-cara-0001'});
    const expiry=joined.body.group.expiresAt;
    const edit={code:g.code,name:'Transferred paid club',description:'New price',kind:'paid',price:200,currency:'CDCoin',period:'week'};
    assert.equal((await api.post('edit',owner,edit)).status,200);
    assert.equal((await api.get(`detail?code=${g.code}`,cara)).body.expiresAt,expiry);
    assert.equal((await api.post('transfer',owner,{code:g.code,nickname:bob.nickname})).status,200);
    const oldBalance=api.db.wallet.balance(owner.id),newBalance=api.db.wallet.balance(bob.id);
    api.db.wallet.creditVerified(dan.id,'test:transfer:dan',200,new Date(api.clock.ms));
    const payment={code:g.code,expectedPrice:100,paymentKey:'transfer-dan-0001'};
    assert.equal((await api.post('join',dan,payment)).body.error,'price_changed');
    payment.expectedPrice=200;
    assert.equal((await api.post('join',dan,payment)).body.status,'joined');
    assert.equal(api.db.wallet.balance(owner.id),oldBalance);
    assert.equal(api.db.wallet.balance(bob.id),newBalance+200);
    assert.equal(api.db.wallet.balance(dan.id),0);
  });

  test('cheklovlar: egalik qilinadigan guruhlar soni', async () => {
    const busy = await api.register('BusyOwner');
    for (let i = 0; i < GROUP_LIMITS.owned; i++) {
      assert.equal((await api.post('create', busy, { name: `Guruh ${i}`, kind: 'private' })).status, 201);
    }
    assert.deepEqual((await api.post('create', busy, { name: 'Yana bitta', kind: 'free' })).body, { error: 'too_many_groups' });
  });
});

test('guruh yaratish cheklovi (daqiqasiga) va ping', async () => {
  const api = await startApi({ create: 100, groupCreate: 2, ping: 3 });
  try {
    const p = await api.register('RateUser');
    assert.equal((await api.post('create', p, { name: 'Bir guruh', kind: 'free' })).status, 201);
    assert.equal((await api.post('create', p, { name: 'Ikki guruh', kind: 'free' })).status, 201);
    assert.equal((await api.post('create', p, { name: 'Uch guruh', kind: 'free' })).status, 429);
    assert.equal((await api.get('mine', p)).status, 200); // o'qish boshqa cheklovda

    for (let i = 0; i < 3; i++) {
      const res = await api.call('GET', '/api/ping');
      assert.equal(res.status, 200);
      assert.equal(res.body.ok, true);
      assert.equal(typeof res.body.t, 'number');
    }
    assert.equal((await api.call('GET', '/api/ping')).status, 429);
  } finally {
    await api.close();
  }
});
