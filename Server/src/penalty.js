export const PENALTY_ZONE = Object.freeze({ x: 64, z: 43, radius: 8 });
export const PENALTY_TURN_MS = 30_000;
const RESULT_MS = 2500;
const fail = (status, error) => [status, { error }];

// Two-player, no-wager shootout. Choices are hidden until both commit: network latency cannot
// let the keeper read the target and then choose. All score/flight parameters originate here.
export function createPenalty(world) {
  let version = 0, turn = 0, phase = 'waiting', deadline = 0, clock = 0, winner = 0;
  let shotChoice = null, diveChoice = null, shot = null, result = '', releaseAt = 0;
  const seats = [null, null], scores = [0, 0], attempts = [0, 0], history = [], votes = new Set();
  const shooterIndex = () => turn % 2;
  const idOf = seat => seat ? world.publicPeer(world.getMember(seat.id, clock))?.publicId ?? 0 : 0;
  function reset(time) {
    turn = 0; scores.fill(0); attempts.fill(0); history.length = 0; winner = 0;
    shotChoice = diveChoice = shot = null; result = ''; votes.clear(); releaseAt = 0;
    phase = seats.every(Boolean) ? 'aiming' : 'waiting'; deadline = phase === 'aiming' ? time + PENALTY_TURN_MS : 0;
    version++; position(time);
  }
  function activity(index, time, action = '', actionAt = time) {
    const seat = seats[index]; if (!seat) return false;
    const keeper = index !== shooterIndex();
    return world.setActivity(seat.id, seat.sessionId, { kind: 'penalty', slot: 0, role: 'player' + (index + 1),
      pose: keeper ? 'keeper' : 'stand', x: 64, y: 0, z: keeper ? 78.5 : 66.5, yaw: keeper ? 180 : 0,
      action, actionAt: action ? actionAt : 0 }, time);
  }
  function position(time) { for (let i = 0; i < 2; i++) if (seats[i]) activity(i, time); }
  function release(index, time) {
    const seat = seats[index]; if (!seat) return;
    if (seats.every(Boolean) && !['finished', 'abandoned'].includes(phase)) {
      phase = 'abandoned'; winner = idOf(seats[1 - index]); deadline = 0;
      shotChoice = diveChoice = null; result = 'abandoned';
    }
    world.clearActivity(seat.id, 'penalty', time, seat.sessionId);
    seats[index] = null; votes.clear(); version++;
    if (!seats.some(Boolean)) reset(time);
  }
  function clean(time) {
    for (let i = 0; i < 2; i++) {
      const seat = seats[i]; if (!seat) continue;
      const member = world.getMember(seat.id, time);
      if (!member || member.sessionId !== seat.sessionId) release(i, time);
    }
    if (phase === 'aiming' && time >= deadline) {
      if (!shotChoice) shotChoice = { aimX: 0, aimY: 0, power: .65, timeout: true };
      if (!diveChoice) diveChoice = { direction: 0 };
      launch(time);
    }
    if (phase === 'flight' && time >= shot.startedAt + shot.durationMs) {
      const shooter = shooterIndex();
      result = shot.outcome; attempts[shooter]++;
      if (result === 'goal') scores[shooter]++;
      history.push({ publicId: idOf(seats[shooter]), result });
      const other = 1 - shooter;
      const remaining = attempts.map(n => Math.max(0, 5 - n));
      const decided = (scores[shooter] > scores[other] + remaining[other]) ||
        (scores[other] > scores[shooter] + remaining[shooter]);
      const evenOvertime = attempts[0] >= 5 && attempts[0] === attempts[1];
      if (decided && (attempts[0] < 5 || attempts[1] < 5) || evenOvertime && scores[0] !== scores[1]) {
        phase = 'finished'; winner = idOf(seats[scores[0] > scores[1] ? 0 : 1]);
      } else if (attempts[0] >= 20 && attempts[0] === attempts[1]) { phase = 'finished'; winner = 0; }
      else { phase = 'result'; releaseAt = time + RESULT_MS; }
      version++;
    }
    if (phase === 'result' && time >= releaseAt) {
      turn++; phase = 'aiming'; deadline = time + PENALTY_TURN_MS;
      shotChoice = diveChoice = shot = null; result = ''; version++; position(time);
    }
  }
  function launch(time) {
    if (!shotChoice || !diveChoice || phase !== 'aiming') return;
    const { aimX, aimY, power, timeout } = shotChoice;
    const targetX = aimX * 4.2, targetY = .15 + aimY * 2.7 + Math.max(0, power - .78) * 1.1;
    const onTarget = !timeout && Math.abs(targetX) <= 3.55 && targetY >= .11 && targetY <= 2.33;
    const keeperX = diveChoice.direction * 2.15;
    const reachX = diveChoice.direction === 0 ? .92 : 1.35;
    const saved = onTarget && Math.abs(targetX - keeperX) <= reachX && targetY <= (diveChoice.direction === 0 ? 2.1 : 2.22);
    const outcome = timeout ? 'timeout' : !onTarget ? 'miss' : saved ? 'save' : 'goal';
    shot = { shotId: turn + 1, startedAt: time + 350, durationMs: Math.round(1150 - power * 480),
      targetX: 64 + targetX, targetY, targetZ: 79, power, dive: diveChoice.direction, outcome };
    phase = 'flight'; deadline = 0; version++;
    activity(shooterIndex(), time, 'kick', time + 350);
    activity(1 - shooterIndex(), time, diveChoice.direction < 0 ? 'dive_left' : diveChoice.direction > 0 ? 'dive_right' : 'dive_center', time + 400);
  }
  function snapshot(time) {
    return { version, phase, turn, deadline, serverTime: time, winner, result,
      player1: seats[0] ? world.publicPeer(world.getMember(seats[0].id, time)) : null,
      player2: seats[1] ? world.publicPeer(world.getMember(seats[1].id, time)) : null,
      shooterId: idOf(seats[shooterIndex()]), keeperId: idOf(seats[1 - shooterIndex()]),
      score1: scores[0], score2: scores[1], attempts1: attempts[0], attempts2: attempts[1],
      shooterReady: !!shotChoice, keeperReady: !!diveChoice,
      // The outcome stays hidden until the ball crosses the goal plane.
      shot: shot ? { ...shot, outcome: phase === 'flight' ? '' : shot.outcome } : null,
      history: [...history], resetVotes: [...votes].map(id => idOf(seats.find(s => s?.id === id))) };
  }
  function dispatch(action, ctx, body) {
    if (!body || typeof body !== 'object') return fail(400, 'bad_json');
    clock = Math.max(clock, +ctx.now); const time = clock;
    clean(time);
    const member = world.member(ctx.player.id, body.sessionId, time);
    if (!member) return fail(409, 'world_session_expired');
    const own = seats.findIndex(s => s?.id === ctx.player.id && s.sessionId === body.sessionId);
    if (action === 'state') return [200, snapshot(time)];
    if (action === 'leave') { if (own >= 0) release(own, time); return [200, snapshot(time)]; }
    if (action === 'join') {
      if (own >= 0) return [200, snapshot(time)];
      if (Math.hypot(member.x - PENALTY_ZONE.x, member.z - PENALTY_ZONE.z) > PENALTY_ZONE.radius)
        return fail(403, 'too_far');
      const index = seats.findIndex(s => !s);
      if (index < 0) return fail(409, 'arena_full');
      seats[index] = { id: ctx.player.id, sessionId: member.sessionId };
      if (!activity(index, time)) { seats[index] = null; return fail(409, 'activity_busy'); }
      reset(time);
    } else {
      if (own < 0) return fail(403, 'not_playing');
      if (action === 'reset') {
        if (!['finished', 'abandoned'].includes(phase)) return fail(409, 'match_active');
        if (!seats.every(Boolean)) reset(time);
        else if (!votes.has(ctx.player.id)) {
          votes.add(ctx.player.id); version++;
          if (votes.size === 2) reset(time);
        }
      } else if (action === 'shoot' || action === 'dive') {
        // Version plus immutable per-turn commitment prevents replay and changing a revealed choice.
        if (!Number.isSafeInteger(body.version) || body.version !== version) return fail(409, 'stale_state');
        if (!Number.isSafeInteger(body.turn) || body.turn !== turn) return fail(409, 'stale_turn');
        if (phase !== 'aiming') return fail(409, 'not_aiming');
        if (action === 'shoot') {
          if (own !== shooterIndex()) return fail(403, 'not_shooter');
          if (shotChoice) return fail(409, 'already_committed');
          if (![body.aimX, body.aimY, body.power].every(Number.isFinite) || Math.abs(body.aimX) > 1.25 ||
            body.aimY < -.1 || body.aimY > 1.2 || body.power < .35 || body.power > 1)
            return fail(400, 'invalid_shot');
          shotChoice = { aimX: body.aimX, aimY: body.aimY, power: body.power };
        } else {
          if (own === shooterIndex()) return fail(403, 'not_keeper');
          if (diveChoice) return fail(409, 'already_committed');
          if (!Number.isInteger(body.direction) || ![-1, 0, 1].includes(body.direction)) return fail(400, 'invalid_dive');
          diveChoice = { direction: body.direction };
        }
        // Keep the turn version while only one choice is committed. Simultaneous submissions
        // from two players must not invalidate each other; each private slot is write-once.
        launch(time);
      } else return fail(404, 'not_found');
    }
    return [200, snapshot(time)];
  }
  return {
    'GET /api/penalty/state': ctx => dispatch('state', ctx, { sessionId: ctx.url.searchParams.get('sessionId') }),
    ...Object.fromEntries(['join', 'shoot', 'dive', 'leave', 'reset'].map(action => [
      'POST /api/penalty/' + action, (ctx, body) => dispatch(action, ctx, body),
    ])),
  };
}
