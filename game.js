(() => {
  'use strict';

  // Mantiqiy oʻlcham: barcha hisob-kitoblar shu koordinatalarda, ekranga esa masshtablanadi.
  const W = 480;
  const H = 720;

  const canvas = document.getElementById('game');
  const ctx = canvas.getContext('2d');
  const stage = document.getElementById('stage');
  const btnPause = document.getElementById('btn-pause');
  const btnSound = document.getElementById('btn-sound');

  const FONT_DISPLAY = '"Russo One", "Arial Black", sans-serif';
  const FONT_UI = '"Chakra Petch", "Segoe UI", system-ui, sans-serif';

  const C = {
    bgTop: '#0b1026',
    bgBottom: '#1a1446',
    ink: '#eef1ff',
    muted: '#8f96c9',
    player: '#ffb547',
    playerDark: '#c96f12',
    flame: '#7ff0ff',
    bullet: '#7ff0ff',
    enemyBullet: '#ff6b9a',
    triple: '#ffd84f',
    shield: '#5ec8ff',
    life: '#ff6b9a',
  };

  const PLAYER_SPEED = 330;
  const FIRE_DELAY = 0.19;
  const MAX_LIVES = 5;

  const ENEMY = {
    basic: { w: 32, h: 28, hp: 1, speed: 115, score: 100, color: '#ff6b6b' },
    zigzag: { w: 30, h: 30, hp: 2, speed: 95, score: 150, color: '#4fd1a5' },
    tank: { w: 46, h: 40, hp: 5, speed: 55, score: 300, color: '#a98bff' },
    boss: { w: 130, h: 70, score: 2000, color: '#ff4f8b' },
  };

  const POWER = {
    triple: { color: C.triple, label: 'Uch oʻq', duration: 10 },
    shield: { color: C.shield, label: 'Qalqon', duration: 12 },
    life: { color: C.life, label: 'Qoʻshimcha jon' },
  };

  const reducedMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  // ---------- Yordamchi funksiyalar ----------
  const rand = (a, b) => a + Math.random() * (b - a);
  const clamp = (v, a, b) => Math.max(a, Math.min(b, v));
  const overlap = (a, b) => Math.abs(a.x - b.x) < (a.w + b.w) / 2 && Math.abs(a.y - b.y) < (a.h + b.h) / 2;
  const fmt = (n) => String(Math.floor(n)).replace(/\B(?=(\d{3})+(?!\d))/g, ' ');

  const store = {
    get(key, fallback) {
      try {
        const v = localStorage.getItem(key);
        return v === null ? fallback : v;
      } catch (e) {
        return fallback;
      }
    },
    set(key, value) {
      try { localStorage.setItem(key, String(value)); } catch (e) { /* saqlash imkonsiz */ }
    },
  };

  // ---------- Ovoz (WebAudio sintez, fayl kerak emas) ----------
  const sfx = {
    ac: null,
    muted: store.get('humo.muted', '0') === '1',
    init() {
      if (!this.ac) {
        const AC = window.AudioContext || window.webkitAudioContext;
        if (AC) {
          try { this.ac = new AC(); } catch (e) { this.ac = null; }
        }
      }
      if (this.ac && this.ac.state === 'suspended') this.ac.resume().catch(() => {});
    },
    tone(freq, dur, type, vol, slide) {
      if (this.muted || !this.ac) return;
      const t = this.ac.currentTime;
      const o = this.ac.createOscillator();
      const g = this.ac.createGain();
      o.type = type;
      o.frequency.setValueAtTime(freq, t);
      if (slide) o.frequency.exponentialRampToValueAtTime(Math.max(30, freq + slide), t + dur);
      g.gain.setValueAtTime(vol, t);
      g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
      o.connect(g).connect(this.ac.destination);
      o.start(t);
      o.stop(t + dur);
    },
    shoot() { this.tone(900, 0.06, 'square', 0.015, -450); },
    hit() { this.tone(420, 0.05, 'triangle', 0.03, -120); },
    boom() { this.tone(170, 0.32, 'sawtooth', 0.06, -130); },
    bigBoom() { this.tone(90, 0.9, 'sawtooth', 0.1, -60); },
    power() { this.tone(520, 0.18, 'triangle', 0.07, 700); },
    hurt() { this.tone(240, 0.45, 'sawtooth', 0.09, -190); },
    level() { this.tone(660, 0.12, 'triangle', 0.06, 0); setTimeout(() => this.tone(990, 0.2, 'triangle', 0.06, 0), 120); },
  };

  // ---------- Holat ----------
  let state = 'menu'; // menu | playing | paused | over
  let g = null;
  let best = Number(store.get('humo.best', '0')) || 0;
  let newRecord = false;
  let overT = 0;
  let clock = 0;

  const stars = Array.from({ length: 110 }, () => ({ x: Math.random() * W, y: Math.random() * H, z: rand(0.15, 1) }));
  let particles = [];
  let texts = [];

  function newGame() {
    g = {
      score: 0,
      lives: 3,
      level: 0,
      player: { x: W / 2, y: H - 90, w: 36, h: 40, cd: 0.4, inv: 0, shield: 0, triple: 0 },
      bullets: [],
      eBullets: [],
      enemies: [],
      powerups: [],
      toSpawn: 0,
      spawnT: 0,
      banner: 0,
      bannerText: '',
      bannerSub: '',
      shake: 0,
    };
    particles = [];
    texts = [];
    nextLevel();
  }

  function nextLevel() {
    g.level++;
    g.eBullets.length = 0;
    g.banner = 2.2;
    if (g.level % 5 === 0) {
      const hp = 45 + g.level * 9;
      g.toSpawn = 0;
      g.enemies.push({
        type: 'boss', x: W / 2, y: -60, w: ENEMY.boss.w, h: ENEMY.boss.h,
        hp, maxHp: hp, t: 0, flash: 0, dir: 1, fireT: 2.2, pattern: 0,
      });
      g.bannerText = `${g.level}-BOSQICH`;
      g.bannerSub = 'Ajdar yaqinlashmoqda!';
    } else {
      g.toSpawn = 6 + g.level * 3;
      g.spawnT = 1.6;
      g.bannerText = `${g.level}-BOSQICH`;
      g.bannerSub = g.level === 1 ? 'Osmonni himoya qiling' : 'Dushmanlar kuchaymoqda';
    }
  }

  function startGame() {
    sfx.init();
    newGame();
    newRecord = false;
    state = 'playing';
    syncButtons();
  }

  function togglePause() {
    if (state === 'playing') {
      state = 'paused';
      pointer.active = false;
    } else if (state === 'paused') {
      state = 'playing';
    }
    syncButtons();
  }

  function gameOver() {
    state = 'over';
    overT = 0;
    pointer.active = false;
    if (g.score > best) {
      best = g.score;
      newRecord = true;
      store.set('humo.best', best);
    }
    explode(g.player.x, g.player.y, C.player, 50, 260);
    sfx.bigBoom();
    syncButtons();
  }

  // ---------- Kiritish ----------
  const keys = { left: false, right: false, up: false, down: false };
  const KEYMAP = {
    ArrowLeft: 'left', KeyA: 'left',
    ArrowRight: 'right', KeyD: 'right',
    ArrowUp: 'up', KeyW: 'up',
    ArrowDown: 'down', KeyS: 'down',
  };

  function primaryAction() {
    sfx.init();
    if (state === 'menu') startGame();
    else if (state === 'over' && overT > 0.8) startGame();
    else if (state === 'paused') togglePause();
  }

  window.addEventListener('keydown', (e) => {
    const k = KEYMAP[e.code];
    if (k) {
      keys[k] = true;
      e.preventDefault();
      return;
    }
    if (e.code === 'Space' || e.code === 'Enter') {
      e.preventDefault();
      if (!e.repeat) primaryAction();
    } else if (e.code === 'KeyP' || e.code === 'Escape') {
      togglePause();
    } else if (e.code === 'KeyM') {
      toggleSound();
    }
  });

  window.addEventListener('keyup', (e) => {
    const k = KEYMAP[e.code];
    if (k) keys[k] = false;
  });

  window.addEventListener('blur', () => {
    keys.left = keys.right = keys.up = keys.down = false;
    pointer.active = false;
  });

  document.addEventListener('visibilitychange', () => {
    if (document.hidden && state === 'playing') togglePause();
  });

  // Sensor/sichqoncha: kema barmoqqa nisbatan suriladi, shuning uchun sakramaydi.
  const pointer = { active: false, id: null, x: 0, y: 0, offX: 0, offY: 0 };

  function toLogical(e) {
    const r = canvas.getBoundingClientRect();
    return { x: ((e.clientX - r.left) / r.width) * W, y: ((e.clientY - r.top) / r.height) * H };
  }

  canvas.addEventListener('pointerdown', (e) => {
    e.preventDefault();
    if (state !== 'playing') {
      primaryAction();
      return;
    }
    const p = toLogical(e);
    pointer.active = true;
    pointer.id = e.pointerId;
    pointer.x = p.x;
    pointer.y = p.y;
    pointer.offX = g.player.x - p.x;
    pointer.offY = g.player.y - p.y;
    try { canvas.setPointerCapture(e.pointerId); } catch (err) { /* ahamiyatsiz */ }
  });

  canvas.addEventListener('pointermove', (e) => {
    if (!pointer.active || e.pointerId !== pointer.id) return;
    const p = toLogical(e);
    pointer.x = p.x;
    pointer.y = p.y;
  });

  const endPointer = (e) => {
    if (e.pointerId === pointer.id) pointer.active = false;
  };
  canvas.addEventListener('pointerup', endPointer);
  canvas.addEventListener('pointercancel', endPointer);

  // ---------- Tugmalar ----------
  const ICONS = {
    pause: '<svg viewBox="0 0 24 24" aria-hidden="true"><rect x="6" y="5" width="4" height="14" rx="1"/><rect x="14" y="5" width="4" height="14" rx="1"/></svg>',
    play: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M8 5.5v13a1 1 0 0 0 1.5.86l10.2-6.5a1 1 0 0 0 0-1.72L9.5 4.64A1 1 0 0 0 8 5.5z"/></svg>',
    soundOn: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 9v6h4l5 4V5L8 9H4zm12.5 3a4.5 4.5 0 0 0-2.5-4v8a4.5 4.5 0 0 0 2.5-4zM14 3.2v2.1a7 7 0 0 1 0 13.4v2.1a9 9 0 0 0 0-17.6z"/></svg>',
    soundOff: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 9v6h4l5 4V5L8 9H4zm12.6 3 2.7-2.7-1.4-1.4-2.7 2.7-2.7-2.7-1.4 1.4 2.7 2.7-2.7 2.7 1.4 1.4 2.7-2.7 2.7 2.7 1.4-1.4z"/></svg>',
  };

  function syncButtons() {
    const paused = state === 'paused';
    btnPause.innerHTML = paused ? ICONS.play : ICONS.pause;
    btnPause.setAttribute('aria-label', paused ? 'Davom etish' : 'Pauza');
    btnPause.hidden = !(state === 'playing' || state === 'paused');
    btnSound.innerHTML = sfx.muted ? ICONS.soundOff : ICONS.soundOn;
    btnSound.setAttribute('aria-label', sfx.muted ? 'Ovozni yoqish' : 'Ovozni oʻchirish');
  }

  function toggleSound() {
    sfx.muted = !sfx.muted;
    store.set('humo.muted', sfx.muted ? '1' : '0');
    if (!sfx.muted) sfx.init();
    syncButtons();
  }

  btnPause.addEventListener('click', () => { togglePause(); btnPause.blur(); });
  btnSound.addEventListener('click', () => { toggleSound(); btnSound.blur(); });

  // ---------- Oʻlcham ----------
  function resize() {
    const host = stage.parentElement;
    const scale = Math.min(host.clientWidth / W, host.clientHeight / H);
    const cssW = Math.max(1, Math.floor(W * scale));
    const cssH = Math.max(1, Math.floor(H * scale));
    stage.style.width = cssW + 'px';
    stage.style.height = cssH + 'px';
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    canvas.width = Math.round(cssW * dpr);
    canvas.height = Math.round(cssH * dpr);
  }
  window.addEventListener('resize', resize);

  // ---------- Effektlar ----------
  function explode(x, y, color, n, power = 180) {
    for (let i = 0; i < n; i++) {
      const a = Math.random() * Math.PI * 2;
      const s = rand(0.2, 1) * power;
      particles.push({ x, y, vx: Math.cos(a) * s, vy: Math.sin(a) * s, life: rand(0.4, 0.9), max: 0.9, color, size: rand(1.5, 3.5) });
    }
  }

  function addText(x, y, text, color = C.ink) {
    texts.push({ x, y, text, color, life: 0.9 });
  }

  function updateEffects(dt) {
    for (const p of particles) {
      p.x += p.vx * dt;
      p.y += p.vy * dt;
      p.vx *= 0.96;
      p.vy *= 0.96;
      p.life -= dt;
    }
    particles = particles.filter((p) => p.life > 0);
    for (const t of texts) {
      t.y -= 40 * dt;
      t.life -= dt;
    }
    texts = texts.filter((t) => t.life > 0);
  }

  function updateStars(dt, speed) {
    for (const s of stars) {
      s.y += (15 + 110 * s.z) * dt * speed;
      if (s.y > H) {
        s.y = -2;
        s.x = Math.random() * W;
      }
    }
  }

  // ---------- Dushmanlar ----------
  function spawnEnemy() {
    const L = g.level;
    const r = Math.random();
    let type = 'basic';
    if (L >= 2 && r < 0.3) type = 'zigzag';
    if (L >= 3 && r > 0.82) type = 'tank';
    const d = ENEMY[type];
    const x = rand(30, W - 30);
    g.enemies.push({
      type, x, baseX: x, y: -30, w: d.w, h: d.h, hp: d.hp, maxHp: d.hp,
      speed: d.speed * (1 + L * 0.06), t: rand(0, 6), flash: 0,
      canShoot: type === 'tank' || (type === 'basic' && L >= 4 && Math.random() < 0.35),
      fireT: rand(0.8, 2.2),
    });
  }

  function enemyShoot(e, angle, speed = 240) {
    const a = angle === undefined ? Math.atan2(g.player.y - e.y, g.player.x - e.x) : angle;
    g.eBullets.push({ x: e.x, y: e.y + e.h / 2 - 4, vx: Math.cos(a) * speed, vy: Math.sin(a) * speed, w: 8, h: 8 });
  }

  function updateBoss(e, dt) {
    if (e.y < 130) {
      e.y += 70 * dt;
      return;
    }
    const rage = e.hp < e.maxHp * 0.4;
    e.x += e.dir * (80 + g.level * 4) * (rage ? 1.5 : 1) * dt;
    if (e.x < 80) { e.x = 80; e.dir = 1; }
    if (e.x > W - 80) { e.x = W - 80; e.dir = -1; }
    e.fireT -= dt;
    if (e.fireT <= 0) {
      e.pattern = (e.pattern + 1) % 3;
      if (e.pattern === 0) {
        for (let i = -3; i <= 3; i++) enemyShoot(e, Math.PI / 2 + i * 0.2, 210);
      } else if (e.pattern === 1) {
        for (let i = -1; i <= 1; i++) {
          const a = Math.atan2(g.player.y - e.y, g.player.x - e.x) + i * 0.12;
          enemyShoot(e, a, 280);
        }
      } else {
        const off = e.t * 2;
        for (let i = 0; i < 10; i++) enemyShoot(e, off + (i / 10) * Math.PI * 2, 170);
      }
      e.fireT = (rage ? 0.9 : 1.4) * Math.max(0.6, 1 - g.level * 0.02);
    }
  }

  function killEnemy(e) {
    e.dead = true;
    const boss = e.type === 'boss';
    const pts = boss ? ENEMY.boss.score + g.level * 200 : ENEMY[e.type].score;
    g.score += pts;
    addText(e.x, e.y, '+' + fmt(pts), boss ? C.player : C.ink);
    explode(e.x, e.y, ENEMY[e.type].color, boss ? 90 : 18, boss ? 320 : 180);
    if (boss) {
      sfx.bigBoom();
      g.shake = 0.7;
      g.eBullets.length = 0;
      dropPower(e.x, e.y, 'life');
    } else {
      sfx.boom();
      if (Math.random() < (e.type === 'tank' ? 0.35 : 0.08)) dropPower(e.x, e.y);
    }
  }

  function dropPower(x, y, forced) {
    let type = forced;
    if (!type) {
      const r = Math.random();
      type = r < 0.45 ? 'triple' : r < 0.87 ? 'shield' : 'life';
    }
    g.powerups.push({ type, x, y, w: 26, h: 26, t: 0 });
  }

  function applyPower(type) {
    const p = g.player;
    sfx.power();
    if (type === 'triple') {
      p.triple = POWER.triple.duration;
      addText(p.x, p.y - 30, 'UCH OʻQ', C.triple);
    } else if (type === 'shield') {
      p.shield = POWER.shield.duration;
      addText(p.x, p.y - 30, 'QALQON', C.shield);
    } else if (g.lives < MAX_LIVES) {
      g.lives++;
      addText(p.x, p.y - 30, '+1 JON', C.life);
    } else {
      g.score += 1000;
      addText(p.x, p.y - 30, '+1 000', C.life);
    }
  }

  function damagePlayer() {
    const p = g.player;
    if (p.inv > 0 || state !== 'playing') return;
    if (p.shield > 0) {
      p.shield = 0;
      p.inv = 0.8;
      explode(p.x, p.y, C.shield, 20, 200);
      sfx.hit();
      return;
    }
    g.lives--;
    p.inv = 2.2;
    p.triple = 0;
    g.shake = 0.35;
    explode(p.x, p.y, C.player, 26);
    sfx.hurt();
    if (g.lives <= 0) gameOver();
  }

  // ---------- Yangilanish ----------
  function update(dt) {
    clock += dt;
    updateStars(dt, state === 'playing' ? 1 : 0.35);
    updateEffects(dt);
    if (state === 'over') overT += dt;
    if (state !== 'playing') return;

    const p = g.player;
    g.shake = Math.max(0, g.shake - dt);
    g.banner = Math.max(0, g.banner - dt);
    p.inv = Math.max(0, p.inv - dt);
    p.shield = Math.max(0, p.shield - dt);
    p.triple = Math.max(0, p.triple - dt);

    // Harakat
    if (pointer.active) {
      const k = Math.min(1, dt * 18);
      p.x += (pointer.x + pointer.offX - p.x) * k;
      p.y += (pointer.y + pointer.offY - p.y) * k;
    } else {
      let dx = (keys.right ? 1 : 0) - (keys.left ? 1 : 0);
      let dy = (keys.down ? 1 : 0) - (keys.up ? 1 : 0);
      if (dx && dy) {
        dx *= Math.SQRT1_2;
        dy *= Math.SQRT1_2;
      }
      p.x += dx * PLAYER_SPEED * dt;
      p.y += dy * PLAYER_SPEED * dt;
    }
    p.x = clamp(p.x, 22, W - 22);
    p.y = clamp(p.y, H * 0.4, H - 30);

    // Avtomatik otish
    p.cd -= dt;
    if (p.cd <= 0) {
      p.cd = FIRE_DELAY;
      g.bullets.push({ x: p.x, y: p.y - 24, vx: 0, vy: -660, w: 4, h: 14 });
      if (p.triple > 0) {
        g.bullets.push({ x: p.x - 10, y: p.y - 12, vx: -160, vy: -640, w: 4, h: 14 });
        g.bullets.push({ x: p.x + 10, y: p.y - 12, vx: 160, vy: -640, w: 4, h: 14 });
      }
      sfx.shoot();
    }

    for (const b of g.bullets) {
      b.x += b.vx * dt;
      b.y += b.vy * dt;
      if (b.y < -20 || b.x < -20 || b.x > W + 20) b.dead = true;
    }
    for (const b of g.eBullets) {
      b.x += b.vx * dt;
      b.y += b.vy * dt;
      if (b.y > H + 20 || b.y < -40 || b.x < -20 || b.x > W + 20) b.dead = true;
    }

    // Dushman chiqishi
    if (g.toSpawn > 0) {
      g.spawnT -= dt;
      if (g.spawnT <= 0) {
        spawnEnemy();
        g.toSpawn--;
        g.spawnT = Math.max(0.3, 1.1 - g.level * 0.06) * rand(0.6, 1.3);
      }
    }

    for (const e of g.enemies) {
      e.t += dt;
      e.flash = Math.max(0, e.flash - dt);
      if (e.type === 'boss') {
        updateBoss(e, dt);
        continue;
      }
      e.y += e.speed * dt;
      if (e.type === 'zigzag') e.x = clamp(e.baseX + Math.sin(e.t * 2.6) * 70, 20, W - 20);
      if (e.canShoot && e.y > 20 && e.y < H * 0.6) {
        e.fireT -= dt;
        if (e.fireT <= 0) {
          enemyShoot(e);
          e.fireT = rand(1.6, 3) / (1 + g.level * 0.05);
        }
      }
      if (e.y > H + 40) e.dead = true;
    }

    // Toʻqnashuvlar
    for (const b of g.bullets) {
      if (b.dead) continue;
      for (const e of g.enemies) {
        if (e.dead || !overlap(b, e)) continue;
        b.dead = true;
        e.hp--;
        e.flash = 0.08;
        explode(b.x, b.y, C.bullet, 3, 90);
        if (e.hp <= 0) killEnemy(e);
        else if (e.type === 'boss' || e.type === 'tank') sfx.hit();
        break;
      }
    }

    const hitbox = { x: p.x, y: p.y + 2, w: 18, h: 26 };
    for (const b of g.eBullets) {
      if (!b.dead && overlap(b, hitbox)) {
        b.dead = true;
        damagePlayer();
      }
    }
    for (const e of g.enemies) {
      if (e.dead || !overlap(e, hitbox)) continue;
      if (e.type !== 'boss') {
        e.dead = true;
        explode(e.x, e.y, ENEMY[e.type].color, 16);
      }
      damagePlayer();
    }

    const grab = { x: p.x, y: p.y, w: 44, h: 48 };
    for (const u of g.powerups) {
      u.t += dt;
      u.y += 90 * dt;
      if (u.y > H + 20) u.dead = true;
      else if (overlap(u, grab)) {
        u.dead = true;
        applyPower(u.type);
      }
    }

    g.bullets = g.bullets.filter((b) => !b.dead);
    g.eBullets = g.eBullets.filter((b) => !b.dead);
    g.enemies = g.enemies.filter((e) => !e.dead);
    g.powerups = g.powerups.filter((u) => !u.dead);

    if (state === 'playing' && g.toSpawn === 0 && g.enemies.length === 0) {
      const bonus = g.level * 250;
      g.score += bonus;
      addText(W / 2, H * 0.55, `Bosqich bonusi +${fmt(bonus)}`, C.player);
      sfx.level();
      nextLevel();
    }
  }

  // ---------- Chizish ----------
  function text(str, x, y, size, color, font = FONT_UI, align = 'center', spacing = 0) {
    ctx.font = `${size}px ${font}`;
    ctx.fillStyle = color;
    ctx.textAlign = align;
    ctx.textBaseline = 'middle';
    if ('letterSpacing' in ctx) ctx.letterSpacing = spacing + 'px';
    ctx.fillText(str, x, y);
    if ('letterSpacing' in ctx) ctx.letterSpacing = '0px';
  }

  function drawBackground() {
    const grad = ctx.createLinearGradient(0, 0, 0, H);
    grad.addColorStop(0, C.bgTop);
    grad.addColorStop(1, C.bgBottom);
    ctx.fillStyle = grad;
    ctx.fillRect(0, 0, W, H);
    for (const s of stars) {
      ctx.globalAlpha = 0.25 + s.z * 0.75;
      ctx.fillStyle = s.z > 0.8 ? '#ffe7b8' : C.ink;
      const size = s.z > 0.8 ? 2 : 1.2;
      ctx.fillRect(s.x, s.y, size, size * (state === 'playing' ? 1 + s.z * 2 : 1));
    }
    ctx.globalAlpha = 1;
  }

  function drawShip(x, y, scale = 1) {
    ctx.save();
    ctx.translate(x, y);
    ctx.scale(scale, scale);
    // Dvigatel olovi
    const f = 8 + Math.random() * 7;
    ctx.fillStyle = C.flame;
    ctx.globalAlpha = 0.85;
    ctx.beginPath();
    ctx.moveTo(-5, 14);
    ctx.lineTo(0, 14 + f);
    ctx.lineTo(5, 14);
    ctx.fill();
    ctx.globalAlpha = 1;
    // Humo qanotlari
    ctx.fillStyle = C.playerDark;
    ctx.beginPath();
    ctx.moveTo(0, -6);
    ctx.quadraticCurveTo(-14, -2, -24, -12);
    ctx.lineTo(-20, 6);
    ctx.lineTo(-8, 12);
    ctx.lineTo(0, 16);
    ctx.lineTo(8, 12);
    ctx.lineTo(20, 6);
    ctx.lineTo(24, -12);
    ctx.quadraticCurveTo(14, -2, 0, -6);
    ctx.fill();
    // Tana
    ctx.fillStyle = C.player;
    ctx.beginPath();
    ctx.moveTo(0, -24);
    ctx.lineTo(8, 6);
    ctx.lineTo(0, 13);
    ctx.lineTo(-8, 6);
    ctx.closePath();
    ctx.fill();
    // Kabina
    ctx.fillStyle = C.bgTop;
    ctx.beginPath();
    ctx.ellipse(0, -5, 3, 6, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();
  }

  function drawPlayer() {
    const p = g.player;
    if (p.inv > 0 && p.shield <= 0 && Math.floor(p.inv * 12) % 2 === 0) return;
    drawShip(p.x, p.y);
    if (p.shield > 0) {
      const fading = p.shield < 2 && Math.floor(p.shield * 8) % 2 === 0;
      ctx.strokeStyle = C.shield;
      ctx.lineWidth = 2;
      ctx.globalAlpha = fading ? 0.25 : 0.55 + Math.sin(clock * 6) * 0.2;
      ctx.beginPath();
      ctx.arc(p.x, p.y, 32, 0, Math.PI * 2);
      ctx.stroke();
      ctx.globalAlpha = 0.08;
      ctx.fillStyle = C.shield;
      ctx.fill();
      ctx.globalAlpha = 1;
    }
  }

  function drawEnemy(e) {
    const color = e.flash > 0 ? '#ffffff' : ENEMY[e.type].color;
    ctx.save();
    ctx.translate(e.x, e.y);
    ctx.fillStyle = color;
    ctx.beginPath();
    if (e.type === 'basic') {
      ctx.moveTo(-16, -12);
      ctx.lineTo(16, -12);
      ctx.lineTo(8, 4);
      ctx.lineTo(0, 14);
      ctx.lineTo(-8, 4);
      ctx.closePath();
      ctx.fill();
      ctx.fillStyle = C.bgTop;
      ctx.fillRect(-6, -6, 4, 4);
      ctx.fillRect(2, -6, 4, 4);
    } else if (e.type === 'zigzag') {
      ctx.rotate(Math.sin(e.t * 2.6) * 0.4);
      ctx.moveTo(0, -15);
      ctx.lineTo(15, 0);
      ctx.lineTo(0, 15);
      ctx.lineTo(-15, 0);
      ctx.closePath();
      ctx.fill();
      ctx.fillStyle = C.bgTop;
      ctx.beginPath();
      ctx.arc(0, 0, 5, 0, Math.PI * 2);
      ctx.fill();
    } else if (e.type === 'tank') {
      for (let i = 0; i < 6; i++) {
        const a = (i / 6) * Math.PI * 2 + Math.PI / 6;
        ctx.lineTo(Math.cos(a) * 23, Math.sin(a) * 20);
      }
      ctx.closePath();
      ctx.fill();
      ctx.fillStyle = C.bgTop;
      ctx.fillRect(-4, 6, 8, 14);
      ctx.beginPath();
      ctx.arc(0, -2, 7, 0, Math.PI * 2);
      ctx.fill();
    } else {
      // Ajdar (boss)
      ctx.moveTo(-65, -10);
      ctx.lineTo(-40, -35);
      ctx.lineTo(40, -35);
      ctx.lineTo(65, -10);
      ctx.lineTo(50, 20);
      ctx.lineTo(20, 35);
      ctx.lineTo(-20, 35);
      ctx.lineTo(-50, 20);
      ctx.closePath();
      ctx.fill();
      ctx.fillStyle = C.bgTop;
      const eye = e.hp < e.maxHp * 0.4 ? '#ffd84f' : C.bgTop;
      ctx.fillRect(-40, -8, 80, 10);
      ctx.fillStyle = eye;
      ctx.beginPath();
      ctx.arc(-18, -3, 5, 0, Math.PI * 2);
      ctx.arc(18, -3, 5, 0, Math.PI * 2);
      ctx.fill();
      ctx.fillStyle = C.bgTop;
      ctx.fillRect(-8, 18, 16, 20);
    }
    ctx.restore();

    if (e.type === 'tank' && e.hp < e.maxHp) {
      ctx.fillStyle = 'rgba(238,241,255,0.2)';
      ctx.fillRect(e.x - 20, e.y - 30, 40, 4);
      ctx.fillStyle = ENEMY.tank.color;
      ctx.fillRect(e.x - 20, e.y - 30, 40 * (e.hp / e.maxHp), 4);
    }
  }

  function drawPowerIcon(type, x, y, r) {
    const color = POWER[type].color;
    ctx.save();
    ctx.translate(x, y);
    ctx.strokeStyle = color;
    ctx.lineWidth = 2;
    ctx.fillStyle = 'rgba(11,16,38,0.85)';
    ctx.beginPath();
    ctx.arc(0, 0, r, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();
    ctx.fillStyle = color;
    const s = r / 13;
    if (type === 'triple') {
      ctx.fillRect(-1.5 * s, -7 * s, 3 * s, 12 * s);
      ctx.save();
      ctx.rotate(-0.4);
      ctx.fillRect(-1.5 * s, -7 * s, 3 * s, 10 * s);
      ctx.restore();
      ctx.save();
      ctx.rotate(0.4);
      ctx.fillRect(-1.5 * s, -7 * s, 3 * s, 10 * s);
      ctx.restore();
    } else if (type === 'shield') {
      ctx.beginPath();
      ctx.moveTo(0, -7 * s);
      ctx.lineTo(6 * s, -4 * s);
      ctx.lineTo(5 * s, 3 * s);
      ctx.lineTo(0, 7 * s);
      ctx.lineTo(-5 * s, 3 * s);
      ctx.lineTo(-6 * s, -4 * s);
      ctx.closePath();
      ctx.fill();
    } else {
      ctx.beginPath();
      ctx.moveTo(0, 6 * s);
      ctx.bezierCurveTo(-9 * s, 0, -6 * s, -8 * s, 0, -3 * s);
      ctx.bezierCurveTo(6 * s, -8 * s, 9 * s, 0, 0, 6 * s);
      ctx.fill();
    }
    ctx.restore();
  }

  function drawBullets() {
    ctx.fillStyle = C.bullet;
    for (const b of g.bullets) ctx.fillRect(b.x - b.w / 2, b.y - b.h / 2, b.w, b.h);
    for (const b of g.eBullets) {
      ctx.fillStyle = C.enemyBullet;
      ctx.beginPath();
      ctx.arc(b.x, b.y, 4.5, 0, Math.PI * 2);
      ctx.fill();
      ctx.fillStyle = '#ffe1ec';
      ctx.beginPath();
      ctx.arc(b.x, b.y, 2, 0, Math.PI * 2);
      ctx.fill();
    }
  }

  function drawEffects() {
    ctx.globalCompositeOperation = 'lighter';
    for (const p of particles) {
      ctx.globalAlpha = Math.max(0, p.life / p.max);
      ctx.fillStyle = p.color;
      ctx.fillRect(p.x - p.size / 2, p.y - p.size / 2, p.size, p.size);
    }
    ctx.globalCompositeOperation = 'source-over';
    for (const t of texts) {
      ctx.globalAlpha = Math.min(1, t.life * 2);
      text(t.text, t.x, t.y, 15, t.color, FONT_UI);
    }
    ctx.globalAlpha = 1;
  }

  function drawMiniShip(x, y) {
    ctx.fillStyle = C.player;
    ctx.beginPath();
    ctx.moveTo(x, y - 8);
    ctx.lineTo(x + 7, y + 6);
    ctx.lineTo(x, y + 3);
    ctx.lineTo(x - 7, y + 6);
    ctx.closePath();
    ctx.fill();
  }

  function drawHud() {
    text('OCHKO', 18, 22, 11, C.muted, FONT_UI, 'left', 2);
    text(fmt(g.score), 18, 42, 24, C.ink, FONT_DISPLAY, 'left');
    text(`REKORD  ${fmt(Math.max(best, g.score))}`, 18, 64, 11, C.muted, FONT_UI, 'left', 1);
    for (let i = 0; i < g.lives; i++) drawMiniShip(26 + i * 20, 86);

    text(`${g.level}-BOSQICH`, W / 2, 26, 13, C.ink, FONT_UI, 'center', 2);

    // Faol kuchaytirgichlar
    const p = g.player;
    let row = 0;
    for (const type of ['triple', 'shield']) {
      const left = p[type];
      if (left <= 0) continue;
      const y = H - 22 - row * 22;
      drawPowerIcon(type, 26, y, 9);
      ctx.fillStyle = 'rgba(238,241,255,0.15)';
      ctx.fillRect(42, y - 2, 70, 4);
      ctx.fillStyle = POWER[type].color;
      ctx.fillRect(42, y - 2, 70 * (left / POWER[type].duration), 4);
      row++;
    }

    const boss = g.enemies.find((e) => e.type === 'boss');
    if (boss) {
      const bw = 260;
      const bx = (W - bw) / 2;
      text('AJDAR', W / 2, 48, 11, ENEMY.boss.color, FONT_UI, 'center', 3);
      ctx.fillStyle = 'rgba(238,241,255,0.15)';
      ctx.fillRect(bx, 60, bw, 6);
      ctx.fillStyle = ENEMY.boss.color;
      ctx.fillRect(bx, 60, bw * (boss.hp / boss.maxHp), 6);
    }

    if (g.banner > 0) {
      const a = Math.min(1, g.banner * 2, (2.2 - g.banner) * 4);
      ctx.globalAlpha = Math.max(0, a);
      text(g.bannerText, W / 2, H * 0.4, 38, C.player, FONT_DISPLAY, 'center', 2);
      text(g.bannerSub, W / 2, H * 0.4 + 38, 16, C.ink, FONT_UI, 'center', 1);
      ctx.globalAlpha = 1;
    }
  }

  function dim(alpha) {
    ctx.fillStyle = `rgba(7,10,28,${alpha})`;
    ctx.fillRect(0, 0, W, H);
  }

  function blink() {
    return reducedMotion ? 1 : 0.55 + Math.sin(clock * 4) * 0.45;
  }

  function drawMenu() {
    text('HUMO', W / 2, 150, 92, C.player, FONT_DISPLAY, 'center', 6);
    text('OSMON QOʻRIQCHISI', W / 2, 212, 18, C.ink, FONT_UI, 'center', 6);

    drawShip(W / 2, 300 + (reducedMotion ? 0 : Math.sin(clock * 2) * 8), 1.6);

    const lines = [
      ['Harakat', '← ↑ → ↓ / WASD yoki barmoq bilan suring'],
      ['Otish', 'Kema oʻzi toʻxtovsiz oʻq uzadi'],
      ['Tugmalar', 'P — pauza,  M — ovoz'],
    ];
    lines.forEach(([k, v], i) => {
      const y = 392 + i * 28;
      text(k.toUpperCase(), 64, y, 11, C.muted, FONT_UI, 'left', 2);
      text(v, 150, y, 14, C.ink, FONT_UI, 'left');
    });

    text('SOVGʻALAR', W / 2, 500, 11, C.muted, FONT_UI, 'center', 3);
    ['triple', 'shield', 'life'].forEach((type, i) => {
      const x = W / 2 + (i - 1) * 130;
      drawPowerIcon(type, x, 536, 13);
      text(POWER[type].label, x, 566, 13, C.ink, FONT_UI);
    });

    ctx.globalAlpha = blink();
    text('BOSHLASH UCHUN BOSING', W / 2, 628, 18, C.player, FONT_DISPLAY, 'center', 2);
    ctx.globalAlpha = 1;
    if (best > 0) text(`Rekord: ${fmt(best)}`, W / 2, 664, 13, C.muted);
  }

  function drawPaused() {
    dim(0.6);
    text('PAUZA', W / 2, H / 2 - 20, 52, C.ink, FONT_DISPLAY, 'center', 4);
    text('Davom etish uchun bosing yoki P', W / 2, H / 2 + 30, 15, C.muted);
  }

  function drawOver() {
    dim(Math.min(0.7, overT));
    text('OʻYIN TUGADI', W / 2, 230, 44, C.ink, FONT_DISPLAY, 'center', 2);
    text('OCHKO', W / 2, 300, 12, C.muted, FONT_UI, 'center', 3);
    text(fmt(g.score), W / 2, 340, 54, C.player, FONT_DISPLAY);
    text(`${g.level}-bosqichgacha yetdingiz`, W / 2, 392, 15, C.ink);
    if (newRecord) {
      text('YANGI REKORD!', W / 2, 428, 16, C.triple, FONT_UI, 'center', 3);
    } else {
      text(`Rekord: ${fmt(best)}`, W / 2, 428, 14, C.muted);
    }
    if (overT > 0.8) {
      ctx.globalAlpha = blink();
      text('QAYTA BOSHLASH UCHUN BOSING', W / 2, 520, 17, C.player, FONT_DISPLAY, 'center', 1);
      ctx.globalAlpha = 1;
    }
  }

  function draw() {
    ctx.setTransform(canvas.width / W, 0, 0, canvas.height / H, 0, 0);
    ctx.save();
    if (g && g.shake > 0 && !reducedMotion) {
      const m = g.shake * 14;
      ctx.translate(rand(-m, m), rand(-m, m));
    }
    drawBackground();

    if (state === 'menu') {
      drawEffects();
      drawMenu();
      ctx.restore();
      return;
    }

    for (const u of g.powerups) drawPowerIcon(u.type, u.x, u.y + Math.sin(u.t * 5) * 2, 13);
    for (const e of g.enemies) drawEnemy(e);
    drawBullets();
    if (state !== 'over') drawPlayer();
    drawEffects();
    ctx.restore();

    drawHud();
    if (state === 'paused') drawPaused();
    if (state === 'over') drawOver();
  }

  // ---------- Asosiy sikl ----------
  let last = performance.now();
  function frame(now) {
    const dt = Math.min(0.033, (now - last) / 1000);
    last = now;
    update(dt);
    draw();
    requestAnimationFrame(frame);
  }

  resize();
  syncButtons();
  requestAnimationFrame(frame);
})();
