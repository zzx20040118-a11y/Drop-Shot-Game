import copy,json,pathlib,re,uuid
root=pathlib.Path('DropShot/Assets');p=root/'Shader/HalfTone/SG_HalfTone.shadergraph';s=p.read_text(encoding='utf-8-sig');d=json.JSONDecoder();a=[];raw={}
while s.strip():
 s=s.lstrip();o,n=d.raw_decode(s);a.append(o);raw[o['m_ObjectId']]=s[:n];s=s[n:]
g=a[0];by={o['m_ObjectId']:o for o in a};uid=lambda:uuid.uuid4().hex
old=next(o for o in a if o.get('m_FunctionName')=='DS_EnvironmentBrightness')
removed={old['m_ObjectId']}|{r['m_Id'] for r in old['m_Slots']}
incoming=next(e for e in g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id']==old['m_ObjectId'])
outgoing=next(e for e in g['m_Edges'] if e['m_OutputSlot']['m_Node']['m_Id']==old['m_ObjectId'])
base_source=copy.deepcopy(incoming['m_OutputSlot']);target=copy.deepcopy(outgoing['m_InputSlot'])
g['m_Edges']=[e for e in g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id'] not in removed and e['m_OutputSlot']['m_Node']['m_Id'] not in removed]
g['m_Nodes']=[r for r in g['m_Nodes'] if r['m_Id'] not in removed];added=[]
def clone(name,x,y):
 src=next(o for o in a if o.get('m_Name')==name);node=copy.deepcopy(src);node['m_ObjectId']=uid();node['m_Group']={'m_Id':''};node['m_DrawState']['m_Position'].update(x=x,y=y);node['m_PreviewExpanded']=False
 slots=[]
 for ref in node['m_Slots']:
  slot=copy.deepcopy(by[ref['m_Id']]);slot['m_ObjectId']=uid();ref['m_Id']=slot['m_ObjectId'];slots.append(slot)
 added.extend([node]+slots);g['m_Nodes'].append({'m_Id':node['m_ObjectId']});return node
sat=clone('Saturate',1400,-500);direct=clone('Multiply',1650,-500);total=clone('Add',1900,-350);multiply=clone('Multiply',2150,-200)
gi=copy.deepcopy(sat);gi.update(m_ObjectId=uid(),m_Type='UnityEditor.ShaderGraph.BakedGINode',m_Name='Baked GI',m_ApplyScaling=True,synonyms=['global illumination'])
gi['m_DrawState']['m_Position'].update(x=1650,y=-150);gi['m_Slots']=[]
def slot(typ,index,name,kind,value,**extra):
 s={'m_SGVersion':0,'m_Type':'UnityEditor.ShaderGraph.'+typ,'m_ObjectId':uid(),'m_Id':index,'m_DisplayName':name,'m_SlotType':kind,'m_Hidden':False,'m_ShaderOutputName':name,'m_StageCapability':3,'m_Value':value,'m_DefaultValue':value,'m_Labels':[]};s.update(extra);gi['m_Slots'].append({'m_Id':s['m_ObjectId']});added.append(s)
slot('NormalMaterialSlot',0,'NormalWS',0,{'x':0.0,'y':0.0,'z':0.0},m_Space=2)
slot('PositionMaterialSlot',2,'PositionWS',0,{'x':0.0,'y':0.0,'z':0.0},m_Space=2)
slot('UVMaterialSlot',3,'StaticUV',0,{'x':0.0,'y':0.0},m_Channel=1)
slot('UVMaterialSlot',4,'DynamicUV',0,{'x':0.0,'y':0.0},m_Channel=2)
slot('Vector3MaterialSlot',1,'Out',1,{'x':0.0,'y':0.0,'z':0.0})
added.append(gi);g['m_Nodes'].append({'m_Id':gi['m_ObjectId']})
def output(node,index):return {'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':index}
def connect(src,node,index):g['m_Edges'].append({'m_OutputSlot':copy.deepcopy(src),'m_InputSlot':{'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':index}})
dot=next(o for o in a if o.get('m_Name')=='Dot Product');main=next(o for o in a if o.get('m_Name')=='Get Main Light')
connect(output(dot,2),sat,0);connect(output(sat,1),direct,1);connect(output(main,2),direct,0)
connect(output(direct,2),total,0);connect(output(gi,1),total,1)
connect(base_source,multiply,0);connect(output(total,2),multiply,1)
g['m_Edges'].append({'m_OutputSlot':output(multiply,2),'m_InputSlot':target})
normal_source=next(e['m_OutputSlot'] for e in g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id']==dot['m_ObjectId'] and e['m_InputSlot']['m_SlotId']==0)
connect(normal_source,gi,0)
p.write_text(json.dumps(g,indent=4)+'\n\n'+'\n\n'.join([raw[o['m_ObjectId']] for o in a[1:] if o['m_ObjectId'] not in removed]+[json.dumps(o,indent=4) for o in added])+'\n',encoding='utf-8')
p=root/'Shader/HalfTone/DS_HalftoneReaction.hlsl';s=p.read_text(encoding='utf-8');s=s.replace('float _DS_EnvironmentDim;\n','');s=re.sub(r'void DS_EnvironmentBrightness_float\([^\n]+\n\{.*?\n\}\n','',s,flags=re.S);p.write_text(s,encoding='utf-8')
for name in ['Game_Hard.unity','Game_Easy.unity']:
 p=root/'Scenes'/name;s=p.read_text(encoding='utf-8')
 blocks=re.split(r'(?=--- !u!)',s)
 own=[block for block in blocks if re.match(r'--- !u!\d+ &21385063[234]\n',block)]
 assert len(own)==3 and any('m_Name: EnvironmentBrightness\n' in b for b in own)
 s=''.join(block for block in blocks if block not in own)
 s=s.replace('  - {fileID: 213850634}\n','');p.write_text(s,encoding='utf-8')
print('Five native lighting nodes added. Old custom brightness node and scene controls removed. User lighting settings preserved.')
