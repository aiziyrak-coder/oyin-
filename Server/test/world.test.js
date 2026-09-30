import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createServer } from 'node:http';
import { PassThrough } from 'node:stream';
import { openDatabase } from '../src/db.js';
import { createApp } from '../src/app.js';
import { createWorld, WORLD_CAPACITY, WORLD_EXPIRY_MS, VOICE_REPLY_BYTES } from '../src/world.js';

function fixture(options = {}) {
  let clock = Date.parse('2026-10-01T10:00:00Z');
  const world = createWorld({ touchPresence() {} }, { spawn: () => ({ x: 0, z: 0 }), ...options });
  const players = new Map();
  const now = () => new Date(clock);
  const make = (n = players.size) => {
    const profile = { id: 'player-' + n, publicId: 100000 + n, nickname: 'Player' + n, avatarId: 'M1', gender: 'male', outfit: '' };
    const response = world.join(profile, {}, now());
    if (response[0] !== 200) return { profile, response };
    const player = { profile, sessionId: response[1].sessionId, self: response[1].self };
    players.set(profile.id, player); return player;
  };
  const state = (p, changes = {}) => {
    const body = { sessionId: p.sessionId, ...p.self, ...changes };
    const result = world.state(p.profile, body, now());
    if (result[0] === 200) p.self = result[1].self;
    return result;
  };
  return { world, players, now, make, state, advance(ms) { clock += ms; },
    post(p, changes = {}) { return world.postVoice(p.profile, { sessionId: p.sessionId, seq: 0, rate: 16000, data: 'AQID', ...changes }, now()); },
    read(p, since = 0) { return world.readVoice(p.profile, new URL(`http://local/?sessionId=${p.sessionId}&since=${since}`), now()); },
  };
}

test('world is public, hides tokens, replaces old sessions, and removes players on leave', () => {
  const f = fixture(), a = f.make(), b = f.make();
  const snap = f.state(a)[1];
  assert.equal(snap.players.length, 1); assert.equal(snap.players[0].publicId, b.profile.publicId);
  assert.equal(snap.self.publicId, a.profile.publicId);
  assert.equal(Object.hasOwn(snap.players[0], 'sessionId'), false);
  assert.equal(Object.hasOwn(snap.players[0], 'id'), false);
  assert.equal(f.state(a, { sessionId: b.sessionId })[0], 409);
  const renewed = f.world.join(a.profile, {}, f.now())[1];
  assert.notEqual(renewed.sessionId, a.sessionId);
  assert.equal(f.world.leave(a.profile, { sessionId: a.sessionId }, f.now())[0], 409);
  a.sessionId = renewed.sessionId;
  assert.equal(f.world.leave(a.profile, { sessionId: a.sessionId }, f.now())[0], 200);
  assert.equal(f.state(b)[1].players.length, 0);
});

test('world rejects malformed data and bounds without mutating valid positions', () => {
  const f = fixture(), a = f.make();
  for (const changes of [{ x: NaN }, { x: Infinity }, { x: '0' }, { x: 146 }, { z: -146 },
    { y: -1 }, { y: 4 }, { yaw: 400000 }, { micOn: 1 }, { speakerOn: 'true' }, { crouching: null }])
    assert.equal(f.state(a, changes)[0], 400, JSON.stringify(changes));
  assert.equal(f.state(a)[1].self.x, 0);
  assert.equal(f.world.state(a.profile, null, f.now())[0], 400);
  assert.equal(f.world.join(a.profile, null, f.now())[0], 400);
  const valid = f.state(a, { yaw: -90, micOn: true, speakerOn: false })[1].self;
  assert.equal(valid.yaw, 270); assert.equal(valid.micOn, false);
});

test('world movement uses time credit, not exploitable per-packet speed tolerance', () => {
  const f = fixture(), a = f.make();
  f.advance(1000);
  const first = f.state(a, { x: 100 });
  assert.equal(first[1].corrected, true); assert.equal(first[1].self.x, 10);
  for (let i = 0; i < 50; i++) assert.equal(f.state(a, { x: 100 })[1].self.x, 10);
  f.advance(125); assert.equal(f.state(a, { x: 11 })[1].corrected, false);
  f.advance(4000); assert.equal(f.state(a, { x: 100 })[1].self.x, 27);
  assert.equal(f.state(a, { x: 100, z: 100 })[1].self.z, 0);
});

test('world clock cannot rewind movement credit, heartbeat or audio retention', () => {
  const f = fixture(), a = f.make(), b = f.make();
  f.state(a, { x: 2, micOn: true, speakerOn: true }); f.state(b, { speakerOn: true });
  const oldTime = f.now();
  f.advance(2000); assert.equal(f.state(a, { x: 18 })[1].self.x, 18);
  for (let i = 0; i < 10; i++) {
    const delayed = f.world.state(a.profile, { sessionId: a.sessionId, ...a.self }, oldTime);
    assert.equal(delayed[1].serverTime, +f.now());
    assert.equal(f.state(a, { x: 100 })[1].self.x, 18);
  }
  f.world.postVoice(a.profile, { sessionId: a.sessionId, seq: 1, rate: 16000, data: 'AQID' }, oldTime);
  f.advance(3001); assert.equal(f.read(b)[1].chunks.length, 0);
  // A late request with an old timestamp must not resurrect a timed-out session.
  f.advance(WORLD_EXPIRY_MS); f.make();
  assert.equal(f.world.state(a.profile, { sessionId: a.sessionId, ...a.self }, oldTime)[1].error, 'world_session_expired');
});

test('HTTP routes validate delayed world, voice and chess bodies at completion time', async () => {
  // Exercise the actual app handler using request streams; no listening socket or live database.
  for (const path of ['/api/world/state', '/api/world/voice', '/api/world/leave', '/api/chess/join']) {
    const db = openDatabase(':memory:'); let clock = Date.parse('2026-10-01T10:00:00Z');
    const app = createApp(db, { now: () => new Date(clock) });
    function send(route, player, body, delayed = false) {
      const req = new PassThrough(); req.url = route; req.method = 'POST';
      req.headers = player ? { authorization: 'Bearer ' + player.token } : {};
      req.socket = { remoteAddress: '127.0.0.1' };
      let done, status;
      const response = new Promise(resolve => { done = resolve; });
      const res = { headersSent: false, setHeader() {}, writeHead(code) { status = code; this.headersSent = true; },
        end(json) { done({ status, body: json ? JSON.parse(json) : undefined }); }, destroy() { done({ status: 500 }); } };
      void app(req, res);
      const text = JSON.stringify(body);
      if (delayed) req.write(text.slice(0, -1)); else req.end(text);
      return { response, finish() { req.end(text.slice(-1)); } };
    }
    try {
      const player = (await send('/api/players', null, { nickname: 'BodyTimeTest', gender: 'male', avatarId: 'M1' }).response).body;
      const joined = (await send('/api/world/join', player, {}).response).body;
      const payload = { sessionId: joined.sessionId, ...joined.self, micOn: true, speakerOn: true,
        tableId: 0, color: 'white', seq: 1, rate: 16000, data: 'AQID' };
      assert.equal((await send('/api/world/state', player, payload).response).status, 200);
      const delayed = send(path, player, payload, true);
      // No intervening world call performs expiry cleanup; only the finishing request can see the new time.
      clock += WORLD_EXPIRY_MS + 100;
      delayed.finish();
      assert.equal((await delayed.response).body.error, 'world_session_expired', path);
      const slowJoin = send('/api/world/join', player, {}, true);
      clock += WORLD_EXPIRY_MS + 100; slowJoin.finish();
      const newWorld = (await slowJoin.response).body;
      assert.equal(newWorld.serverTime, clock);
      assert.equal((await send('/api/world/state', player, { sessionId: newWorld.sessionId, ...newWorld.self }).response).status, 200);
    } finally { db.close(); }
  }
});

test('world enforces capacity, nonoverlapping sidewalk spawns and 15 second heartbeat expiry', () => {
  const world = createWorld({ touchPresence() {} }); const clock = new Date('2026-10-01T10:00:00Z');
  const all = [];
  for (let i = 0; i < WORLD_CAPACITY; i++) {
    const profile = { id: 'capacity' + i, publicId: i, nickname: 'Capacity' + i };
    const result = world.join(profile, {}, clock);
    assert.equal(result[0], 200);
    assert.ok([-38, -9, 9, 38].includes(result[1].self.x));
    for (const previous of all) assert.ok(Math.hypot(previous.self.x - result[1].self.x, previous.self.z - result[1].self.z) >= 1.2);
    all.push({ profile, ...result[1] });
  }
  assert.equal(world.join({ id: 'overflow' }, {}, clock)[1].error, 'world_full');
  assert.equal(world.join(all[0].profile, {}, clock)[0], 200); // rejoin does not consume another slot
  const expired = new Date(+clock + WORLD_EXPIRY_MS);
  assert.equal(world.member(all[1].profile.id, all[1].sessionId, +expired), null);
  assert.equal(world.join({ id: 'new' }, {}, expired)[0], 200);
});

test('world voice requires both opt-ins, validates audio and prevents sequence replay', () => {
  const f = fixture(), a = f.make(), b = f.make();
  assert.equal(f.post(a)[1].error, 'microphone_off');
  f.state(a, { micOn: true, speakerOn: true }); f.state(b, { speakerOn: true });
  for (const changes of [{ seq: -1 }, { seq: 1.5 }, { rate: 7999 }, { rate: 48001 }, { data: '' },
    { data: '!!!' }, { data: 'AQI' }, { data: Buffer.alloc(12001).toString('base64') },
    { data: Buffer.alloc(5000).toString('base64'), rate: 8000 }]) assert.equal(f.post(a, changes)[0], 400);
  assert.equal(f.post(a)[0], 200);
  assert.equal(f.post(a)[1].error, 'stale_audio');
  assert.equal(f.read(a)[1].chunks.length, 0); // never echo own microphone
  assert.equal(f.read(b)[1].chunks.length, 1);
  assert.equal(f.read(b, -1)[1].chunks.length, 0); // first poll flushes old audio
  assert.equal(f.read(b, 'garbage')[0], 400);
  assert.equal(f.read(b, 999)[1].chunks.length, 0);
});

test('world voice respects distance, mute, reconnect and expiry with no buffered replay', () => {
  let counter = 0;
  const f = fixture({ spawn: () => ({ x: counter++ === 2 ? 40 : 0, z: 0 }) }), a = f.make(), b = f.make(), c = f.make();
  for (const p of [a, b, c]) f.state(p, { speakerOn: true, micOn: true });
  f.post(a);
  assert.equal(f.read(b)[1].chunks.length, 1); assert.equal(f.read(c)[1].chunks.length, 0);
  f.state(b, { speakerOn: false }); assert.equal(f.read(b)[1].chunks.length, 0);
  f.state(b, { speakerOn: true }); assert.equal(f.read(b)[1].chunks.length, 0);
  f.post(a, { seq: 1 }); assert.equal(f.read(b)[1].chunks.length, 1);
  f.state(a, { micOn: false }); assert.equal(f.read(b)[1].chunks.length, 0);
  f.state(a, { micOn: true }); f.post(a, { seq: 2 });
  const oldSession = a.sessionId;
  a.sessionId = f.world.join(a.profile, {}, f.now())[1].sessionId;
  assert.equal(f.read(b)[1].chunks.length, 0);
  assert.equal(f.post(a, { sessionId: oldSession, seq: 3 })[0], 409);
  f.advance(WORLD_EXPIRY_MS); assert.equal(f.read(b)[1].error, 'world_session_expired');
});

test('world voice clips retention to three seconds, caps per-sender chunks and rejects ended sessions', () => {
  const f = fixture(), a = f.make(), b = f.make();
  f.state(a, { micOn: true, speakerOn: true }); f.state(b, { speakerOn: true });
  for (let seq = 0; seq < 30; seq++) f.post(a, { seq });
  assert.equal(f.read(b)[1].chunks.length, 20);
  f.advance(3001); assert.equal(f.read(b)[1].chunks.length, 0);
  f.world.leave(a.profile, { sessionId: a.sessionId }, f.now());
  assert.equal(f.post(a, { seq: 30 })[0], 409);
});

test('crowded world audio replies stay bounded and favor recent packets without replay', () => {
  const f = fixture(), receiver = f.make(); f.state(receiver, { speakerOn: true });
  const data = Buffer.alloc(5000, 42).toString('base64');
  for (let i = 0; i < 20; i++) {
    const sender = f.make(); f.state(sender, { micOn: true, speakerOn: true });
    for (let seq = 0; seq < 20; seq++) f.post(sender, { seq, data });
  }
  const reply = f.read(receiver)[1];
  assert.ok(reply.chunks.length > 0 && reply.chunks.length <= 64);
  assert.ok(Buffer.byteLength(JSON.stringify(reply)) <= VOICE_REPLY_BYTES);
  assert.equal(reply.chunks.at(-1).seq, reply.cursor);
  assert.equal(f.read(receiver, reply.cursor)[1].chunks.length, 0);
});

test('HTTP world/chess/voice authenticate every route and rate-limit each player separately', async () => {
  const db = openDatabase(':memory:');
  const server = createServer(createApp(db, { rateLimits: { worldJoin: 1, world: 2, worldVoice: 2, chessRead: 2, chessWrite: 2 } }));
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const base = `http://127.0.0.1:${server.address().port}`;
  const call = async (path, player, body) => {
    const response = await fetch(base + path, { method: body ? 'POST' : 'GET', headers: {
      'Content-Type': 'application/json', ...(player ? { Authorization: `Bearer ${player.token}` } : {}),
    }, body: body ? JSON.stringify(body) : undefined });
    return { status: response.status, body: await response.json() };
  };
  try {
    for (const path of ['/api/world/join', '/api/world/state', '/api/world/leave', '/api/world/voice',
      '/api/chess/join', '/api/chess/move', '/api/chess/leave', '/api/chess/reset'])
      assert.equal((await call(path, null, {})).status, 401);
    for (const path of ['/api/world/voice', '/api/chess/table']) assert.equal((await call(path)).status, 401);
    const a = (await call('/api/players', null, { nickname: 'WorldAlice', gender: 'male', avatarId: 'M1' })).body;
    const b = (await call('/api/players', null, { nickname: 'WorldBobby', gender: 'male', avatarId: 'M1' })).body;
    const joined = (await call('/api/world/join', a, {})).body;
    assert.ok(joined.sessionId);
    assert.equal((await call('/api/world/join', a, {})).status, 429);
    assert.equal((await call('/api/world/join', b, {})).status, 200);
    const state = { sessionId: joined.sessionId, ...joined.self };
    assert.equal((await call('/api/world/state', a, state)).status, 200);
    assert.equal((await call('/api/world/state', a, state)).status, 200);
    assert.equal((await call('/api/world/state', a, state)).status, 429);
    const tablePath = `/api/chess/table?sessionId=${joined.sessionId}&tableId=0`;
    assert.equal((await call(tablePath, a)).body.board.length, 64);
    await call(tablePath, a); assert.equal((await call(tablePath, a)).status, 429);
    const voicePath = `/api/world/voice?sessionId=${joined.sessionId}&since=-1`;
    assert.equal((await call(voicePath, a)).status, 200);
    await call(voicePath, a); assert.equal((await call(voicePath, a)).status, 429);
    assert.equal((await call('/api/chess/join', b, { sessionId: joined.sessionId, tableId: 0, color: 'white' })).status, 409);
  } finally { server.closeAllConnections(); await new Promise(resolve => server.close(resolve)); db.close(); }
});

test('HTTP integration: unrelated players walk to the hall, share audio, play a real game and leave', async () => {
  let clock = Date.parse('2026-10-01T10:00:00Z');
  const db = openDatabase(':memory:');
  const server = createServer(createApp(db, { now: () => new Date(clock) }));
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const base = `http://127.0.0.1:${server.address().port}`;
  const call = async (path, player, body) => {
    const response = await fetch(base + path, { method: body ? 'POST' : 'GET', headers: {
      'Content-Type': 'application/json', ...(player ? { Authorization: `Bearer ${player.token}` } : {}),
    }, body: body ? JSON.stringify(body) : undefined });
    return { status: response.status, body: await response.json() };
  };
  try {
    const players = [];
    for (const nickname of ['ChessHTTP_A', 'ChessHTTP_B', 'ChessHTTP_C']) {
      const player = (await call('/api/players', null, { nickname, gender: 'male', avatarId: 'M1' })).body;
      player.world = (await call('/api/world/join', player, {})).body; players.push(player);
    }
    const [a, b, c] = players;
    // Time advances in small steps and both seats physically approach the table, with no admin teleport endpoint.
    for (let step = 0; step < 35; step++) {
      clock += 500;
      for (const [index, player] of players.entries()) {
        const { self, sessionId } = player.world;
        const target = index < 2 ? { x: -14 + index, z: 59 } : { x: 0, z: -20 };
        const dx = target.x - self.x, dz = target.z - self.z;
        const scale = Math.min(1, 4 / Math.max(.001, Math.hypot(dx, dz)));
        const result = await call('/api/world/state', player, { sessionId, ...self,
          x: self.x + dx * scale, z: self.z + dz * scale, micOn: true, speakerOn: true });
        assert.equal(result.status, 200); assert.equal(result.body.corrected, false); player.world = result.body;
      }
    }
    const payload = (player, extra = {}) => ({ sessionId: player.world.sessionId, tableId: 0, ...extra });
    assert.equal(a.world.players.length, 2);
    assert.equal((await call('/api/chess/join', c, payload(c, { color: 'white' }))).body.error, 'too_far');
    assert.equal((await call('/api/chess/join', a, payload(a, { color: 'white' }))).body.status, 'waiting');
    let board = (await call('/api/chess/join', b, payload(b, { color: 'black' }))).body;
    const voice = await call('/api/world/voice', a, { sessionId: a.world.sessionId, seq: 1, rate: 16000, data: 'AQID' });
    assert.equal(voice.status, 200);
    const heard = await call(`/api/world/voice?sessionId=${b.world.sessionId}&since=0`, b);
    assert.equal(heard.body.chunks[0].publicId, a.publicId);
    assert.equal((await call(`/api/world/voice?sessionId=${c.world.sessionId}&since=0`, c)).body.chunks.length, 0);
    for (const [player, from, to] of [[a, 'f2', 'f3'], [b, 'e7', 'e5'], [a, 'g2', 'g4'], [b, 'd8', 'h4']]) {
      const move = await call('/api/chess/move', player, payload(player, { from, to, promotion: 'q', version: board.version }));
      assert.equal(move.status, 200); board = move.body;
    }
    assert.equal(board.status, 'checkmate'); assert.equal(board.winner, 'black');
    const watched = await call(`/api/chess/table?sessionId=${c.world.sessionId}&tableId=0`, c);
    assert.equal(watched.body.fen, board.fen);
    assert.equal((await call('/api/world/leave', a, { sessionId: a.world.sessionId })).status, 200);
    assert.equal((await call(`/api/chess/table?sessionId=${b.world.sessionId}&tableId=0`, b)).body.white, null);
  } finally { server.closeAllConnections(); await new Promise(resolve => server.close(resolve)); db.close(); }
});
