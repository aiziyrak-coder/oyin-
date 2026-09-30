// Render-only remote-avatar helper for an ISOLATED cloned test database, never a public/real server.
// Creates one temporary test identity in that clone. No PlayerPrefs, stored credentials, or audio.
// Optional --chess occupies black at table 0 and makes exactly one legal reply. It is OFF by default.
// Usage: node tools/world-visual-bot.mjs --allow-test --base-url http://127.0.0.1:8088 --seconds 120
import { randomInt } from 'node:crypto';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { setTimeout as sleep } from 'node:timers/promises';

export function parseOptions(args) {
  let allowed = false, rawUrl = '', seconds = 120, chess = false, penalty = false;
  for (let i = 0; i < args.length; i++) {
    if (args[i] === '--allow-test') allowed = true;
    else if (args[i] === '--chess') chess = true;
    else if (args[i] === '--penalty') penalty = true;
    else if (args[i] === '--base-url') rawUrl = args[++i] ?? '';
    else if (args[i] === '--seconds') seconds = Number(args[++i]);
    else throw new Error('Unknown option. Use --allow-test --base-url http://127.0.0.1:8088 [--seconds 120] [--chess|--penalty].');
  }
  if (!allowed) throw new Error('Refusing to run without explicit --allow-test. Use an isolated cloned database.');
  if (!rawUrl) throw new Error('An explicit --base-url pointing to a loopback test server is required.');
  let url;
  try { url = new URL(rawUrl); } catch { throw new Error('Invalid loopback base URL.'); }
  if (!['http:', 'https:'].includes(url.protocol) || !['127.0.0.1', 'localhost', '[::1]'].includes(url.hostname) ||
      url.username || url.password || url.search || url.hash || url.pathname !== '/')
    throw new Error('Only an explicit loopback HTTP(S) origin without credentials/path/query is allowed.');
  // The normal local player database listens on 8080; avoid it even if the operator supplies the wrong port.
  if (!url.port || url.port === '8080') throw new Error('Use a separate test port (for example 8088), not the normal server.');
  if (!Number.isInteger(seconds) || seconds < 5 || seconds > 120) throw new Error('--seconds must be an integer from 5 to 120.');
  if (chess && penalty) throw new Error('Choose one test activity at a time: --chess or --penalty.');
  return { baseUrl: url.origin, seconds, chess, penalty };
}

export function chooseChessReply(table, publicId) {
  if (table.black?.publicId !== publicId || table.turn !== 'black' || !['playing', 'check'].includes(table.status)) return null;
  const moves = table.legalMoves ?? [];
  return moves.find(move => move.from === 'e7' && move.to === 'e5') ?? moves[0] ?? null;
}

// Deliberately predictable two-turn partner for DevPenaltySmoke, not a competitive AI.
// It never chooses from an opponent's private aim or claimed client score.
export function choosePenaltyAction(arena, publicId) {
  if (arena?.player2?.publicId !== publicId || arena.phase !== 'aiming') return null;
  if (arena.turn === 0 && arena.keeperId === publicId && !arena.keeperReady)
    return { action: 'dive', direction: 0 };
  if (arena.turn === 1 && arena.shooterId === publicId && !arena.shooterReady)
    return { action: 'shoot', aimX: 0, aimY: .4, power: .72 };
  return null;
}

export function penaltyFollowTarget(target, arena) {
  // followStep adds (+2,+1). This virtual target lands at the public entry kiosk,
  // not at the already-teleported shooter's spot 23.5 metres inside the arena.
  return target?.activity === 'penalty' || (arena?.player1 && !arena.player2)
    ? { x: 62, z: 42 } : target;
}

export function followStep(self, target, elapsedSeconds) {
  if (!target) return { x: self.x, z: self.z, yaw: self.yaw };
  const x = Math.max(-140, Math.min(140, target.x + 2));
  const z = Math.max(-140, Math.min(140, target.z + 1));
  const dx = x - self.x, dz = z - self.z, distance = Math.hypot(dx, dz);
  const step = Math.min(distance, 6 * Math.max(0, Math.min(.4, elapsedSeconds)));
  const scale = distance > .001 ? step / distance : 0;
  return { x: self.x + dx * scale, z: self.z + dz * scale,
    yaw: distance > .15 ? (Math.atan2(dx, dz) * 180 / Math.PI + 360) % 360 : self.yaw };
}

async function main() {
  const { baseUrl, seconds, chess, penalty } = parseOptions(process.argv.slice(2));
  const deadline = Date.now() + seconds * 1000;
  let stopped = false, token = '', sessionId = '', following = 0;
  const stop = () => { stopped = true; };
  process.on('SIGINT', stop); process.on('SIGTERM', stop);
  async function api(path, body, cleanup = false) {
    const remaining = deadline - Date.now();
    if (remaining <= 0 || (stopped && !cleanup)) throw new Error('Test duration ended.');
    let response;
    try {
      response = await fetch(baseUrl + path, { method: body === undefined ? 'GET' : 'POST', redirect: 'error',
        headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: 'Bearer ' + token } : {}) },
        body: JSON.stringify(body), signal: AbortSignal.timeout(Math.max(1, Math.min(2000, remaining))) });
    } catch { throw new Error('Test server request failed or timed out.'); }
    let data;
    try { data = await response.json(); } catch { throw new Error('Test server returned invalid JSON.'); }
    return { ok: response.ok, status: response.status, data };
  }
  try {
    let created;
    for (let attempt = 0; attempt < 3; attempt++) {
      const nickname = 'CityTestGuest' + randomInt(36 ** 3).toString(36).padStart(3, '0');
      created = await api('/api/players', { nickname, gender: 'male', avatarId: 'M2' });
      if (created.ok) break;
      if (created.status !== 409) throw new Error('Test profile creation failed (HTTP ' + created.status + ').');
    }
    if (!created?.ok || typeof created.data.token !== 'string') throw new Error('Could not create a unique test guest.');
    token = created.data.token;
    const joined = await api('/api/world/join', {});
    if (!joined.ok || !joined.data.self || typeof joined.data.sessionId !== 'string')
      throw new Error('Test world join failed (HTTP ' + joined.status + ').');
    sessionId = joined.data.sessionId;
    let snapshot = joined.data, previous = performance.now(), nextLog = 0, nextChess = 0, chessReplied = false;
    let nextPenalty = 0, arena = null;
    console.log('Test guest ' + created.data.nickname + ' #' + created.data.publicId + ' joined the isolated world; microphone and speaker OFF.');
    // Reserve three seconds for orderly leave. No reconnect loop can create additional identities/sessions.
    while (!stopped && Date.now() < deadline - 3000) {
      const tick = performance.now();
      const target = snapshot.players?.find(peer => peer.publicId === following) ?? snapshot.players?.[0];
      following = target?.publicId ?? 0;
      const destination = snapshot.self.activity ? null : penalty ? penaltyFollowTarget(target, arena) : target;
      const step = followStep(snapshot.self, destination, (tick - previous) / 1000); previous = tick;
      const result = await api('/api/world/state', { sessionId, ...step, y: .15,
        crouching: false, micOn: false, speakerOn: false, movementRevision: snapshot.self.movementRevision ?? 0 });
      if (!result.ok || !result.data.self) throw new Error('Test world update failed (HTTP ' + result.status + ').');
      snapshot = result.data;
      if (chess && !chessReplied && tick >= nextChess && Date.now() < deadline - 6500 &&
          Math.hypot(snapshot.self.x + 14, snapshot.self.z - 59) <= 3) {
        nextChess = tick + 1000; // At most one board read per second; stale move responses retry a fresh board later.
        const read = await api('/api/chess/table?sessionId=' + encodeURIComponent(sessionId) + '&tableId=0');
        if (read.ok) {
          let table = read.data;
          if (!table.black && table.white) {
            const seated = await api('/api/chess/join', { sessionId, tableId: 0, color: 'black' });
            if (seated.ok) { table = seated.data; console.log('Test guest took the black seat at table 0.'); }
            // too_far, seat_taken, and other permission conflicts leave the world loop untouched.
          }
          const move = chooseChessReply(table, created.data.publicId);
          if (move) {
            const played = await api('/api/chess/move', { sessionId, tableId: 0,
              from: move.from, to: move.to, promotion: move.promotion || 'q', version: table.version });
            if (played.ok) {
              chessReplied = true;
              console.log('Test chess black reply: ' + move.from + '-' + move.to + '. No further bot moves.');
            }
          }
        }
      }
      if (penalty && tick >= nextPenalty && Date.now() < deadline - 6500) {
        nextPenalty = tick + 500;
        const read = await api('/api/penalty/state?sessionId=' + encodeURIComponent(sessionId));
        if (read.ok) {
          arena = read.data;
          if (arena.player1 && !arena.player2 && Math.hypot(snapshot.self.x - 64, snapshot.self.z - 43) <= 8) {
            const entered = await api('/api/penalty/join', { sessionId });
            if (entered.ok) { arena = entered.data; console.log('Test guest joined penalty player 2.'); }
          }
          if (arena.player2?.publicId === created.data.publicId && arena.phase === 'abandoned') {
            const left = await api('/api/penalty/leave', { sessionId });
            if (left.ok) { arena = left.data; console.log('Test guest released abandoned penalty match.'); }
          }
          const choice = choosePenaltyAction(arena, created.data.publicId);
          if (choice) {
            const { action, ...fields } = choice;
            const committed = await api('/api/penalty/' + action, { sessionId, version: arena.version, turn: arena.turn, ...fields });
            if (committed.ok) { arena = committed.data; console.log('Test penalty turn ' + arena.turn + ': committed ' + action + '.'); }
          }
        }
      }
      if (tick >= nextLog) {
        nextLog = tick + 5000;
        const p = snapshot.self;
        console.log(`Public position x=${p.x.toFixed(1)} z=${p.z.toFixed(1)}; following=${following || 'waiting'}; peers=${snapshot.players?.length ?? 0}.`);
      }
      await sleep(Math.max(0, 180 - (performance.now() - tick)));
    }
  } finally {
    if (sessionId && token) {
      try {
        const left = await api('/api/world/leave', { sessionId }, true);
        console.log(left.ok ? 'Test guest left the world.' : 'World leave was not acknowledged; heartbeat expiry will remove the guest.');
      } catch { console.log('Test server unavailable during leave; heartbeat expiry will remove the guest.'); }
    }
    token = ''; sessionId = '';
    process.off('SIGINT', stop); process.off('SIGTERM', stop);
  }
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main().catch(error => { console.error(error.message); process.exitCode = 1; });
}
