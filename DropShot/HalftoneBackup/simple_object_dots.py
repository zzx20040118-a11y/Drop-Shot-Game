import json,re,copy,uuid
from pathlib import Path
root=Path(__file__).resolve().parents[1];p=root/'Assets/Shader/HalfTone/SG_HalfTone.shadergraph'
objects=[json.loads(s) for s in re.split(r'\n\s*\n',p.read_text(encoding='utf-8-sig').strip())]
g=objects[0];lookup={o['m_ObjectId']:o for o in objects}
def uid():return uuid.uuid4().hex
if any(o.get('m_FunctionName')=='ObjectScreenDots' for o in objects):raise SystemExit('Already applied')
backup=root/'HalftoneBackup/SG_HalfTone_BeforeObjectDots.shadergraph'
if not backup.exists():backup.write_bytes(p.read_bytes())
def slots(n):return [lookup[r['m_Id']] for r in n['m_Slots']]
def sid(n,name):return next(s['m_Id'] for s in slots(n) if s['m_DisplayName']==name)
def edge(a,ap,b,bp):g['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':a['m_ObjectId']},'m_SlotId':sid(a,ap)},'m_InputSlot':{'m_Node':{'m_Id':b['m_ObjectId']},'m_SlotId':sid(b,bp)}})
def clone(kind,x,y):
    proto=next(o for o in objects if o['m_Type'].endswith('.'+kind+'Node'))
    n=copy.deepcopy(proto);n['m_ObjectId']=uid();n['m_Slots']=[];n['m_PreviewExpanded']=False
    n['m_DrawState']['m_Position']={'serializedVersion':'2','x':x,'y':y,'width':240,'height':200}
    for s in slots(proto):
        s=copy.deepcopy(s);s['m_ObjectId']=uid();objects.append(s);lookup[s['m_ObjectId']]=s;n['m_Slots'].append({'m_Id':s['m_ObjectId']})
    objects.append(n);lookup[n['m_ObjectId']]=n;g['m_Nodes'].append({'m_Id':n['m_ObjectId']});return n
def custom(name,inputs,outdim,body,x,y):
    n={'m_SGVersion':1,'m_Type':'UnityEditor.ShaderGraph.CustomFunctionNode','m_ObjectId':uid(),'m_Group':{'m_Id':''},'m_Name':name+' (Custom Function)','m_DrawState':{'m_Expanded':True,'m_Position':{'serializedVersion':'2','x':x,'y':y,'width':250,'height':250}},'m_Slots':[],'synonyms':[],'m_Precision':1,'m_PreviewExpanded':False,'m_DismissedVersion':0,'m_PreviewMode':0,'m_CustomColors':{'m_SerializableColors':[]},'m_SourceType':1,'m_FunctionName':name,'m_FunctionSource':'','m_FunctionSourceUsePragmas':False,'m_FunctionBody':body}
    for i,(field,dim) in enumerate(inputs+[('Out',outdim)]):
        value=0 if dim==1 else dict(zip('xyzw'[:dim],[0]*dim))
        s={'m_SGVersion':0,'m_Type':f'UnityEditor.ShaderGraph.Vector{dim}MaterialSlot','m_ObjectId':uid(),'m_Id':i,'m_DisplayName':field,'m_SlotType':1 if field=='Out' else 0,'m_Hidden':False,'m_ShaderOutputName':field,'m_StageCapability':2,'m_Value':value,'m_DefaultValue':value,'m_Labels':[]}
        n['m_Slots'].append({'m_Id':s['m_ObjectId']});objects.append(s);lookup[s['m_ObjectId']]=s
    objects.append(n);lookup[n['m_ObjectId']]=n;g['m_Nodes'].append({'m_Id':n['m_ObjectId']});return n
screen=next(o for o in objects if o['m_Type'].endswith('.ScreenPositionNode'))
xy=screen['m_DrawState']['m_Position'];anchor=custom('ObjectScreenDots',[('Screen',4)],4,'''Out = Screen;
#if !defined(SHADERGRAPH_PREVIEW)
float4 pivot = ComputeScreenPos(TransformObjectToHClip(float3(0,0,0)));
float w = abs(pivot.w) < 0.00001 ? 0.00001 : pivot.w;
Out.xy -= pivot.xy / w;
#endif''',xy['x']+230,xy['y']-220)
for e in g['m_Edges']:
    if e['m_OutputSlot']['m_Node']['m_Id']==screen['m_ObjectId']:
        e['m_OutputSlot']={'m_Node':{'m_Id':anchor['m_ObjectId']},'m_SlotId':sid(anchor,'Out')}
edge(screen,'Out',anchor,'Screen')
def prop(name,ref,value,x,y):
    p=copy.deepcopy(next(o for o in objects if o['m_Type'].endswith('.Vector1ShaderProperty')))
    p.update(m_ObjectId=uid(),m_Guid={'m_GuidSerialized':str(uuid.uuid4())},m_Name=name,m_DefaultReferenceName=ref,m_OverrideReferenceName=ref,m_RefNameGeneratedByDisplayName=name,m_Value=value,m_FloatType=1,m_RangeValues={'x':0,'y':1},m_GeneratePropertyBlock=True)
    objects.append(p);g['m_Properties'].append({'m_Id':p['m_ObjectId']});next(o for o in objects if o['m_Type'].endswith('.CategoryData'))['m_ChildObjectList'].append({'m_Id':p['m_ObjectId']})
    n=clone('Property',x,y);n['m_Property']={'m_Id':p['m_ObjectId']};s=slots(n)[0];s.update(m_Type='UnityEditor.ShaderGraph.Vector1MaterialSlot',m_DisplayName='Out',m_ShaderOutputName='Out',m_Value=value,m_DefaultValue=value);return n
output=next(o for o in objects if o.get('m_SerializedDescriptor')=='SurfaceDescription.BaseColor')
rimedge=next(e for e in g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id']==output['m_ObjectId'])
rim=lookup[rimedge['m_OutputSlot']['m_Node']['m_Id']]
incoming=next(e for e in g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id']==rim['m_ObjectId'] and e['m_InputSlot']['m_SlotId']==sid(rim,'A'))
xy=rim['m_DrawState']['m_Position'];x=xy['x'];y=xy['y']-600
smooth=prop('Smoothness','_Smoothness',.5,x-500,y-200);metal=prop('Metallic','_Metallic',0,x-500,y)
spec=custom('SimpleSpecular',[('Position',3),('Normal',3),('LightDirection',3),('LightColor',3),('Attenuation',1),('Base',3),('Smoothness',1),('Metallic',1)],3,'''float3 n = normalize(Normal);
float3 v = GetWorldSpaceNormalizeViewDir(Position);
float3 h = normalize(LightDirection + v + 0.00001);
float s = saturate(Smoothness);
float highlight = pow(saturate(dot(n,h)), exp2(3 + 8*s));
float3 tint = lerp(float3(0.04,0.04,0.04), Base, saturate(Metallic));
Out = tint * LightColor * highlight * (1 + 3*s)
    * saturate(dot(n,LightDirection)) * Attenuation;''',x-200,y)
light=next(o for o in objects if o['m_Type'].endswith('.SubGraphNode') and o['m_Name']=='Get Main Light')
pos=next(o for o in objects if o['m_Type'].endswith('.PositionNode'))
normal=next(o for o in objects if o['m_Type'].endswith('.TransformNode'))
base=lookup['71745dbd2e3a4aec932333a3957a9e8e']
for a,ap,bp in [(pos,'Out','Position'),(normal,'Out','Normal'),(light,'Direction','LightDirection'),(light,'Color','LightColor'),(light,'Attenuation','Attenuation'),(base,'Out','Base'),(smooth,'Out','Smoothness'),(metal,'Out','Metallic')]:edge(a,ap,spec,bp)
add=clone('Add',x+100,y)
incoming['m_InputSlot']={'m_Node':{'m_Id':add['m_ObjectId']},'m_SlotId':sid(add,'A')}
edge(spec,'Out',add,'B');edge(add,'Out',rim,'A')
p.write_text('\n\n'.join(json.dumps(o,indent=4) for o in objects)+'\n')
print('Added per-object screen anchor and a small specular branch; existing dot/rim branches unchanged.')
