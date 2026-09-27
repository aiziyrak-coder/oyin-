"""Qisqa, zamonaviy logo ovozlari uchun oddiy sintez bloklari."""
import wave
import numpy as np

SR = 48000
rng = np.random.default_rng(3)


def timeline(duration):
    n = int(SR * duration)
    return np.arange(n) / SR, np.zeros(n), np.zeros(n)


def env(t, start, attack, decay):
    x = t - start
    return np.where(x < 0, 0.0, np.minimum(x / attack, 1.0) * np.exp(-np.maximum(x - attack, 0) / decay))


def onepole_lp(x, cutoff):
    cutoff = np.broadcast_to(cutoff, x.shape)
    a = np.exp(-2 * np.pi * cutoff / SR)
    y = np.empty_like(x); s = 0.0
    for i in range(len(x)):
        s = (1 - a[i]) * x[i] + a[i] * s
        y[i] = s
    return y


def pluck(t, start, freq, decay, amp):
    """Toza "chertish" tovushi: asosiy ton + ikki garmonika, tez so'nadi."""
    x = np.maximum(t - start, 0)
    tone = np.sin(2 * np.pi * freq * x) + 0.3 * np.sin(4 * np.pi * freq * x) * np.exp(-x / (decay * 0.4)) \
        + 0.12 * np.sin(6 * np.pi * freq * x) * np.exp(-x / (decay * 0.2))
    return tone * env(t, start, 0.004, decay) * amp


def swish(t, start, length, f0, f1, amp):
    """Havo "vish" tovushi: filtrlangan shovqin, chastotasi f0 dan f1 ga siljiydi."""
    k = np.clip((t - start) / length, 0, 1)
    shape = np.sin(np.pi * k) ** 2 * ((t >= start) & (t <= start + length))
    noise = rng.standard_normal(len(t))
    band = onepole_lp(noise, f0 + (f1 - f0) * k)
    band = band - onepole_lp(band, f0 * 0.5)
    return band * shape * amp


def room(x, seconds=0.9, decay=0.22, wet=0.18):
    """Kichik xona aks-sadosi (katta, kosmik reverb emas)."""
    n = int(SR * seconds)
    tt = np.arange(n) / SR
    ir = rng.standard_normal(n) * np.exp(-tt / decay)
    ir = onepole_lp(ir, 6000 * np.exp(-tt / 0.4) + 1500)
    ir[: int(SR * 0.008)] = 0
    ir /= np.sqrt(np.sum(ir ** 2))
    size = 1 << int(np.ceil(np.log2(len(x) + n)))
    return x + wet * np.fft.irfft(np.fft.rfft(x, size) * np.fft.rfft(ir, size), size)[: len(x)]


def save(path, left, right, peak=0.8):
    mix = np.stack([left, right], axis=1)
    fade = int(SR * 0.3)
    mix[-fade:] *= np.linspace(1, 0, fade)[:, None] ** 2
    mix *= peak / np.max(np.abs(mix))
    with wave.open(path, "wb") as w:
        w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((mix * 32767).astype(np.int16).tobytes())
