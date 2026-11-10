import json,re,copy,uuid
from pathlib import Path
root=Path(__file__).resolve().parents[1];p=root/'Assets/Shader/HalfTone/SG_HalfTone.shadergraph'
objects=[json.loads(s) for s in re.split(r'\n\s*\n',p.read_text(encoding='utf-8-sig').strip())]
g=objects[0];lookup={o['m_ObjectId']:o for o in objects}
def uid():return uuid.uuid4().hex
if any(o.get('m_OverrideReferenceName')=='_ShadowStart' for o in objects):raise SystemExit('Already added')
backup=root/'HalftoneBackup/SG_HalfTone_BeforeShadowRange.shadergraph'
if not backup.exists():backup.write_bytes(p.read_bytes())
def slots(n):return [lookup[r['m_Id']] for r in n['m_Slots']]
def sid(n,name):return next(s['m_Id'] for s in slots(n) if s['m_DisplayName']==name)
def clone(kind,x,y):
    proto=next(o for o in objects if o['m_Type'].endswith('.'+kind+'Node'))
    n=copy.deepcopy(proto);n.update(m_ObjectId=uid(),m_Slots=[],m_PreviewExpanded=False)
    n['m_DrawState']['m_Position']={'serializedVersion':'2','x':x,'y':y,'width':210,'height':160}
    for s in slots(proto):
        s=copy.deepcopy(s);s['m_ObjectId']=uid();objects.append(s);lookup[s['m_ObjectId']]=s;n['m_Slots'].append({'m_Id':s['m_ObjectId']})
    objects.append(n);lookup[n['m_ObjectId']]=n;g['m_Nodes'].append({'m_Id':n['m_ObjectId']});return n
remap=next(o for o in objects if o['m_Type'].endswith('.RemapNode'))
xy=remap['m_DrawState']['m_Position'];x=xy['x'];y=xy['y']-400
smooth=clone('Smoothstep',x+250,y)
for e in g['m_Edges']:
    if e['m_OutputSlot']['m_Node']['m_Id']==remap['m_ObjectId']:
        e['m_OutputSlot']={'m_Node':{'m_Id':smooth['m_ObjectId']},'m_SlotId':sid(smooth,'Out')}
def edge(a,ap,b,bp):g['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':a['m_ObjectId']},'m_SlotId':sid(a,ap)},'m_InputSlot':{'m_Node':{'m_Id':b['m_ObjectId']},'m_SlotId':sid(b,bp)}})
edge(remap,'Out',smooth,'In')
for i,(name,ref,value,port) in enumerate([('Shadow Start','_ShadowStart',0,'Edge1'),('Shadow End','_ShadowEnd',1,'Edge2')]):
    prop=copy.deepcopy(next(o for o in objects if o.get('m_Name')=='HighlightStart' and o['m_Type'].endswith('ShaderProperty')))
    prop.update(m_ObjectId=uid(),m_Guid={'m_GuidSerialized':str(uuid.uuid4())},m_Name=name,m_DefaultReferenceName=ref,m_OverrideReferenceName=ref,m_RefNameGeneratedByDisplayName=name,m_Value=value,m_FloatType=0,m_GeneratePropertyBlock=True)
    objects.append(prop);g['m_Properties'].append({'m_Id':prop['m_ObjectId']});next(o for o in objects if o['m_Type'].endswith('.CategoryData'))['m_ChildObjectList'].append({'m_Id':prop['m_ObjectId']})
    n=clone('Property',x-50,y+i*180);n['m_Property']={'m_Id':prop['m_ObjectId']}
    s=slots(n)[0];s.update(m_Type='UnityEditor.ShaderGraph.Vector1MaterialSlot',m_DisplayName='Out',m_ShaderOutputName='Out',m_Value=value,m_DefaultValue=value)
    edge(n,'Out',smooth,port)
p.write_text('\n\n'.join(json.dumps(o,indent=4) for o in objects)+'\n')
print('Added Shadow Start and Shadow End before Shadow Dot Size.')
