import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { createHash, randomBytes } from 'node:crypto';
import { existsSync, mkdirSync, mkdtempSync, rmSync, utimesSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { DatabaseSync } from 'node:sqlite';
import { after, before, describe, test } from 'node:test';
import { fileURLToPath } from 'node:url';
import {
  defaultDbPath, legacyDbPaths, migrateLegacyDatabase, parseTrustProxy, resolveDbPath, serverConfig,
} from '../src/config.js';
import { openDatabase } from '../src/db.js';

const sha256 = text => createHash('sha256').update(text).digest('hex');
const SERVER_JS = fileURLToPath(new URL('../src/server.js', import.meta.url));

/**
 * Eski server kabi baza yaratadi (WAL rejimi, o'yinchi yozilgan) va tokenini qaytaradi.
 * checkpoint=false: server majburan to'xtatilgandek, ma'lumot faqat -wal faylida qoladi.
 */
function createLegacyDatabase(file, nickname, { checkpoint = true } = {}) {
  mkdirSync(join(file, '..'), { recursive: true });
  const token = randomBytes(32).toString('hex');
  const db = new DatabaseSync(file);
  db.exec('PRAGMA journal_mode = WAL; PRAGMA wal_autocheckpoint = 0;');
  db.exec(`CREATE TABLE players (id TEXT PRIMARY KEY, nickname TEXT NOT NULL, nickname_key TEXT NOT NULL UNIQUE,
    gender TEXT NOT NULL, token_hash TEXT NOT NULL, created_at TEXT NOT NULL, avatar_id TEXT NOT NULL DEFAULT '')`);
  db.prepare('INSERT INTO players VALUES (?, ?, ?, ?, ?, ?, ?)')
    .run(`id-${nickname}`, nickname, nickname.toLowerCase(), 'male', sha256(token), '2026-09-01T10:00:00.000Z', 'M2');
  if (checkpoint) db.close();
  return { token, db };
}

/** Bazadagi token egasi (openDatabase orqali: eski ustunlar ham qo'shiladi). */
function nicknameByToken(file, token) {
  const db = openDatabase(file);
  try {
    return db.findPlayerByTokenHash(sha256(token))?.nickname;
  } finally {
    db.close();
  }
}

describe('server sozlamalari', () => {
  test('bazaning standart joyi: foydalanuvchi papkasi, ishga tushirilgan papka emas', () => {
    assert.equal(defaultDbPath({ env: { LOCALAPPDATA: 'C:\\Users\\Ali\\AppData\\Local' }, platform: 'win32', home: 'C:\\Users\\Ali' }),
      'C:\\Users\\Ali\\AppData\\Local\\CraDev\\server\\cradev.db');
    assert.equal(defaultDbPath({ env: {}, platform: 'win32', home: 'C:\\Users\\Ali' }),
      'C:\\Users\\Ali\\AppData\\Local\\CraDev\\server\\cradev.db');
    assert.equal(defaultDbPath({ env: {}, platform: 'linux', home: '/home/ali' }), '/home/ali/.local/share/cradev/cradev.db');
    assert.equal(defaultDbPath({ env: { XDG_DATA_HOME: '/data' }, platform: 'linux', home: '/home/ali' }), '/data/cradev/cradev.db');
    assert.equal(defaultDbPath({ env: {}, platform: 'darwin', home: '/Users/ali' }), '/Users/ali/.local/share/cradev/cradev.db');
  });

  test('eski joylar: ishga tushirilgan papka, exe yoni va Server/data', () => {
    const build = resolve('games', 'build7');
    const exe = join(build, 'CraDevServer.exe');
    assert.deepEqual(legacyDbPaths({ cwd: build, execPath: exe, script: exe }), [join(build, 'data', 'cradev.db')]);
    const repo = resolve('repo');
    const node = resolve('bin', 'node');
    assert.deepEqual(legacyDbPaths({ cwd: repo, execPath: node, script: join(repo, 'Server', 'src', 'server.js') }),
      [join(repo, 'data', 'cradev.db'), join(resolve('bin'), 'data', 'cradev.db'), join(repo, 'Server', 'data', 'cradev.db')]);
  });

  test('HOST, PORT va TRUST_PROXY', () => {
    assert.deepEqual(serverConfig({}), { port: 8080, hosts: ['127.0.0.1', '::1'], trustProxy: 0 });
    assert.deepEqual(serverConfig({ HOST: ' 0.0.0.0 ', PORT: '9000', TRUST_PROXY: '1' }),
      { port: 9000, hosts: ['0.0.0.0'], trustProxy: 1 });
    assert.throws(() => serverConfig({ PORT: 'abc' }), /PORT/);
    for (const [value, expected] of [[undefined, 0], ['', 0], ['0', 0], ['1', 1], ['2', 2], ['true', 1], ['YES', 1],
      ['false', 0], ['maybe', 0], ['-1', 0], ['99', 10]]) {
      assert.equal(parseTrustProxy(value), expected, String(value));
    }
  });
});

describe('bazani eski joydan ko\'chirish', () => {
  let dir;
  before(() => { dir = mkdtempSync(join(tmpdir(), 'cradev-config-')); });
  after(() => rmSync(dir, { recursive: true, force: true }));

  test('standart joyda baza yo\'q: eski baza (WAL ichidagisi ham) ko\'chiriladi, eskisi o\'zgarmaydi', () => {
    const legacy = join(dir, 'a', 'data', 'cradev.db');
    const { token, db: stillOpen } = createLegacyDatabase(legacy, 'Veteran', { checkpoint: false });
    assert.ok(existsSync(`${legacy}-wal`));
    const target = join(dir, 'a-home', 'cradev.db');
    mkdirSync(join(target, '..'), { recursive: true });

    const result = migrateLegacyDatabase(target, [legacy, join(dir, 'missing', 'cradev.db')]);
    stillOpen.close();
    assert.deepEqual(result, { migratedFrom: resolve(legacy), otherLegacy: [] });
    assert.equal(nicknameByToken(target, token), 'Veteran');
    assert.equal(nicknameByToken(legacy, token), 'Veteran'); // zaxira joyida
    assert.ok(!existsSync(`${target}.migrating`));

    // Keyingi ishga tushishda standart baza bor: hech narsa ko'chirilmaydi
    createLegacyDatabase(join(dir, 'a2', 'data', 'cradev.db'), 'Newer');
    assert.deepEqual(migrateLegacyDatabase(target, [join(dir, 'a2', 'data', 'cradev.db')]), { migratedFrom: null, otherLegacy: [] });
    assert.equal(nicknameByToken(target, token), 'Veteran');
  });

  test('bir nechta eski baza: eng oxirgi o\'zgartirilgani olinadi, qolganlari aytiladi', () => {
    const older = join(dir, 'b1', 'data', 'cradev.db');
    const newer = join(dir, 'b2', 'data', 'cradev.db');
    createLegacyDatabase(older, 'OldOne');
    const { token } = createLegacyDatabase(newer, 'NewOne');
    const past = new Date(Date.now() - 3600_000);
    utimesSync(older, past, past);
    const target = join(dir, 'b-home', 'cradev.db');
    mkdirSync(join(target, '..'), { recursive: true });

    const result = migrateLegacyDatabase(target, [older, newer, older]);
    assert.deepEqual(result, { migratedFrom: resolve(newer), otherLegacy: [resolve(older)] });
    assert.equal(nicknameByToken(target, token), 'NewOne');
  });

  test('eski baza o\'qilmasa xato: bo\'sh baza yaratilmaydi', () => {
    const broken = join(dir, 'c', 'data', 'cradev.db');
    mkdirSync(join(broken, '..'), { recursive: true });
    writeFileSync(broken, 'bu SQLite fayli emas '.repeat(300));
    const target = join(dir, 'c-home', 'cradev.db');
    mkdirSync(join(target, '..'), { recursive: true });
    assert.throws(() => migrateLegacyDatabase(target, [broken]), /ko'chirib bo'lmadi/);
    assert.ok(!existsSync(target));
    assert.ok(!existsSync(`${target}.migrating`));
  });

  test('DB_PATH berilsa aynan u ishlatiladi va hech narsa ko\'chirilmaydi', () => {
    const legacy = join(dir, 'd', 'data', 'cradev.db');
    createLegacyDatabase(legacy, 'Ignored');
    const custom = join(dir, 'd-ci', 'check.db');
    assert.deepEqual(resolveDbPath({ env: { DB_PATH: custom }, legacy: [legacy] }),
      { path: custom, fromEnv: true, migratedFrom: null, otherLegacy: [] });
    assert.equal(resolveDbPath({ env: { DB_PATH: ':memory:' }, legacy: [legacy] }).path, ':memory:');
    assert.ok(!existsSync(custom));

    // DB_PATH yo'q: standart joy (XDG_DATA_HOME) va ko'chirish
    const env = { XDG_DATA_HOME: join(dir, 'd-xdg'), LOCALAPPDATA: join(dir, 'd-local') };
    const result = resolveDbPath({ env, legacy: [legacy] });
    assert.equal(result.fromEnv, false);
    assert.equal(result.path, defaultDbPath({ env }));
    assert.equal(result.migratedFrom, resolve(legacy));
    assert.ok(existsSync(result.path));
  });
});

describe('server.js ishga tushishi', () => {
  let dir;
  before(() => { dir = mkdtempSync(join(tmpdir(), 'cradev-start-')); });
  after(() => rmSync(dir, { recursive: true, force: true }));

  /** server.js ni `cwd` papkasidan ishga tushiradi; tayyor bo'lgach { base, output, stop } qaytaradi. */
  function start(cwd, env) {
    return new Promise((resolvePromise, reject) => {
      const child = spawn(process.execPath, ['--disable-warning=ExperimentalWarning', SERVER_JS], {
        cwd,
        // DB_PATH bo'sh: standart joy va eski bazani ko'chirish tekshiriladi
        env: { ...process.env, DB_PATH: '', TRUST_PROXY: '', HOST: '127.0.0.1', PORT: '0', ...env },
        stdio: ['ignore', 'pipe', 'pipe'],
      });
      let output = '';
      const timer = setTimeout(() => { child.kill(); reject(new Error(`server ishga tushmadi:\n${output}`)); }, 10_000);
      const collect = chunk => {
        output += chunk;
        const match = /ishga tushdi: (http:\/\/127\.0\.0\.1:\d+)/.exec(output);
        if (match) {
          clearTimeout(timer);
          resolvePromise({
            base: match[1],
            output: () => output,
            stop: () => new Promise(done => { child.on('exit', done); child.kill('SIGTERM'); }),
          });
        }
      };
      child.stdout.on('data', collect);
      child.stderr.on('data', collect);
      child.on('exit', code => { clearTimeout(timer); reject(new Error(`server to'xtadi (${code}):\n${output}`)); });
    });
  }

  test('boshqa papkadan ishga tushsa ham o\'sha baza; eski data/cradev.db bir marta ko\'chiriladi', async () => {
    const oldFolder = join(dir, 'build-1');
    const legacy = join(oldFolder, 'data', 'cradev.db');
    const { token } = createLegacyDatabase(legacy, 'Survivor');
    // Server/data/cradev.db (dev server bazasi) ham nomzod: sinov bazasi har doim eng yangisi bo'lsin
    const future = new Date(Date.now() + 86_400_000);
    utimesSync(legacy, future, future);
    const env = { XDG_DATA_HOME: join(dir, 'home-data'), LOCALAPPDATA: join(dir, 'home-data') };
    const expected = defaultDbPath({ env });

    const first = await start(oldFolder, env);
    try {
      assert.match(first.output(), new RegExp(`Baza: ${expected.replace(/[\\.]/g, '\\$&')}`));
      assert.match(first.output(), /Eski baza ko'chirildi/);
      const me = await fetch(`${first.base}/api/players/me`, { headers: { Authorization: `Bearer ${token}` } });
      assert.equal(me.status, 200);
      assert.equal((await me.json()).nickname, 'Survivor');
    } finally {
      await first.stop();
    }

    // Yangi build papkasi (eski bazasiz): profil yo'qolmaydi. HOST berilmagan: 127.0.0.1 (va bo'lsa ::1)
    const newFolder = join(dir, 'build-2');
    mkdirSync(newFolder);
    const second = await start(newFolder, { ...env, HOST: '' });
    try {
      assert.doesNotMatch(second.output(), /ko'chirildi/);
      const me = await fetch(`${second.base}/api/players/me`, { headers: { Authorization: `Bearer ${token}` } });
      assert.equal(me.status, 200);
    } finally {
      await second.stop();
    }
    assert.ok(!existsSync(join(newFolder, 'data')));
  });
});
