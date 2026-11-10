import copy,json,re,uuid
from pathlib import Path
root=Path(__file__).resolve().parents[1]
p=root/'Assets/Shader/HalfTone/SG_HalfTone.shadergraph'
def read(p):return [json.loads(s) for s in re.split(r'\n\s*\n',p.read_text(encoding='utf-8-sig').strip())]
def uid():return uuid.uuid4().hex
objects=read(p); graph=objects[0]; lookup={o['m_ObjectId']:o for o in objects}
backup=root/'HalftoneBackup/SG_HalfTone_BeforeFresnel.shadergraph'
if not backup.exists():backup.write_bytes(p.read_bytes())
if any(o.get('m_OverrideReferenceName')=='_RimWidth' for o in objects):raise SystemExit('Rim already exists')
samples=root/'Library/PackageCache/com.unity.shadergraph@aa8306cf39d6/Samples~/NodeReference/Math'
fresnel=read(samples/'FresnelEffect.shadergraph'); minus=read(samples/'OneMinus.shadergraph')
def clone_node(kind,x,y,source=objects):
    proto=next(o for o in source if o['m_Type'].endswith('.'+kind+'Node'))
    source_lookup={o['m_ObjectId']:o for o in source}
    n=copy.deepcopy(proto);n['m_ObjectId']=uid();n['m_Group']={'m_Id':''};n['m_Slots']=[]
    n['m_DrawState']['m_Position']={'serializedVersion':'2','x':x,'y':y,'width':210,'height':140}
    n['m_PreviewExpanded']=False
    for ref in proto['m_Slots']:
        s=copy.deepcopy(source_lookup[ref['m_Id']]);s['m_ObjectId']=uid();objects.append(s);lookup[s['m_ObjectId']]=s
        n['m_Slots'].append({'m_Id':s['m_ObjectId']})
    objects.append(n);lookup[n['m_ObjectId']]=n;graph['m_Nodes'].append({'m_Id':n['m_ObjectId']});return n
def slot(n,name):return next(lookup[s['m_Id']] for s in n['m_Slots'] if lookup[s['m_Id']]['m_DisplayName']==name)
def connect(a,ap,b,bp):graph['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':a['m_ObjectId']},'m_SlotId':slot(a,ap)['m_Id']},'m_InputSlot':{'m_Node':{'m_Id':b['m_ObjectId']},'m_SlotId':slot(b,bp)['m_Id']}})
output=next(o for o in objects if o.get('m_SerializedDescriptor')=='SurfaceDescription.BaseColor')
edge=next(e for e in graph['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id']==output['m_ObjectId'])
previous=copy.deepcopy(edge['m_OutputSlot'])
previous_node=lookup[previous['m_Node']['m_Id']]
xy=previous_node['m_DrawState']['m_Position'];x=xy['x']+350;y=xy['y']
f=clone_node('Fresnel',x,y+350,fresnel);slot(f,'Power')['m_Value']=1
inv=clone_node('OneMinus',x+250,y+550,minus)
step=clone_node('Step',x+500,y+350)
lerp=clone_node('Lerp',x+800,y)
edge['m_InputSlot']={'m_Node':{'m_Id':lerp['m_ObjectId']},'m_SlotId':slot(lerp,'A')['m_Id']}
def prop(kind,name,reference,value,px,py):
    proto=next(o for o in objects if o['m_Type'].endswith('.'+kind+'ShaderProperty'))
    prop=copy.deepcopy(proto);prop.update(m_ObjectId=uid(),m_Guid={'m_GuidSerialized':str(uuid.uuid4())},m_Name=name,m_DefaultReferenceName=reference,m_OverrideReferenceName=reference,m_RefNameGeneratedByDisplayName=name,m_Value=value)
    if kind=='Vector1':prop.update(m_FloatType=1,m_RangeValues={'x':0,'y':.8})
    objects.append(prop);graph['m_Properties'].append({'m_Id':prop['m_ObjectId']})
    for o in objects:
        if o['m_Type'].endswith('.CategoryData'):o['m_ChildObjectList'].append({'m_Id':prop['m_ObjectId']});break
    n=clone_node('Property',px,py);n['m_Property']={'m_Id':prop['m_ObjectId']}
    return n
width=prop('Vector1','Outline Width','_RimWidth',.35,x,y+550)
color=prop('Color','Outline Color','_RimColor',{'r':.015,'g':.02,'b':.03,'a':1},x+500,y-150)
# Recreate property slots with the correct types (template may be a float property).
for n,kind in [(width,'Vector1'),(color,'Vector4')]:
    s=lookup[n['m_Slots'][0]['m_Id']];s['m_Type']='UnityEditor.ShaderGraph.'+kind+'MaterialSlot'
    s['m_DisplayName']='Out';s['m_Value']=0 if kind=='Vector1' else {'x':0,'y':0,'z':0,'w':1}
connect(width,'Out',inv,'In');connect(inv,'Out',step,'Edge');connect(f,'Out',step,'In')
connect(color,'Out',lerp,'B');connect(step,'Out',lerp,'T');connect(lerp,'Out',output,'Base Color')
p.write_text('\n\n'.join(json.dumps(o,indent=4) for o in objects)+'\n')
print('Added Fresnel -> Step -> final Lerp, with Outline Width / Color.')
