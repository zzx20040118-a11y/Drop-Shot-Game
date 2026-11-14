import copy,json,pathlib,re,uuid
root=pathlib.Path('DropShot/Assets');uid=lambda:uuid.uuid4().hex;D=json.JSONDecoder()
def parse(p):
 s=p.read_text(encoding='utf-8-sig');a=[]
 while s.strip():
  s=s.lstrip();o,n=D.raw_decode(s);a.append(o);s=s[n:]
 return a
trail=parse(root/'Shader/SG_BallTrail.shadergraph');half=parse(root/'Shader/HalfTone/SG_HalfTone.shadergraph');pool=trail+half;by={o['m_ObjectId']:o for o in pool}
g=copy.deepcopy(trail[0]);g.update(m_ObjectId=uid(),m_Properties=[],m_Nodes=[],m_Edges=[],m_CategoryData=[],m_Path='DropShot',m_GroupDatas=[],m_StickyNoteDatas=[])
objs=[g]
def clone(name,x=0,y=0):
 original=next(o for o in pool if o.get('m_Name')==name);n=copy.deepcopy(original);n['m_ObjectId']=uid();n['m_Group']={'m_Id':''};n['m_DrawState']['m_Position'].update(x=x,y=y);n['m_PreviewExpanded']=False;slots=[]
 for ref in n['m_Slots']:
  s=copy.deepcopy(by[ref['m_Id']]);s['m_ObjectId']=uid();ref['m_Id']=s['m_ObjectId'];slots.append(s)
 objs.extend([n]+slots);g['m_Nodes'].append({'m_Id':n['m_ObjectId']});return n,slots
blocks=[]
for name in ['VertexDescription.Position','VertexDescription.Normal','VertexDescription.Tangent']:
 n,_=clone(name);blocks.append({'m_Id':n['m_ObjectId']})
g['m_VertexContext']={'m_Position':{'x':450.0,'y':-150.0},'m_Blocks':blocks}
base,slots=clone('SurfaceDescription.BaseColor');slots[0].update(m_Value={'x':1.0,'y':1.0,'z':1.0},m_DefaultValue={'x':1.0,'y':1.0,'z':1.0},m_DefaultColor={'r':1.0,'g':1.0,'b':1.0,'a':1.0})
alpha,_=clone('SurfaceDescription.Alpha')
g['m_FragmentContext']={'m_Position':{'x':450.0,'y':100.0},'m_Blocks':[{'m_Id':base['m_ObjectId']},{'m_Id':alpha['m_ObjectId']}]}
category={'m_SGVersion':0,'m_Type':'UnityEditor.ShaderGraph.CategoryData','m_ObjectId':uid(),'m_Name':'','m_ChildObjectList':[]};objs.append(category);g['m_CategoryData']=[{'m_Id':category['m_ObjectId']}]
prop=copy.deepcopy(next(o for o in half if o.get('m_Type','').endswith('.Vector1ShaderProperty')))
prop.update(m_ObjectId=uid(),m_Name='Ring Width',m_DefaultReferenceName='_RingWidth',m_OverrideReferenceName='_RingWidth',m_RefNameGeneratedByDisplayName='Ring Width',m_Value=0.05,m_FloatType=1,m_RangeValues={'x':0.005,'y':0.3},m_GeneratePropertyBlock=True,overrideHLSLDeclaration=False,hlslDeclarationOverride=0,m_Hidden=False)
prop['m_Guid']={'m_GuidSerialized':str(uuid.uuid4())};objs.append(prop);g['m_Properties'].append({'m_Id':prop['m_ObjectId']});category['m_ChildObjectList'].append({'m_Id':prop['m_ObjectId']})
tex=copy.deepcopy(next(o for o in half if o.get('m_DefaultReferenceName')=='_BaseTexture'))
tex.update(m_ObjectId=uid(),m_Name='Sprite Texture',m_DefaultReferenceName='_MainTex',m_OverrideReferenceName='_MainTex',m_RefNameGeneratedByDisplayName='Sprite Texture',m_GeneratePropertyBlock=True,m_Hidden=True,m_PerRendererData=True)
tex['m_Guid']={'m_GuidSerialized':str(uuid.uuid4())};objs.append(tex);g['m_Properties'].append({'m_Id':tex['m_ObjectId']});category['m_ChildObjectList'].append({'m_Id':tex['m_ObjectId']})
property_node,slots=clone('Property',-800,300);property_node['m_Property']={'m_Id':prop['m_ObjectId']};slots[0].update(m_Type='UnityEditor.ShaderGraph.Vector1MaterialSlot',m_DisplayName='Ring Width',m_Value=0.05,m_DefaultValue=0.05,m_LiteralMode=False)
inner_size,slots=clone('Subtract',-550,300);slots[0]['m_Value']={'x':0.94,'y':0.94,'z':0.94,'w':0.94}
def slot(typ,index,name,kind,value,**extra):
 r={'m_SGVersion':0,'m_Type':'UnityEditor.ShaderGraph.'+typ,'m_ObjectId':uid(),'m_Id':index,'m_DisplayName':name,'m_SlotType':kind,'m_Hidden':False,'m_ShaderOutputName':name,'m_StageCapability':2,'m_Value':value,'m_DefaultValue':value,'m_Labels':[]};r.update(extra);objs.append(r);return {'m_Id':r['m_ObjectId']}
def ellipse(x,y):
 n=copy.deepcopy(property_node);n.update(m_ObjectId=uid(),m_Type='UnityEditor.ShaderGraph.EllipseNode',m_Name='Ellipse',synonyms=['circle']);n.pop('m_Property',None);n['m_DrawState']['m_Position'].update(x=x,y=y,width=210,height=180)
 n['m_Slots']=[slot('UVMaterialSlot',0,'UV',0,{'x':0.0,'y':0.0},m_Channel=0),slot('Vector1MaterialSlot',2,'Width',0,0.94,m_LiteralMode=False),slot('Vector1MaterialSlot',3,'Height',0,0.94,m_LiteralMode=False),slot('Vector1MaterialSlot',4,'Out',1,0.0,m_LiteralMode=False)]
 objs.append(n);g['m_Nodes'].append({'m_Id':n['m_ObjectId']});return n
outer=ellipse(-250,-100);inner=ellipse(-250,300);mask,_=clone('Subtract',30,100)
def connect(a,out,b,inp):g['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':a['m_ObjectId']},'m_SlotId':out},'m_InputSlot':{'m_Node':{'m_Id':b['m_ObjectId']},'m_SlotId':inp}})
connect(property_node,0,inner_size,1);connect(inner_size,2,inner,2);connect(inner_size,2,inner,3);connect(outer,4,mask,0);connect(inner,4,mask,1);connect(mask,2,alpha,0)
target=copy.deepcopy(next(o for o in trail if o.get('m_Type','').endswith('.UniversalTarget')));target.update(m_ObjectId=uid(),m_AllowMaterialOverride=False,m_SurfaceType=1,m_ZWriteControl=2,m_RenderFace=0,m_CastShadows=False,m_ReceiveShadows=False,m_DisableTint=False)
subtarget={'m_SGVersion':0,'m_Type':'UnityEditor.Rendering.Universal.ShaderGraph.UniversalSpriteUnlitSubTarget','m_ObjectId':uid()};target['m_ActiveSubTarget']={'m_Id':subtarget['m_ObjectId']};g['m_ActiveTargets']=[{'m_Id':target['m_ObjectId']}];objs.extend([target,subtarget])
path=root/'Shader/SG_ShortNoteRing.shadergraph';path.write_text('\n\n'.join(json.dumps(o,indent=4) for o in objs)+'\n',encoding='utf-8')
guid=uid();pathlib.Path(str(path)+'.meta').write_text('fileFormatVersion: 2\nguid: '+guid+'\nScriptedImporter:\n  internalIDToNameTable: []\n  externalObjects: {}\n  serializedVersion: 2\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n  script: {fileID: 11500000, guid: 625f186215c104763be7675a00d941aa, type: 3}\n',encoding='utf-8')
p=root/'VFX/ShortNoteRing.mat';s=p.read_text();s=re.sub(r'm_Shader: \{[^\n]+\}','m_Shader: {fileID: -6465566751694194690, guid: '+guid+', type: 3}',s);p.write_text(s,encoding='utf-8')
print('Created Sprite Unlit Shader Graph using two Ellipse nodes and two Subtract nodes; no dark-edge or custom-function nodes.')
