import assert from 'node:assert/strict';
import { test } from 'node:test';
import { Chess } from 'chess.js';
import { createChess, CHESS_TABLES } from '../src/chess.js';
import { createWorld } from '../src/world.js';

function fixture(fen) {
  let clock = Date.parse('2026-10-01T10:00:00Z');
  const world = createWorld({ touchPresence() {} }, { spawn: () => ({ x: -14, z: 59 }) });
  const routes = createChess(world, fen ? { createGame: () => new Chess(fen) } : {});
  const now = () => new Date(clock), players = [];
  function make() {
    const index = players.length;
    const profile = { id: 'chess-' + index, publicId: 100001 + index, nickname: 'Chess' + index, avatarId: 'M1', outfit: '' };
    const joined = world.join(profile, {}, now())[1];
    const p = { profile, sessionId: joined.sessionId, self: joined.self }; players.push(p); return p;
  }
  function call(p, action, body = {}) {
    const data = { sessionId: p.sessionId, tableId: 0, ...body };
    const ctx = { player: p.profile, now: now(), url: new URL('http://local/?' + new URLSearchParams(data)) };
    return routes[(action === 'table' ? 'GET' : 'POST') + ' /api/chess/' + action](ctx, data);
  }
  const state = (p, change = {}) => {
    const result = world.state(p.profile, { sessionId: p.sessionId, ...p.self, ...change }, now());
    if (result[0] === 200) p.self = result[1].self; return result;
  };
  const a = make(), b = make(), c = make();
  function both() { call(a, 'join', { color: 'white' }); return call(b, 'join', { color: 'black' })[1]; }
  function move(from, to, promotion, player) {
    const board = call(a, 'table')[1];
    return call(player ?? (board.turn === 'white' ? a : b), 'move', { version: board.version, from, to, ...(promotion ? { promotion } : {}) });
  }
  return { a, b, c, world, routes, call, both, move, state, now, advance(ms) { clock += ms; } };
}

test('chess exposes ten independent boards, correct square order and contested seats', () => {
  const f = fixture();
  assert.equal(CHESS_TABLES.length, 10);
  for (let tableId = 0; tableId < 10; tableId++) {
    const table = f.call(f.c, 'table', { tableId })[1];
    assert.equal(table.status, 'waiting'); assert.equal(table.board.length, 64);
    assert.equal(table.board[0], 'R'); assert.equal(table.board[4], 'K'); assert.equal(table.board[60], 'k');
  }
  assert.equal(f.call(f.a, 'join', { color: 'white' })[1].status, 'waiting');
  assert.equal(f.call(f.c, 'join', { color: 'white' })[1].error, 'seat_taken');
  assert.equal(f.call(f.a, 'join', { color: 'black' })[1].error, 'leave_first');
  const table = f.call(f.b, 'join', { color: 'black' })[1];
  assert.equal(table.status, 'playing'); assert.equal(table.legalMoves.length, 20);
  assert.equal(table.white.publicId, f.a.profile.publicId); assert.equal(table.black.publicId, f.b.profile.publicId);
  assert.equal(f.call(f.a, 'join', { color: 'white' })[1].version, table.version); // idempotent same seat
  assert.equal(f.call(f.c, 'table', { tableId: 1 })[1].white, null);
});

test('chess rejects remote players, stale sessions, invalid tables, colors, moves and old versions', () => {
  const f = fixture(); const table = f.both();
  assert.equal(f.call(f.c, 'move', { from: 'e2', to: 'e4', version: table.version })[1].error, 'not_seated');
  assert.equal(f.call(f.b, 'move', { from: 'e7', to: 'e5', version: table.version })[1].error, 'not_your_turn');
  assert.equal(f.call(f.a, 'move', { from: 'e2', to: 'e5', version: table.version })[1].error, 'illegal_move');
  for (const change of [{ from: 'E2', to: 'e4' }, { from: 'a0', to: 'a1' }, { from: 'e2', to: 'e4', promotion: 'k' },
    { from: 2, to: 'e4' }, { from: 'e2', to: null }])
    assert.equal(f.call(f.a, 'move', { version: table.version, ...change })[0], 400);
  assert.equal(f.call(f.a, 'move', { from: 'e2', to: 'e4', version: table.version - 1 })[1].error, 'stale_board');
  assert.equal(f.call(f.c, 'join', { color: 'red' })[1].error, 'invalid_color');
  assert.equal(f.call(f.a, 'table', { tableId: -1 })[0], 400);
  assert.equal(f.call(f.a, 'table', { tableId: 10 })[0], 400);
  assert.equal(f.call(f.a, 'table', { sessionId: f.b.sessionId })[1].error, 'world_session_expired');
  assert.equal(f.call(f.c, 'join', { color: 'white', tableId: 9 })[1].error, 'too_far');
  const moved = f.move('e2', 'e4'); assert.equal(moved[0], 200);
  assert.equal(moved[1].board[12], ''); assert.equal(moved[1].board[28], 'P');
  assert.equal(f.call(f.a, 'move', { from: 'e2', to: 'e4', version: table.version })[1].error, 'stale_board');
});

test('chess recognizes checkmate and will not accept moves after game over', () => {
  const f = fixture(); f.both();
  for (const [from, to] of [['f2', 'f3'], ['e7', 'e5'], ['g2', 'g4'], ['d8', 'h4']])
    assert.equal(f.move(from, to)[0], 200);
  const table = f.call(f.c, 'table')[1];
  assert.equal(table.status, 'checkmate'); assert.equal(table.winner, 'black'); assert.equal(table.legalMoves.length, 0);
  assert.equal(f.move('a2', 'a3')[1].error, 'game_over');
});

test('chess validates castling including the attacked transit square', () => {
  const f = fixture(); f.both();
  for (const [from, to] of [['e2', 'e4'], ['e7', 'e5'], ['g1', 'f3'], ['b8', 'c6'], ['f1', 'c4'], ['g8', 'f6']])
    assert.equal(f.move(from, to)[0], 200);
  const castled = f.move('e1', 'g1')[1];
  assert.equal(castled.board[6], 'K'); assert.equal(castled.board[5], 'R'); assert.equal(castled.board[7], '');
  const attacked = fixture('r3k2r/8/8/8/8/8/5r2/R3K2R w KQkq - 0 1'); attacked.both();
  assert.equal(attacked.move('e1', 'g1')[1].error, 'illegal_move');
});

test('chess en passant capture removes the bypassed pawn and expires after another move', () => {
  const f = fixture(); f.both();
  for (const [from, to] of [['e2', 'e4'], ['a7', 'a6'], ['e4', 'e5'], ['d7', 'd5']])
    assert.equal(f.move(from, to)[0], 200);
  const captured = f.move('e5', 'd6')[1];
  assert.equal(captured.board[43], 'P'); assert.equal(captured.board[35], '');
  const late = fixture('7k/8/8/3pP3/8/8/8/K7 w - d6 0 2'); late.both();
  assert.equal(late.move('a1', 'a2')[0], 200); assert.equal(late.move('h8', 'h7')[0], 200);
  assert.equal(late.move('e5', 'd6')[1].error, 'illegal_move');
});

test('chess supports all four promotions and defaults omitted promotion to queen', () => {
  for (const promotion of ['q', 'r', 'b', 'n', undefined]) {
    const f = fixture('7k/P7/8/8/8/8/8/K7 w - - 0 1'); f.both();
    const table = f.move('a7', 'a8', promotion);
    assert.equal(table[0], 200); assert.equal(table[1].board[56], (promotion ?? 'q').toUpperCase());
  }
});

test('chess reports check, stalemate, insufficient material and threefold draws', () => {
  const check = fixture('4r2k/8/8/8/8/8/P7/4K3 w - - 0 1'); check.both();
  assert.equal(check.call(check.a, 'table')[1].status, 'check');
  assert.equal(check.move('a2', 'a3')[1].error, 'illegal_move'); // cannot ignore check
  const stale = fixture('7k/5K2/6Q1/8/8/8/8/8 b - - 0 1'); stale.both();
  assert.equal(stale.call(stale.a, 'table')[1].status, 'stalemate');
  const draw = fixture('7k/8/8/8/8/8/8/K7 w - - 0 1'); draw.both();
  assert.equal(draw.call(draw.a, 'table')[1].status, 'draw');
  const repetition = fixture(); repetition.both();
  for (let i = 0; i < 2; i++) for (const [from, to] of [['g1', 'f3'], ['g8', 'f6'], ['f3', 'g1'], ['f6', 'g8']])
    assert.equal(repetition.move(from, to)[0], 200);
  assert.equal(repetition.call(repetition.a, 'table')[1].status, 'draw');
});

test('chess rematch requires both votes and a move cancels stale reset consent', () => {
  const f = fixture(); f.both(); f.move('e2', 'e4');
  const requested = f.call(f.a, 'reset')[1];
  assert.deepEqual(requested.resetVotes, [f.a.profile.publicId]); assert.equal(requested.board[28], 'P');
  assert.equal(f.call(f.c, 'reset')[1].error, 'not_seated');
  const accepted = f.call(f.b, 'reset')[1];
  assert.equal(accepted.board[12], 'P'); assert.equal(accepted.board[28], ''); assert.equal(accepted.resetVotes.length, 0);
  f.call(f.a, 'reset'); const moved = f.move('e2', 'e4')[1]; assert.equal(moved.resetVotes.length, 0);
  const once = f.call(f.b, 'reset')[1]; const twice = f.call(f.b, 'reset')[1];
  assert.equal(once.version, twice.version); assert.equal(twice.board[28], 'P');
});

test('chess leaves/abandons games, resets empty boards, and releases walked-away seats', () => {
  const f = fixture(); f.both(); f.move('e2', 'e4');
  const abandoned = f.call(f.a, 'leave')[1];
  assert.equal(abandoned.status, 'abandoned'); assert.equal(abandoned.winner, 'black'); assert.equal(abandoned.white, null);
  f.call(f.b, 'leave');
  const empty = f.call(f.c, 'table')[1]; assert.equal(empty.status, 'waiting'); assert.equal(empty.board[12], 'P');
  f.both(); f.advance(1000); f.state(f.a, { x: -8 });
  assert.equal(f.call(f.b, 'table')[1].white, null);
  assert.equal(f.call(f.c, 'join', { color: 'white' })[0], 200);
});

test('chess reconnect and expired heartbeat release seats even if client never sends leave', () => {
  const f = fixture(); f.both();
  f.world.join(f.a.profile, {}, f.now());
  assert.equal(f.call(f.b, 'table')[1].white, null);
  f.call(f.c, 'join', { color: 'white' });
  f.advance(10000); f.state(f.c);
  f.advance(5000);
  const table = f.call(f.c, 'table')[1]; assert.equal(table.black, null); assert.equal(table.white.publicId, f.c.profile.publicId);
  assert.equal(f.call(f.b, 'table')[1].error, 'world_session_expired');
});
