import { randomUUID, randomInt } from 'node:crypto';
import { publicOutfit } from './outfit.js';

export const WORLD_LIMIT = 145;
export const WORLD_CAPACITY = 128;
export const WORLD_EXPIRY_MS = 15_000;
export const VOICE_RADIUS = 18;
export const VOICE_REPLY_BYTES = 256_000;
const SPEED = 8, MAX_MOVE_CREDIT = 16, VOICE_KEEP_MS = 3000, VOICE_CHUNKS = 20;
const BASE64 = /^[A-Za-z0-9+/]+={0,2}$/;
const fail = (status, error) => [status, { error }];

// Public world presence is deliberately separate from private lobby parties. State is ephemeral;
// reconnecting/restarting invalidates its unguessable session token and all old audio.
export function createWorld(db, { capacity = WORLD_CAPACITY, spawn = chooseSpawn } = {}) {
  const members = new Map();
  let voiceCursor = 0, lastTime = -Infinity;
  // Request bodies can complete out of order and the system clock can step backwards.
  // Neither event may rewind a session's heartbeat, movement credit, or audio retention clock.
  function currentTime(value) { lastTime = Math.max(lastTime, +value); return lastTime; }
  function remove(id) { members.delete(id); }
  function clean(time) {
    time = currentTime(time);
    for (const [id, member] of members) if (time - member.seen >= WORLD_EXPIRY_MS) remove(id);
    for (const member of members.values())
      member.audio = member.audio.filter(chunk => time - chunk.time <= VOICE_KEEP_MS);
  }
  function member(id, sessionId, time) {
    clean(time);
    const value = members.get(id);
    return value && typeof sessionId === 'string' && value.sessionId === sessionId ? value : null;
  }
  function publicPeer(value) {
    if (!value) return null;
    const p = value.profile;
    return { publicId: p.publicId, nickname: p.nickname, avatarId: p.avatarId, gender: p.gender,
      outfit: publicOutfit(p.outfit), x: value.x, y: value.y, z: value.z, yaw: value.yaw,
      crouching: value.crouching, micOn: value.micOn, speakerOn: value.speakerOn,
      activity: value.activity?.kind ?? '', activitySlot: value.activity?.slot ?? -1,
      activityRole: value.activity?.role ?? '', pose: value.activity?.pose ?? 'stand',
      action: value.activity?.action ?? '', actionAt: value.activity?.actionAt ?? 0,
      movementRevision: value.movementRevision ?? 0 };
  }
  // Internal claims only: no client endpoint may assign a pose or teleport itself into a game.
  function setActivity(id, sessionId, activity, now) {
    const time = currentTime(now), value = member(id, sessionId, time);
    if (!value || !activity || !['chess', 'penalty'].includes(activity.kind) ||
      !Number.isInteger(activity.slot) || activity.slot < 0 || typeof activity.role !== 'string' ||
      !['sit', 'stand', 'keeper'].includes(activity.pose) ||
      ![activity.x, activity.y, activity.z, activity.yaw].every(Number.isFinite) ||
      Math.abs(activity.x) > WORLD_LIMIT || Math.abs(activity.z) > WORLD_LIMIT ||
      activity.y < -.5 || activity.y > 3.5) return false;
    if (value.activity && (value.activity.kind !== activity.kind || value.activity.slot !== activity.slot ||
      value.activity.role !== activity.role)) return false;
    if (!value.activity) {
      value.returnPosition = { x: value.x, y: value.y, z: value.z, yaw: value.yaw };
      value.movementRevision++;
    }
    value.activity = { ...activity };
    value.x = activity.x; value.y = activity.y; value.z = activity.z; value.yaw = activity.yaw;
    value.crouching = false; value.credit = 0; value.moved = time;
    return true;
  }
  function clearActivity(id, kind, now, sessionId) {
    const time = currentTime(now); clean(time);
    const value = members.get(id);
    if (!value || !value.activity || value.activity.kind !== kind ||
      (sessionId !== undefined && value.sessionId !== sessionId)) return false;
    Object.assign(value, value.returnPosition);
    value.activity = null; value.returnPosition = null; value.movementRevision++;
    value.credit = 0; value.moved = time; value.crouching = false;
    return true;
  }
  function snapshot(value, time, corrected = false) {
    return { sessionId: value.sessionId, self: publicPeer(value),
      players: [...members.values()].filter(other => other !== value).map(publicPeer),
      serverTime: time, corrected };
  }
  function join(player, body, now) {
    if (!body) return fail(400, 'bad_json');
    const time = currentTime(now);
    clean(time);
    if (!members.has(player.id) && members.size >= capacity) return fail(409, 'world_full');
    const position = spawn([...members.values()].filter(m => m.id !== player.id));
    const value = { id: player.id, profile: player, sessionId: randomUUID(), ...position,
      y: .15, yaw: 0, crouching: false, micOn: false, speakerOn: false,
      seen: time, moved: time, presenceTouched: time, credit: 2, audio: [], lastVoiceSeq: -1, hearAfter: voiceCursor,
      activity: null, returnPosition: null, movementRevision: 0 };
    members.set(player.id, value);
    db.touchPresence(player.id, new Date(time));
    return [200, snapshot(value, time)];
  }
  function state(player, body, now) {
    if (!body) return fail(400, 'bad_json');
    const time = currentTime(now), value = member(player.id, body.sessionId, time);
    if (!value) return fail(409, 'world_session_expired');
    if (![body.x, body.y, body.z, body.yaw].every(Number.isFinite) ||
      Math.abs(body.x) > WORLD_LIMIT || Math.abs(body.z) > WORLD_LIMIT || body.y < -.5 || body.y > 3.5 ||
      Math.abs(body.yaw) > 360_000) return fail(400, 'invalid_position');
    if (!['crouching', 'micOn', 'speakerOn'].every(key => typeof body[key] === 'boolean'))
      return fail(400, 'invalid_world_state');
    // A token bucket (rather than per-packet tolerance) cannot be bypassed by sending faster packets.
    const dt = Math.max(0, Math.min(2, (time - value.moved) / 1000));
    value.credit = Math.min(MAX_MOVE_CREDIT, value.credit + SPEED * dt);
    const dx = body.x - value.x, dz = body.z - value.z, distance = Math.hypot(dx, dz);
    const accepted = Math.min(distance, value.credit);
    // A late movement packet from before sitting/standing must not undo the server's teleport.
    const stalePose = body.movementRevision !== undefined && body.movementRevision !== value.movementRevision;
    const locked = !!value.activity || stalePose;
    const corrected = locked || distance > value.credit + .001;
    if (!locked) {
      if (distance > 0) { value.x += dx * accepted / distance; value.z += dz * accepted / distance; }
      value.credit = Math.max(0, value.credit - accepted);
      value.y = body.y; value.yaw = ((body.yaw % 360) + 360) % 360;
      value.crouching = body.crouching;
    } else value.credit = 0;
    if (body.speakerOn && !value.speakerOn) value.hearAfter = voiceCursor;
    value.speakerOn = body.speakerOn;
    value.micOn = body.micOn && body.speakerOn;
    if (!value.micOn) value.audio = [];
    value.profile = player; value.seen = time; value.moved = time;
    // Friends/online time remain correct while in-world without writing SQLite eight times a second.
    if (time - value.presenceTouched >= 30_000) {
      db.touchPresence(player.id, new Date(time)); value.presenceTouched = time;
    }
    return [200, snapshot(value, time, corrected)];
  }
  function leave(player, body, now) {
    if (!body) return fail(400, 'bad_json');
    const value = member(player.id, body.sessionId, currentTime(now));
    if (!value) return fail(409, 'world_session_expired');
    remove(player.id);
    return [200, { ok: true }];
  }
  function postVoice(player, body, now) {
    if (!body) return fail(400, 'bad_json');
    const time = currentTime(now), value = member(player.id, body.sessionId, time);
    if (!value) return fail(409, 'world_session_expired');
    if (!value.speakerOn || !value.micOn) return fail(409, 'microphone_off');
    if (!Number.isSafeInteger(body.seq) || body.seq < 0) return fail(400, 'invalid_sequence');
    if (!Number.isInteger(body.rate) || body.rate < 8000 || body.rate > 48000) return fail(400, 'invalid_rate');
    if (typeof body.data !== 'string' || body.data.length > 16000 || !BASE64.test(body.data))
      return fail(400, 'invalid_audio');
    const bytes = Buffer.from(body.data, 'base64');
    if (bytes.length < 1 || bytes.length > 12000 || bytes.length > body.rate * .6 ||
      bytes.toString('base64') !== body.data) return fail(400, 'invalid_audio');
    if (body.seq <= value.lastVoiceSeq) return fail(409, 'stale_audio');
    value.lastVoiceSeq = body.seq;
    value.audio.push({ seq: ++voiceCursor, time, rate: body.rate, data: body.data,
      x: value.x, y: value.y, z: value.z });
    if (value.audio.length > VOICE_CHUNKS) value.audio.shift();
    return [200, { cursor: voiceCursor }];
  }
  function readVoice(player, url, now) {
    const time = currentTime(now), value = member(player.id, url.searchParams.get('sessionId'), time);
    if (!value) return fail(409, 'world_session_expired');
    const raw = url.searchParams.get('since') ?? '-1';
    if (!/^-?\d+$/.test(raw) || !Number.isSafeInteger(Number(raw))) return fail(400, 'invalid_cursor');
    const since = Number(raw), chunks = [];
    if (value.speakerOn && since >= 0 && since <= voiceCursor) {
      for (const other of members.values()) {
        if (other === value || !other.micOn || !other.speakerOn || distance3(value, other) > VOICE_RADIUS) continue;
        for (const chunk of other.audio) {
          if (chunk.seq <= Math.max(since, value.hearAfter) || distance3(value, chunk) > VOICE_RADIUS) continue;
          chunks.push({ ...chunk, publicId: other.profile.publicId, nickname: other.profile.nickname });
        }
      }
      // A crowded square or slow receiver must never produce an unbounded multi-megabyte reply.
      // Prefer recent speech; old packets skipped under congestion must not replay later.
      chunks.sort((a, b) => b.seq - a.seq);
      let budget = VOICE_REPLY_BYTES;
      let kept = 0;
      for (const chunk of chunks) {
        const cost = chunk.data.length + 512;
        if (kept >= 64 || cost > budget) continue;
        chunks[kept++] = chunk; budget -= cost;
      }
      chunks.length = kept;
      chunks.sort((a, b) => a.seq - b.seq);
    }
    return [200, { cursor: voiceCursor, chunks }];
  }
  return { join, state, leave, postVoice, readVoice, member, publicPeer, setActivity, clearActivity,
    getMember(id, time) { clean(time); return members.get(id) ?? null; },
    routes: {
      'POST /api/world/join': (ctx, body) => join(ctx.player, body, ctx.now),
      'POST /api/world/state': (ctx, body) => state(ctx.player, body, ctx.now),
      'POST /api/world/leave': (ctx, body) => leave(ctx.player, body, ctx.now),
    },
  };
}

function distance3(a, b) { return Math.hypot(a.x - b.x, a.y - b.y, a.z - b.z); }
function chooseSpawn(occupied) {
  const spots = [];
  // Four sidewalks, not road lanes, building footprints, or the chess entrance.
  for (const x of [-38, -9, 9, 38]) for (let z = -28; z <= 28; z += 1.75) spots.push({ x, z });
  const offset = randomInt(spots.length);
  for (let i = 0; i < spots.length; i++) {
    const spot = spots[(offset + i) % spots.length];
    if (occupied.every(other => Math.hypot(other.x - spot.x, other.z - spot.z) >= 1.2)) return spot;
  }
  // Capacity is lower than the available cells. This only occurs when moving players block every cell.
  return spots.reduce((best, spot) => {
    const gap = Math.min(...occupied.map(other => Math.hypot(other.x - spot.x, other.z - spot.z)));
    return gap > best.gap ? { ...spot, gap } : best;
  }, { ...spots[offset], gap: -1 });
}
