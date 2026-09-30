import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createPenalty, PENALTY_TURN_MS } from '../src/penalty.js';
import { createWorld } from '../src/world.js';

function fixture() {
  let clock = Date.parse('2026-10-01T10:00:00Z');
  const world = createWorld({ touchPresence() {} }, { spawn: () => ({ x: 64, z: 43 }) });
  const routes = createPenalty(world), players = [];
  const now = () => new Date(clock);
  function make() {
    const n = players.length, profile = { id: 'penalty-' + n, publicId: 200001 + n, nickname: 'Player' + n, avatarId: 'M1', outfit: '' };
    const joined = world.join(profile, {}, now())[1];
    const p = { profile, sessionId: joined.sessionId, self: joined.self }; players.push(p); return p;
  }
  function call(p, action, extra = {}) {
    const body = { sessionId: p.sessionId, ...extra };
    const ctx = { player: p.profile, now: now(), url: new URL('http://local/?' + new URLSearchParams(body)) };
    return routes[(action === 'state' ? 'GET' : 'POST') + ' /api/penalty/' + action](ctx, body);
  }
  function advance(ms) {
    // Presence loop continues while the minigame is open.
    for (let elapsed = 0; elapsed < ms; elapsed += 5000) {
      clock += Math.min(5000, ms - elapsed);
      for (const p of players) {
        const m = world.getMember(p.profile.id, clock);
        if (m && m.sessionId === p.sessionId)
          world.state(p.profile, { sessionId: p.sessionId, ...world.publicPeer(m) }, now());
      }
    }
  }
  const a = make(), b = make(), c = make();
  const state = () => call(a, 'state')[1];
  const both = () => { call(a, 'join'); return call(b, 'join')[1]; };
  function kick(aimX = .72, direction = -1, aimY = .4, power = .72) {
    const s = state(), shooter = s.shooterId === a.profile.publicId ? a : b, keeper = shooter === a ? b : a;
    assert.equal(call(shooter, 'shoot', { version: s.version, turn: s.turn, aimX, aimY, power })[0], 200);
    const flight = call(keeper, 'dive', { version: s.version, turn: s.turn, direction });
    assert.equal(flight[0], 200); advance(1600);
    return state();
  }
  return { world, routes, a, b, c, call, state, both, kick, advance, now,
    expire(ms) { clock += ms; }, next() { advance(2600); return state(); } };
}

test('penalty two seats are exclusive, authenticated, near kiosk and claimed as world activity', () => {
  const f = fixture();
  assert.equal(f.call(f.a, 'state', { sessionId: f.b.sessionId })[1].error, 'world_session_expired');
  const one = f.call(f.a, 'join')[1]; assert.equal(one.phase, 'waiting');
  assert.equal(f.world.getMember(f.a.profile.id, +f.now()).activity.kind, 'penalty');
  assert.equal(f.call(f.a, 'join')[1].version, one.version);
  const both = f.call(f.b, 'join')[1]; assert.equal(both.phase, 'aiming');
  assert.equal(both.shooterId, f.a.profile.publicId); assert.equal(both.keeperId, f.b.profile.publicId);
  assert.equal(f.call(f.c, 'join')[1].error, 'arena_full');
  assert.equal(f.call(f.c, 'shoot', {})[1].error, 'not_playing');
});

test('penalty refuses distant and conflicting activity joins without taking a seat', () => {
  const f = fixture();
  const a = f.world.getMember(f.a.profile.id, +f.now()); a.x = 80;
  assert.equal(f.call(f.a, 'join')[1].error, 'too_far');
  a.x = 64;
  assert.equal(f.world.setActivity(f.a.profile.id, f.a.sessionId, { kind: 'chess', slot: 0, role: 'white', pose: 'sit', x: 64, y: 0, z: 43, yaw: 0 }, +f.now()), true);
  assert.equal(f.call(f.a, 'join')[1].error, 'activity_busy');
  assert.equal(f.state().player1, null);
});

test('penalty private simultaneous commitments are hidden and write-once; forged score is ignored', () => {
  const f = fixture(), s = f.both();
  const submitted = f.call(f.a, 'shoot', { version: s.version, turn: 0, aimX: .72, aimY: .4, power: .72, score1: 999, outcome: 'goal' })[1];
  assert.equal(submitted.shot, null); assert.equal(submitted.score1, 0); assert.equal(submitted.shooterReady, true);
  assert.equal(JSON.stringify(submitted).includes('aimX'), false);
  assert.equal(f.call(f.a, 'shoot', { version: s.version, turn: 0, aimX: 0, aimY: 0, power: 1 })[1].error, 'already_committed');
  const flight = f.call(f.b, 'dive', { version: s.version, turn: 0, direction: -1 })[1];
  assert.equal(flight.phase, 'flight'); assert.equal(flight.shot.outcome, '');
  assert.equal(flight.score1, 0);
  assert.equal(f.call(f.b, 'dive', { version: s.version, turn: 0, direction: 1 })[1].error, 'stale_state');
  f.advance(1600); assert.equal(f.state().result, 'goal'); assert.equal(f.state().score1, 1);
});

test('penalty validates roles, versions, turns, aim, power and dive directions', () => {
  const f = fixture(), s = f.both(), base = { version: s.version, turn: 0 };
  assert.equal(f.call(f.b, 'shoot', base)[1].error, 'not_shooter');
  assert.equal(f.call(f.a, 'dive', base)[1].error, 'not_keeper');
  assert.equal(f.call(f.a, 'shoot', { ...base, version: -1 })[1].error, 'stale_state');
  assert.equal(f.call(f.a, 'shoot', { ...base, turn: 99 })[1].error, 'stale_turn');
  for (const extra of [{ aimX: NaN }, { aimX: 2 }, { aimY: 9 }, { power: 0 }, { power: 1.1 }])
    assert.equal(f.call(f.a, 'shoot', { ...base, aimX: 0, aimY: .5, power: .7, ...extra })[1].error, 'invalid_shot');
  for (const direction of [-2, 2, '1', .5]) assert.equal(f.call(f.b, 'dive', { ...base, direction })[1].error, 'invalid_dive');
  assert.equal(f.call(f.a, 'reset')[1].error, 'match_active');
});

test('penalty matching dive saves, outside target misses, next turn swaps roles and poses', () => {
  const f = fixture(); f.both();
  const save = f.kick(.6, 1); assert.equal(save.result, 'save'); assert.equal(save.score1, 0);
  const next = f.next(); assert.equal(next.shooterId, f.b.profile.publicId);
  assert.equal(f.world.getMember(f.a.profile.id, +f.now()).pose ?? f.world.getMember(f.a.profile.id, +f.now()).activity.pose, 'keeper');
  const miss = f.kick(1.1, -1); assert.equal(miss.result, 'miss'); assert.equal(miss.attempts2, 1);
  f.next(); const high = f.kick(.5, -1, 1.1, 1); assert.equal(high.result, 'miss');
});

test('penalty timeout defaults safely and cannot leave the match stalled', () => {
  const f = fixture(); f.both(); f.advance(PENALTY_TURN_MS + 1);
  assert.equal(f.state().phase, 'flight'); f.advance(1600);
  assert.equal(f.state().result, 'timeout'); assert.equal(f.state().score1, 0);
  assert.equal(f.next().phase, 'aiming');
});

test('penalty five rounds and sudden death wait for equal attempts; rematch requires both', () => {
  const f = fixture(); f.both();
  for (let n = 0; n < 10; n++) { assert.equal(f.kick().result, 'goal'); f.next(); }
  assert.equal(f.state().phase, 'aiming'); assert.equal(f.state().attempts1, 5); assert.equal(f.state().attempts2, 5);
  f.kick(); f.next(); assert.equal(f.state().phase, 'aiming');
  const end = f.kick(.6, 1); assert.equal(end.phase, 'finished'); assert.equal(end.winner, f.a.profile.publicId);
  const first = f.call(f.a, 'reset')[1]; assert.equal(first.phase, 'finished'); assert.deepEqual(first.resetVotes, [f.a.profile.publicId]);
  const again = f.call(f.b, 'reset')[1]; assert.equal(again.phase, 'aiming'); assert.equal(again.score1, 0); assert.equal(again.turn, 0);
});

test('penalty ends mathematically decided series early and clears positions on leaving', () => {
  const f = fixture(); f.both();
  for (let n = 0; n < 3; n++) {
    f.kick(); f.next(); const s = f.kick(.6, 1);
    if (n < 2) f.next(); else { assert.equal(s.phase, 'finished'); assert.equal(s.winner, f.a.profile.publicId); }
  }
  f.call(f.a, 'leave');
  const a = f.world.getMember(f.a.profile.id, +f.now()); assert.equal(a.activity, null); assert.equal(a.z, 43);
  f.call(f.b, 'leave'); assert.equal(f.state().phase, 'waiting'); assert.equal(f.state().score1, 0);
});

test('penalty disconnect and reconnect abandon safely without clearing a newer activity', () => {
  const f = fixture(); f.both(); f.world.join(f.a.profile, {}, f.now());
  const s = f.call(f.b, 'state')[1]; assert.equal(s.phase, 'abandoned'); assert.equal(s.player1, null); assert.equal(s.winner, f.b.profile.publicId);
  assert.equal(f.call(f.a, 'join')[1].error, 'world_session_expired');
  f.expire(15000); assert.equal(f.call(f.b, 'state')[1].error, 'world_session_expired');
});
