from pathlib import Path
import copy,json,re,uuid,shutil,math
root=Path('DropShot/Assets');folder=root/'VFX';p=folder/'SG_BallTrailHoles.shadergraph';backup=Path('work/before-alternating-holes');backup.mkdir(exist_ok=True);shutil.copy2(p,backup/p.name)
def uid():return uuid.uuid4().hex
def load(p):
 t=p.read_text(encoding='utf-8-sig');d=json.JSONDecoder();o=[]
 while t.strip():
  a,n=d.raw_decode(t.lstrip());o.append(a);t=t.lstrip()[n:]
 return o
previous=load(p);prev={o['m_ObjectId']:o for o in previous};objs=load(root/'Shader/SG_BallTrail.shadergraph');g=objs[0];by={o['m_ObjectId']:o for o in objs}
# Use the original graph and append a small, analytic two-circle alpha mask.
floatTemplate=next(o for o in previous if o.get('m_DefaultReferenceName')=='_HoleSize')
slotFloat=next(o for o in previous if o['m_Type'].endswith('.Vector1MaterialSlot'))
def node(source,name,x,y):
 n=copy.deepcopy(source);n['m_ObjectId']=uid();n['m_Name']=name;n['m_DrawState']['m_Position'].update(x=x,y=y)
 for r in n['m_Slots']:
  sl=copy.deepcopy(by.get(r['m_Id'],prev.get(r['m_Id'])));sl['m_ObjectId']=uid();r['m_Id']=sl['m_ObjectId'];objs.append(sl);by[sl['m_ObjectId']]=sl
 objs.append(n);by[n['m_ObjectId']]=n;g['m_Nodes'].append({'m_Id':n['m_ObjectId']});return n
mulTemplate=by['ca04299d569b4270939700baff6e7d6c'];addTemplate=by['06a84b38ebba4c2897113433ba9916d4'];oneTemplate=by['67bca1383e514fdb870f4c922d2a5cd5']
def edge(a,sa,b,sb):g['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':a},'m_SlotId':sa},'m_InputSlot':{'m_Node':{'m_Id':b},'m_SlotId':sb}})
def constant(n,slotid,value):
 sl=next(by[r['m_Id']] for r in n['m_Slots'] if by[r['m_Id']]['m_Id']==slotid)
 if isinstance(sl.get('m_Value'),dict):sl['m_Value']={k:value for k in sl['m_Value']}
 else:sl['m_Value']=value

def scalar(name,ref,value,lo,hi,x,y):
 prop=copy.deepcopy(floatTemplate);prop.update(m_ObjectId=uid(),m_Name=name,m_DefaultReferenceName=ref,m_OverrideReferenceName=ref,m_Value=value,m_RangeValues={'x':lo,'y':hi});prop['m_Guid']['m_GuidSerialized']=str(uuid.uuid4());objs.append(prop);g['m_Properties'].append({'m_Id':prop['m_ObjectId']})
 n=node(by['10662360028741d2a833184fea82121c'],'Property',x,y);n['m_Property']['m_Id']=prop['m_ObjectId'];return n['m_ObjectId']
def point(name,ref,value,x,y):
 prop=copy.deepcopy(floatTemplate);prop.update(m_ObjectId=uid(),m_Type='UnityEditor.ShaderGraph.Internal.Vector2ShaderProperty',m_Name=name,m_DefaultReferenceName=ref,m_OverrideReferenceName=ref,m_Value={'x':value[0],'y':value[1]},m_Hidden=True);prop['m_Guid']['m_GuidSerialized']=str(uuid.uuid4());objs.append(prop);g['m_Properties'].append({'m_Id':prop['m_ObjectId']})
 n=node(by['10662360028741d2a833184fea82121c'],'Property',x,y);n['m_Property']['m_Id']=prop['m_ObjectId']
 sl=by[n['m_Slots'][0]['m_Id']];id=sl['m_ObjectId'];sl.clear();sl.update(m_SGVersion=0,m_Type='UnityEditor.ShaderGraph.Vector2MaterialSlot',m_ObjectId=id,m_Id=0,m_DisplayName=name,m_SlotType=1,m_Hidden=False,m_ShaderOutputName='Out',m_StageCapability=3,m_Value={'x':0.,'y':0.},m_DefaultValue={'x':0.,'y':0.},m_Labels=[])
 return n['m_ObjectId']
size=scalar('Hole Diameter','_HoleSize',.09,0,.2,-2200,1300);speed=scalar('Alternation Speed','_HoleSpeed',.9,.1,3,-2500,1000)
left=point('Hole Left','_HoleLeft',(.85,.25),-1400,1700);right=point('Hole Right','_HoleRight',(.92,.25),-1400,2100)
spSource=next(o for o in previous if o['m_Type'].endswith('.ScreenPositionNode'));sp=node(spSource,'Screen Position',-2600,500)
scSource=next(o for o in previous if o['m_Type'].endswith('.ScreenNode'));screen=node(scSource,'Screen',-2600,700)
div=node(mulTemplate,'Divide',-2300,700);div['m_Type']='UnityEditor.ShaderGraph.DivideNode';edge(screen['m_ObjectId'],0,div['m_ObjectId'],0);edge(screen['m_ObjectId'],1,div['m_ObjectId'],1)
split=node(by['3e123b80750d485b9827485c0f651d23'],'Split',-2300,400);edge(sp['m_ObjectId'],0,split['m_ObjectId'],0)
x=node(mulTemplate,'Multiply',-2000,400);edge(split['m_ObjectId'],1,x['m_ObjectId'],0);edge(div['m_ObjectId'],2,x['m_ObjectId'],1)
coords=node(by['4f76c4cb6da5404f8d2f5adae235705f'],'Combine',-1700,400);edge(x['m_ObjectId'],2,coords['m_ObjectId'],0);edge(split['m_ObjectId'],2,coords['m_ObjectId'],1)
phase=node(mulTemplate,'Multiply',-2200,1000);edge('ee6fcfa98e5d44a1b89814905557d2cd',0,phase['m_ObjectId'],0);edge(speed,0,phase['m_ObjectId'],1)
radians=node(mulTemplate,'Multiply',-1900,1000);edge(phase['m_ObjectId'],2,radians['m_ObjectId'],0);constant(radians,1,math.tau)
sine=node(oneTemplate,'Sine',-1650,1000);sine['m_Type']='UnityEditor.ShaderGraph.SineNode';edge(radians['m_ObjectId'],2,sine['m_ObjectId'],0)
negative=node(mulTemplate,'Multiply',-1650,1250);edge(sine['m_ObjectId'],1,negative['m_ObjectId'],0);constant(negative,1,-1)
def slot(n,id,name,type,output=False,value=0):
 sl=copy.deepcopy(slotFloat);sl.update(m_ObjectId=uid(),m_Id=id,m_DisplayName=name,m_ShaderOutputName=name,m_SlotType=1 if output else 0,m_Type='UnityEditor.ShaderGraph.'+type,m_Value=value,m_DefaultValue=value,m_StageCapability=2 if output else 3)
 if isinstance(value,dict):sl['m_Labels']=[]
 objs.append(sl);by[sl['m_ObjectId']]=sl;n['m_Slots'].append({'m_Id':sl['m_ObjectId']})
ellipses=[]
for i,(center,signal,signalSlot) in enumerate([(left,sine['m_ObjectId'],1),(right,negative['m_ObjectId'],2)]):
 clamp=node(oneTemplate,'Saturate',-1350,1000+i*650);clamp['m_Type']='UnityEditor.ShaderGraph.SaturateNode';edge(signal,signalSlot,clamp['m_ObjectId'],0)
 diameter=node(mulTemplate,'Multiply',-1100,1000+i*650);edge(clamp['m_ObjectId'],1,diameter['m_ObjectId'],0);edge(size,0,diameter['m_ObjectId'],1)
 safe=node(addTemplate,'Add',-850,1000+i*650);edge(diameter['m_ObjectId'],2,safe['m_ObjectId'],0);constant(safe,1,.00001)
 offset=node(by['d19ae4dd4b76478ca2d1953f762dd757'],'Subtract',-1100,1300+i*650);edge(coords['m_ObjectId'],6,offset['m_ObjectId'],0);edge(center,0,offset['m_ObjectId'],1)
 shift=node(addTemplate,'Add',-850,1300+i*650);edge(offset['m_ObjectId'],2,shift['m_ObjectId'],0);constant(shift,1,.5)
 ellipse=node(oneTemplate,'Ellipse',-550,1100+i*650);ellipse['m_Type']='UnityEditor.ShaderGraph.EllipseNode';ellipse['m_Slots']=[]
 slot(ellipse,0,'UV','Vector2MaterialSlot',value={'x':0.,'y':0.});slot(ellipse,2,'Width','Vector1MaterialSlot',value=.5);slot(ellipse,3,'Height','Vector1MaterialSlot',value=.5);slot(ellipse,4,'Out','Vector1MaterialSlot',True)
 edge(shift['m_ObjectId'],2,ellipse['m_ObjectId'],0);edge(safe['m_ObjectId'],2,ellipse['m_ObjectId'],2);edge(safe['m_ObjectId'],2,ellipse['m_ObjectId'],3);ellipses.append(ellipse['m_ObjectId'])
union=node(addTemplate,'Add',-200,1200);edge(ellipses[0],4,union['m_ObjectId'],0);edge(ellipses[1],4,union['m_ObjectId'],1)
clamp=node(oneTemplate,'Saturate',50,1200);clamp['m_Type']='UnityEditor.ShaderGraph.SaturateNode';edge(union['m_ObjectId'],2,clamp['m_ObjectId'],0)
remain=node(oneTemplate,'One Minus',300,1200);edge(clamp['m_ObjectId'],1,remain['m_ObjectId'],0)
alpha=node(mulTemplate,'Multiply',550,900);edge('45331618346546e284922e047a480971',7,alpha['m_ObjectId'],0);edge(remain['m_ObjectId'],1,alpha['m_ObjectId'],1)
g['m_Edges'][:]=[e for e in g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id']!='35deefe2cb404997a9ec8aa441f0874e'];edge(alpha['m_ObjectId'],2,'35deefe2cb404997a9ec8aa441f0874e',0)
by={o['m_ObjectId']:o for o in objs};assert len(by)==len(objs)
for e in g['m_Edges']:
 for k,expected in [('m_OutputSlot',1),('m_InputSlot',0)]:
  ep=e[k];n=by[ep['m_Node']['m_Id']];sl=next(by[r['m_Id']] for r in n['m_Slots'] if by[r['m_Id']]['m_Id']==ep['m_SlotId']);assert sl['m_SlotType']==expected
p.write_text('\n\n'.join(json.dumps(o,indent=4) for o in objs)+'\n',encoding='utf-8')
for name in ['M_OriginalTrailHoles_In.mat','M_OriginalTrailHoles_Out.mat']:
 p=folder/name;shutil.copy2(p,backup/name);t=p.read_text();t=re.sub(r'    - _HoleSize: .*','    - _HoleSize: 0.09',t);t=re.sub(r'    - _HoleSpeed: .*','    - _HoleSpeed: 0.9',t);p.write_text(t)
guid=uid();(root/'Scripts/TrailHolePositions.cs.meta').write_text('fileFormatVersion: 2\nguid: '+guid+'\n')
p=root/'Scenes/Game_B_Hard.unity';shutil.copy2(p,backup/p.name);t=p.read_text(encoding='utf-8-sig');blocks=''
for id,go,transform in [('2149801042','953533038','953533042'),('2149801052','666114097','666114098')]:
 m=re.search(r'--- !u!1 &'+go+r'\n.*?(?=--- !u!)',t,re.S);assert m;b=m[0].replace('  m_Layer: 0',f'  - component: {{fileID: {id}}}\n  m_Layer: 0');t=t[:m.start()]+b+t[m.end():]
 blocks+=f'''--- !u!114 &{id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::TrailHolePositions
  targetCamera: {{fileID: 50635348}}
  ball: {{fileID: {transform}}}
  sideOffset: 0.035
  downOffset: 0.08
'''
t=t.replace('--- !u!1660057539 &9223372036854775807',blocks+'--- !u!1660057539 &9223372036854775807');p.write_text(t,encoding='utf-8')
print('Analytic left/right circles replace texture mask. Alternating sine gates; common centers supplied to original inner/outer renderers.')
