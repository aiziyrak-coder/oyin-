from inp import *
img = load(PAGE); m = loadm('mask.png')
a = fill(img, m, (0, 0, 518, 518))
save(a, 'rA.png'); zoom(a, 'rA_3x.png')
