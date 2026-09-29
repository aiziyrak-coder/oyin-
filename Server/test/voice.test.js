import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createServer } from 'node:http';
import { openDatabase } from '../src/db.js';
import { createApp } from '../src/app.js';

test('Lobby ovozli chat: mic/karnay holati, bo\'laklar faqat guruh ichida, o\'ziga qaytmaydi', async () => {
  const db = openDatabase(':memory:'); let time = Date.now();
  const server = createServer(createApp(db, { now: () => new Date(time), rateLimits: { create: 100, party: 1000, voice: 1000 } }));
  await new Promise(r => server.listen(0, '127.0.0.1', r));
  const base = `http://127.0.0.1:${server.address().port}/api/`;
  async function call(path, player, body = {}, method = 'POST') {
    const res = await fetch(base + path, { method, headers: { 'Content-Type': 'application/json',
      ...(player ? { Authorization: `Bearer ${player.token}` } : {}) }, body: method === 'GET' ? undefined : JSON.stringify(body) });
    return { status: res.status, data: await res.json() };
  }
  const poll = (p, since) => call('party/voice?since=' + since, p, null, 'GET');
  const audio = Buffer.alloc(3200, 0x55).toString('base64');
  try {
    const ps = [];
    for (let i = 0; i < 3; i++) ps.push((await call('players', null, { nickname: 'VoiceTester' + i, gender: 'male', avatarId: 'M1' })).data);
    const [h, g, s] = ps;
    for (const p of [g, s]) { await call('friends/request', h, { nickname: p.nickname }); await call('friends/accept', p, { nickname: h.nickname }); }
    // Yolg'iz: yuborib bo'lmaydi, o'qish bo'sh
    assert.equal((await call('party/voice', h, { rate: 16000, data: audio })).status, 409);
    assert.deepEqual((await poll(h, -1)).data, { cursor: -1, chunks: [] });
    assert.equal((await call('party/voice', null, { rate: 16000, data: audio })).status, 401);
    await call('party/invite', h, { nickname: g.nickname });
    const inv = (await call('party/heartbeat', g, { pingMs: 1 })).data.invitations[0].id;
    await call('party/accept', g, { invitationId: inv });
    // Holat: noto'g'ri tur 400, mikrofon karnaysiz yoqilmaydi
    assert.equal((await call('party/heartbeat', h, { pingMs: 5, micOn: 'yes' })).status, 400);
    let state = (await call('party/heartbeat', h, { pingMs: 5, micOn: true, speakerOn: false })).data;
    let me = state.members.find(m => m.nickname === h.nickname);
    assert.equal(me.micOn, false); assert.equal(me.speakerOn, false);
    assert.equal((await call('party/voice', h, { rate: 16000, data: audio })).status, 409);
    state = (await call('party/heartbeat', h, { pingMs: 5, micOn: true, speakerOn: true })).data;
    me = state.members.find(m => m.nickname === h.nickname);
    assert.equal(me.micOn, true); assert.equal(me.speakerOn, true);
    await call('party/heartbeat', g, { pingMs: 5, micOn: true, speakerOn: true });
    // Tekshiruvlar
    assert.equal((await call('party/voice', h, { rate: 100, data: audio })).status, 400);
    assert.equal((await call('party/voice', h, { rate: 16000, data: '!!!' })).status, 400);
    assert.equal((await call('party/voice', h, { rate: 16000, data: Buffer.alloc(12001).toString('base64') })).status, 400);
    const start = (await poll(g, -1)).data.cursor;
    assert.equal((await call('party/voice', h, { rate: 16000, data: audio })).status, 200);
    assert.equal((await call('party/voice', h, { rate: 16000, data: audio })).status, 200);
    const got = (await poll(g, start)).data;
    assert.equal(got.chunks.length, 2); assert.equal(got.chunks[0].nickname, h.nickname);
    assert.equal(got.chunks[0].data, audio); assert.equal(got.chunks[0].rate, 16000);
    assert.equal((await poll(g, got.cursor)).data.chunks.length, 0);
    // O'z ovozi qaytmaydi, begona guruh o'qiy olmaydi
    assert.equal((await poll(h, start)).data.chunks.length, 0);
    assert.equal((await poll(s, start)).data.chunks.length, 0);
    assert.equal((await call('party/voice', s, { rate: 16000, data: audio })).status, 409);
    // Mikrofon o'chsa bo'laklari yuborilmaydi; 3 s dan eski bo'laklar tashlanadi
    await call('party/voice', h, { rate: 16000, data: audio });
    await call('party/heartbeat', h, { pingMs: 5, micOn: false, speakerOn: true });
    assert.equal((await poll(g, got.cursor)).data.chunks.length, 0);
    await call('party/heartbeat', h, { pingMs: 5, micOn: true, speakerOn: true });
    time += 3500;
    assert.equal((await poll(g, got.cursor)).data.chunks.length, 0);
    // Katta tana 20 KB dan oshsa rad etiladi
    assert.equal((await call('party/voice', h, { rate: 16000, data: 'A'.repeat(30000) })).status, 400);
    // Chiqib ketgan a'zoning bo'lagi ko'rinmaydi
    const c2 = (await poll(g, -1)).data.cursor;
    await call('party/voice', h, { rate: 16000, data: audio });
    await call('party/leave', g);
    assert.equal((await poll(g, c2)).data.chunks.length, 0);
  } finally { server.closeAllConnections(); await new Promise(r => server.close(r)); db.close(); }
});
