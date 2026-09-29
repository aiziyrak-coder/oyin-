import { randomUUID } from 'node:crypto';
import { nicknameKey } from './nickname.js';
import { publicOutfit } from './outfit.js';

// Lobby vaqtinchalik: profil va do'stlik bazasiga yozmaydi.
export function createParties(db) {
  const rooms = new Map(), membership = new Map(), invites = new Map();
  function leave(id) {
    const room = rooms.get(membership.get(id));
    if (!room) return;
    if (room.host === id) {
      for (const member of room.members.keys()) membership.delete(member);
      rooms.delete(room.id);
      for (const [key, invite] of invites) if (invite.room === room.id) invites.delete(key);
    } else { room.members.delete(id); membership.delete(id); }
  }
  function clean(time) {
    for (const [key, invite] of invites) if (invite.until <= time) invites.delete(key);
    for (const room of rooms.values())
      for (const [id, member] of room.members) if (time - member.seen > 45000) leave(id);
  }
  function own(id, time) {
    let room = rooms.get(membership.get(id));
    if (!room) {
      room = { id: randomUUID(), host: id, members: new Map([[id, { seat: 0, seen: time, ping: -1 }]]) };
      rooms.set(room.id, room); membership.set(id, room.id);
    }
    return room;
  }
  function snapshot(id, time) {
    const room = own(id, time);
    return { roomId: room.id, host: room.host === id, members: [...room.members].map(([key, m]) => {
      const p = db.getPlayer(key);
      return { nickname: p.nickname, avatarId: p.avatarId, gender: p.gender, outfit: publicOutfit(p.outfit),
        seat: m.seat, online: time - m.seen < 12000, pingMs: m.ping };
    }), invitations: [...invites.values()].filter(i => i.target === id).map(i => ({
      id: i.id, nickname: db.getPlayer(i.host).nickname,
    })) };
  }
  return function action(kind, player, body, now) {
    const time = +now, id = player.id;
    clean(time);
    const fail = (status, error) => [status, { error }];
    if (!body || typeof body !== 'object') return fail(400, 'bad_json');
    if (kind === 'heartbeat') {
      if (!Number.isInteger(body.pingMs) || body.pingMs < -1 || body.pingMs > 10000) return fail(400, 'invalid_ping');
      const member = own(id, time).members.get(id);
      member.seen = time; member.ping = body.pingMs;
    } else if (kind === 'invite') {
      const room = own(id, time);
      if (room.host !== id) return fail(403, 'host_only');
      const target = typeof body.nickname === 'string' && db.findPlayerByNicknameKey(nicknameKey(body.nickname.trim()));
      if (!target || db.friendship(id, target.id) !== 'friends') return fail(403, 'friends_only');
      if (room.members.has(target.id)) return fail(409, 'already_joined');
      const pending = [...invites.values()].filter(i => i.room === room.id);
      if (pending.some(i => i.target === target.id)) return [200, snapshot(id, time)];
      if (room.members.size + pending.length >= 5) return fail(409, 'party_full');
      if ([...invites.values()].filter(i => i.target === target.id).length >= 8) return fail(429, 'too_many_invites');
      const invite = { id: randomUUID(), room: room.id, host: id, target: target.id, until: time + 60000 };
      invites.set(invite.id, invite);
    } else if (kind === 'accept' || kind === 'decline') {
      const invite = invites.get(body.invitationId);
      if (!invite || invite.target !== id) return fail(404, 'no_invite');
      if (kind === 'accept') {
        const room = rooms.get(invite.room);
        if (!room || room.members.size >= 5) return fail(409, 'party_full');
        if (db.friendship(invite.host, id) !== 'friends') return fail(403, 'friends_only');
        const previous = rooms.get(membership.get(id));
        if (previous && previous.members.size > 1) return fail(409, 'leave_first');
        leave(id);
        const seats = new Set([...room.members.values()].map(m => m.seat));
        let seat = 1; while (seats.has(seat)) seat++;
        room.members.set(id, { seat, seen: time, ping: -1 }); membership.set(id, room.id);
        for (const [key, other] of invites) if (other.target === id) invites.delete(key);
      }
      invites.delete(invite.id);
    } else if (kind === 'leave') leave(id);
    else return fail(404, 'not_found');
    return [200, snapshot(id, time)];
  };
}
