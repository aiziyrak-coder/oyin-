import { Chess } from 'chess.js';

export const CHESS_TABLES = Array.from({ length: 10 }, (_, i) => ({
  tableId: i, x: [-14, -7, 0, 7, 14][i % 5], z: i < 5 ? 59 : 70,
}));
const fail = (status, error) => [status, { error }];
const colorName = color => color === 'w' ? 'white' : 'black';
const opposite = color => color === 'white' ? 'black' : 'white';

// Chess positions/seats exist only in this running public world. No wager or wallet path exists.
// chess.js is the sole legal-move authority; clients never submit FEN or a replacement board.
export function createChess(world, { createGame = () => new Chess() } = {}) {
  const tables = CHESS_TABLES.map(position => ({ ...position, game: createGame(), version: 0,
    white: null, black: null, outcome: '', winner: '', votes: new Set(), lastFrom: '', lastTo: '', drawOffer: '' }));
  function reset(table) {
    table.game.reset(); table.outcome = ''; table.winner = ''; table.votes.clear();
    table.lastFrom = ''; table.lastTo = ''; table.drawOffer = ''; table.version++;
  }
  function release(table, color, time) {
    if (!table[color]) return;
    const seat = table[color];
    world.clearActivity(seat.id, 'chess', time, seat.sessionId);
    if (table.white && table.black && !table.outcome && !table.game.isGameOver()) {
      table.outcome = 'abandoned'; table.winner = opposite(color);
    }
    table[color] = null; table.votes.clear(); table.drawOffer = ''; table.version++;
    if (!table.white && !table.black) reset(table);
  }
  function clean(time) {
    for (const table of tables) for (const color of ['white', 'black']) {
      const seat = table[color];
      if (!seat) continue;
      const member = world.getMember(seat.id, time);
      if (!member || member.sessionId !== seat.sessionId || distance(member, table) > 5) release(table, color, time);
    }
  }
  function seatPeer(seat, time) { return seat ? world.publicPeer(world.getMember(seat.id, time)) : null; }
  function snapshot(table, time) {
    const game = table.game;
    let status = table.outcome, winner = table.winner;
    if (!status) {
      if (game.isCheckmate()) { status = 'checkmate'; winner = opposite(colorName(game.turn())); }
      else if (game.isStalemate()) status = 'stalemate';
      else if (game.isDraw()) status = 'draw';
      else if (!table.white || !table.black) status = 'waiting';
      else status = game.isCheck() ? 'check' : 'playing';
    }
    // Stable Unity-friendly order: a1,b1,...h1,a2,...h8; uppercase white, lowercase black.
    const board = [];
    for (let rank = 1; rank <= 8; rank++) for (const file of 'abcdefgh') {
      const piece = game.get(file + rank);
      board.push(piece ? piece.color === 'w' ? piece.type.toUpperCase() : piece.type : '');
    }
    const history = game.history({ verbose: true }).map((move, i) => ({ ply: i + 1,
      number: Number(move.before.split(' ')[5]), color: colorName(move.color), san: move.san,
      from: move.from, to: move.to, piece: move.piece, captured: move.captured ?? '', promotion: move.promotion ?? '' }));
    const checkSquare = game.isCheck() ? board.findIndex(piece => piece === (game.turn() === 'w' ? 'K' : 'k')) : -1;
    return { tableId: table.tableId, version: table.version, fen: game.fen(), board,
      turn: colorName(game.turn()), white: seatPeer(table.white, time), black: seatPeer(table.black, time),
      status, winner, lastFrom: table.lastFrom, lastTo: table.lastTo,
      timeControl: 'untimed', drawOffer: table.drawOffer, history,
      checkSquare: checkSquare < 0 ? '' : 'abcdefgh'[checkSquare % 8] + (Math.floor(checkSquare / 8) + 1),
      whiteCaptures: history.filter(move => move.color === 'white' && move.captured).map(move => move.captured),
      blackCaptures: history.filter(move => move.color === 'black' && move.captured).map(move => move.captured.toUpperCase()),
      legalMoves: ['playing', 'check'].includes(status)
        ? game.moves({ verbose: true }).map(move => ({ from: move.from, to: move.to, promotion: move.promotion ?? '' })) : [],
      resetVotes: [...table.votes].map(id => world.getMember(id, time)?.profile.publicId).filter(Boolean) };
  }
  function dispatch(action, ctx, body) {
    if (!body || typeof body !== 'object') return fail(400, 'bad_json');
    const time = +ctx.now;
    clean(time);
    const member = world.member(ctx.player.id, body.sessionId, time);
    if (!member) return fail(409, 'world_session_expired');
    if (!Number.isInteger(body.tableId) || body.tableId < 0 || body.tableId >= tables.length)
      return fail(400, 'invalid_table');
    const table = tables[body.tableId];
    const ownColor = ['white', 'black'].find(color => table[color]?.id === ctx.player.id);
    if (action === 'table') return [200, snapshot(table, time)];
    if (action === 'leave') {
      if (ownColor) release(table, ownColor, time);
      return [200, snapshot(table, time)];
    }
    if (distance(member, table) > 3) return fail(403, 'too_far');
    if (action === 'join') {
      if (!['white', 'black'].includes(body.color)) return fail(400, 'invalid_color');
      if (ownColor) return ownColor === body.color ? [200, snapshot(table, time)] : fail(409, 'leave_first');
      if (tables.some(other => other.white?.id === ctx.player.id || other.black?.id === ctx.player.id))
        return fail(409, 'leave_first');
      if (table[body.color]) return fail(409, 'seat_taken');
      const side = body.color === 'white' ? -1 : 1;
      if (!world.setActivity(ctx.player.id, member.sessionId, { kind: 'chess', slot: table.tableId,
        role: body.color, pose: 'sit', x: table.x, y: 0, z: table.z + side * 1.22,
        yaw: side === -1 ? 0 : 180 }, time)) return fail(409, 'activity_busy');
      table[body.color] = { id: ctx.player.id, sessionId: member.sessionId };
      table.votes.clear(); table.version++;
    } else if (action === 'move') {
      if (!ownColor) return fail(403, 'not_seated');
      if (!Number.isSafeInteger(body.version) || body.version !== table.version) return fail(409, 'stale_board');
      if (!table.white || !table.black) return fail(409, 'waiting_for_player');
      if (table.outcome || table.game.isGameOver()) return fail(409, 'game_over');
      if (ownColor !== colorName(table.game.turn())) return fail(403, 'not_your_turn');
      if (typeof body.from !== 'string' || typeof body.to !== 'string' ||
        !/^[a-h][1-8]$/.test(body.from) || !/^[a-h][1-8]$/.test(body.to) ||
        (body.promotion !== undefined && body.promotion !== '' && !['q', 'r', 'b', 'n'].includes(body.promotion)))
        return fail(400, 'invalid_move');
      let move;
      try { move = table.game.move({ from: body.from, to: body.to, promotion: body.promotion || 'q' }); }
      catch { return fail(400, 'illegal_move'); }
      table.lastFrom = move.from; table.lastTo = move.to; table.votes.clear(); table.version++;
      // An offered draw remains available through the offering player's move; the opponent's move declines it.
      if (table.drawOffer && table.drawOffer !== ownColor) table.drawOffer = '';
    } else if (action === 'resign' || action === 'draw') {
      if (!ownColor) return fail(403, 'not_seated');
      if (!Number.isSafeInteger(body.version) || body.version !== table.version) return fail(409, 'stale_board');
      if (!table.white || !table.black) return fail(409, 'waiting_for_player');
      if (table.outcome || table.game.isGameOver()) return fail(409, 'game_over');
      if (action === 'resign') { table.outcome = 'resigned'; table.winner = opposite(ownColor); table.drawOffer = ''; }
      else if (table.drawOffer === ownColor) return [200, snapshot(table, time)];
      else if (table.drawOffer) { table.outcome = 'draw'; table.winner = ''; table.drawOffer = ''; }
      else table.drawOffer = ownColor;
      table.votes.clear(); table.version++;
    } else if (action === 'reset') {
      if (!ownColor) return fail(403, 'not_seated');
      // An opponent cannot erase a live game: both seated players must agree. Votes clear on every move.
      if (!table.white || !table.black) reset(table);
      else if (!table.votes.has(ctx.player.id)) {
        table.votes.add(ctx.player.id); table.version++;
        if (table.votes.size === 2) reset(table);
      }
    } else return fail(404, 'not_found');
    return [200, snapshot(table, time)];
  }
  return {
    'GET /api/chess/table': ctx => {
      const raw = ctx.url.searchParams.get('tableId') ?? '';
      if (!/^\d+$/.test(raw)) return fail(400, 'invalid_table');
      return dispatch('table', ctx, { sessionId: ctx.url.searchParams.get('sessionId'), tableId: Number(raw) });
    },
    ...Object.fromEntries(['join', 'move', 'leave', 'reset', 'resign', 'draw'].map(action => [
      'POST /api/chess/' + action, (ctx, body) => dispatch(action, ctx, body),
    ])),
  };
}

function distance(member, table) { return Math.hypot(member.x - table.x, member.z - table.z); }
