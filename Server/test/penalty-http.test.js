import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createServer } from 'node:http';
import { createApp } from '../src/app.js';
import { openDatabase } from '../src/db.js';

async function fixture(limits={}) {
  let clock=Date.parse('2026-10-01T10:00:00Z');
  const db=openDatabase(':memory:');
  const server=createServer(createApp(db,{now:()=>new Date(clock),rateLimits:limits}));
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  const base='http://127.0.0.1:'+server.address().port;
  async function call(path,player,body) {
    const response=await fetch(base+path,{method:body===undefined?'GET':'POST',headers:{'Content-Type':'application/json',...(player?{Authorization:'Bearer '+player.token}:{})},body:body===undefined?undefined:JSON.stringify(body)});
    return {status:response.status,body:await response.json()};
  }
  async function player(nickname) {
    const p=(await call('/api/players',null,{nickname,gender:'male',avatarId:'M1'})).body;
    p.world=(await call('/api/world/join',p,{})).body; return p;
  }
  return {call,player,advance(ms){clock+=ms;},async close(){server.closeAllConnections();await new Promise(resolve=>server.close(resolve));db.close();}};
}

test('HTTP penalty authenticates every route and rate-limits reads/writes per player independently',async()=>{
  const f=await fixture({penaltyRead:2,penaltyWrite:2});
  try {
    assert.equal((await f.call('/api/penalty/state')).status,401);
    for(const action of ['join','shoot','dive','leave','reset']) assert.equal((await f.call('/api/penalty/'+action,null,{})).status,401);
    const a=await f.player('PenaltyHttpA'),b=await f.player('PenaltyHttpB');
    const path='/api/penalty/state?sessionId='+a.world.sessionId;
    assert.equal((await f.call(path,a)).status,200);assert.equal((await f.call(path,a)).status,200);assert.equal((await f.call(path,a)).status,429);
    assert.equal((await f.call('/api/penalty/state?sessionId='+b.world.sessionId,b)).status,200);
    assert.equal((await f.call('/api/penalty/join',a,{sessionId:b.world.sessionId})).body.error,'world_session_expired');
    assert.equal((await f.call('/api/penalty/leave',a,{sessionId:a.world.sessionId})).status,200);
    assert.equal((await f.call('/api/penalty/leave',a,{sessionId:a.world.sessionId})).status,429);
    assert.equal((await f.call('/api/penalty/leave',b,{sessionId:b.world.sessionId})).status,200);
  } finally {await f.close();}
});

test('HTTP penalty clients walk to entry, share goal/save results and restore safe world positions',async()=>{
  const f=await fixture();
  try {
    const a=await f.player('PenaltyFlowA'),b=await f.player('PenaltyFlowB');
    const players=[a,b];
    const request=(p,extra={})=>({sessionId:p.world.sessionId,...extra});
    for(let step=0;step<45;step++) {
      f.advance(500);
      for(const p of players) {
        const s=p.world.self,dx=64-s.x,dz=43-s.z,scale=Math.min(1,3.5/Math.max(.001,Math.hypot(dx,dz)));
        const next=await f.call('/api/world/state',p,{...request(p),...s,x:s.x+dx*scale,z:s.z+dz*scale});
        assert.equal(next.status,200);assert.equal(next.body.corrected,false);p.world=next.body;
      }
    }
    assert.equal((await f.call('/api/penalty/join',a,request(a))).body.phase,'waiting');
    let arena=(await f.call('/api/penalty/join',b,request(b))).body;
    assert.equal(arena.phase,'aiming');assert.equal(arena.player1.publicId,a.publicId);assert.equal(arena.player2.publicId,b.publicId);
    const before=arena.version;
    assert.equal((await f.call('/api/penalty/shoot',a,request(a,{version:before,turn:0,aimX:.72,aimY:.4,power:.72}))).status,200);
    assert.equal((await f.call('/api/penalty/dive',b,request(b,{version:before,turn:0,direction:0}))).body.phase,'flight');
    f.advance(1600);
    arena=(await f.call('/api/penalty/state?sessionId='+a.world.sessionId,a)).body;
    assert.equal(arena.result,'goal');assert.equal(arena.score1,1);
    const watched=(await f.call('/api/penalty/state?sessionId='+b.world.sessionId,b)).body;
    assert.equal(watched.version,arena.version);assert.equal(watched.score1,1);assert.equal(watched.shot.targetX,arena.shot.targetX);
    f.advance(2600);
    arena=(await f.call('/api/penalty/state?sessionId='+a.world.sessionId,a)).body;
    assert.equal(arena.turn,1);assert.equal(arena.keeperId,a.publicId);
    const second=arena.version;
    assert.equal((await f.call('/api/penalty/dive',a,request(a,{version:second,turn:1,direction:0}))).status,200);
    assert.equal((await f.call('/api/penalty/shoot',b,request(b,{version:second,turn:1,aimX:0,aimY:.4,power:.72}))).body.phase,'flight');
    f.advance(1600);
    arena=(await f.call('/api/penalty/state?sessionId='+a.world.sessionId,a)).body;
    assert.equal(arena.result,'save');assert.equal(arena.score2,0);assert.equal(arena.attempts2,1);
    assert.equal((await f.call('/api/penalty/leave',a,request(a))).body.phase,'abandoned');
    const restored=await f.call('/api/world/state',a,{...request(a),...a.world.self});
    assert.equal(restored.status,200);assert.equal(restored.body.self.activity,'');
    assert.equal(restored.body.self.x,64);assert.equal(restored.body.self.z,43);
    assert.equal((await f.call('/api/penalty/leave',b,request(b))).body.phase,'waiting');
  } finally {await f.close();}
});
