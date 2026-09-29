import sys, numpy as np
sys.path.insert(0,'.')
from m import L
x0,y0,x1,y1=map(int,sys.argv[1:5]); mode=sys.argv[5] if len(sys.argv)>5 else 'bright'
l=L[y0:y1,x0:x1]
border=np.concatenate([l[0,:],l[-1,:],l[:,0],l[:,-1]]); bg=np.median(border)
fg=np.percentile(l,99.5) if mode=='bright' else np.percentile(l,0.5)
cov=np.clip((l-bg)/(fg-bg),0,1)
chars=" .:-=+*#%@"
print("     "+"".join(str((x0+i)//10%10) if (x0+i)%10==0 else " " for i in range(x1-x0)))
print("     "+"".join(str((x0+i)%10) for i in range(x1-x0)))
for j in range(y1-y0):
    print("%4d "%(y0+j)+"".join(chars[min(9,int(v*9.99))] for v in cov[j]))
