"""CraDev logo ovozi (qisqa, toza): belgi "pop" -> yozuv chiqishida "vish" -> yorqin akkord chertilishi.
Ishlatish: make_sound.py <chiqish.wav>   (vaqtlar ovoz boshidan, intro'da ovoz 0.2 s da boshlanadi)"""
import os, sys
import numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "tools"))
from sfx import timeline, env, pluck, swish, room, save, SR

t, L, R = timeline(2.8)

# Belgi paydo bo'lishi: qisqa "pop"
pop_t = 0.05
x = np.maximum(t - pop_t, 0)
freq = 150 + 250 * np.exp(-x / 0.03)
pop = np.sin(2 * np.pi * np.cumsum(np.where(t >= pop_t, freq, 0)) / SR) * env(t, pop_t, 0.002, 0.09) * 0.55
L += pop; R += pop

# Yozuv belgidan chiqib keladi: yengil "vish"
L += swish(t, 0.62, 0.42, 700, 3200, 0.5)
R += swish(t, 0.66, 0.42, 700, 3200, 0.5)

# Akkord (E-dur): chapdan o'ngga yengil "strum"
for i, (f, pan) in enumerate(((329.63, -0.3), (415.30, -0.1), (493.88, 0.1), (659.25, 0.3))):
    p = pluck(t, 0.86 + i * 0.022, f, 0.75, 0.16)
    L += p * (1 - pan); R += p * (1 + pan)
sub = pluck(t, 0.86, 82.41, 0.5, 0.35)
L += sub; R += sub

save(sys.argv[1], room(L), room(R))
