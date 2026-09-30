import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createServer } from 'node:http';
import { openDatabase } from '../src/db.js';
import { createApp } from '../src/app.js';
import { COIN_PACKS } from '../src/wallet.js';

test('wallet: integer ledger, receipts, full transfer, rollback and private authenticated API', async () => {
  const db = openDatabase(':memory:');
  const now = new Date('2026-09-30T10:00:00Z');
  const server = createServer(createApp(db));
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const base = `http://127.0.0.1:${server.address().port}`;
  async function call(path, token, body) {
    const response = await fetch(base + path, { method: body ? 'POST' : 'GET',
      headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
      body: body ? JSON.stringify(body) : undefined });
    return { status: response.status, body: await response.json() };
  }
  try {
    const a = (await call('/api/players', null, { nickname: 'WalletAlice', gender: 'male', avatarId: 'M1' })).body;
    const b = (await call('/api/players', null, { nickname: 'WalletBobby', gender: 'male', avatarId: 'M1' })).body;
    assert.equal((await call('/api/wallet')).status, 401);
    assert.equal((await call('/api/wallet', a.token)).body.balance, 0);
    const group = (await call('/api/groups/create', b.token, {
      name: 'Wallet paid group', kind: 'paid', price: 50, currency: 'CDCoin', period: 'month',
    })).body;
    assert.equal((await call('/api/groups/join', a.token, { code: group.code, expectedPrice: 1, paymentKey: 'wrong-price-00001' })).body.error, 'price_changed');
    assert.equal((await call('/api/groups/join', a.token, { code: group.code, expectedPrice: 50 })).body.error, 'invalid_idempotency_key');
    const legacy = db.groups.create(b.id, { name: 'Legacy group', nameKey: 'legacy group', description: '', kind: 'paid', price: 5000, currency: 'UZS', period: 'month' }, now);
    assert.equal((await call('/api/groups/join', a.token, { code: legacy, expectedPrice: 5000, paymentKey: 'legacy-test-00001' })).body.error, 'legacy_currency');
    for (const value of [-1, 0, 1.5, NaN, Infinity, 1000000001])
      assert.throws(() => db.wallet.creditVerified(a.id, 'test:bad:receipt', value, now), /invalid_price/);
    assert.equal(db.wallet.creditVerified(a.id, 'test:receipt:unique', 1000, now), true);
    assert.equal(db.wallet.creditVerified(a.id, 'test:receipt:unique', 1000, now), false);
    assert.throws(() => db.wallet.creditVerified(b.id, 'test:receipt:unique', 1000, now), /idempotency_conflict/);
    assert.throws(() => db.wallet.purchaseGroup(a.id, b.id, 'AB23CD', 100, 'rollback-key-00001', now, () => { throw new Error('grant failed'); }), /grant failed/);
    assert.equal(db.wallet.balance(a.id), 1000);
    assert.equal(db.wallet.balance(b.id), 0);
    let granted = 0;
    const buy = () => db.wallet.purchaseGroup(a.id, b.id, 'AB23CD', 100, 'success-key-00001', now, () => granted++);
    assert.equal(buy(), true); assert.equal(buy(), false); assert.equal(granted, 1);
    assert.equal(db.wallet.balance(a.id), 900); assert.equal(db.wallet.balance(b.id), 100);
    assert.throws(() => db.wallet.purchaseGroup(a.id, b.id, 'AB23CD', 901, 'too-much-key-0001', now, () => granted++), /insufficient_coins/);
    assert.throws(() => db.wallet.purchaseGroup(a.id, b.id, 'AB23CD', 99, 'success-key-00001', now, () => granted++), /idempotency_conflict/);
    const wa = (await call('/api/wallet?playerId=' + b.id, a.token)).body;
    assert.equal(wa.balance, 900); assert.equal(wa.transactions.length, 2);
    assert.equal(wa.checkoutAvailable, false);
    for (const pack of COIN_PACKS) assert.equal(pack.amountUzs, pack.coins * 100);
    assert.equal((await call('/api/wallet/checkout', a.token, { provider: 'payme', coins: 1000, paid: true })).status, 503);
    assert.equal((await call('/api/wallet/credit', a.token, { coins: 1000 })).status, 404);
    assert.equal(db.wallet.balance(a.id), 900);
    db.wallet.creditVerified(b.id, 'test:limit:receipt', 999999900, now);
    assert.throws(() => db.wallet.purchaseGroup(a.id, b.id, 'AB23CD', 1, 'balance-limit-0001', now, () => granted++), /balance_limit/);
    assert.equal(db.wallet.balance(a.id), 900); // receiver overflow rolls the debit back
    assert.equal(granted, 1);
  } finally {
    server.closeAllConnections(); await new Promise(resolve => server.close(resolve)); db.close();
  }
});
