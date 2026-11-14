import copy,json,pathlib,uuid
p=pathlib.Path('DropShot/Assets/Shader/HalfTone/SG_HalfTone.shadergraph');s=p.read_text(encoding='utf-8-sig');d=json.JSONDecoder();a=[];raw=[]
while s.strip():
 s=s.lstrip();o,n=d.raw_decode(s);a.append(o);raw.append(s[:n]);s=s[n:]
g=a[0];by={o['m_ObjectId']:o for o in a}; uid=lambda:uuid.uuid4().hex
assert not any(o.get('m_FunctionName')=='DS_EnvironmentBrightness' for o in a)
source=next(o for o in a if o.get('m_FunctionName')=='DS_HalftoneReaction')
node=copy.deepcopy(source);node.update(m_ObjectId=uid(),m_Name='Environment Brightness (Custom Function)',m_FunctionName='DS_EnvironmentBrightness',m_Group={'m_Id':''})
node['m_DrawState']['m_Position'].update(x=1840.0,y=0.0,width=250.0,height=160.0)
slots=[copy.deepcopy(by[source['m_Slots'][i]['m_Id']]) for i in [0,4]]
for i,o in enumerate(slots):o.update(m_ObjectId=uid(),m_Id=i)
node['m_Slots']=[{'m_Id':o['m_ObjectId']} for o in slots]
rim=next(o for o in a if o.get('m_OverrideReferenceName')=='_RimColor')
pn=next(o for o in a if o.get('m_Property',{}).get('m_Id')==rim['m_ObjectId'])
lerp_id=next(e['m_InputSlot']['m_Node']['m_Id'] for e in g['m_Edges'] if e['m_OutputSlot']['m_Node']['m_Id']==pn['m_ObjectId'])
edge=next(e for e in g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id']==lerp_id and e['m_InputSlot']['m_SlotId']==0)
old_target=copy.deepcopy(edge['m_InputSlot']);edge['m_InputSlot']={'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':0}
g['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':1},'m_InputSlot':old_target})
g['m_Nodes'].append({'m_Id':node['m_ObjectId']})
p.write_text(json.dumps(g,indent=4)+'\n\n'+'\n\n'.join(raw[1:]+[json.dumps(o,indent=4) for o in [node]+slots])+'\n',encoding='utf-8')
p=pathlib.Path('DropShot/Assets/Scripts/HalftoneEnvironmentBrightness.cs.meta');p.write_text('fileFormatVersion: 2\nguid: '+uid()+'\n',encoding='utf-8')
print('Inserted brightness before outline blending; glow and outline branches are preserved.')
