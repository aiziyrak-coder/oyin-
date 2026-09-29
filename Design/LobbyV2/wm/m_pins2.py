from ink import *
# label regions (inside bubble fill), icon regions (inside disk)
L = {
 'mag': ((272,157,332,179), (241,156,265,180)),
 'oquv': ((500,117,578,138), (467,116,493,139)),
 'biz': ((337,96,407,116), (306,95,331,117)),
 'kon': ((553,194,643,215), (523,193,548,216)),
 'tur': ((272,243,333,265), (240,242,265,266)),
 'tad': ((418,289,476,311), (387,288,412,312)),
 'xiz': ((585,305,640,326), (552,304,578,327)),
}
for k,(lr,ir) in L.items():
    l = ink(*lr, bgq=0.4); i = ink(*ir, bgq=0.5, fgq=0.98)
    print(k, 'label', l['box'], l['color'], l['bg'], 'rows', l['rows'])
    print('   icon', i['box'], i['color'], 'disk', i['bg'])
