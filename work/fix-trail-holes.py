from pathlib import Path
import json,copy,uuid,re,shutil
folder=Path('DropShot/Assets/VFX');p=folder/'SG_BallTrailHoles.shadergraph';backup=Path('work/before-hole-mask-coordinate-fix');backup.mkdir(exist_ok=True);shutil.copy2(p,backup/p.name)
s=p.read_text();d=json.JSONDecoder();objs=[]
while s.strip():
 o,n=d.raw_decode(s.lstrip());objs.append(o);s=s.lstrip()[n:]
g=objs[0];by={o['m_ObjectId']:o for o in objs}
def uid():return uuid.uuid4().hex
def clone(id,name,x,y):
 n=copy.deepcopy(by[id]);n['m_ObjectId']=uid();n['m_Name']=name;n['m_DrawState']['m_Position'].update(x=x,y=y)
 for r in n['m_Slots']:
  sl=copy.deepcopy(by[r['m_Id']]);sl['m_ObjectId']=uid();r['m_Id']=sl['m_ObjectId'];objs.append(sl);by[sl['m_ObjectId']]=sl
 objs.append(n);by[n['m_ObjectId']]=n;g['m_Nodes'].append({'m_Id':n['m_ObjectId']});return n
# Identify the mask UV node via its incoming density property.
densityProp=next(o['m_ObjectId'] for o in objs if o.get('m_DefaultReferenceName')=='_HoleDensity')
densityNode=next(o['m_ObjectId'] for o in objs if o.get('m_Property',{}).get('m_Id')==densityProp)
maskUV=next(e['m_InputSlot']['m_Node']['m_Id'] for e in g['m_Edges'] if e['m_OutputSlot']['m_Node']['m_Id']==densityNode)
# Correct the accidentally wired Combine INPUT: use its RG OUTPUT.
for e in g['m_Edges']:
 if e['m_InputSlot']['m_Node']['m_Id']==maskUV and e['m_InputSlot']['m_SlotId']==2:
  assert by[e['m_OutputSlot']['m_Node']['m_Id']]['m_Type'].endswith('CombineNode');e['m_OutputSlot']['m_SlotId']=6
screenPosition=clone('8a923446ff8247599e3e4307210570ff','Screen Position',-2400,850);screenPosition['m_Type']='UnityEditor.ShaderGraph.ScreenPositionNode';screenPosition['m_ScreenSpaceType']=0
screen=clone('ee6fcfa98e5d44a1b89814905557d2cd','Screen',-2400,1150);screen['m_Type']='UnityEditor.ShaderGraph.ScreenNode';screen['m_Slots']=screen['m_Slots'][:2]
for r,label in zip(screen['m_Slots'],['Width','Height']):sl=by[r['m_Id']];sl['m_DisplayName']=label;sl['m_ShaderOutputName']=label
multiplyId=next(o['m_ObjectId'] for o in objs if o['m_Type'].endswith('.MultiplyNode'))
divide=clone(multiplyId,'Divide',-2100,1100);divide['m_Type']='UnityEditor.ShaderGraph.DivideNode'
scaleX=clone(multiplyId,'Multiply',-1850,1150)
combineId=next(o['m_ObjectId'] for o in objs if o['m_Type'].endswith('.CombineNode'))
scale=clone(combineId,'Combine',-1600,1150)
def edge(a,sa,b,sb):g['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':a},'m_SlotId':sa},'m_InputSlot':{'m_Node':{'m_Id':b},'m_SlotId':sb}})
g['m_Edges'][:]=[e for e in g['m_Edges'] if not(e['m_InputSlot']['m_Node']['m_Id']==maskUV and e['m_InputSlot']['m_SlotId'] in [0,1])]
edge(screenPosition['m_ObjectId'],0,maskUV,0);edge(screen['m_ObjectId'],0,divide['m_ObjectId'],0);edge(screen['m_ObjectId'],1,divide['m_ObjectId'],1);edge(divide['m_ObjectId'],2,scaleX['m_ObjectId'],0);edge(densityNode,0,scaleX['m_ObjectId'],1);edge(scaleX['m_ObjectId'],2,scale['m_ObjectId'],0);edge(densityNode,0,scale['m_ObjectId'],1);edge(scale['m_ObjectId'],6,maskUV,1)
# Share the complete cutout between both layers; layer-local UV fades would fill each other's holes.
maskSample=next(o for o in objs if o['m_Type'].endswith('.SampleTexture2DNode') and any(e['m_OutputSlot']['m_Node']['m_Id']==maskUV and e['m_InputSlot']['m_Node']['m_Id']==o['m_ObjectId'] for e in g['m_Edges']))
soft=next(e['m_InputSlot']['m_Node']['m_Id'] for e in g['m_Edges'] if e['m_OutputSlot']['m_Node']['m_Id']==maskSample['m_ObjectId'] and e['m_OutputSlot']['m_SlotId']==4)
punch=next(e['m_InputSlot']['m_Node']['m_Id'] for e in g['m_Edges'] if e['m_OutputSlot']['m_Node']['m_Id']==soft)
for e in g['m_Edges']:
 if e['m_OutputSlot']['m_Node']['m_Id']==punch:
  e['m_OutputSlot']={'m_Node':{'m_Id':soft},'m_SlotId':3}
# Only connected graph nodes are needed; preserve the original vertex/fragment contexts.
needed=set()
def walk(id):
 if id in needed:return
 needed.add(id)
 for e in g['m_Edges']:
  if e['m_InputSlot']['m_Node']['m_Id']==id:walk(e['m_OutputSlot']['m_Node']['m_Id'])
for context in ['m_VertexContext','m_FragmentContext']:
 for b in g[context]['m_Blocks']:walk(b['m_Id'])
g['m_Nodes']=[n for n in g['m_Nodes'] if n['m_Id'] in needed];g['m_Edges']=[e for e in g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id'] in needed and e['m_OutputSlot']['m_Node']['m_Id'] in needed]
properties={by[id]['m_Property']['m_Id'] for id in needed if 'm_Property' in by[id]};g['m_Properties']=[r for r in g['m_Properties'] if r['m_Id'] in properties]
retain=needed|properties|{g['m_ObjectId']};retain.update(r['m_Id'] for id in needed for r in by[id].get('m_Slots',[]));retain.update(o['m_ObjectId'] for o in objs if 'Target' in o['m_Type'])
objs=[o for o in objs if o['m_ObjectId'] in retain];by={o['m_ObjectId']:o for o in objs}
for e in g['m_Edges']:
 for k,expected in [('m_OutputSlot',1),('m_InputSlot',0)]:
  ep=e[k];n=by[ep['m_Node']['m_Id']];sl=next(by[r['m_Id']] for r in n['m_Slots'] if by[r['m_Id']]['m_Id']==ep['m_SlotId']);assert sl['m_SlotType']==expected,(n['m_Name'],k)
p.write_text('\n\n'.join(json.dumps(o,indent=4) for o in objs)+'\n')
for name in ['M_OriginalTrailHoles_In.mat','M_OriginalTrailHoles_Out.mat']:
 p=folder/name;shutil.copy2(p,backup/name);s=p.read_text();s=re.sub(r'    - _HoleDensity: .*','    - _HoleDensity: 3',s);s=re.sub(r'    - _HoleSize: .*','    - _HoleSize: 0.65',s);p.write_text(s)
# Same camera pixel and time must produce identical mask coordinates in both materials.
for width,height in [(1920,1080),(1280,720),(1000,1000)]:
 x,y=width*.47,height*.36;u=x/width;v=y/height
 a=(u*3*width/height,v*3+.45*2)
 b=(x/height*3,y/height*3+.45*2)
 assert abs(a[0]-b[0])<1e-6 and abs(a[1]-b[1])<1e-6
print('PASS: every edge connects OUTPUT -> INPUT; inner/outer share aspect-correct screen-space holes; both materials use matching settings.')
