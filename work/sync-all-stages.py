from pathlib import Path
import re,json,shutil,hashlib
# Reuse the inspected hierarchy traversal without its reporting section.
sourcecode=Path('work/inspect-stage-sync.py').read_text(encoding='utf-8-sig');exec(sourcecode.split('source=parse(')[0])
sourcepath=Path('DropShot/Assets/Scenes/Game_B_Hard.unity');sourcestr=sourcepath.read_text(encoding='utf-8-sig');source=parse(sourcestr);selected=closure(source);sourcehash=hashlib.sha256(sourcepath.read_bytes()).hexdigest()
backup=Path('work/before-sync-all-stages');backup.mkdir(exist_ok=False);shutil.copy2(sourcepath,backup/sourcepath.name)
for path in sorted(Path('DropShot/Assets/Scenes').glob('Game_*.unity')):
 if path==sourcepath:continue
 raw=path.read_bytes();s=raw.decode('utf-8-sig');bs=parse(s);old=closure(bs);removed=old-selected
 # No gameplay components may reference an object removed by the stage replacement.
 for k,b in bs.items():
  if k not in old:
   refs=set(re.findall(r'\{fileID: (-?\d+)\}',b));assert not refs&removed,(path.name,k,refs&removed)
 assert not selected&(set(bs)-old)
 shutil.copy2(path,backup/path.name)
 def replace(m):
  id=m[1]
  if id not in old:return m[0]
  return source.get(id,'')
 s=re.sub(r'--- !u!\d+ &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)',replace,s,flags=re.S)
 added=''.join(source[k] for k in source if k in selected-old)
 marker='--- !u!1660057539 &9223372036854775807';assert s.count(marker)==1;s=s.replace(marker,added+marker)
 pat=r'(--- !u!114 &881995960\n.*?)(?=--- !u!|\Z)';m=re.search(pat,s,re.S);assert m
 b,n=re.subn(r'  stageSwitcher: \{fileID: \d+\}', '  stageSwitcher: {fileID: 889800002}',m[0]);assert n==1;s=s[:m.start()]+b+s[m.end():]
 path.write_bytes((b'\xef\xbb\xbf' if raw.startswith(b'\xef\xbb\xbf') else b'')+s.encode('utf-8'))
 after=parse(s);assert len(after)==len(re.findall(r'^--- !u!',s,re.M))
 assert closure(after)==selected
 for k in selected:assert after[k]==source[k],(path.name,k)
 # Entire gameplay, UI, input, difficulty, lighting and scene root lists remain unchanged.
 for k in set(bs)-old-{'881995960'}:assert after[k]==bs[k],(path.name,k)
 assert after['881995960'].replace('  stageSwitcher: {fileID: 889800002}',re.search(r'  stageSwitcher: [^\n]+',bs['881995960'])[0])==bs['881995960']
 assert all(after[k]==source[k] for k in ['889800012','889800022','889800032','889800002'])
 print(path.name+': PASS; all 3 stage hierarchies and glow settings match B Hard; switcher linked; every other existing block unchanged.')
assert hashlib.sha256(sourcepath.read_bytes()).hexdigest()==sourcehash
print('B Hard unchanged; backups saved in',backup)
