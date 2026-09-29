import json,sys
p=sys.argv[1]
s=json.load(open(p,encoding='utf-8'))
print(s['page'],s['size'])
for e in s['elements']:
    b=e['box']
    extra={k:v for k,v in e.items() if k not in('id','kind','box','notes','group','remove')}
    print(f"{e['id']:28s} {e['kind']:7s} [{b[0]:.1f},{b[1]:.1f},{b[2]:.1f},{b[3]:.1f}] rm={e.get('remove')} {json.dumps(extra,ensure_ascii=False)}")
    if len(sys.argv)>2: print('     N:',e.get('notes',''))
print('character',json.dumps(s.get('character'),ensure_ascii=False))
for p in s.get('removePolygons',[]):
    print('poly',p['name'],p.get('pad'),p['points'])
for a in s.get('assets',[]):
    print('asset',json.dumps(a,ensure_ascii=False))
