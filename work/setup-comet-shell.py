from pathlib import Path
import json,copy,uuid,re,shutil
root=Path('DropShot/Assets'); folder=root/'Models/BallEffect'; scene=root/'Scenes/Game_B_Hard.unity'
backup=Path('work/before-comet-shell');backup.mkdir(exist_ok=True);shutil.copy2(scene,backup/scene.name)
def uid():return uuid.uuid4().hex
def load(p):
 s=p.read_text(encoding='utf-8-sig');d=json.JSONDecoder();a=[]
 while s.strip():
  o,n=d.raw_decode(s.lstrip());a.append(o);s=s.lstrip()[n:]
 return a
source=root/'Shader/HalfTone/SG_HalfTone.shadergraph';objs=load(source);g=objs[0];by={o['m_ObjectId']:o for o in objs}
extras=load(root/'Shader/SG_BallTrail.shadergraph')+load(root/'Shader/SG_LongNote_Jelly.shadergraph');templates={o['m_ObjectId']:o for o in extras};templates.update(by)
def find(t):return next(o for o in templates.values() if o['m_Type'].endswith('.'+t))
def node(t,x=0,y=0):
 n=copy.deepcopy(find(t));n['m_ObjectId']=uid();n['m_DrawState']['m_Position'].update(x=x,y=y)
 for r in n['m_Slots']:
  sl=copy.deepcopy(templates[r['m_Id']]);sl['m_ObjectId']=uid();r['m_Id']=sl['m_ObjectId'];objs.append(sl);by[sl['m_ObjectId']]=sl
 objs.append(n);by[n['m_ObjectId']]=n;g['m_Nodes'].append({'m_Id':n['m_ObjectId']});return n['m_ObjectId']
def edge(a,sa,b,sb):g['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':a},'m_SlotId':sa},'m_InputSlot':{'m_Node':{'m_Id':b},'m_SlotId':sb}})
def scalar(name,ref,value,lo,hi):
 p=copy.deepcopy(find('Vector1ShaderProperty'));p.update(m_ObjectId=uid(),m_Name=name,m_DefaultReferenceName=ref,m_OverrideReferenceName=ref,m_RefNameGeneratedByDisplayName=ref[1:],m_Value=value,m_RangeValues={'x':lo,'y':hi});p['m_Guid']['m_GuidSerialized']=str(uuid.uuid4());objs.append(p);by[p['m_ObjectId']]=p;g['m_Properties'].append({'m_Id':p['m_ObjectId']})
 # Clone a scalar PropertyNode, not a colour PropertyNode.
 template=next(o for o in templates.values() if o['m_Type'].endswith('.PropertyNode') and templates[o['m_Property']['m_Id']]['m_Type'].endswith('.Vector1ShaderProperty'))
 n=copy.deepcopy(template);n['m_ObjectId']=uid();sl=copy.deepcopy(templates[n['m_Slots'][0]['m_Id']]);sl['m_ObjectId']=uid();n['m_Slots']=[{'m_Id':sl['m_ObjectId']}];n['m_Property']={'m_Id':p['m_ObjectId']};objs.extend([sl,n]);by[sl['m_ObjectId']]=sl;by[n['m_ObjectId']]=n;g['m_Nodes'].append({'m_Id':n['m_ObjectId']})
 if g.get('m_CategoryData'):by[g['m_CategoryData'][0]['m_Id']]['m_ChildObjectList'].append({'m_Id':p['m_ObjectId']})
 return n['m_ObjectId']
opacity=scalar('Opacity','_Opacity',.32,0,1);amount=scalar('Sway Amount','_SwayAmount',.12,0,.5);speed=scalar('Sway Speed','_SwaySpeed',3,0,10)
start=scalar('Tail Start (Automatic)','_TailStart',-.2,-10,10);end=scalar('Tail End (Automatic)','_TailEnd',3.95,-10,10);radius=scalar('Mesh Radius (Automatic)','_MeshRadius',1,0,10)
pos=node('PositionNode',-2000,1700);by[pos]['m_Space']=0
split=node('SplitNode',-1800,1700);edge(pos,0,split,0)
weight=node('SmoothstepNode',-1500,1700);edge(start,0,weight,0);edge(end,0,weight,1);edge(split,3,weight,2)
time=node('TimeNode',-2000,2200);tick=node('MultiplyNode',-1750,2200);edge(time,0,tick,0);edge(speed,0,tick,1)
phase=node('AddNode',-1500,2200);edge(tick,2,phase,0);edge(split,3,phase,1)
sine=node('SineNode',-1250,2200);edge(phase,2,sine,0)
a=node('MultiplyNode',-1000,2200);edge(sine,1,a,0);edge(amount,0,a,1)
b=node('MultiplyNode',-750,2200);edge(a,2,b,0);edge(weight,3,b,1)
c=node('MultiplyNode',-500,2200);edge(b,2,c,0);edge(radius,0,c,1)
combine=node('CombineNode',-250,2200)
for ref in by[combine]['m_Slots']:
 sl=by[ref['m_Id']]
 if sl['m_SlotType']==0:sl['m_Value']=0.0
edge(c,2,combine,0)
vertex='0875ff7e0b784f329d365d4c8ac1b62c';alpha='3a25cca564cb4027b601c2075591c4e1'
for block,typ,sourceSlot in [(vertex,'AddNode',5),(alpha,'MultiplyNode',0)]:
 old=next(e for e in g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id']==block);g['m_Edges'].remove(old)
 n=node(typ,200,2200 if block==vertex else 2600);g['m_Edges'].append({'m_OutputSlot':old['m_OutputSlot'],'m_InputSlot':{'m_Node':{'m_Id':n},'m_SlotId':0}})
 edge(combine if block==vertex else opacity,sourceSlot,n,1);edge(n,2,block,0)
for o in objs:
 if o['m_Type'].endswith('.UniversalTarget'):o.update(m_AllowMaterialOverride=False,m_SurfaceType=1,m_ZWriteControl=2,m_AlphaMode=0,m_RenderFace=2,m_CastShadows=False,m_ReceiveShadows=False)
g['m_Path']='DropShot/Comet'
# Check every reference, including graph categories and every edge direction.
ids={o['m_ObjectId'] for o in objs};assert len(ids)==len(objs)
def check(v):
 if isinstance(v,dict):
  if set(v)=={'m_Id'} and isinstance(v['m_Id'],str) and v['m_Id']:assert v['m_Id'] in ids,v
  for x in v.values():check(x)
 elif isinstance(v,list):
  for x in v:check(x)
for o in objs:check(o)
for e in g['m_Edges']:
 for k,direction in [('m_InputSlot',0),('m_OutputSlot',1)]:
  ep=e[k];n=by[ep['m_Node']['m_Id']];sl=next(by[r['m_Id']] for r in n['m_Slots'] if by[r['m_Id']]['m_Id']==ep['m_SlotId']);assert sl['m_SlotType']==direction,(n['m_Name'],ep)
sg=uid();mp=uid();sc=uid();p=folder/'SG_CometShell.shadergraph';assert not p.exists();p.write_text('\n\n'.join(json.dumps(o,indent=4) for o in objs)+'\n',encoding='utf-8');p.with_suffix('.shadergraph.meta').write_text(source.with_suffix('.shadergraph.meta').read_text().replace('6e7bb58b6c240f4408370ffbfc67e63c',sg))
s=(root/'Material/M_Golf.mat').read_text();s=s.replace('6e7bb58b6c240f4408370ffbfc67e63c',sg);s=re.sub(r'(--- !u!21 [\s\S]*?  m_Name: )[^\n]*',lambda m:m[1]+'M_CometShell',s,count=1);s=re.sub(r'm_CustomRenderQueue: -?\d+','m_CustomRenderQueue: 3000',s)
s=s.replace('    m_Floats:\n','    m_Floats:\n    - _Opacity: 0.32\n    - _SwayAmount: 0.12\n    - _SwaySpeed: 3\n    - _TailStart: -0.2\n    - _TailEnd: 3.95\n    - _MeshRadius: 1\n')
for key,val in [('_RimWidth','0'),('_Glow','0'),('_Surface','1'),('_ZWrite','0'),('_SrcBlend','5'),('_DstBlend','10'),('_Cull','2')]:
 s=re.sub(r'(    - '+key+r': )[^\n]+',lambda m:m[1]+val,s)
s=re.sub(r'(    - _BaseColor: )[^\n]+',lambda m:m[1]+'{r: 1, g: 0.8, b: 0.37, a: 1}',s)
p=folder/'M_CometShell.mat';p.write_text(s);p.with_suffix('.mat.meta').write_text('fileFormatVersion: 2\nguid: '+mp+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
(root/'Scripts/BallCometShell.cs.meta').write_text('fileFormatVersion: 2\nguid: '+sc+'\n')
s=scene.read_text(encoding='utf-8-sig');blocks=''
for go,renderer,id,rolling in [(953533038,323064026,2149801102,1),(666114097,666114106,2149801112,0)]:
 pat=rf'--- !u!1 &{go}\n.*?(?=--- !u!)';m=re.search(pat,s,re.S);assert m;b=m[0].replace('  m_Layer:',f'  - component: {{fileID: {id}}}\n  m_Layer:',1);s=s[:m.start()]+b+s[m.end():]
 blocks+=f'''--- !u!114 &{id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {sc}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::BallCometShell
  modelPrefab: {{fileID: 919132149155446097, guid: 661a58fe35d181840aa8e6dcc0a0e3b6, type: 3}}
  shellMaterial: {{fileID: 2100000, guid: {mp}, type: 2}}
  ballRenderer: {{fileID: {renderer}}}
  flight: {{fileID: 666114104}}
  rollingBall: {rolling}
  size: 1.08
  turnSpeed: 12
'''
s=s.replace('--- !u!1660057539 &9223372036854775807',blocks+'--- !u!1660057539 &9223372036854775807')
count=0
def hide(m):
 global count
 b=m[0]
 for guid,go in [('3e2897e7c74bce04c89de42f7c248350','5017178436238072501'),('cfbea87fe3365ad4fb0f9742a8bc5934','449500957533937570')]:
  if f'm_SourcePrefab: {{fileID: 100100000, guid: {guid}, type: 3}}' not in b:continue
  pat=rf'    - target: \{{fileID: {go}, guid: {guid}, type: 3\}}\n      propertyPath: m_IsActive\n      value: \d+\n      objectReference: .*\n'
  entry=f'    - target: {{fileID: {go}, guid: {guid}, type: 3}}\n      propertyPath: m_IsActive\n      value: 0\n      objectReference: {{fileID: 0}}\n'
  b=re.sub(pat,lambda _:entry,b) if re.search(pat,b) else b.replace('    m_RemovedComponents: []',entry+'    m_RemovedComponents: []');count+=1
 return b
s=re.sub(r'--- !u!1001 &\d+\n.*?(?=--- !u!|\Z)',hide,s,flags=re.S);assert count==4
scene.write_text(s,encoding='utf-8')
print('PASS: full graph references and slot directions; created independent shell shader/material; attached to both B Hard balls; disabled four original trails.')

