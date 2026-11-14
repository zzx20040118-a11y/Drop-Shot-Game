import copy,json,pathlib,uuid
p=pathlib.Path('DropShot/Assets/Shader/HalfTone/SG_HalfTone.shadergraph');s=p.read_text(encoding='utf-8-sig');d=json.JSONDecoder();a=[];raw={}
while s.strip():
 s=s.lstrip();o,n=d.raw_decode(s);a.append(o);raw[o['m_ObjectId']]=s[:n];s=s[n:]
g=a[0];by={o['m_ObjectId']:o for o in a};uid=lambda:uuid.uuid4().hex
native_ids={'095c2b21e45641209292b98f2b4c6b6f','16e652fda50b4183b9c93862c8325b5c','6ddf8a8c24124baf813a6f7674e347f8','02f2131e33644063a42cc9ba7642b25c','9ca505b671ad46a3b6d687eaec7ab7c6'}
assert native_ids.issubset(by)
final='02f2131e33644063a42cc9ba7642b25c'
base=copy.deepcopy(next(e['m_OutputSlot'] for e in g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id']==final and e['m_InputSlot']['m_SlotId']==0))
target=copy.deepcopy(next(e['m_InputSlot'] for e in g['m_Edges'] if e['m_OutputSlot']['m_Node']['m_Id']==final))
removed=native_ids|{r['m_Id'] for n in native_ids for r in by[n]['m_Slots']}
g['m_Nodes']=[r for r in g['m_Nodes'] if r['m_Id'] not in native_ids]
g['m_Edges']=[e for e in g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id'] not in native_ids and e['m_OutputSlot']['m_Node']['m_Id'] not in native_ids]
added=[]
def clone(name,x,y):
 src=next(o for o in a if o.get('m_Name')==name and o['m_ObjectId'] not in native_ids);node=copy.deepcopy(src);node['m_ObjectId']=uid();node['m_Group']={'m_Id':''};node['m_DrawState']['m_Position'].update(x=x,y=y);node['m_PreviewExpanded']=False
 slots=[]
 for r in node['m_Slots']:
  slot=copy.deepcopy(by[r['m_Id']]);slot['m_ObjectId']=uid();r['m_Id']=slot['m_ObjectId'];slots.append(slot)
 added.extend([node]+slots);g['m_Nodes'].append({'m_Id':node['m_ObjectId']});return node,slots
prop=copy.deepcopy(next(o for o in a if o.get('m_Type','').endswith('.Vector1ShaderProperty')))
prop.update(m_ObjectId=uid(),m_Name='Brightness',m_RefNameGeneratedByDisplayName='Brightness',m_DefaultReferenceName='_EnvironmentBrightness',m_OverrideReferenceName='_EnvironmentBrightness',m_GeneratePropertyBlock=True,m_Hidden=False,m_Value=0.9,m_FloatType=1,m_RangeValues={'x':0.0,'y':1.0})
prop['m_Guid']={'m_GuidSerialized':str(uuid.uuid4())};added.append(prop);g['m_Properties'].append({'m_Id':prop['m_ObjectId']})
node,slots=clone('Property',1560,-150);node['m_Property']={'m_Id':prop['m_ObjectId']}
slots[0].update(m_Type='UnityEditor.ShaderGraph.Vector1MaterialSlot',m_DisplayName='Brightness',m_Value=0.9,m_DefaultValue=0.9,m_ShaderOutputName='Out',m_LiteralMode=False)
mul,_=clone('Multiply',1830,-150)
def output(node,index):return {'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':index}
def connect(src,node,index):g['m_Edges'].append({'m_OutputSlot':src,'m_InputSlot':{'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':index}})
connect(base,mul,0);connect(output(node,0),mul,1);g['m_Edges'].append({'m_OutputSlot':output(mul,2),'m_InputSlot':target})
p.write_text(json.dumps(g,indent=4)+'\n\n'+'\n\n'.join([raw[o['m_ObjectId']] for o in a[1:] if o['m_ObjectId'] not in removed]+[json.dumps(o,indent=4) for o in added])+'\n',encoding='utf-8')
print('Removed the five native-lighting nodes; added a Brightness slider and Multiply, default 0.9. No scripts or custom shader code added.')
