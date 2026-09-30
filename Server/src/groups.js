import { randomInt } from 'node:crypto';
import { nicknameKey } from './nickname.js';
import { WalletError } from './wallet.js';

// Guruhlar (Telegram'dagidek): o'yinchi guruh yaratadi va ko'p guruhga a'zo bo'ladi.
// Turlar: free - hamma qo'shiladi (qidiruvda ko'rinadi); private - qidiruvda yo'q, kod/havola orqali so'rov yuboriladi
// va admin tasdiqlaydi; paid - CDCoin obunasi. To'lov to'liq egaga o'tadi (komissiya 0%).
// Hisoblar va a'zolik bitta tranzaksiyada; obuna avtomatik yangilanmaydi.

export const GROUP_LIMITS = {
  owned: 50, // bitta o'yinchi yaratishi mumkin bo'lgan guruhlar
  memberships: 200, // bitta o'yinchi a'zo bo'lishi mumkin bo'lgan guruhlar (o'zinikilari bilan)
  members: 500, // guruhdagi a'zolar
  admins: 20,
  pendingPerPlayer: 50, // o'yinchining javob kutayotgan qo'shilish so'rovlari
  searchResults: 20,
};
export const GROUP_KINDS = ['free', 'private', 'paid'];
export const CURRENCIES = ['CDCoin'];
export const PERIODS = { week: 7, month: 30, year: 365 };
export const NAME_MIN = 3, NAME_MAX = 40, DESCRIPTION_MAX = 300, PRICE_MAX = 1_000_000_000;

// Kod alifbosi: chalkash belgilarsiz (0/O, 1/I/L yo'q). Ko'rinishi: NW-XXXXXX, havola: newworld://group/XXXXXX
const CODE_ALPHABET = 'ABCDEFGHJKMNPQRSTUVWXYZ23456789';
const CODE_PATTERN = /^[ABCDEFGHJKMNPQRSTUVWXYZ23456789]{6}$/;
const CONTROL = /[\u0000-\u001f\u007f-\u009f]/;
const CONTROL_EXCEPT_NEWLINE = /[\u0000-\u0009\u000b-\u001f\u007f-\u009f]/;

/** Foydalanuvchi kiritgan kod yoki havoladan kod: "NW-AB12CD", "ab12cd", "newworld://group/AB12CD". Noto'g'ri - null. */
export function parseGroupCode(input) {
  if (typeof input !== 'string') return null;
  let text = input.trim();
  const link = /group\/([A-Za-z0-9-]+)\/?$/i.exec(text);
  if (link) text = link[1];
  text = text.replace(/^NW[-\s]?/i, '').toUpperCase();
  return CODE_PATTERN.test(text) ? text : null;
}

/** Guruh nomi: bo'shliqlar bitta, 3..40 belgi, boshqaruv belgilarisiz. */
export function cleanGroupName(value) {
  if (typeof value !== 'string') return null;
  const name = value.replace(/\s+/g, ' ').trim();
  if (name.length < NAME_MIN || name.length > NAME_MAX || CONTROL.test(name)) return null;
  return name;
}

/**
 * Jadvallar va so'rovlar. `sql` - DatabaseSync, `cutoff(now)` - onlayn oynasi boshlanishi (ISO).
 * Barcha vaqtlar ISO UTC satr.
 */
export function createGroupStore(sql, cutoff) {
  sql.exec(`
    CREATE TABLE IF NOT EXISTS social_groups (
      id           INTEGER PRIMARY KEY AUTOINCREMENT,
      code         TEXT NOT NULL UNIQUE,
      name         TEXT NOT NULL,
      name_key     TEXT NOT NULL,
      description  TEXT NOT NULL DEFAULT '',
      owner_id     TEXT NOT NULL REFERENCES players (id),
      kind         TEXT NOT NULL CHECK (kind IN ('free', 'private', 'paid')),
      price        INTEGER NOT NULL DEFAULT 0,
      currency     TEXT NOT NULL DEFAULT 'UZS',
      period       TEXT NOT NULL DEFAULT '',
      created_at   TEXT NOT NULL
    );
    CREATE INDEX IF NOT EXISTS social_groups_name_key ON social_groups (name_key);
    CREATE INDEX IF NOT EXISTS social_groups_owner ON social_groups (owner_id);
    CREATE TABLE IF NOT EXISTS group_members (
      group_id    INTEGER NOT NULL REFERENCES social_groups (id) ON DELETE CASCADE,
      player_id   TEXT NOT NULL REFERENCES players (id),
      role        TEXT NOT NULL CHECK (role IN ('owner', 'admin', 'member')),
      joined_at   TEXT NOT NULL,
      expires_at  TEXT,
      PRIMARY KEY (group_id, player_id)
    );
    CREATE INDEX IF NOT EXISTS group_members_player ON group_members (player_id);
    CREATE INDEX IF NOT EXISTS group_members_expires ON group_members (expires_at) WHERE expires_at IS NOT NULL;
    CREATE TABLE IF NOT EXISTS group_requests (
      group_id    INTEGER NOT NULL REFERENCES social_groups (id) ON DELETE CASCADE,
      player_id   TEXT NOT NULL REFERENCES players (id),
      created_at  TEXT NOT NULL,
      PRIMARY KEY (group_id, player_id)
    );
    CREATE INDEX IF NOT EXISTS group_requests_player ON group_requests (player_id);
  `);

  const ONLINE = "COALESCE(p.show_online = 1 AND p.last_seen >= :cutoff, 0)";
  const GROUP_COLUMNS = `
    g.id, g.owner_id AS ownerId, g.code, g.name, g.description, g.kind, g.price, g.currency, g.period, g.created_at AS createdAt,
    (SELECT nickname FROM players WHERE id = g.owner_id) AS owner,
    (SELECT COUNT(*) FROM group_members WHERE group_id = g.id) AS memberCount,
    (SELECT COUNT(*) FROM group_members gm JOIN players p ON p.id = gm.player_id
       WHERE gm.group_id = g.id AND ${ONLINE}) AS onlineCount,
    (SELECT role FROM group_members WHERE group_id = g.id AND player_id = :me) AS role,
    (SELECT expires_at FROM group_members WHERE group_id = g.id AND player_id = :me) AS expiresAt,
    EXISTS (SELECT 1 FROM group_requests WHERE group_id = g.id AND player_id = :me) AS requested,
    (SELECT COUNT(*) FROM group_requests WHERE group_id = g.id) AS pending`;

  const purge = sql.prepare('DELETE FROM group_members WHERE expires_at IS NOT NULL AND expires_at <= ?');
  const byCode = sql.prepare(`SELECT ${GROUP_COLUMNS} FROM social_groups g WHERE g.code = :code`);
  const mine = sql.prepare(`
    SELECT ${GROUP_COLUMNS} FROM group_members m JOIN social_groups g ON g.id = m.group_id
    WHERE m.player_id = :me
    ORDER BY CASE m.role WHEN 'owner' THEN 0 WHEN 'admin' THEN 1 ELSE 2 END, g.name_key`);
  const myRequests = sql.prepare(`
    SELECT ${GROUP_COLUMNS} FROM group_requests r JOIN social_groups g ON g.id = r.group_id
    WHERE r.player_id = :me ORDER BY r.created_at DESC`);
  const searchByName = sql.prepare(`
    SELECT ${GROUP_COLUMNS} FROM social_groups g
    WHERE g.name_key >= :query AND g.name_key < :query || char(65535) AND instr(g.name_key, :query) = 1
      AND g.kind <> 'private'
    ORDER BY g.name_key LIMIT :limit`);
  const members = sql.prepare(`
    SELECT p.nickname, p.public_id AS publicId, p.avatar_id AS avatarId, p.gender, ${ONLINE} AS online,
           m.role, COALESCE(m.expires_at, '') AS expiresAt
    FROM group_members m JOIN players p ON p.id = m.player_id
    WHERE m.group_id = :group
    ORDER BY CASE m.role WHEN 'owner' THEN 0 WHEN 'admin' THEN 1 ELSE 2 END, online DESC, p.nickname_key`);
  const requests = sql.prepare(`
    SELECT p.nickname, p.public_id AS publicId, p.avatar_id AS avatarId, p.gender, ${ONLINE} AS online,
           r.created_at AS requestedAt
    FROM group_requests r JOIN players p ON p.id = r.player_id
    WHERE r.group_id = :group ORDER BY r.created_at, p.nickname_key`);
  const insertGroup = sql.prepare(`
    INSERT INTO social_groups (code, name, name_key, description, owner_id, kind, price, currency, period, created_at)
    VALUES (:code, :name, :nameKey, :description, :owner, :kind, :price, :currency, :period, :createdAt)`);
  const codeTaken = sql.prepare('SELECT 1 FROM social_groups WHERE code = ?');
  const insertMember = sql.prepare(`
    INSERT INTO group_members (group_id, player_id, role, joined_at, expires_at) VALUES (?, ?, ?, ?, ?)
    ON CONFLICT (group_id, player_id) DO UPDATE SET expires_at = excluded.expires_at`);
  const memberRole = sql.prepare('SELECT role FROM group_members WHERE group_id = ? AND player_id = ?');
  const countMembers = sql.prepare('SELECT COUNT(*) AS n FROM group_members WHERE group_id = ?');
  const countAdmins = sql.prepare("SELECT COUNT(*) AS n FROM group_members WHERE group_id = ? AND role = 'admin'");
  const countOwned = sql.prepare('SELECT COUNT(*) AS n FROM social_groups WHERE owner_id = ?');
  const countMemberships = sql.prepare('SELECT COUNT(*) AS n FROM group_members WHERE player_id = ?');
  const countPending = sql.prepare('SELECT COUNT(*) AS n FROM group_requests WHERE player_id = ?');
  const hasRequest = sql.prepare('SELECT 1 FROM group_requests WHERE group_id = ? AND player_id = ?');
  const insertRequest = sql.prepare('INSERT OR IGNORE INTO group_requests (group_id, player_id, created_at) VALUES (?, ?, ?)');
  const deleteRequest = sql.prepare('DELETE FROM group_requests WHERE group_id = ? AND player_id = ?');
  const deleteMember = sql.prepare('DELETE FROM group_members WHERE group_id = ? AND player_id = ?');
  const setRole = sql.prepare('UPDATE group_members SET role = ? WHERE group_id = ? AND player_id = ?');
  const deleteGroup = sql.prepare('DELETE FROM social_groups WHERE id = ?');
  const editGroup = sql.prepare('UPDATE social_groups SET name=?, name_key=?, description=?, price=?, currency=?, period=? WHERE id=? AND owner_id=?');
  const changeOwner = sql.prepare('UPDATE social_groups SET owner_id=? WHERE id=? AND owner_id=?');
  const ownerRole = sql.prepare('UPDATE group_members SET role=?, expires_at=NULL WHERE group_id=? AND player_id=?');
  const shared = sql.prepare(`
    SELECT 1 FROM group_members a JOIN group_members b ON b.group_id = a.group_id
    WHERE a.player_id = ? AND b.player_id = ?
      AND (a.expires_at IS NULL OR a.expires_at > ?)
      AND (b.expires_at IS NULL OR b.expires_at > ?) LIMIT 1`);

  function freeCode() {
    for (let attempt = 0; attempt < 100; attempt++) {
      let code = '';
      for (let i = 0; i < 6; i++) code += CODE_ALPHABET[randomInt(CODE_ALPHABET.length)];
      if (codeTaken.get(code) === undefined) return code;
    }
    throw new Error("Bo'sh guruh kodi topilmadi");
  }

  return {
    /** Obunasi tugagan a'zoliklarni o'chiradi (har so'rov boshida). */
    purge(now) { purge.run(now.toISOString()); },
    byCode(me, code, now) { return byCode.get({ me, code, cutoff: cutoff(now) }); },
    mine(me, now) { return mine.all({ me, cutoff: cutoff(now) }); },
    myRequests(me, now) { return myRequests.all({ me, cutoff: cutoff(now) }); },
    searchByName(me, query, now) {
      return searchByName.all({ me, query, cutoff: cutoff(now), limit: GROUP_LIMITS.searchResults });
    },
    members(group, now) { return members.all({ group, cutoff: cutoff(now) }); },
    requests(group, now) { return requests.all({ group, cutoff: cutoff(now) }); },
    /** Yangi guruh va uning egasi (bitta tranzaksiyada). @returns kod */
    create(owner, fields, now) {
      const code = freeCode();
      sql.exec('BEGIN');
      try {
        const { lastInsertRowid } = insertGroup.run({ ...fields, code, owner, createdAt: now.toISOString() });
        insertMember.run(Number(lastInsertRowid), owner, 'owner', now.toISOString(), null);
        sql.exec('COMMIT');
      } catch (err) {
        sql.exec('ROLLBACK');
        throw err;
      }
      return code;
    },
    role(group, player) { return memberRole.get(group, player)?.role ?? null; },
    memberCount(group) { return countMembers.get(group).n; },
    adminCount(group) { return countAdmins.get(group).n; },
    ownedCount(player) { return countOwned.get(player).n; },
    membershipCount(player) { return countMemberships.get(player).n; },
    pendingCount(player) { return countPending.get(player).n; },
    hasRequest(group, player) { return hasRequest.get(group, player) !== undefined; },
    addRequest(group, player, now) { insertRequest.run(group, player, now.toISOString()); },
    removeRequest(group, player) { return deleteRequest.run(group, player).changes > 0; },
    addMember(group, player, role, now, expiresAt = null) {
      insertMember.run(group, player, role, now.toISOString(), expiresAt);
    },
    removeMember(group, player) { return deleteMember.run(group, player).changes > 0; },
    setRole(group, player, role) { setRole.run(role, group, player); },
    deleteGroup(group) { deleteGroup.run(group); },
    edit(group, owner, fields) { return editGroup.run(fields.name,fields.name.toLowerCase(),fields.description,fields.price,fields.currency,fields.period,group,owner).changes>0; },
    transfer(group, from, to) {
      sql.exec('BEGIN IMMEDIATE');
      try {
        if (!changeOwner.run(to,group,from).changes) throw new WalletError('owner_only');
        if(!ownerRole.run('owner',group,to).changes)throw new WalletError('not_member');
        ownerRole.run('admin',group,from);
        sql.exec('COMMIT');
      } catch(error) {sql.exec('ROLLBACK');throw error;}
    },
    /** Ikki o'yinchi kamida bitta umumiy guruhda (lobbyga taklif uchun). */
    shareGroup(a, b, now) { const instant=now.toISOString();return shared.get(a, b, instant, instant) !== undefined; },
  };
}

/**
 * Guruh route'lari: { 'METOD /yo'l': run(ctx, body) } - app.js ularni auth va cheklov bilan ro'yxatga oladi.
 * `db` - openDatabase() natijasi (db.groups, db.getPlayer, db.findPlayerByNicknameKey, db.friendship).
 *
 * GET  /api/groups/mine               -> { groups: [group], requests: [group] } (mening guruhlarim, yuborgan so'rovlarim)
 * GET  /api/groups/search?q=<matn>    -> [group] (kod aniq - har qanday tur; nom boshi >= 2 belgi - faqat free/paid)
 * GET  /api/groups/detail?code=<kod>  -> detail (a'zolar faqat a'zoga, so'rovlar faqat admin/egaga)
 * POST /api/groups/create { name, description?, kind, price?, currency?, period? } -> 201 detail
 * POST /api/groups/join    { code, expectedPrice?, paymentKey? } -> { status, group: detail }
 * POST /api/groups/cancel  { code }                -> detail (o'z so'rovini bekor qilish)
 * POST /api/groups/leave   { code }                -> { ok } (egasi chiqa olmaydi: owner_cannot_leave)
 * POST /api/groups/approve { code, nickname }      -> detail (admin/ega; faqat bepul/yopiq guruhlar)
 * POST /api/groups/reject  { code, nickname }      -> detail
 * POST /api/groups/remove  { code, nickname }      -> detail (admin oddiy a'zoni, ega adminni ham)
 * POST /api/groups/role    { code, nickname, role: 'admin' | 'member' } -> detail (faqat ega)
 * POST /api/groups/delete  { code }                -> { ok } (faqat ega)
 *
 * group  = { code, name, description, kind, price, currency, period, owner, memberCount, onlineCount,
 *            role: 'owner'|'admin'|'member'|'', requested, pending, expiresAt, createdAt }
 * detail = group + { members: [member], requests: [member] }
 * member = { nickname, publicId, avatarId, gender, online, role, expiresAt, friendship }
 */
export function groupRoutes(db) {
  const store = db.groups;
  const fail = (status, error) => [status, { error }];

  function view(row) {
    return {
      code: row.code, name: row.name, description: row.description, kind: row.kind, price: row.price,
      currency: row.currency, period: row.period, owner: row.owner ?? '', memberCount: row.memberCount,
      onlineCount: row.onlineCount, role: row.role ?? '', requested: row.requested === 1,
      // So'rovlar soni faqat ularni ko'ra oladiganlarga
      pending: row.role === 'owner' || row.role === 'admin' ? row.pending : 0,
      expiresAt: row.expiresAt ?? '', createdAt: row.createdAt,
    };
  }
  function detail(me, code, now) {
    const row = store.byCode(me, code, now);
    if (!row) return null;
    const result = { ...view(row), members: [], requests: [] };
    if (row.role) {
      result.members = store.members(row.id, now).map(m => ({
        nickname: m.nickname, publicId: m.publicId, avatarId: m.avatarId, gender: m.gender, online: m.online === 1,
        role: m.role, expiresAt: m.expiresAt,
        friendship: m.nickname === db.getPlayer(me)?.nickname ? 'self' : relation(me, m.nickname),
      }));
    }
    if (row.role === 'owner' || row.role === 'admin') {
      result.requests = store.requests(row.id, now).map(r => ({
        nickname: r.nickname, publicId: r.publicId, avatarId: r.avatarId, gender: r.gender, online: r.online === 1,
        role: '', expiresAt: '', friendship: relation(me, r.nickname),
      }));
    }
    return result;
  }
  function relation(me, nickname) {
    const other = db.findPlayerByNicknameKey(nicknameKey(nickname));
    return other ? db.friendship(me, other.id) : 'none';
  }
  /** Tanadagi kod bo'yicha guruh qatori va chaqiruvchining roli; xato bo'lsa { error }. */
  function target(me, body, now) {
    if (!body) return { error: fail(400, 'bad_json') };
    const code = parseGroupCode(body.code);
    if (!code) return { error: fail(400, 'invalid_code') };
    const row = store.byCode(me, code, now);
    if (!row) return { error: fail(404, 'group_not_found') };
    return { row, code };
  }
  function playerIn(body) {
    if (typeof body.nickname !== 'string') return null;
    return db.findPlayerByNicknameKey(nicknameKey(body.nickname.trim())) ?? null;
  }
  const isAdmin = role => role === 'owner' || role === 'admin';

  function expiry(row, now) {
    if (row.kind !== 'paid') return null;
    return new Date(now.getTime() + (PERIODS[row.period] ?? 30) * 86_400_000).toISOString();
  }

  return {
    'GET /api/groups/mine': ({ player, now }) => {
      store.purge(now);
      return [200, { groups: store.mine(player.id, now).map(view), requests: store.myRequests(player.id, now).map(view) }];
    },
    'GET /api/groups/search': ({ player, now, url }) => {
      store.purge(now);
      const query = (url.searchParams.get('q') ?? '').trim();
      if (query.length < 1 || query.length > 64) return fail(400, 'invalid_query');
      const code = parseGroupCode(query);
      if (code) {
        const row = store.byCode(player.id, code, now);
        if (row) return [200, [view(row)]];
      }
      const key = query.replace(/\s+/g, ' ').toLowerCase();
      if (key.length < 2) return fail(400, 'query_too_short');
      return [200, store.searchByName(player.id, key, now).map(view)];
    },
    'GET /api/groups/detail': ({ player, now, url }) => {
      store.purge(now);
      const code = parseGroupCode(url.searchParams.get('code') ?? '');
      if (!code) return fail(400, 'invalid_code');
      const result = detail(player.id, code, now);
      return result ? [200, result] : fail(404, 'group_not_found');
    },
    'POST /api/groups/create': ({ player, now }, body) => {
      if (!body) return fail(400, 'bad_json');
      const name = cleanGroupName(body.name);
      if (!name) return fail(400, 'invalid_name');
      const description = body.description === undefined ? '' : body.description;
      if (typeof description !== 'string' || description.trim().length > DESCRIPTION_MAX
          || CONTROL_EXCEPT_NEWLINE.test(description)) return fail(400, 'invalid_description');
      if (!GROUP_KINDS.includes(body.kind)) return fail(400, 'invalid_kind');
      const fields = { name, nameKey: name.toLowerCase(), description: description.trim(), kind: body.kind,
        price: 0, currency: 'CDCoin', period: '' };
      if (body.kind === 'paid') {
        if (!Number.isInteger(body.price) || body.price < 1 || body.price > PRICE_MAX) return fail(400, 'invalid_price');
        if (!CURRENCIES.includes(body.currency)) return fail(400, 'invalid_currency');
        if (!Object.hasOwn(PERIODS, body.period)) return fail(400, 'invalid_period');
        Object.assign(fields, { price: body.price, currency: body.currency, period: body.period });
      }
      if (store.ownedCount(player.id) >= GROUP_LIMITS.owned) return fail(403, 'too_many_groups');
      if (store.membershipCount(player.id) >= GROUP_LIMITS.memberships) return fail(403, 'too_many_memberships');
      const code = store.create(player.id, fields, now);
      return [201, detail(player.id, code, now)];
    },
    'POST /api/groups/join': ({ player, now }, body) => {
      store.purge(now);
      const { row, code, error } = target(player.id, body, now);
      if (error) return error;
      if (row.role) return [200, { status: 'member', group: detail(player.id, code, now) }];
      if (row.kind === 'paid') {
        if (row.currency !== 'CDCoin') return fail(409, 'legacy_currency');
        if (body.expectedPrice !== row.price) return fail(409, 'price_changed');
        if (row.memberCount >= GROUP_LIMITS.members) return fail(409, 'group_full');
        if (store.membershipCount(player.id) >= GROUP_LIMITS.memberships) return fail(403, 'too_many_memberships');
        try {
          const purchased = db.wallet.purchaseGroup(player.id, row.ownerId, code, row.price, body.paymentKey, now, () => {
            const current=store.byCode(player.id,code,now);
            if(!current||current.ownerId!==row.ownerId||current.price!==row.price||current.currency!==row.currency)throw new WalletError('price_changed');
            // Recheck under the wallet's write lock if more than one server opens this database.
            if (store.role(row.id, player.id)) throw new WalletError('already_member');
            if (store.memberCount(row.id) >= GROUP_LIMITS.members) throw new WalletError('group_full');
            if (store.membershipCount(player.id) >= GROUP_LIMITS.memberships) throw new WalletError('too_many_memberships');
            store.addMember(row.id, player.id, 'member', now, expiry(row, now));
            store.removeRequest(row.id, player.id);
          });
          return [200, { status: purchased ? 'joined' : 'already_processed', group: detail(player.id, code, now) }];
        } catch (err) {
          if (err instanceof WalletError) return fail(409, err.message);
          throw err;
        }
      }
      if (row.kind === 'free') {
        if (row.memberCount >= GROUP_LIMITS.members) return fail(409, 'group_full');
        if (store.membershipCount(player.id) >= GROUP_LIMITS.memberships) return fail(403, 'too_many_memberships');
        store.addMember(row.id, player.id, 'member', now);
        store.removeRequest(row.id, player.id);
        return [200, { status: 'joined', group: detail(player.id, code, now) }];
      }
      if (!store.hasRequest(row.id, player.id)) {
        if (store.pendingCount(player.id) >= GROUP_LIMITS.pendingPerPlayer) return fail(403, 'too_many_pending');
        store.addRequest(row.id, player.id, now);
      }
      return [200, { status: 'requested', group: detail(player.id, code, now) }];
    },
    'POST /api/groups/edit': ({ player, now }, body) => {
      const {row,code,error}=target(player.id,body,now);if(error)return error;
      if(row.role!=='owner')return fail(403,'owner_only');
      const name=cleanGroupName(body.name);
      if(!name)return fail(400,'invalid_name');
      if(typeof body.description!=='string'||body.description.trim().length>DESCRIPTION_MAX||CONTROL_EXCEPT_NEWLINE.test(body.description))return fail(400,'invalid_description');
      if(body.kind!==row.kind)return fail(400,'invalid_kind');
      const fields={name,description:body.description.trim(),price:0,currency:'CDCoin',period:''};
      if(row.kind==='paid') {
        if(!Number.isInteger(body.price)||body.price<1||body.price>PRICE_MAX)return fail(400,'invalid_price');
        if(body.currency!=='CDCoin')return fail(400,'invalid_currency');
        if(!Object.hasOwn(PERIODS,body.period))return fail(400,'invalid_period');
        Object.assign(fields,{price:body.price,period:body.period});
      }
      if(!store.edit(row.id,player.id,fields))return fail(403,'owner_only');return [200,detail(player.id,code,now)];
    },
    'POST /api/groups/transfer': ({ player, now }, body) => {
      store.purge(now);
      const {row,code,error}=target(player.id,body,now);if(error)return error;
      if(row.role!=='owner')return fail(403,'owner_only');
      const other=playerIn(body);
      if(!other||other.id===player.id||!store.role(row.id,other.id))return fail(400,'not_member');
      if(store.ownedCount(other.id)>=GROUP_LIMITS.owned)return fail(403,'too_many_groups');
      if(store.adminCount(row.id)-(store.role(row.id,other.id)==='admin'?1:0)>=GROUP_LIMITS.admins)return fail(409,'too_many_admins');
      try {store.transfer(row.id,player.id,other.id);return [200,detail(player.id,code,now)];}
      catch(err){if(err instanceof WalletError)return fail(409,err.message);throw err;}
    },
    'POST /api/groups/cancel': ({ player, now }, body) => {
      const { row, code, error } = target(player.id, body, now);
      if (error) return error;
      store.removeRequest(row.id, player.id);
      return [200, detail(player.id, code, now)];
    },
    'POST /api/groups/leave': ({ player, now }, body) => {
      const { row, error } = target(player.id, body, now);
      if (error) return error;
      if (row.role === 'owner') return fail(400, 'owner_cannot_leave');
      if (!row.role) return fail(404, 'not_member');
      store.removeMember(row.id, player.id);
      return [200, { ok: true }];
    },
    'POST /api/groups/approve': ({ player, now }, body) => {
      store.purge(now);
      const { row, code, error } = target(player.id, body, now);
      if (error) return error;
      if (!isAdmin(row.role)) return fail(403, 'admin_only');
      if (row.kind === 'paid') return fail(409, 'payment_required');
      const other = playerIn(body);
      if (!other || !store.hasRequest(row.id, other.id)) return fail(404, 'no_request');
      if (!store.role(row.id, other.id)) {
        if (row.memberCount >= GROUP_LIMITS.members) return fail(409, 'group_full');
        if (store.membershipCount(other.id) >= GROUP_LIMITS.memberships) return fail(409, 'too_many_memberships');
      }
      store.addMember(row.id, other.id, store.role(row.id, other.id) ?? 'member', now, expiry(row, now));
      store.removeRequest(row.id, other.id);
      return [200, detail(player.id, code, now)];
    },
    'POST /api/groups/reject': ({ player, now }, body) => {
      const { row, code, error } = target(player.id, body, now);
      if (error) return error;
      if (!isAdmin(row.role)) return fail(403, 'admin_only');
      const other = playerIn(body);
      if (!other || !store.removeRequest(row.id, other.id)) return fail(404, 'no_request');
      return [200, detail(player.id, code, now)];
    },
    'POST /api/groups/remove': ({ player, now }, body) => {
      const { row, code, error } = target(player.id, body, now);
      if (error) return error;
      if (!isAdmin(row.role)) return fail(403, 'admin_only');
      const other = playerIn(body);
      const role = other && store.role(row.id, other.id);
      if (!role) return fail(404, 'not_member');
      if (other.id === player.id || role === 'owner' || (role === 'admin' && row.role !== 'owner')) {
        return fail(403, 'cannot_remove');
      }
      store.removeMember(row.id, other.id);
      return [200, detail(player.id, code, now)];
    },
    'POST /api/groups/role': ({ player, now }, body) => {
      const { row, code, error } = target(player.id, body, now);
      if (error) return error;
      if (row.role !== 'owner') return fail(403, 'owner_only');
      if (body.role !== 'admin' && body.role !== 'member') return fail(400, 'invalid_role');
      const other = playerIn(body);
      const role = other && store.role(row.id, other.id);
      if (!role) return fail(404, 'not_member');
      if (role === 'owner') return fail(403, 'cannot_change_owner');
      if (body.role === 'admin' && role !== 'admin' && store.adminCount(row.id) >= GROUP_LIMITS.admins) {
        return fail(409, 'too_many_admins');
      }
      store.setRole(row.id, other.id, body.role);
      return [200, detail(player.id, code, now)];
    },
    'POST /api/groups/delete': ({ player, now }, body) => {
      const { row, error } = target(player.id, body, now);
      if (error) return error;
      if (row.role !== 'owner') return fail(403, 'owner_only');
      store.deleteGroup(row.id);
      return [200, { ok: true }];
    },
  };
}
