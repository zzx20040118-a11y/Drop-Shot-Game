import copy,json,re,uuid,shutil
from pathlib import Path
root=Path(__file__).resolve().parents[1]
folder=root/'Assets/Shader/HalfTone'; path=folder/'SG_HalfTone.shadergraph'
backup=root/'HalftoneBackup/BeforeUnifiedRepair'
backup.mkdir(exist_ok=True)
def read(p):return [json.loads(s) for s in re.split(r'\n\s*\n',p.read_text(encoding='utf-8-sig').strip())]
for p in folder.iterdir():
    if p.is_file() and not (backup/p.name).exists():shutil.copy2(p,backup/p.name)
for p in (root/'Assets').rglob('*.mat'):
    if '6e7bb58b6c240f4408370ffbfc67e63c' in p.read_text(encoding='utf-8-sig'):
        dest=backup/p.relative_to(root/'Assets');dest.parent.mkdir(parents=True,exist_ok=True)
        if not dest.exists():shutil.copy2(p,dest)
objects=read(path);g=objects[0];d={o['m_ObjectId']:o for o in objects}
templates={}
samples=root/'Library/PackageCache/com.unity.shadergraph@aa8306cf39d6/Samples~/NodeReference/Math'
for collection in [objects]+[read(samples/(s+'.shadergraph')) for s in ['Saturate','Absolute','DDX','DDY','Maximum','OneMinus']]:
    lookup={o['m_ObjectId']:o for o in collection}
    for o in collection:
        if o['m_Type'].endswith('Node') and o['m_Type'] not in templates:
            templates[o['m_Type']]=(o,[lookup[r['m_Id']] for r in o.get('m_Slots',[])])
def uid():return uuid.uuid4().hex
def sid(n,name):return next(d[r['m_Id']]['m_Id'] for r in n['m_Slots'] if d[r['m_Id']]['m_DisplayName']==name)
def setval(n,name,value):
    s=next(d[r['m_Id']] for r in n['m_Slots'] if d[r['m_Id']]['m_DisplayName']==name)
    if isinstance(s.get('m_Value'),dict):s['m_Value']={k:value for k in s['m_Value']}
    else:s['m_Value']=value
def edge(a,ap,b,bp):
    g['m_Edges']=[e for e in g['m_Edges'] if not(e['m_InputSlot']['m_Node']['m_Id']==b['m_ObjectId'] and e['m_InputSlot']['m_SlotId']==sid(b,bp))]
    g['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':a['m_ObjectId']},'m_SlotId':sid(a,ap)},'m_InputSlot':{'m_Node':{'m_Id':b['m_ObjectId']},'m_SlotId':sid(b,bp)}})
def node(kind,x,y):
    proto,slots=templates['UnityEditor.ShaderGraph.'+kind+'Node']
    n=copy.deepcopy(proto);n.update(m_ObjectId=uid(),m_Slots=[],m_Group={'m_Id':''},m_PreviewExpanded=False)
    n['m_DrawState']={'m_Expanded':True,'m_Position':{'serializedVersion':'2','x':x,'y':y,'width':210,'height':150}}
    for s in slots:
        s=copy.deepcopy(s);s['m_ObjectId']=uid();objects.append(s);d[s['m_ObjectId']]=s;n['m_Slots'].append({'m_Id':s['m_ObjectId']})
    objects.append(n);d[n['m_ObjectId']]=n;g['m_Nodes'].append({'m_Id':n['m_ObjectId']});return n
def unary(kind,source,x,y):
    n=node(kind,x,y);edge(source,'Out',n,'In');return n
def binary(kind,a,b,x,y):
    n=node(kind,x,y)
    for src,port in [(a,'A'),(b,'B')]:
        if isinstance(src,dict):edge(src,'Out',n,port)
        else:setval(n,port,src)
    return n
def prop(name):
    p=next(o for o in objects if o['m_Type'].endswith('ShaderProperty') and o.get('m_Name')==name)
    n=next(o for o in objects if o.get('m_Property',{}).get('m_Id')==p['m_ObjectId'])
    return p,n
def propedge(name,target,port):
    p,n=prop(name);out=next(d[r['m_Id']]['m_DisplayName'] for r in n['m_Slots'])
    edge(n,out,target,port)

normal,_=prop('NormalMap');normal['m_DefaultType']=3
light=d['a5cbf4a946c840389a8c17e4f79f81d8']
light01=unary('Saturate',light,800,1250)
darkness=unary('OneMinus',light01,1050,1250)
hi=d['7357d0107fab4df196e9b1d3fdad5f0a'];sh=d['a9566b9341ea482690cbb8d68c795ad6']
edge(light01,'Out',hi,'In');edge(darkness,'Out',sh,'In')
defaults={'HighlightStart':.7,'HighlightEnd':1,'HighlightDotSize':.65,'Shadow Start':.65,'Shadow End':1,'Shadow Dot Size':.7,'Softness':.01}
for name,value in defaults.items():prop(name)[0]['m_Value']=value
for name in ['HighlightStart','HighlightEnd','HighlightDotSize','Shadow Start','Shadow End','Shadow Dot Size']:
    prop(name)[0].update(m_FloatType=1,m_RangeValues={'x':0,'y':1})
prop('Shadow Color')[0]['m_Value']={'r':.12,'g':.12,'b':.12,'a':1}

distance=d['cd1429064fb44b6d94afb366935d73d5']
dx=unary('DDX',distance,900,2100);dy=unary('DDY',distance,900,2320)
ax=unary('Absolute',dx,1150,2100);ay=unary('Absolute',dy,1150,2320)
sumderiv=binary('Add',ax,ay,1400,2100)
pixelwidth=binary('Maximum',sumderiv,.001,1650,2100)
aa=node('Maximum',1900,2100);edge(pixelwidth,'Out',aa,'A');propedge('Softness',aa,'B')

def dotmask(strength,size,x,y):
    sizeclamp=node('Saturate',x,y+230);propedge(size,sizeclamp,'In')
    scaled=binary('Multiply',sizeclamp,.45,x+250,y+230)
    radius=binary('Multiply',strength,scaled,x+500,y)
    low=binary('Subtract',radius,aa,x+750,y)
    high=binary('Add',radius,aa,x+750,y+230)
    smooth=node('Smoothstep',x+1000,y);edge(low,'Out',smooth,'Edge1');edge(high,'Out',smooth,'Edge2');edge(distance,'Out',smooth,'In')
    mask=unary('OneMinus',smooth,x+1250,y)
    gate=node('Step',x+1000,y+230);setval(gate,'Edge',.0001);edge(radius,'Out',gate,'In')
    return binary('Multiply',mask,gate,x+1500,y)
shadowmask=dotmask(sh,'Shadow Dot Size',1400,1300)
highlightmask=dotmask(hi,'HighlightDotSize',1400,500)
exclude=unary('OneMinus',shadowmask,3150,1150)
highlightmask=binary('Multiply',highlightmask,exclude,3400,500)
shadowlerp=d['6fc7416ddcb94b16b725b1507e4b4361']
base=d['71745dbd2e3a4aec932333a3957a9e8e']
edge(base,'Out',shadowlerp,'A');propedge('Shadow Color',shadowlerp,'B');edge(shadowmask,'Out',shadowlerp,'T')
highlightlerp=d['b3914f44a7a74c768a6c861f89fbcbd9']
edge(shadowlerp,'Out',highlightlerp,'A');edge(highlightmask,'Out',highlightlerp,'T')
rim=d['70ae6ecab2054e869867e88c88f887d3']
edge(highlightlerp,'Out',rim,'A')

# Keep only nodes reachable from output and the existing vertex blocks.
keep={r['m_Id'] for r in g['m_VertexContext']['m_Blocks']+g['m_FragmentContext']['m_Blocks']}
while True:
    parents={e['m_OutputSlot']['m_Node']['m_Id'] for e in g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id'] in keep}
    if parents<=keep:break
    keep|=parents
g['m_Nodes']=[r for r in g['m_Nodes'] if r['m_Id'] in keep]
g['m_Edges']=[e for e in g['m_Edges'] if e['m_OutputSlot']['m_Node']['m_Id'] in keep and e['m_InputSlot']['m_Node']['m_Id'] in keep]
props={d[k]['m_Property']['m_Id'] for k in keep if 'm_Property' in d[k]}
g['m_Properties']=[r for r in g['m_Properties'] if r['m_Id'] in props]
g['m_Keywords']=[];g['m_GroupDatas']=[]
categories=[o for o in objects if o['m_Type'].endswith('.CategoryData')]
for category in categories:category['m_ChildObjectList']=[r for r in category['m_ChildObjectList'] if r['m_Id'] in props]
needed=keep|props|{o['m_ObjectId'] for o in categories}|{g['m_ObjectId']}
needed|={r['m_Id'] for k in keep for r in d[k].get('m_Slots',[])}
needed|={r['m_Id'] for r in g['m_ActiveTargets']}
for k in list(needed):
    if k in d and 'm_ActiveSubTarget' in d[k]:needed.add(d[k]['m_ActiveSubTarget']['m_Id'])
objects=[o for o in objects if o['m_ObjectId'] in needed]
path.write_text('\n\n'.join(json.dumps(o,indent=4,ensure_ascii=False) for o in objects)+'\n',encoding='utf-8')

# Use the correct URP screen-shadow keyword when that renderer feature is active.
lighting=folder/'Lighting.hlsl'
lighting.write_text(lighting.read_text(encoding='utf-8-sig').replace('#if SHADOWS_SCREEN','#if defined(_MAIN_LIGHT_SHADOWS_SCREEN)'),encoding='utf-8')
print('Unified lighting, independent AA dot masks, direct colors, removed dead branches.')
print('Active nodes:',len(keep),'Properties:',len(props))
