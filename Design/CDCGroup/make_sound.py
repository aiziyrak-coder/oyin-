"""CDCGroup ovozi: havo shovqini ko'tariladi -> yumshoq zarba -> chapdan o'ngga o'tuvchi metall "shiing".
Ishlatish: make_sound.py <chiqish.wav>"""
import sys, wave
import numpy as np

SR = 48000
DUR = 5.0
IMPACT = 0.95   # emblema joyiga tushadi (ovoz boshidan)
SHING = 1.8     # yaltirash logoning ustidan o'tadi
rng = np.random.default_rng(11)
n = int(SR * DUR)
t = np.arange(n) / SR
L = np.zeros(n); R = np.zeros(n); dry = np.zeros(n)

def env_exp(tt, tau): return np.where(tt >= 0, np.exp(-np.maximum(tt, 0) / tau), 0.0)
def attack(tt, a): return np.clip(tt / a, 0, 1)
def onepole_lp(x, cutoff):
    cutoff = np.broadcast_to(cutoff, x.shape)
    a = np.exp(-2 * np.pi * cutoff / SR)
    y = np.empty_like(x); s = 0.0
    for i in range(len(x)):
        s = (1 - a[i]) * x[i] + a[i] * s
        y[i] = s
    return y

# 1) Havo shovqini: sekin ko'tarilib, zarbada tugaydi
k = np.clip(t / IMPACT, 0, 1)
for out in (L, R):
    noise = rng.standard_normal(n)
    band = onepole_lp(noise, 200 + 3800 * k ** 2)
    band = band - onepole_lp(band, 120)
    out += band * (k ** 2.5) * 0.28 * np.where(t > IMPACT, np.exp(-(t - IMPACT) / 0.05), 1)

# 2) Yumshoq zarba
ti = t - IMPACT
freq = 50 + 45 * np.exp(-np.maximum(ti, 0) / 0.07)
dry += np.sin(2 * np.pi * np.cumsum(np.where(ti >= 0, freq, 0)) / SR) * env_exp(ti, 0.45) * attack(ti, 0.004) * 0.6

# 3) Metall "shiing": noharmonik obertonlar + yuqori chastotali shitirlash, chapdan o'ngga suriladi
ts = t - SHING
metal = np.zeros(n)
base = 1480.0
for ratio, tau, amp in ((1.0, 1.4, 1.0), (1.47, 1.0, 0.6), (2.09, 0.7, 0.45), (2.56, 0.5, 0.35), (3.12, 0.35, 0.25), (4.1, 0.22, 0.18)):
    metal += np.sin(2 * np.pi * base * ratio * np.maximum(ts, 0) * (1 + 0.0004 * np.sin(2 * np.pi * 5 * ts))) * env_exp(ts, tau) * amp
metal *= attack(ts, 0.003) * 0.09
hiss = rng.standard_normal(n)
hiss = hiss - onepole_lp(hiss, 5000)
hiss *= attack(ts, 0.01) * env_exp(ts, 0.18) * 0.12
pan = np.clip(-0.6 + 1.2 * np.clip(ts / 0.8, 0, 1), -0.6, 0.6)  # -1 chap, +1 o'ng
sig = metal + hiss
L += sig * (1 - pan) / 1.6
R += sig * (1 + pan) / 1.6

# 4) Juda yengil havo akkordi (A-sus2): fon uchun
chord = [110.0, 164.81, 246.94, 329.63]
pad = attack(t - 0.3, 1.2) * env_exp(t - IMPACT, 2.2)
for i, f in enumerate(chord):
    for out, det in ((L, 0.999), (R, 1.001)):
        out += np.sin(2 * np.pi * np.cumsum(np.full(n, f * det)) / SR + i) * pad * 0.03

# 5) Sun'iy reverb
ir_len = int(SR * 2.8)
tr = np.arange(ir_len) / SR
def make_ir():
    ir = rng.standard_normal(ir_len) * np.exp(-tr / 0.8)
    ir = onepole_lp(ir, 5000 * np.exp(-tr / 1.0) + 900)
    ir[: int(SR * 0.015)] = 0
    return ir / np.sqrt(np.sum(ir ** 2))
def conv(x, ir):
    size = 1 << int(np.ceil(np.log2(len(x) + len(ir))))
    return np.fft.irfft(np.fft.rfft(x, size) * np.fft.rfft(ir, size), size)[: len(x)]
L = L + 0.38 * conv(L, make_ir()) + dry + 0.12 * conv(dry, make_ir())
R = R + 0.38 * conv(R, make_ir()) + dry + 0.12 * conv(dry, make_ir())

mix = np.tanh(np.stack([L, R], axis=1) * 1.1)
mix *= (np.clip((DUR - t) / 1.0, 0, 1) ** 2)[:, None]
mix[: int(SR * 0.005)] *= np.linspace(0, 1, int(SR * 0.005))[:, None]
mix *= 0.85 / np.max(np.abs(mix))
with wave.open(sys.argv[1], "wb") as w:
    w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR)
    w.writeframes((mix * 32767).astype(np.int16).tobytes())
for s in np.arange(0, DUR, 0.5):
    seg = mix[int(s * SR): int((s + 0.5) * SR)]
    print(f"{s:4.1f}s {20*np.log10(np.sqrt(np.mean(seg**2)) + 1e-9):6.1f} dB")
