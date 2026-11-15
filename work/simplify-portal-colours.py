from pathlib import Path
import json,copy,re,uuid,shutil
root=Path('DropShot/Assets');folder=root/'VFX/PortalEspacial/Mat';scene=root/'Scenes/Game_B_Hard.unity'
backup=Path('work/before-portal-original-style');backup.mkdir(exist_ok=True)
for p in [scene,folder/'SG_PortalRainbow_BHard.shadergraph',folder/'M_PortalRainbow_BHard.mat']:shutil.copy2(p,backup/p.name)
def uid():return uuid.uuid4().hex
def load(p):
 s=p.read_text();d=json.JSONDecoder();out=[]
 while s.strip():
  o,n=d.raw_decode(s.lstrip());out.append(o);s=s.lstrip()[n:]
 return out
objs=load(folder/'SG_Portal.shadergraph');g=objs[0];by={o['m_ObjectId']:o for o in objs}
float_template=next(o for o in load(root/'Shader/HalfTone/SG_HalfTone.shadergraph') if o['m_Type'].endswith('Vector1ShaderProperty'))
def clone_node(src,name,x,y):
 n=copy.deepcopy(by[src]);n['m_ObjectId']=uid();n['m_Name']=name;n['m_DrawState']['m_Position'].update(x=x,y=y)
 for ref in n['m_Slots']:
  sl=copy.deepcopy(by[ref['m_Id']]);sl['m_ObjectId']=uid();ref['m_Id']=sl['m_ObjectId'];objs.append(sl);by[sl['m_ObjectId']]=sl
 objs.append(n);by[n['m_ObjectId']]=n;g['m_Nodes'].append({'m_Id':n['m_ObjectId']});return n
scalar_template=next(o for o in objs if o['m_Type'].endswith('Vector1MaterialSlot'))
def float_property(name,ref,value,maximum,x,y):
 p=copy.deepcopy(float_template);p.update(m_ObjectId=uid(),m_Name=name,m_DefaultReferenceName=ref,m_OverrideReferenceName=ref,m_Value=value,m_RangeValues={'x':0.,'y':maximum});p['m_Guid']['m_GuidSerialized']=str(uuid.uuid4());objs.append(p);g['m_Properties'].append({'m_Id':p['m_ObjectId']})
 n=clone_node('34a13c33e92e4f3588125f9900b0269f','Property',x,y);n['m_Property']['m_Id']=p['m_ObjectId']
 old=by[n['m_Slots'][0]['m_Id']];new=copy.deepcopy(scalar_template);new.update(m_ObjectId=old['m_ObjectId'],m_Id=0,m_DisplayName=name,m_SlotType=1,m_ShaderOutputName='Out',m_StageCapability=3,m_Value=value,m_DefaultValue=value)
 old.clear();old.update(new);return n
bright=float_property('Brightness','_Brightness',1.35,4.,-200,650)
spread=float_property('Colour Spread','_ColourSpread',1.25,2.,-1100,850)
hue=clone_node('1b86239405414188b6a7a6457aa63d46','Hue',-450,100);hue['m_Type']='UnityEditor.ShaderGraph.HueNode';hue['m_HueMode']=1
for ref,slotid,name in zip(hue['m_Slots'],[0,1,2],['In','Offset','Out']):
 sl=by[ref['m_Id']];objid=sl['m_ObjectId'];sl.clear();scalar=slotid==1
 sl.update(m_SGVersion=0,m_Type='UnityEditor.ShaderGraph.'+('Vector1MaterialSlot' if scalar else 'Vector3MaterialSlot'),m_ObjectId=objid,m_Id=slotid,m_DisplayName=name,m_SlotType=1 if slotid==2 else 0,m_Hidden=False,m_ShaderOutputName=name,m_StageCapability=3,m_Value=0. if scalar else {'x':0.,'y':0.,'z':0.},m_DefaultValue=0. if scalar else {'x':0.,'y':0.,'z':0.},m_Labels=[])
scale=clone_node('1b86239405414188b6a7a6457aa63d46','Multiply',-800,850)
output=clone_node('1b86239405414188b6a7a6457aa63d46','Multiply',-50,150)
def edge(a,sa,b,sb):g['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':a},'m_SlotId':sa},'m_InputSlot':{'m_Node':{'m_Id':b},'m_SlotId':sb}})
g['m_Edges'][:]=[e for e in g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id']!='7c567b42506a4696873232a04a964a08']
edge('ee8ce689a800416d94cc69ffb08a9c04',1,scale['m_ObjectId'],0);edge(spread['m_ObjectId'],0,scale['m_ObjectId'],1);edge(scale['m_ObjectId'],2,hue['m_ObjectId'],1)
edge('1b86239405414188b6a7a6457aa63d46',2,hue['m_ObjectId'],0);edge(hue['m_ObjectId'],2,output['m_ObjectId'],0);edge(bright['m_ObjectId'],0,output['m_ObjectId'],1);edge(output['m_ObjectId'],2,'7c567b42506a4696873232a04a964a08',0)
(folder/'SG_PortalRainbow_BHard.shadergraph').write_text('\n\n'.join(json.dumps(o,indent=4) for o in objs)+'\n')
p=folder/'M_PortalRainbow_BHard.mat';s=p.read_text();s=re.sub(r'    - _Color: .*','    - _Color: {r: 1, g: 1, b: 1, a: 1}',s);s=s.replace('    m_Floats:\n','    m_Floats:\n    - _Brightness: 1.35\n    - _ColourSpread: 1.25\n');p.write_text(s)
# Remove only the two overrides introduced in the previous attempt.
s=scene.read_text()
for renderer in [315728224406089637,1235624873456642676]:
 pat=rf'    - target: \{{fileID: {renderer}, guid: 8b8351c027f776e4583703d0b4d3a6fb, type: 3\}}\n      propertyPath: m_Materials.Array.data\[0\]\n      value: *\n      objectReference: .*\n'
 s,n=re.subn(pat,'',s);assert n==1
scene.write_text(s)
# Archive the now-unused assets from the first attempt.
for name in ['M_PortalCentre_BHard.mat','M_PortalRim_BHard.mat','PortalRainbowRamp_BHard.png']:
 for suffix in ['', '.meta']:
  p=folder/(name+suffix);target=backup/p.name
  assert p.resolve().is_relative_to(root.resolve()) and not target.exists()
  p.rename(target)
print('Restored original rim/backing and original spiral transparency; added only hue variation and RGB brightness.')
