"""CDCGroup logo ovozi (qisqa, toza): chiziq chizilishida "vish" -> yumshoq past ton.
Ishlatish: make_sound.py <chiqish.wav>   (vaqtlar ovoz boshidan, splash'da ovoz 0.2 s da boshlanadi)"""
import os, sys
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "tools"))
from sfx import timeline, pluck, swish, room, save

t, L, R = timeline(2.4)

# Chiziq markazdan cho'zilishi: ikki tomonga tarqaluvchi "vish"
L += swish(t, 0.0, 0.55, 500, 2600, 0.45)
R += swish(t, 0.02, 0.55, 500, 2600, 0.45)

# Yozuv chiqqanda: yumshoq, past, ishonchli ton (A)
for f, amp, decay in ((110.0, 0.30, 1.1), (220.0, 0.22, 0.9), (329.63, 0.10, 0.7), (440.0, 0.06, 0.5)):
    p = pluck(t, 0.38, f, decay, amp)
    L += p; R += p

save(sys.argv[1], room(L, wet=0.2), room(R, wet=0.2))
