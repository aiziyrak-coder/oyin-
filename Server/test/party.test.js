import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createServer } from 'node:http';
import { openDatabase } from '../src/db.js';
import { createApp } from '../src/app.js';

test('Lobby: taklif, rozilik, 5 o‘rin, xavfsizlik, ping va uzilish', async () => {
  const db=openDatabase(':memory:');let time=Date.now();
  const server=createServer(createApp(db,{now:()=>new Date(time),rateLimits:{create:100,party:1000}}));
  await new Promise(r=>server.listen(0,'127.0.0.1',r));
  async function call(path,player,body={}) {
    const res=await fetch(`http://127.0.0.1:${server.address().port}/api/${path}`,{method:'POST',
      headers:{'Content-Type':'application/json',...(player?{Authorization:`Bearer ${player.token}`}:{})},body:JSON.stringify(body)});
    return {status:res.status,data:await res.json()};
  }
  const hb=p=>call('party/heartbeat',p,{pingMs:42});
  try {
    const players=[];for(let i=0;i<7;i++)players.push((await call('players',null,{nickname:'PartyTester'+i,gender:'male',avatarId:'M1'})).data);
    const [host,...guests]=players;
    assert.equal((await call('party/heartbeat',null,{pingMs:2})).status,401);
    assert.equal((await call('party/heartbeat',host,{pingMs:-5})).status,400);
    assert.equal((await call('party/invite',host,{nickname:guests[0].nickname})).status,403);
    for(const guest of guests){await call('friends/request',host,{nickname:guest.nickname});await call('friends/accept',guest,{nickname:host.nickname});}
    const first=(await hb(host)).data;
    assert.equal(first.members.length,1);assert.equal(first.members[0].pingMs,42);
    assert.equal('id' in first.members[0],false);assert.equal('token' in first.members[0],false);
    for(let i=0;i<4;i++)assert.equal((await call('party/invite',host,{nickname:guests[i].nickname})).status,200);
    assert.equal((await call('party/invite',host,{nickname:guests[4].nickname})).status,409);
    const invitation=(await hb(guests[0])).data.invitations[0].id;
    assert.equal((await call('party/accept',guests[4],{invitationId:invitation})).status,404);
    assert.equal((await call('party/accept',guests[0],{invitationId:invitation})).status,200);
    assert.equal((await call('party/accept',guests[0],{invitationId:invitation})).status,404);
    assert.equal((await call('party/invite',guests[0],{nickname:guests[5].nickname})).status,403);
    for(let i=1;i<4;i++){const id=(await hb(guests[i])).data.invitations[0].id;assert.equal((await call('party/accept',guests[i],{invitationId:id})).status,200);}
    const full=(await hb(host)).data;assert.equal(full.members.length,5);assert.equal(new Set(full.members.map(m=>m.seat)).size,5);
    assert.equal((await call('party/invite',host,{nickname:guests[4].nickname})).status,409);
    await call('party/leave',guests[2]);assert.equal((await hb(host)).data.members.length,4);
    await call('party/invite',host,{nickname:guests[4].nickname});
    const decline=(await hb(guests[4])).data.invitations[0].id;
    await call('party/decline',guests[4],{invitationId:decline});assert.equal((await hb(guests[4])).data.invitations.length,0);
    time+=13000;assert.equal((await hb(host)).data.members.find(m=>m.nickname===guests[0].nickname).online,false);
    await call('party/leave',host);assert.equal((await hb(guests[0])).data.members.length,1);
    await call('party/invite',host,{nickname:guests[0].nickname});const expired=(await hb(guests[0])).data.invitations[0].id;
    time+=61000;assert.equal((await call('party/accept',guests[0],{invitationId:expired})).status,404);
    assert.equal((await hb(host)).data.members.length,1);
  } finally {server.closeAllConnections();await new Promise(r=>server.close(r));db.close();}
});

test('Lobby: a\'zolarga faqat to\'g\'ri kiyim yuboriladi (bazadagi eski noto\'g\'ri kiyim bo\'sh)', async () => {
  const db = openDatabase(':memory:');
  const server = createServer(createApp(db, { rateLimits: { create: 100, party: 1000 } }));
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const call = async (path, player, body = {}) => {
    const res = await fetch(`http://127.0.0.1:${server.address().port}/api/${path}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', ...(player ? { Authorization: `Bearer ${player.token}` } : {}) },
      body: JSON.stringify(body),
    });
    return { status: res.status, data: await res.json() };
  };
  try {
    const host = (await call('players', null, { nickname: 'OutfitHost', gender: 'male', avatarId: 'M1' })).data;
    const guest = (await call('players', null, { nickname: 'OutfitGuest', gender: 'female', avatarId: 'F1' })).data;
    await call('friends/request', host, { nickname: guest.nickname });
    await call('friends/accept', guest, { nickname: host.nickname });
    // Tekshiruvdan oldingi versiya yozib qo'ygan kiyimlar (API endi ularni qabul qilmaydi)
    const valid = '{"top":"top_denim","topColor":"3E5F8F"}';
    db.updatePlayer(host.id, { outfit: valid });
    db.updatePlayer(guest.id, { outfit: `{"evil":[1,2,3],"top":"${'x'.repeat(900)}"}` });

    await call('party/heartbeat', host, { pingMs: 10 });
    assert.equal((await call('party/invite', host, { nickname: guest.nickname })).status, 200);
    const invitation = (await call('party/heartbeat', guest, { pingMs: 10 })).data.invitations[0].id;
    assert.equal((await call('party/accept', guest, { invitationId: invitation })).status, 200);

    const members = (await call('party/heartbeat', host, { pingMs: 10 })).data.members;
    assert.deepEqual(Object.fromEntries(members.map(m => [m.nickname, m.outfit])), { OutfitHost: valid, OutfitGuest: '' });
  } finally {
    server.closeAllConnections();
    await new Promise(resolve => server.close(resolve));
    db.close();
  }
});
