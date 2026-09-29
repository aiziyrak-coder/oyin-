import { mkdirSync } from 'node:fs';
import { createServer } from 'node:http';
import { dirname } from 'node:path';
import { createApp } from './app.js';
import { resolveDbPath, serverConfig } from './config.js';
import { openDatabase } from './db.js';

// Sozlamalar (PORT, HOST, DB_PATH, TRUST_PROXY): config.js
let config, db, dbInfo;
try {
  config = serverConfig();
  dbInfo = resolveDbPath();
  if (dbInfo.path !== ':memory:') mkdirSync(dirname(dbInfo.path), { recursive: true });
  db = openDatabase(dbInfo.path);
} catch (err) {
  console.error(`CraDev server ishga tushmadi: ${err.message}`);
  console.error("Boshqa baza faylini DB_PATH bilan ko'rsatish mumkin.");
  process.exit(1);
}

console.log(`Baza: ${dbInfo.path}${dbInfo.fromEnv ? ' (DB_PATH)' : ''}`);
if (dbInfo.migratedFrom) console.log(`Eski baza ko'chirildi: ${dbInfo.migratedFrom} (eski fayl zaxira sifatida qoldi)`);
for (const other of dbInfo.otherLegacy) console.log(`Boshqa eski baza ham bor (ko'chirilmadi): ${other}`);
if (config.trustProxy) console.log(`X-Forwarded-For'ga ishoniladi (proksilar: ${config.trustProxy})`);

const handler = createApp(db, { trustProxy: config.trustProxy });
const servers = [];

config.hosts.forEach((host, index) => {
  const server = createServer(handler);
  const primary = index === 0;
  server.on('error', err => {
    // IPv6 yo'q kompyuterda ::1 ochilmaydi: bu xato emas, 127.0.0.1 yetarli
    if (!primary && (err.code === 'EADDRNOTAVAIL' || err.code === 'EAFNOSUPPORT')) return;
    console.error(`CraDev server ${host}:${config.port} da ochilmadi: ${err.message}`);
    if (err.code === 'EADDRINUSE') console.error("Bu portda boshqa (yoki eski) server ishlayapti. Uni yoping yoki PORT'ni o'zgartiring.");
    process.exit(1);
  });
  server.listen(config.port, host, () => {
    servers.push(server);
    const shown = host.includes(':') ? `[${host}]` : host;
    console.log(`CraDev server ishga tushdi: http://${shown}:${server.address().port}`);
    if (primary && (host === '0.0.0.0' || host === '::')) {
      console.log("Server tarmoqdagi boshqa kompyuterlarga ochiq. Internetda HTTPS proksi ortida ishlating.");
    }
  });
});

for (const signal of ['SIGINT', 'SIGTERM']) {
  process.on(signal, () => {
    let open = servers.length;
    if (open === 0) {
      db.close();
      process.exit(0);
    }
    for (const server of servers) {
      server.close(() => {
        if (--open > 0) return;
        db.close();
        process.exit(0);
      });
    }
  });
}
