import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createServer } from 'node:http';
import { openDatabase } from '../src/db.js';
import { createApp } from '../src/app.js';

test('chat: friendship, group membership, history, expiry, idempotency and validation', async () => {
  const db=openDatabase(':memory:');let clock=Date.parse('2026-09-30T10:00:00Z');
  const server=createServer(createApp(db,{now:()=>new Date(clock),rateLimits:{chatSend:1000}}));
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  const base=`http://127.0.0.1:${server.address().port}`;
  async function call(path,player,body){
    const response=await fetch(base+path,{method:body?'POST':'GET',headers:{'Content-Type':'application/json',...(player?{Authorization:`Bearer ${player.token}`}:{})},body:body?JSON.stringify(body):undefined});
    return {status:response.status,body:await response.json()};
  }
  async function player(nickname){return(await call('/api/players',null,{nickname,gender:'male',avatarId:'M1'})).body;}
  try {
    const a=await player('ChatAlice'),b=await player('ChatBobby'),c=await player('ChatCara');
    const direct={kind:'direct',target:String(b.publicId),text:'Salom <b>literal</b>',clientId:'test-message-0001'};
    assert.equal((await call('/api/chat/send',null,direct)).status,401);
    assert.equal((await call('/api/chat/send',a,direct)).status,403);
    await call('/api/friends/request',a,{nickname:b.nickname});await call('/api/friends/accept',b,{nickname:a.nickname});
    const sent=await call('/api/chat/send',a,direct);assert.equal(sent.status,201);
    assert.equal((await call('/api/chat/send',a,direct)).body.id,sent.body.id);
    assert.equal((await call('/api/chat/send',a,{...direct,text:'other'})).status,409);
    const read=`/api/chat/messages?kind=direct&target=${a.publicId}`;
    assert.equal((await call(read,c)).status,403);
    assert.equal((await call(read,b)).body.items[0].text,direct.text);
    for(const text of ['', ' '.repeat(3), 'x'.repeat(1001), '\u0000'])
      assert.equal((await call('/api/chat/send',a,{...direct,text,clientId:'invalid-test-0001'})).status,400);
    for(let i=0;i<55;i++)await call('/api/chat/send',a,{...direct,text:'message '+i,clientId:'history-message-'+String(i).padStart(4,'0')});
    const latest=(await call(read,b)).body;assert.equal(latest.items.length,50);assert.equal(latest.hasMore,true);
    const older=(await call(read+'&before='+latest.items[0].id,b)).body;assert.equal(older.items.length,6);
    assert.equal((await call(read+'&after='+latest.items.at(-1).id,b)).body.items.length,0);
    assert.equal((await call(read+'&after=-1',b)).status,400);
    await call('/api/friends/remove',a,{nickname:b.nickname});assert.equal((await call(read,b)).status,403);
    const group=(await call('/api/groups/create',a,{name:'Chat group',kind:'free'})).body;
    const groupMessage={kind:'group',target:group.code,text:'Members only',clientId:'group-message-0001'};
    assert.equal((await call('/api/chat/send',b,groupMessage)).status,403);
    await call('/api/chat/send',a,groupMessage);clock+=1000;
    await call('/api/groups/join',b,{code:group.code});
    const groupRead='/api/chat/messages?kind=group&target='+group.code;
    assert.equal((await call(groupRead,b)).body.items.length,0); // before-join history is private
    assert.equal((await call('/api/chat/send',b,{...groupMessage,clientId:'group-message-0002'})).status,201);
    assert.equal((await call(groupRead,a)).body.items.length,2);
    await call('/api/groups/leave',b,{code:group.code});assert.equal((await call(groupRead,b)).status,403);
    const paid=(await call('/api/groups/create',a,{name:'Paid chat group',kind:'paid',price:10,currency:'CDCoin',period:'week'})).body;
    db.wallet.creditVerified(b.id,'chat-test:receipt',10,new Date(clock));
    await call('/api/groups/join',b,{code:paid.code,expectedPrice:10,paymentKey:'chat-paid-key-0001'});
    assert.equal((await call('/api/chat/messages?kind=group&target='+paid.code,b)).status,200);
    clock+=7*86400000;assert.equal((await call('/api/chat/messages?kind=group&target='+paid.code,b)).status,403);
  } finally {server.closeAllConnections();await new Promise(resolve=>server.close(resolve));db.close();}
});
