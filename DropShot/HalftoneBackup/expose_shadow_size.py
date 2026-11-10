import copy,json,re,uuid
from pathlib import Path
root=Path(__file__).resolve().parents[1]
p=root/'Assets/Shader/HalfTone/SG_HalfTone.shadergraph'
objects=[json.loads(s) for s in re.split(r'\n\s*\n',p.read_text(encoding='utf-8-sig').strip())]
graph=objects[0]; lookup={o['m_ObjectId']:o for o in objects}
def uid():return uuid.uuid4().hex
if any(o.get('m_OverrideReferenceName')=='_ShadowDotSize' for o in objects):raise SystemExit('Already added')
backup=root/'HalftoneBackup/SG_HalfTone_BeforeShadowSize.shadergraph'
if not backup.exists():backup.write_bytes(p.read_bytes())
remap=next(o for o in objects if o['m_Type'].endswith('.RemapNode'))
xy=remap['m_DrawState']['m_Position'];x=xy['x']+240;y=xy['y']
prop=copy.deepcopy(next(o for o in objects if o['m_Type'].endswith('.Vector1ShaderProperty')))
prop.update(m_ObjectId=uid(),m_Guid={'m_GuidSerialized':str(uuid.uuid4())},m_Name='Shadow Dot Size',m_DefaultReferenceName='_ShadowDotSize',m_OverrideReferenceName='_ShadowDotSize',m_RefNameGeneratedByDisplayName='ShadowDotSize',m_Value=1,m_FloatType=1,m_RangeValues={'x':0,'y':2},m_GeneratePropertyBlock=True)
objects.append(prop);graph['m_Properties'].append({'m_Id':prop['m_ObjectId']})
next(o for o in objects if o['m_Type'].endswith('.CategoryData'))['m_ChildObjectList'].append({'m_Id':prop['m_ObjectId']})
def node(kind,px,py):
    proto=next(o for o in objects if o['m_Type'].endswith('.'+kind+'Node'))
    n=copy.deepcopy(proto);n['m_ObjectId']=uid();n['m_Slots']=[];n['m_PreviewExpanded']=False
    n['m_DrawState']['m_Position']={'serializedVersion':'2','x':px,'y':py,'width':210,'height':150}
    for r in proto['m_Slots']:
        s=copy.deepcopy(lookup[r['m_Id']]);s['m_ObjectId']=uid();objects.append(s);lookup[s['m_ObjectId']]=s;n['m_Slots'].append({'m_Id':s['m_ObjectId']})
    objects.append(n);lookup[n['m_ObjectId']]=n;graph['m_Nodes'].append({'m_Id':n['m_ObjectId']});return n
n=node('Property',x,y+250);n['m_Property']={'m_Id':prop['m_ObjectId']}
s=lookup[n['m_Slots'][0]['m_Id']];s.update(m_Type='UnityEditor.ShaderGraph.Vector1MaterialSlot',m_DisplayName='Shadow Dot Size',m_ShaderOutputName='Out',m_Value=1,m_DefaultValue=1)
mult=node('Multiply',x,y)
def sid(n,name):return next(lookup[r['m_Id']]['m_Id'] for r in n['m_Slots'] if lookup[r['m_Id']]['m_DisplayName']==name)
for e in graph['m_Edges']:
    if e['m_OutputSlot']['m_Node']['m_Id']==remap['m_ObjectId']:
        e['m_OutputSlot']={'m_Node':{'m_Id':mult['m_ObjectId']},'m_SlotId':sid(mult,'Out')}
def edge(a,ap,b,bp):graph['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':a['m_ObjectId']},'m_SlotId':sid(a,ap)},'m_InputSlot':{'m_Node':{'m_Id':b['m_ObjectId']},'m_SlotId':sid(b,bp)}})
edge(remap,'Out',mult,'A');edge(n,'Shadow Dot Size',mult,'B')
p.write_text('\n\n'.join(json.dumps(o,indent=4) for o in objects)+'\n')
print('Exposed Shadow Dot Size; default 1 preserves existing appearance.')
