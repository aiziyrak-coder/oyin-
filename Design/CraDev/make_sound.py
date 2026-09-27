"""CraDev intro ovozi: shovqinli ko'tarilish -> past chastotali zarba -> akkord -> jiringlash, sun'iy reverb bilan."""
import sys, wave
import numpy as np

SR = 48000
DUR = 6.0
IMPACT = 0.45   # zarba vaqti (ovoz boshidan, soniya)
CHIME = 1.95    # jiringlash vaqti
rng = np.random.default_rng(42)
n = int(SR * DUR)
t = np.arange(n) / SR
L = np.zeros(n); R = np.zeros(n)
dry_only = np.zeros(n)  # reverb'siz qism (sub bass)

def env_exp(tt, tau): return np.where(tt >= 0, np.exp(-np.maximum(tt, 0) / tau), 0.0)
def attack(tt, a): return np.clip(tt / a, 0, 1)

def onepole_lp(x, cutoff):
    """Vaqt bo'yicha o'zgaruvchan kesish chastotali oddiy past chastota filtri."""
    cutoff = np.broadcast_to(cutoff, x.shape)
    a = np.exp(-2 * np.pi * cutoff / SR)
    y = np.empty_like(x); s = 0.0
    for i in range(len(x)):
        s = (1 - a[i]) * x[i] + a[i] * s
        y[i] = s
    return y

# 1) Ko'tarilish (whoosh): filtrlangan shovqin, chastotasi va balandligi oshib boradi
m = t < IMPACT + 0.05
tr = t[m]
k = np.clip(tr / IMPACT, 0, 1)
for ch, out in ((0, L), (1, R)):
    noise = rng.standard_normal(m.sum())
    lp = onepole_lp(noise, 300 + 5200 * k ** 2)
    hp = lp - onepole_lp(lp, 150)
    riser = hp * (k ** 2.2) * 0.35 * np.where(tr > IMPACT, np.exp(-(tr - IMPACT) / 0.02), 1)
    out[m] += riser

# 2) Zarba: pastga sirpanuvchi sub bass + qisqa "chiq" tovushi
ti = t - IMPACT
freq = 42 + (115 - 42) * np.exp(-np.maximum(ti, 0) / 0.09)
phase = 2 * np.pi * np.cumsum(np.where(ti >= 0, freq, 0)) / SR
sub = np.sin(phase) * env_exp(ti, 0.75) * attack(ti, 0.004) * 0.9
dry_only += sub
click = onepole_lp(rng.standard_normal(n) * env_exp(ti, 0.035), 2500) * 1.4
L += click; R += click

# 3) Akkord (D-maj9 ochiq joylashuv): sekin kirib, uzoq so'nadi
chord = [146.83, 220.00, 329.63, 369.99, 440.00, 554.37, 659.25]
pad_env = attack(ti, 0.35) * env_exp(ti, 2.1)
for i, f in enumerate(chord):
    amp = 0.055 * (1.0 if f < 500 else 0.7)
    vib = 1 + 0.0015 * np.sin(2 * np.pi * (4.5 + i * 0.3) * t + i)
    for out, det in ((L, 1 - 0.0012), (R, 1 + 0.0012)):
        ph = 2 * np.pi * np.cumsum(f * det * vib) / SR
        out += (np.sin(ph) + 0.18 * np.sin(2 * ph)) * amp * pad_env

# 4) Jiringlash: yozuv ustidan nur o'tganda uchta qo'ng'iroq tovushi
for j, (f, dt) in enumerate(((1318.51, 0.0), (1760.0, 0.07), (2217.46, 0.14))):
    tc = t - (CHIME + dt)
    bell = np.zeros(n)
    for ratio, tau, a in ((1.0, 0.9, 1.0), (2.76, 0.35, 0.45), (5.40, 0.12, 0.25)):
        bell += np.sin(2 * np.pi * f * ratio * np.maximum(tc, 0)) * env_exp(tc, tau) * a
    bell *= attack(tc, 0.002) * 0.07
    pan = (-0.4, 0.0, 0.4)[j]
    L += bell * (1 - pan) / 1.2; R += bell * (1 + pan) / 1.2

# 5) Sun'iy reverb: so'nuvchi shovqin impulsi bilan konvolyutsiya
ir_len = int(SR * 2.6)
ti_r = np.arange(ir_len) / SR
def make_ir():
    ir = rng.standard_normal(ir_len) * np.exp(-ti_r / 0.7)
    ir = onepole_lp(ir, 4000 * np.exp(-ti_r / 1.2) + 800)
    ir[: int(SR * 0.012)] = 0  # erta aks-sado oldidan kichik pauza
    return ir / np.sqrt(np.sum(ir ** 2))
def conv(x, ir):
    size = 1 << int(np.ceil(np.log2(len(x) + len(ir))))
    return np.fft.irfft(np.fft.rfft(x, size) * np.fft.rfft(ir, size), size)[: len(x)]
wet = 0.32
Lw = conv(L, make_ir()); Rw = conv(R, make_ir())
L = L + wet * Lw + dry_only + 0.15 * conv(dry_only, make_ir())
R = R + wet * Rw + dry_only + 0.15 * conv(dry_only, make_ir())

# Yakuniy ishlov: yumshoq cheklash, oxirini so'ndirish, -1 dBFS ga normallash
mix = np.stack([L, R], axis=1)
mix = np.tanh(mix * 1.1)
fade = np.clip((DUR - t) / 1.2, 0, 1) ** 2
mix *= fade[:, None]
mix[: int(SR * 0.005)] *= np.linspace(0, 1, int(SR * 0.005))[:, None]
mix *= 0.89 / np.max(np.abs(mix))
pcm = (mix * 32767).astype(np.int16)
with wave.open(sys.argv[1], "wb") as w:
    w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())
rms = np.sqrt(np.mean(mix ** 2))
print(f"peak {np.max(np.abs(mix)):.3f}  rms {20*np.log10(rms):.1f} dBFS  len {DUR}s")
for s in np.arange(0, DUR, 0.5):
    seg = mix[int(s * SR): int((s + 0.5) * SR)]
    print(f"{s:4.1f}s  {20*np.log10(np.sqrt(np.mean(seg**2)) + 1e-9):6.1f} dB")
