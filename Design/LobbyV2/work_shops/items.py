from m import ink, color
items=[(88.5,108.7),(109.8,133.5),(134.5,157.8),(158.8,182.6),(183.5,207.7),(208.7,232.4),(233.4,257.2),(258.2,282.4)]
for i,(t,b) in enumerate(items):
    t=int(t)+2; b=int(b)-1
    r1=ink(18,t,36,b,'bright')[0]
    r2=ink(36,t,100,b,'bright')[0]
    print(i,'icon',r1,'label',r2)
