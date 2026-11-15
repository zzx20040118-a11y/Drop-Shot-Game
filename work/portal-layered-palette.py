from pathlib import Path
import json,copy,re,uuid,struct,zlib,shutil
root=Path('DropShot/Assets');folder=root/'VFX/PortalEspacial/Mat';backup=Path('work/before-portal-layered-palette');backup.mkdir(exist_ok=True)
for name in ['SG_PortalRainbow_BHard.shadergraph','M_PortalRainbow_BHard.mat','M_PortalRingColour_BHard.mat']:shutil.copy2(folder/name,backup/name)
def uid():return uuid.uuid4().hex
def load(p):
 s=p.read_text();d=json.JSONDecoder();out=[]
 while s.strip():
  o,n=d.raw_decode(s.lstrip());out.append(o);s=s.lstrip()[n:]
 return out
prev=load(folder/'SG_PortalRainbow_BHard.shadergraph');objs=load(folder/'SG_Portal.shadergraph');g=objs[0];by={o['m_ObjectId']:o for o in objs}
def node(src,x,y):
 n=copy.deepcopy(by[src]);n['m_ObjectId']=uid();n['m_DrawState']['m_Position'].update(x=x,y=y)
 for r in n['m_Slots']:
  sl=copy.deepcopy(by[r['m_Id']]);sl['m_ObjectId']=uid();r['m_Id']=sl['m_ObjectId'];objs.append(sl);by[sl['m_ObjectId']]=sl
 objs.append(n);by[n['m_ObjectId']]=n;g['m_Nodes'].append({'m_Id':n['m_ObjectId']});return n
# Controlled radial palette: warm band is broad, cool colours are accents.
stops=[(0.,'301B54'),(.20,'7444AB'),(.34,'D15CBA'),(.46,'FF967C'),(.65,'FFD39B'),(.73,'A5E2BC'),(.83,'75BBD3'),(1.,'6758AB')]
cols=[(t,tuple(int(h[i:i+2],16) for i in (0,2,4))) for t,h in stops];pixels=[]
for x in range(512):
 t=x/511
 for (a,ca),(b,cb) in zip(cols,cols[1:]):
  if a<=t<=b:
   f=(t-a)/(b-a);f=f*f*(3-2*f);pixels.extend(round(aa+(bb-aa)*f) for aa,bb in zip(ca,cb));pixels.append(255);break
raw=b''.join(b'\0'+bytes(pixels) for _ in range(4))
def chunk(tag,data):return struct.pack('>I',len(data))+tag+data+struct.pack('>I',zlib.crc32(tag+data)&0xffffffff)
p=folder/'PortalSoftPalette_BHard.png';assert not p.exists();p.write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',512,4,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(raw))+chunk(b'IEND',b''))
tguid=uid();meta=(root/'VFX/PortalEspacial/Tex/Noise.png.meta').read_text();meta=re.sub(r'^guid: \w+',f'guid: {tguid}',meta,flags=re.M);meta=meta.replace('enableMipMap: 1','enableMipMap: 0');meta=re.sub(r'wrap[UVW]: \d+',lambda m:m[0].split(':')[0]+': 1',meta);p.with_suffix('.png.meta').write_text(meta)
# Copy just the two public controls from the previous version.
for prop in [o for o in prev if o.get('m_DefaultReferenceName') in ['_Brightness','_ColourSpread']]:
 prop=copy.deepcopy(prop);prop['m_Value']=1.8 if prop['m_DefaultReferenceName']=='_Brightness' else 2.;objs.append(prop);g['m_Properties'].append({'m_Id':prop['m_ObjectId']})
 if prop['m_DefaultReferenceName']=='_ColourSpread':prop['m_Name']='Colour Scale';prop['m_RangeValues']={'x':.5,'y':3.}
# Reuse validated property nodes and scalar slots for those controls.
controls={}
prevby={o['m_ObjectId']:o for o in prev}
for ref in ['_Brightness','_ColourSpread']:
 prop=next(o for o in objs if o.get('m_DefaultReferenceName')==ref)
 n=next(o for o in prev if o.get('m_Property',{}).get('m_Id')==prop['m_ObjectId']);n=copy.deepcopy(n)
 for sl in n['m_Slots']:objs.append(copy.deepcopy(prevby[sl['m_Id']]))
 objs.append(n);g['m_Nodes'].append({'m_Id':n['m_ObjectId']});controls[ref]=n
prop=copy.deepcopy(by['41ee62a3eec04b69b21c8d70e72537e1']);prop.update(m_ObjectId=uid(),m_Name='Colour Palette',m_DefaultReferenceName='_ColourPalette',m_OverrideReferenceName='_ColourPalette');prop['m_Guid']['m_GuidSerialized']=str(uuid.uuid4());prop['m_Value']['m_SerializedTexture']=json.dumps({'texture':{'fileID':2800000,'guid':tguid,'type':3}});objs.append(prop);g['m_Properties'].append({'m_Id':prop['m_ObjectId']})
pn=node('3df86a741db7441db0d47d00ff71dc74',-750,900);pn['m_Property']['m_Id']=prop['m_ObjectId']
sub=node('a220d3ce16bc4576bb7d507ba6f3b18e',-1500,850)
# Explicit Vector2 slots avoid hidden Z/W contributions in the radius.
for r in sub['m_Slots']:
 sl=by[r['m_Id']];sl['m_Type']='UnityEditor.ShaderGraph.Vector2MaterialSlot';v=.5 if sl['m_Id']==1 else 0.;sl['m_Value']={'x':v,'y':v};sl['m_DefaultValue']={'x':v,'y':v}
length=node('ee8ce689a800416d94cc69ffb08a9c04',-1200,850)
scale=node('1b86239405414188b6a7a6457aa63d46',-950,850)
sample=node('86d5d97168da4ae6aa2276c9b9beeb50',-500,850)
colour=node('1b86239405414188b6a7a6457aa63d46',-200,250)
bright=node('1b86239405414188b6a7a6457aa63d46',100,250)
def edge(a,sa,b,sb):g['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':a},'m_SlotId':sa},'m_InputSlot':{'m_Node':{'m_Id':b},'m_SlotId':sb}})
g['m_Edges'][:]=[e for e in g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id']!='7c567b42506a4696873232a04a964a08']
edge('676b304bdd09489ab7a42bf4228c7a33',3,sub['m_ObjectId'],0);edge(sub['m_ObjectId'],2,length['m_ObjectId'],0);edge(length['m_ObjectId'],1,scale['m_ObjectId'],0);edge(controls['_ColourSpread']['m_ObjectId'],0,scale['m_ObjectId'],1);edge(scale['m_ObjectId'],2,sample['m_ObjectId'],2);edge(pn['m_ObjectId'],0,sample['m_ObjectId'],1)
edge('1b86239405414188b6a7a6457aa63d46',2,colour['m_ObjectId'],0);edge(sample['m_ObjectId'],0,colour['m_ObjectId'],1);edge(colour['m_ObjectId'],2,bright['m_ObjectId'],0);edge(controls['_Brightness']['m_ObjectId'],0,bright['m_ObjectId'],1);edge(bright['m_ObjectId'],2,'7c567b42506a4696873232a04a964a08',0)
(folder/'SG_PortalRainbow_BHard.shadergraph').write_text('\n\n'.join(json.dumps(o,indent=4) for o in objs)+'\n')
for name,value in [('M_PortalRainbow_BHard.mat',2.1),('M_PortalRingColour_BHard.mat',2.5)]:
 p=folder/name;s=p.read_text();s=re.sub(r'    - _Brightness: .*',f'    - _Brightness: {value}',s);s=re.sub(r'    - _ColourSpread: .*','    - _ColourSpread: 2',s)
 # Explicit material slot keeps the palette visible and replaceable in Inspector.
 s=s.replace('    m_TexEnvs:\n',f'    m_TexEnvs:\n    - _ColourPalette:\n        m_Texture: {{fileID: 2800000, guid: {tguid}, type: 3}}\n        m_Scale: {{x: 1, y: 1}}\n        m_Offset: {{x: 0, y: 0}}\n');p.write_text(s)
by={o['m_ObjectId']:o for o in objs};assert len(by)==len(objs)
for e in g['m_Edges']:
 for key in ['m_InputSlot','m_OutputSlot']:
  ep=e[key];n=by[ep['m_Node']['m_Id']];assert any(by[slot['m_Id']]['m_Id']==ep['m_SlotId'] for slot in n['m_Slots'])
original=load(folder/'SG_Portal.shadergraph')
for e in original[0]['m_Edges']:
 if e['m_InputSlot']['m_Node']['m_Id']!='7c567b42506a4696873232a04a964a08':assert e in g['m_Edges']
print('PASS: radial palette replaces repeated hue shifting; original opacity/shape/animation connections unchanged.')
