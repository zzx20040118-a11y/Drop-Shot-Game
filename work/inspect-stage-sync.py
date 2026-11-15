from pathlib import Path
import re,json
roots={'889800011','889800021','889800031','99707239','889800001'}
def parse(s):return {m[1]:m[0] for m in re.finditer(r'--- !u!\d+ &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)',s,re.S)}
def closure(bs):
 selected=set(roots)
 while True:
  old=len(selected)
  for k,b in bs.items():
   father=re.search(r'  m_(?:Father|TransformParent): \{fileID: (-?\d+)\}',b)
   if father and father[1] in selected:selected.add(k)
   pi=re.search(r'  m_PrefabInstance: \{fileID: (-?\d+)\}',b)
   if pi and pi[1]!='0' and pi[1] in selected:selected.add(k)
   if k in selected:
    go=re.search(r'  m_GameObject: \{fileID: (-?\d+)\}',b)
    if go and go[1]!='0':selected.add(go[1])
    selected.update(re.findall(r'  - component: \{fileID: (-?\d+)\}',b))
    children=re.search(r'  m_Children:\n(.*?)  m_Father:',b,re.S)
    if children:selected.update(re.findall(r'fileID: (-?\d+)',children[1]))
  if len(selected)==old:break
 assert selected<=set(bs),selected-set(bs)
 return selected
source=parse(Path('DropShot/Assets/Scenes/Game_B_Hard.unity').read_text(encoding='utf-8-sig'));ss=closure(source)
print('Source blocks:',len(ss))
external=set()
for k in ss:external.update(x for x in re.findall(r'\{fileID: (-?\d+)\}',source[k]) if x!='0' and x not in ss)
print('External scene references:',external)
for p in Path('DropShot/Assets/Scenes').glob('Game_*.unity'):
 bs=parse(p.read_text(encoding='utf-8-sig'));target=closure(bs)
 print(p.name,'old',len(target),'new',len(ss),'replaced',len(ss&target),'removed',len(target-ss),'added',len(ss-target),'collisions',ss& (set(bs)-target),'missing external',external-set(bs))
 for k,b in bs.items():
  if '  stageSwitcher:' in b:print('spawner',k,re.search(r'  stageSwitcher: [^\n]*',b)[0])
Path('work/stage-sync-helper.py').write_text('')
Path('work/stage-sync-selection.json').write_text(json.dumps(sorted(ss)))
Path('work/stage-sync-closure.py').write_text('')
