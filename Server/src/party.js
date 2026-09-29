import { randomUUID } from 'node:crypto';
import { nicknameKey } from './nickname.js';
import { publicOutfit } from './outfit.js';

const BASE64 = /^[A-Za-z0-9+/]+={0,2}$/;
const VOICE_KEEP_MS = 3000, VOICE_MAX_CHUNKS = 150, VOICE_MAX_BYTES = 12000;

// Lobby vaqtinchalik: profil va do'stlik bazasiga yozmaydi.
export function createParties(db) {
  // Lobbyga faqat do'st yoki umumiy guruhdagi a'zo chaqiriladi (xato kodi eski mijozlar uchun 'friends_only')
  const related = (a, b) => db.friendship(a, b) === 'friends' || (db.groups?.shareGroup(a, b) ?? false);
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
      room = { id: randomUUID(), host: id, voice: [], voiceSeq: 0, members: new Map([[id, { seat: 0, seen: time, ping: -1, mic: false, speaker: false }]]) };
      rooms.set(room.id, room); membership.set(id, room.id);
    }
    return room;
  }
  function snapshot(id, time) {
    const room = own(id, time);
    return { roomId: room.id, host: room.host === id, members: [...room.members].map(([key, m]) => {
      const p = db.getPlayer(key);
      return { nickname: p.nickname, avatarId: p.avatarId, gender: p.gender, outfit: publicOutfit(p.outfit),
        seat: m.seat, online: time - m.seen < 12000, pingMs: m.ping, micOn: m.mic, speakerOn: m.speaker };
    }), invitations: [...invites.values()].filter(i => i.target === id).map(i => ({
      id: i.id, nickname: db.getPlayer(i.host).nickname,
    })) };
  }
  // ---- Ovozli chat: har guruhda oxirgi VOICE_KEEP_MS ovoz bo'laklari xotirada (halqa bufer).
  // Bo'lak: 8-bit mu-law PCM, base64. Faqat guruh a'zolari yozadi/o'qiydi, o'zining ovozi qaytarilmaydi.
  function postVoice(player, body, now) {
    const time = +now;
    clean(time);
    if (!body || typeof body !== 'object') return [400, { error: 'bad_json' }];
    const room = rooms.get(membership.get(player.id));
    if (!room || room.members.size < 2) return [409, { error: 'no_party' }];
    const member = room.members.get(player.id);
    if (!member.speaker) return [409, { error: 'speaker_off' }];
    if (!Number.isInteger(body.rate) || body.rate < 8000 || body.rate > 48000) return [400, { error: 'invalid_rate' }];
    if (typeof body.data !== 'string' || !BASE64.test(body.data)) return [400, { error: 'invalid_audio' }];
    const bytes = Buffer.from(body.data, 'base64').length;
    // Bir bo'lak ko'pi bilan ~0.5 s (16 kHz da 8000 bayt)
    if (bytes < 1 || bytes > VOICE_MAX_BYTES || bytes > body.rate * 0.6) return [400, { error: 'invalid_audio' }];
    member.seen = time; member.mic = true;
    room.voice.push({ seq: ++room.voiceSeq, from: player.id, rate: body.rate, data: body.data, time });
    while (room.voice.length > VOICE_MAX_CHUNKS || (room.voice.length && time - room.voice[0].time > VOICE_KEEP_MS))
      room.voice.shift();
    return [200, { cursor: room.voiceSeq }];
  }
  function readVoice(player, since, now) {
    const time = +now;
    clean(time);
    const room = rooms.get(membership.get(player.id));
    if (!room || room.members.size < 2) return [200, { cursor: -1, chunks: [] }];
    const member = room.members.get(player.id);
    member.seen = time;
    // Birinchi so'rov (since < 0) yoki boshqa guruhdan qolgan kursor: faqat joriy kursor qaytadi
    if (!Number.isInteger(since) || since < 0 || since > room.voiceSeq) return [200, { cursor: room.voiceSeq, chunks: [] }];
    const chunks = [];
    for (const c of room.voice) {
      if (c.seq <= since || c.from === player.id || time - c.time > VOICE_KEEP_MS) continue;
      const other = room.members.get(c.from);
      if (!other || !other.mic) continue;
      chunks.push({ seq: c.seq, nickname: db.getPlayer(c.from).nickname, rate: c.rate, data: c.data });
    }
    return [200, { cursor: room.voiceSeq, chunks }];
  }
  action.postVoice = postVoice;
  action.readVoice = readVoice;
  return action;
  function action(kind, player, body, now) {
    const time = +now, id = player.id;
    clean(time);
    const fail = (status, error) => [status, { error }];
    if (!body || typeof body !== 'object') return fail(400, 'bad_json');
    if (kind === 'heartbeat') {
      if (!Number.isInteger(body.pingMs) || body.pingMs < -1 || body.pingMs > 10000) return fail(400, 'invalid_ping');
      const member = own(id, time).members.get(id);
      for (const key of ['micOn', 'speakerOn'])
        if (body[key] !== undefined && typeof body[key] !== 'boolean') return fail(400, 'invalid_voice_state');
      member.seen = time; member.ping = body.pingMs;
      // Mikrofon karnaysiz ishlamaydi (mijoz qoidasi serverda ham saqlanadi)
      if (typeof body.speakerOn === 'boolean') member.speaker = body.speakerOn;
      if (typeof body.micOn === 'boolean') member.mic = body.micOn && member.speaker;
    } else if (kind === 'invite') {
      const room = own(id, time);
      if (room.host !== id) return fail(403, 'host_only');
      const target = typeof body.nickname === 'string' && db.findPlayerByNicknameKey(nicknameKey(body.nickname.trim()));
      if (!target || !related(id, target.id)) return fail(403, 'friends_only');
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
        if (!related(invite.host, id)) return fail(403, 'friends_only');
        const previous = rooms.get(membership.get(id));
        if (previous && previous.members.size > 1) return fail(409, 'leave_first');
        leave(id);
        const seats = new Set([...room.members.values()].map(m => m.seat));
        let seat = 1; while (seats.has(seat)) seat++;
        room.members.set(id, { seat, seen: time, ping: -1, mic: false, speaker: false }); membership.set(id, room.id);
        for (const [key, other] of invites) if (other.target === id) invites.delete(key);
      }
      invites.delete(invite.id);
    } else if (kind === 'leave') leave(id);
    else return fail(404, 'not_found');
    return [200, snapshot(id, time)];
  }
}
