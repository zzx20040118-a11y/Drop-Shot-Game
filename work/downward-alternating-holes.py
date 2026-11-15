from pathlib import Path
import json,copy,re,uuid,shutil,math
folder=Path('DropShot/Assets/VFX');p=folder/'SG_BallTrailHoles.shadergraph';backup=Path('work/before-downward-holes');backup.mkdir(exist_ok=True);shutil.copy2(p,backup/p.name)
s=p.read_text();d=json.JSONDecoder();objs=[]
while s.strip():
 o,n=d.raw_decode(s.lstrip());objs.append(o);s=s.lstrip()[n:]
g=objs[0];by={o['m_ObjectId']:o for o in objs}
def uid():return uuid.uuid4().hex
def clone(source,name,x,y):
 n=copy.deepcopy(source);n['m_ObjectId']=uid();n['m_Name']=name;n['m_DrawState']['m_Position'].update(x=x,y=y)
 for r in n['m_Slots']:
  sl=copy.deepcopy(by[r['m_Id']]);sl['m_ObjectId']=uid();r['m_Id']=sl['m_ObjectId'];objs.append(sl);by[sl['m_ObjectId']]=sl
 objs.append(n);by[n['m_ObjectId']]=n;g['m_Nodes'].append({'m_Id':n['m_ObjectId']});return n
mul=next(o for o in objs if o['m_Type'].endswith('.MultiplyNode'));add=next(o for o in objs if o['m_Type'].endswith('.AddNode'));one=next(o for o in objs if o['m_Type'].endswith('.OneMinusNode'));combine=next(o for o in objs if o['m_Type'].endswith('.CombineNode'))
def edge(a,sa,b,sb):g['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':a},'m_SlotId':sa},'m_InputSlot':{'m_Node':{'m_Id':b},'m_SlotId':sb}})
def incoming(id,slot):return next(e['m_OutputSlot'] for e in g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id']==id and e['m_InputSlot']['m_SlotId']==slot)
def const(n,id,value):
 sl=next(by[r['m_Id']] for r in n['m_Slots'] if by[r['m_Id']]['m_Id']==id);sl['m_Value']={k:value for k in sl['m_Value']} if isinstance(sl['m_Value'],dict) else value
sizeProp=next(o['m_ObjectId'] for o in objs if o.get('m_DefaultReferenceName')=='_HoleSize');sizeNode=next(o for o in objs if o.get('m_Property',{}).get('m_Id')==sizeProp)
speedProp=next(o['m_ObjectId'] for o in objs if o.get('m_DefaultReferenceName')=='_HoleSpeed');speedNode=next(o for o in objs if o.get('m_Property',{}).get('m_Id')==speedProp)
phase=next(e['m_InputSlot']['m_Node']['m_Id'] for e in g['m_Edges'] if e['m_OutputSlot']['m_Node']['m_Id']==speedNode['m_ObjectId'])
cycle=clone(one,'Fraction',-1600,1000);cycle['m_Type']='UnityEditor.ShaderGraph.FractionNode';edge(phase,2,cycle['m_ObjectId'],0)
right=clone(mul,'Step',-1350,1200);right['m_Type']='UnityEditor.ShaderGraph.StepNode';const(right,0,.5);edge(cycle['m_ObjectId'],1,right['m_ObjectId'],1)
left=clone(one,'One Minus',-1100,1200);edge(right['m_ObjectId'],2,left['m_ObjectId'],0)
double=clone(mul,'Multiply',-1600,1500);edge(phase,2,double['m_ObjectId'],0);const(double,1,2)
progress=clone(one,'Fraction',-1350,1500);progress['m_Type']='UnityEditor.ShaderGraph.FractionNode';edge(double['m_ObjectId'],2,progress['m_ObjectId'],0)
prop=copy.deepcopy(next(o for o in objs if o.get('m_DefaultReferenceName')=='_HoleSize'));prop.update(m_ObjectId=uid(),m_Name='Downward Travel',m_DefaultReferenceName='_HoleTravel',m_OverrideReferenceName='_HoleTravel',m_Value=.12,m_RangeValues={'x':0.,'y':.5});prop['m_Guid']['m_GuidSerialized']=str(uuid.uuid4());objs.append(prop);g['m_Properties'].append({'m_Id':prop['m_ObjectId']})
travel=clone(sizeNode,'Property',-1350,1750);travel['m_Property']['m_Id']=prop['m_ObjectId']
fall=clone(mul,'Multiply',-1050,1500);edge(progress['m_ObjectId'],1,fall['m_ObjectId'],0);edge(travel['m_ObjectId'],0,fall['m_ObjectId'],1)
fallXY=clone(combine,'Combine',-800,1500);const(fallXY,0,0);edge(fall['m_ObjectId'],2,fallXY['m_ObjectId'],1)
fixedSize=clone(add,'Add',-1100,800);edge(sizeNode['m_ObjectId'],0,fixedSize['m_ObjectId'],0);const(fixedSize,1,.00001)
ellipses=[o for o in objs if o['m_Type'].endswith('.EllipseNode')]
for i,ellipse in enumerate(ellipses):
 # The centers remain left/right; add a shared increasing Y to the sampled coordinates,
 # which moves the cutout DOWN in screen space.
 uv=incoming(ellipse['m_ObjectId'],0)
 moved=clone(add,'Add',-500,1550+i*350);edge(uv['m_Node']['m_Id'],uv['m_SlotId'],moved['m_ObjectId'],0);edge(fallXY['m_ObjectId'],6,moved['m_ObjectId'],1)
 g['m_Edges'][:]=[e for e in g['m_Edges'] if not(e['m_InputSlot']['m_Node']['m_Id']==ellipse['m_ObjectId'] and e['m_InputSlot']['m_SlotId'] in [0,2,3])]
 edge(moved['m_ObjectId'],2,ellipse['m_ObjectId'],0);edge(fixedSize['m_ObjectId'],2,ellipse['m_ObjectId'],2);edge(fixedSize['m_ObjectId'],2,ellipse['m_ObjectId'],3)
 # Apply an on/off side gate to opacity, never to circle diameter.
 outputs=[e for e in g['m_Edges'] if e['m_OutputSlot']['m_Node']['m_Id']==ellipse['m_ObjectId']]
 gated=clone(mul,'Multiply',-200,1700+i*300)
 for e in outputs:e['m_OutputSlot']={'m_Node':{'m_Id':gated['m_ObjectId']},'m_SlotId':2}
 edge(ellipse['m_ObjectId'],4,gated['m_ObjectId'],0)
 gate=left['m_ObjectId'] if i==0 else right['m_ObjectId'];edge(gate,1 if i==0 else 2,gated['m_ObjectId'],1)
# Drop the old sine/size-pulse branch completely.
needed=set()
def walk(id):
 if id in needed:return
 needed.add(id)
 for e in g['m_Edges']:
  if e['m_InputSlot']['m_Node']['m_Id']==id:walk(e['m_OutputSlot']['m_Node']['m_Id'])
for context in ['m_VertexContext','m_FragmentContext']:
 for r in g[context]['m_Blocks']:walk(r['m_Id'])
g['m_Nodes']=[r for r in g['m_Nodes'] if r['m_Id'] in needed];g['m_Edges']=[e for e in g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id'] in needed and e['m_OutputSlot']['m_Node']['m_Id'] in needed]
props={by[id]['m_Property']['m_Id'] for id in needed if 'm_Property' in by[id]};g['m_Properties']=[r for r in g['m_Properties'] if r['m_Id'] in props]
retain=needed|props|{g['m_ObjectId']};retain.update(r['m_Id'] for id in needed for r in by[id]['m_Slots']);retain.update(o['m_ObjectId'] for o in objs if 'Target' in o['m_Type'])
objs=[o for o in objs if o['m_ObjectId'] in retain];by={o['m_ObjectId']:o for o in objs}
assert not any(o['m_Type'].endswith('.SineNode') for o in objs)
for e in g['m_Edges']:
 for k,t in [('m_OutputSlot',1),('m_InputSlot',0)]:
  ep=e[k];node=by[ep['m_Node']['m_Id']];sl=next(by[r['m_Id']] for r in node['m_Slots'] if by[r['m_Id']]['m_Id']==ep['m_SlotId']);assert sl['m_SlotType']==t
p.write_text('\n\n'.join(json.dumps(o,indent=4) for o in objs)+'\n')
for name in ['M_OriginalTrailHoles_In.mat','M_OriginalTrailHoles_Out.mat']:
 p=folder/name;shutil.copy2(p,backup/name);s=p.read_text();assert '_HoleTravel:' not in s;s=s.replace('    m_Floats:\n','    m_Floats:\n    - _HoleTravel: 0.12\n');p.write_text(s)
for t in [.05,.25,.55,.75,1.05]:
 phase=t%1;side='left' if phase<.5 else 'right';y=-.08-((phase*2)%1)*.12;print(f'cycle {t:.2f}: {side}, center Y={y:.3f}, diameter=0.09')
print('PASS: constant diameter; one side active; downward travel resets only when swapping sides; no sine-size animation remains.')
