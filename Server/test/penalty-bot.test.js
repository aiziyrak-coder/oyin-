import assert from 'node:assert/strict';
import { test } from 'node:test';
import { parseOptions, choosePenaltyAction, penaltyFollowTarget, followStep, chooseChessReply } from '../../tools/world-visual-bot.mjs';

test('penalty helper remains explicit loopback-only, duration bounded, and one activity at a time', () => {
  const base = ['--allow-test', '--base-url', 'http://127.0.0.1:8088'];
  const options = parseOptions([...base, '--penalty']);
  assert.equal(options.penalty, true); assert.equal(options.chess, false); assert.equal(options.seconds, 120);
  assert.equal(parseOptions(base).penalty, false);
  for (const args of [base.slice(1), ['--allow-test','--base-url','https://remote.example:8088'],
    ['--allow-test','--base-url','http://127.0.0.1:8080'], [...base,'--seconds','121'], [...base,'--penalty','--chess']])
    assert.throws(() => parseOptions(args));
});

test('penalty helper approaches entry kiosk rather than following shooter onto pitch', () => {
  const shooter = { publicId: 1001, x: 64, z: 66.5, activity: 'penalty' };
  const target = penaltyFollowTarget(shooter, null);
  const end = followStep({ x:64,z:43,yaw:0 }, target, .2);
  assert.equal(end.x, 64); assert.equal(end.z, 43);
  const near = followStep({ x:60,z:40,yaw:0 }, target, .2);
  assert.ok(Math.hypot(near.x-60,near.z-40)<=1.200001);
  const normal = { x:3,z:4 }; assert.equal(penaltyFollowTarget(normal,null),normal);
  assert.deepEqual(penaltyFollowTarget(normal,{player1:{publicId:1001},player2:null}),target);
});

test('penalty helper only commits once to each of the two scripted test turns', () => {
  const arena = { player1:{publicId:10},player2:{publicId:20},phase:'aiming',turn:0,shooterId:10,keeperId:20,keeperReady:false,shooterReady:false };
  assert.deepEqual(choosePenaltyAction(arena,20),{action:'dive',direction:0});
  assert.equal(choosePenaltyAction(arena,10),null);
  assert.equal(choosePenaltyAction({...arena,keeperReady:true},20),null);
  assert.equal(choosePenaltyAction({...arena,phase:'flight'},20),null);
  const second = {...arena,turn:1,shooterId:20,keeperId:10};
  assert.deepEqual(choosePenaltyAction(second,20),{action:'shoot',aimX:0,aimY:.4,power:.72});
  assert.equal(choosePenaltyAction({...second,shooterReady:true},20),null);
  assert.equal(choosePenaltyAction({...second,turn:3},20),null);
  assert.equal(choosePenaltyAction(null,20),null);
});

test('penalty helper preserves existing black chess reply and visual follow behavior', () => {
  const move={from:'e7',to:'e5'};
  assert.deepEqual(chooseChessReply({black:{publicId:20},turn:'black',status:'playing',legalMoves:[move]},20),move);
  assert.equal(chooseChessReply({black:{publicId:20},turn:'white',status:'playing'},20),null);
  assert.deepEqual(followStep({x:1,z:2,yaw:45},null,.2),{x:1,z:2,yaw:45});
});
