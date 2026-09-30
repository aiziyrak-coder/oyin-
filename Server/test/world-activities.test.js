import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createWorld } from '../src/world.js';

function fixture() {
  let time = 100000;
  const world = createWorld({ touchPresence() {} }, { spawn: () => ({ x: 1, z: 2 }) });
  const p = { id: 'a', publicId: 123456, nickname: 'Test', avatarId: 'M1', gender: 'male' };
  const join = () => world.join(p, {}, time)[1];
  const s = join();
  const activity = { kind: 'chess', slot: 0, role: 'white', pose: 'sit', x: -14, y: 0, z: 57.78, yaw: 0 };
  return { world, p, s, activity, join, claim(a = activity, session = s.sessionId) { return world.setActivity(p.id, session, a, time); },
    clear(session = s.sessionId) { return world.clearActivity(p.id, 'chess', time, session); },
    state(changes = {}) { return world.state(p, { sessionId: s.sessionId, ...s.self, ...changes }, time); },
    advance(ms) { time += ms; } };
}
test('activity is server-owned: claims validate ownership, coordinates, exclusivity, and public shape', () => {
  const f = fixture();
  assert.equal(f.claim(f.activity, 'wrong'), false);
  assert.equal(f.claim({ ...f.activity, x: Infinity }), false);
  assert.equal(f.claim({ ...f.activity, kind: 'admin' }), false);
  assert.equal(f.claim(), true);
  assert.equal(f.claim({ ...f.activity, kind: 'penalty' }), false);
  assert.equal(f.claim({ ...f.activity, slot: 1 }), false);
  assert.equal(f.claim({ ...f.activity, role: 'black' }), false);
  const self = f.state({ x: 100, z: 100, y: 3, yaw: 90, activity: '', pose: 'stand' })[1].self;
  assert.equal(self.x, -14); assert.equal(self.z, 57.78); assert.equal(self.y, 0);
  assert.equal(self.pose, 'sit'); assert.equal(self.activity, 'chess'); assert.equal(self.movementRevision, 1);
  assert.equal(Object.hasOwn(self, 'returnPosition'), false);
});
test('release restores entry point and ignores in-flight movement until new revision acknowledged', () => {
  const f = fixture(); f.claim(); f.advance(500);
  const seated = f.state()[1].self;
  assert.equal(f.clear('wrong'), false); assert.equal(f.clear(), true);
  const late = f.state({ ...seated })[1];
  assert.equal(late.corrected, true); assert.equal(late.self.x, 1); assert.equal(late.self.z, 2);
  assert.equal(late.self.pose, 'stand'); assert.equal(late.self.movementRevision, 2);
  f.advance(500);
  const moved = f.state({ ...late.self, x: 2 })[1];
  assert.equal(moved.corrected, false); assert.equal(moved.self.x, 2);
});
test('old game cleanup never alters a new world session or its activity', () => {
  const f = fixture(); f.claim(); const renewed = f.join();
  assert.equal(f.claim(f.activity, renewed.sessionId), true);
  assert.equal(f.clear(), false);
  assert.equal(f.world.getMember(f.p.id, 100000).sessionId, renewed.sessionId);
  assert.equal(f.world.getMember(f.p.id, 100000).activity.kind, 'chess');
});
test('activity updates preserve original return position; activity cannot keep a disconnected client alive', () => {
  const f = fixture(); f.claim(); f.advance(1000);
  assert.equal(f.claim({ ...f.activity, x: -12, action: 'kick', actionAt: 101000 }), true);
  assert.equal(f.clear(), true); assert.equal(f.state()[1].self.x, 1);
  f.claim(); f.advance(15000); assert.equal(f.claim(), false); assert.equal(f.clear(), false);
});
