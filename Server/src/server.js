import { mkdirSync } from 'node:fs';
import { createServer } from 'node:http';
import { dirname } from 'node:path';
import { createApp } from './app.js';
import { openDatabase } from './db.js';

const port = Number(process.env.PORT ?? 8080);
const dbPath = process.env.DB_PATH ?? 'data/cradev.db';

mkdirSync(dirname(dbPath), { recursive: true });
const db = openDatabase(dbPath);
const server = createServer(createApp(db));

server.listen(port, () => {
  console.log(`CraDev server ishga tushdi: http://localhost:${port}  (baza: ${dbPath})`);
});

for (const signal of ['SIGINT', 'SIGTERM']) {
  process.on(signal, () => {
    server.close(() => {
      db.close();
      process.exit(0);
    });
  });
}
