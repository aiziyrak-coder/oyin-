from ink import *
import numpy as np
# red dot: R - B
sub = IM[12:28, 578:592]
v = sub[...,0]-sub[...,2]
a = np.clip((v-v.min())/(v.max()-v.min()),0,1)
ys,xs = np.where(a>0.5)
print('dot', xs.min()+578, xs.max()+578+1, ys.min()+12, ys.max()+12+1, hexc(np.median(sub[a>0.8],axis=0)))
print(np.round(a,1))
# bell white excluding red: use B channel
r = ink(566,14,592,39, chan=2); print('bell B', r['box'], r['color'])
# brand icon fill blue
r = ink(14,10,44,40, mode='dist', ref=[5,20,40]); print('icon', r['box'], r['color'])
print('icon fill', med(21,18,25,22), med(33,30,37,34))
# brand icon inner white glyph
r = ink(20,17,38,34, mode='bright', bgq=0.4); print('glyph', r['box'], r['color'])
