from ink import *
regs = {
 'brand_text': (42,17,100,37),
 'dunyo': (199,18,237,34),
 'uz': (524,19,541,33),
 'lang_chev': (541,21,553,31),
 'globe': (502,15,525,38),
 'bell': (567,14,583,39),
 'name': (638,18,706,36),
 'prof_chev': (706,20,723,31),
}
for k,r in regs.items():
    res = ink(*r)
    print(k, res['box'], res['color'], res['bg'], res['rows'])
