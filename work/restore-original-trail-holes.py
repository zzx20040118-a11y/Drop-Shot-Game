from pathlib import Path
import re,json,copy,uuid,shutil
root=Path('DropShot/Assets');folder=root/'VFX';scene=root/'Scenes/Game_B_Hard.unity';backup=Path('work/before-restore-original-trail');backup.mkdir(exist_ok=True);shutil.copy2(scene,backup/scene.name)
def uid():return uuid.uuid4().hex
def load(p):
 s=p.read_text(encoding='utf-8-sig');d=json.JSONDecoder();o=[]
 while s.strip():
  a,n=d.raw_decode(s.lstrip());o.append(a);s=s.lstrip()[n:]
 return o
original=load(root/'Shader/SG_BallTrail.shadergraph');objs=load(folder/'SG_PopPlaneTrail.shadergraph');g=objs[0];by={o['m_ObjectId']:o for o in objs}
alphaId='35deefe2cb404997a9ec8aa441f0874e';baseId='4396c65bee11452f8d92a791588ce02b'
last=next(e['m_OutputSlot']['m_Node']['m_Id'] for e in g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id']==alphaId)
masked=next(e['m_OutputSlot'] for e in g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id']==last and e['m_InputSlot']['m_SlotId']==0)
g['m_Edges'][:]=[e for e in g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id'] not in [alphaId,baseId]]
g['m_Edges'].append({'m_OutputSlot':masked,'m_InputSlot':{'m_Node':{'m_Id':alphaId},'m_SlotId':0}})
g['m_Edges'].append(copy.deepcopy(next(e for e in original[0]['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id']==baseId)))
# Keep only the original graph plus the hole-mask dependencies. No front overlay or tint changes.
needed=set()
def walk(id):
 if id in needed:return
 needed.add(id)
 for e in g['m_Edges']:
  if e['m_InputSlot']['m_Node']['m_Id']==id:walk(e['m_OutputSlot']['m_Node']['m_Id'])
for n in original[0]['m_Nodes']:walk(n['m_Id'])
g['m_Nodes']=[n for n in g['m_Nodes'] if n['m_Id'] in needed];g['m_Edges']=[e for e in g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id'] in needed and e['m_OutputSlot']['m_Node']['m_Id'] in needed]
properties={by[id]['m_Property']['m_Id'] for id in needed if 'm_Property' in by[id]};g['m_Properties']=[p for p in g['m_Properties'] if p['m_Id'] in properties];g['m_CategoryData']=[]
retain=needed|properties|{g['m_ObjectId']};retain.update(sl['m_Id'] for id in needed for sl in by[id].get('m_Slots',[]))
for o in original:
 if 'Target' in o['m_Type']:
  retain.add(o['m_ObjectId']);by[o['m_ObjectId']].clear();by[o['m_ObjectId']].update(o)
guid=uid();p=folder/'SG_BallTrailHoles.shadergraph';assert not p.exists();p.write_text('\n\n'.join(json.dumps(o,indent=4) for o in objs if o['m_ObjectId'] in retain)+'\n',encoding='utf-8');p.with_suffix('.shadergraph.meta').write_text((root/'Shader/SG_BallTrail.shadergraph.meta').read_text().replace('ccfe26423a37c1240b84dfc0294e2ff6',guid))
materials={}
for label,file in [('In','Shader Graphs_SG_BallTrail01 2.mat'),('Out','Shader Graphs_SG_BallTrail01 1.mat')]:
 src=root/'Material/M_BallTrail'/file;s=src.read_text();s=s.replace('ccfe26423a37c1240b84dfc0294e2ff6',guid)
 s=re.sub(r'(--- !u!21 [\s\S]*?  m_Name: )[^\n]*',lambda m:m[1]+'M_OriginalTrailHoles_'+label,s,count=1)
 s=s.replace('    m_Floats:\n','    m_Floats:\n    - _HoleSpeed: 0.45\n    - _HoleDensity: 1\n    - _HoleSize: 0.5\n')
 p=folder/('M_OriginalTrailHoles_'+label+'.mat');p.write_text(s);mg=uid();materials[label]=mg;p.with_suffix('.mat.meta').write_text('fileFormatVersion: 2\nguid: '+mg+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
s=scene.read_text(encoding='utf-8-sig');baseline=Path('work/before-comic-jump/Game_B_Hard.unity').read_text(encoding='utf-8-sig');old={re.search(r'&([0-9]+)',m[0])[1]:m[0] for m in re.finditer(r'--- !u!1001 &\d+\n.*?(?=--- !u!|\Z)',baseline,re.S)}
trailInfo={'3e2897e7c74bce04c89de42f7c248350':('In','5017178436238072501','96979239636276006'),'cfbea87fe3365ad4fb0f9742a8bc5934':('Out','449500957533937570','8701625324592318388')}
count=0
def restore(m):
 global count
 b=m[0];id=re.search(r'&(\d+)',b)[1]
 for pg,(label,go,renderer) in trailInfo.items():
  if f'm_SourcePrefab: {{fileID: 100100000, guid: {pg}, type: 3}}' not in b:continue
  pat=rf'    - target: \{{fileID: {go}, guid: {pg}, type: 3\}}\n      propertyPath: m_IsActive\n      value: \d+\n      objectReference: .*\n'
  originalActive=re.search(pat,old[id]);b=re.sub(pat,lambda _:originalActive[0] if originalActive else '',b)
  prop=rf"    - target: \{{fileID: {renderer}, guid: {pg}, type: 3\}}\n      propertyPath: '?m_Materials.Array.data\[0\]'?\n      value: *\n      objectReference: .*\n"
  entry=f'    - target: {{fileID: {renderer}, guid: {pg}, type: 3}}\n      propertyPath: m_Materials.Array.data[0]\n      value: \n      objectReference: {{fileID: 2100000, guid: {materials[label]}, type: 2}}\n'
  if re.search(prop,b):b=re.sub(prop,entry,b)
  else:b=b.replace('    m_RemovedComponents: []',entry+'    m_RemovedComponents: []')
  count+=1
 return b
s=re.sub(r'--- !u!1001 &\d+\n.*?(?=--- !u!|\Z)',restore,s,flags=re.S);assert count==4
# Remove only the two added Plane components; keep the older dot trial disabled.
for id in ['2149801022','2149801032']:
 s,n=re.subn(r'--- !u!114 &'+id+r'\n.*?(?=--- !u!|\Z)','',s,flags=re.S);assert n==1
 s,n=re.subn(r'  - component: \{fileID: '+id+r'\}\n','',s);assert n==1
scene.write_text(s,encoding='utf-8')
# Archive assets from the mistaken two-plane version, leaving the shared mask in place.
for relative in ['Scripts/BallPopPlaneTrail.cs','Scripts/BallPopPlaneTrail.cs.meta','VFX/SG_PopPlaneTrail.shadergraph','VFX/SG_PopPlaneTrail.shadergraph.meta','VFX/M_PopPlaneTrail.mat','VFX/M_PopPlaneTrail.mat.meta','VFX/M_PopPlaneFront.mat','VFX/M_PopPlaneFront.mat.meta']:
 src=root/relative;dst=backup/src.name;assert src.resolve().is_relative_to(root.resolve()) and not dst.exists();src.rename(dst)
# Confirm original colour/UV animation nodes are byte-for-byte equivalent in the new graph.
origby={o['m_ObjectId']:o for o in original}
for id in [n['m_Id'] for n in original[0]['m_Nodes']]:assert by[id]==origby[id]
print('PASS: all 4 original trail instances restored; original materials cloned with mask-only shader; new planes removed; original nodes unchanged.')
