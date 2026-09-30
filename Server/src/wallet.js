// Server-owned integer ledger. No client/API operation can mint CDCoin.
export const COIN_UZS = 100;
export const COIN_PACKS = [100, 500, 1000, 5000].map(coins => ({ id: `cdc-${coins}`, coins, amountUzs: coins * COIN_UZS }));
const MAX_BALANCE = 1_000_000_000;
export class WalletError extends Error {}
const fail = code => { throw new WalletError(code); };

export function createWalletStore(sql) {
  sql.exec(`
    CREATE TABLE IF NOT EXISTS coin_accounts (
      player_id TEXT PRIMARY KEY REFERENCES players(id),
      balance INTEGER NOT NULL DEFAULT 0 CHECK(typeof(balance) = 'integer' AND balance BETWEEN 0 AND 1000000000)
    );
    CREATE TABLE IF NOT EXISTS coin_operations (
      id INTEGER PRIMARY KEY, key TEXT NOT NULL UNIQUE, fingerprint TEXT NOT NULL, created_at TEXT NOT NULL
    );
    CREATE TABLE IF NOT EXISTS coin_ledger (
      id INTEGER PRIMARY KEY, operation_id INTEGER NOT NULL REFERENCES coin_operations(id),
      player_id TEXT NOT NULL REFERENCES players(id), delta INTEGER NOT NULL CHECK(delta <> 0),
      balance INTEGER NOT NULL, kind TEXT NOT NULL, reference TEXT NOT NULL, created_at TEXT NOT NULL,
      UNIQUE(operation_id, player_id)
    );
    CREATE INDEX IF NOT EXISTS coin_ledger_player ON coin_ledger(player_id, id DESC);
  `);
  const ensure = sql.prepare('INSERT OR IGNORE INTO coin_accounts(player_id) VALUES (?)');
  const balance = sql.prepare('SELECT balance FROM coin_accounts WHERE player_id = ?');
  const operation = sql.prepare('SELECT id, fingerprint FROM coin_operations WHERE key = ?');
  const addOperation = sql.prepare('INSERT INTO coin_operations(key, fingerprint, created_at) VALUES (?, ?, ?)');
  const update = sql.prepare('UPDATE coin_accounts SET balance = ? WHERE player_id = ?');
  const entry = sql.prepare('INSERT INTO coin_ledger(operation_id, player_id, delta, balance, kind, reference, created_at) VALUES (?, ?, ?, ?, ?, ?, ?)');
  const history = sql.prepare('SELECT delta, balance, kind, reference, created_at AS createdAt FROM coin_ledger WHERE player_id = ? ORDER BY id DESC LIMIT 50');
  function transaction(key, fingerprint, now, apply) {
    sql.exec('BEGIN IMMEDIATE');
    try {
      const prior = operation.get(key);
      if (prior) {
        if (prior.fingerprint !== fingerprint) fail('idempotency_conflict');
        sql.exec('COMMIT');
        return false;
      }
      const id = Number(addOperation.run(key, fingerprint, now.toISOString()).lastInsertRowid);
      apply(id);
      sql.exec('COMMIT');
      return true;
    } catch (error) { sql.exec('ROLLBACK'); throw error; }
  }
  function change(id, player, delta, kind, reference, now) {
    ensure.run(player);
    const next = balance.get(player).balance + delta;
    if (next < 0) fail('insufficient_coins');
    if (!Number.isSafeInteger(next) || next > MAX_BALANCE) fail('balance_limit');
    update.run(next, player);
    entry.run(id, player, delta, next, kind, reference, now.toISOString());
  }
  function amount(coins) {
    if (!Number.isSafeInteger(coins) || coins < 1 || coins > MAX_BALANCE) fail('invalid_price');
  }
  return {
    balance(player) { return balance.get(player)?.balance ?? 0; },
    history(player) { return history.all(player); },
    // Internal only. A future authenticated provider adapter must verify amount, order ownership,
    // settlement and reversals before calling this; deliberately no public mint endpoint.
    creditVerified(player, receipt, coins, now) {
      amount(coins);
      if (typeof receipt !== 'string' || !/^[a-zA-Z0-9:_-]{8,160}$/.test(receipt)) fail('invalid_receipt');
      return transaction(`receipt:${receipt}`, JSON.stringify([player, coins]), now,
        id => change(id, player, coins, 'topup', receipt, now));
    },
    purchaseGroup(player, owner, code, coins, key, now, grant) {
      amount(coins);
      if (player === owner) fail('self');
      if (typeof key !== 'string' || !/^[a-zA-Z0-9_-]{16,80}$/.test(key)) fail('invalid_idempotency_key');
      return transaction(`group:${player}:${key}`, JSON.stringify([owner, code, coins]), now, id => {
        change(id, player, -coins, 'group_payment', code, now);
        change(id, owner, coins, 'group_income', code, now); // 0% platform commission
        grant(); // Membership and both balances commit together, or all roll back.
      });
    },
  };
}

export function walletRoutes(db) {
  return {
    'GET /api/wallet': ({ player }) => [200, { balance: db.wallet.balance(player.id), currency: 'CDCoin',
      coinUzs: COIN_UZS, packs: COIN_PACKS, checkoutAvailable: false, transactions: db.wallet.history(player.id) }],
    // Until merchant credentials + verified callbacks + reversal handling are implemented,
    // fail closed. Never accept a client-supplied balance, receipt, or payment-success flag.
    'POST /api/wallet/checkout': () => [503, { error: 'payments_not_configured' }],
  };
}
