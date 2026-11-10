import json,re,copy,uuid
from pathlib import Path
root=Path(__file__).resolve().parents[1];p=root/'Assets/Shader/HalfTone/SG_HalfTone.shadergraph'
objects=[json.loads(s) for s in re.split(r'\n\s*\n',p.read_text(encoding='utf-8-sig').strip())]
g=objects[0];lookup={o['m_ObjectId']:o for o in objects}
def uid():return uuid.uuid4().hex
backup=root/'HalftoneBackup/SG_HalfTone_BeforeShadowColor.shadergraph'
if not backup.exists():backup.write_bytes(p.read_bytes())
def slots(n):return [lookup[r['m_Id']] for r in n['m_Slots']]
def sid(n,name):return next(s['m_Id'] for s in slots(n) if s['m_DisplayName']==name)
def clone(kind,x,y):
    proto=next(o for o in objects if o['m_Type'].endswith('.'+kind+'Node'))
    n=copy.deepcopy(proto);n.update(m_ObjectId=uid(),m_Slots=[],m_PreviewExpanded=False)
    n['m_DrawState']['m_Position']={'serializedVersion':'2','x':x,'y':y,'width':210,'height':150}
    for s in slots(proto):
        s=copy.deepcopy(s);s['m_ObjectId']=uid();objects.append(s);lookup[s['m_ObjectId']]=s;n['m_Slots'].append({'m_Id':s['m_ObjectId']})
    objects.append(n);lookup[n['m_ObjectId']]=n;g['m_Nodes'].append({'m_Id':n['m_ObjectId']});return n
if not any(o.get('m_OverrideReferenceName')=='_ShadowColor' for o in objects):
    prop=copy.deepcopy(next(o for o in objects if o.get('m_Name')=='HighlightColor' and o['m_Type'].endswith('ShaderProperty')))
    prop.update(m_ObjectId=uid(),m_Guid={'m_GuidSerialized':str(uuid.uuid4())},m_Name='Shadow Color',m_DefaultReferenceName='_ShadowColor',m_OverrideReferenceName='_ShadowColor',m_RefNameGeneratedByDisplayName='ShadowColor',m_Value={'r':1,'g':1,'b':1,'a':1},m_GeneratePropertyBlock=True)
    objects.append(prop);lookup[prop['m_ObjectId']]=prop;g['m_Properties'].append({'m_Id':prop['m_ObjectId']})
    target=lookup['6fc7416ddcb94b16b725b1507e4b4361']
    xy=target['m_DrawState']['m_Position'];x=xy['x'];y=xy['y']-350
    n=clone('Property',x-300,y-170);n['m_Property']={'m_Id':prop['m_ObjectId']}
    s=slots(n)[0];s.update(m_Type='UnityEditor.ShaderGraph.Vector4MaterialSlot',m_DisplayName='Out',m_ShaderOutputName='Out',m_Value={'x':1,'y':1,'z':1,'w':1},m_DefaultValue={'x':1,'y':1,'z':1,'w':1})
    multiply=clone('Multiply',x,y)
    old=next(e for e in g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id']==target['m_ObjectId'] and e['m_InputSlot']['m_SlotId']==sid(target,'A'))
    old['m_InputSlot']={'m_Node':{'m_Id':multiply['m_ObjectId']},'m_SlotId':sid(multiply,'A')}
    def edge(a,ap,b,bp):g['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':a['m_ObjectId']},'m_SlotId':sid(a,ap)},'m_InputSlot':{'m_Node':{'m_Id':b['m_ObjectId']},'m_SlotId':sid(b,bp)}})
    edge(n,'Out',multiply,'B');edge(multiply,'Out',target,'A')
lookup={o['m_ObjectId']:o for o in objects}
props=[lookup[r['m_Id']] for r in g['m_Properties']]
byname={o['m_Name']:o for o in props}
high=['HighlightStart','HighlightEnd','HighlightDotSize','HighlightColor']
shadow=['Shadow Start','Shadow End','Shadow Dot Size','Shadow Color','ShadingMultiplier','LitThreshold','FalloffThreshold']
other=[o['m_Name'] for o in props if o['m_Name'] not in high+shadow]
g['m_Properties']=[{'m_Id':byname[name]['m_ObjectId']} for name in high+shadow+other]
g['m_CategoryData']=[]
objects=[o for o in objects if not o['m_Type'].endswith('.CategoryData')]
for title,names in [('Highlight',high),('Shadow',shadow),('Other Settings',other)]:
    category={'m_SGVersion':0,'m_Type':'UnityEditor.ShaderGraph.CategoryData','m_ObjectId':uid(),'m_Name':title,'m_ChildObjectList':[{'m_Id':byname[name]['m_ObjectId']} for name in names]}
    if title=='Other Settings':category['m_ChildObjectList']+=g['m_Keywords']
    objects.append(category);g['m_CategoryData'].append({'m_Id':category['m_ObjectId']})
p.write_text('\n\n'.join(json.dumps(o,indent=4) for o in objects)+'\n')
print('Controls grouped Highlight, Shadow, Other Settings; Shadow Color defaults to white tint.')
