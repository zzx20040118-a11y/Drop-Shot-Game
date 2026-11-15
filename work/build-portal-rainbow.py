from pathlib import Path
import json,copy,re,uuid,struct,zlib,shutil
root=Path('DropShot/Assets'); folder=root/'VFX/PortalEspacial/Mat'; scene=root/'Scenes/Game_B_Hard.unity'
backup=Path('work/before-bhard-rainbow');backup.mkdir(exist_ok=True);shutil.copy2(scene,backup/scene.name)
def uid():return uuid.uuid4().hex
def readgraph(p):
 s=p.read_text();d=json.JSONDecoder();out=[]
 while s.strip():
  a,n=d.raw_decode(s.lstrip());out.append(a);s=s.lstrip()[n:]
 return out
objs=readgraph(folder/'SG_Portal.shadergraph');g=objs[0];by={o['m_ObjectId']:o for o in objs}
def clone_node(src,x,y):
 n=copy.deepcopy(by[src]);n['m_ObjectId']=uid();n['m_DrawState']['m_Position'].update(x=x,y=y)
 for ref in n['m_Slots']:
  sl=copy.deepcopy(by[ref['m_Id']]);sl['m_ObjectId']=uid();ref['m_Id']=sl['m_ObjectId'];objs.append(sl);by[sl['m_ObjectId']]=sl
 objs.append(n);by[n['m_ObjectId']]=n;g['m_Nodes'].append({'m_Id':n['m_ObjectId']});return n
 defunused=0
def edge(a,sa,b,sb):g['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':a},'m_SlotId':sa},'m_InputSlot':{'m_Node':{'m_Id':b},'m_SlotId':sb}})
def disconnect(b,sb):g['m_Edges'][:]=[e for e in g['m_Edges'] if not(e['m_InputSlot']['m_Node']['m_Id']==b and e['m_InputSlot']['m_SlotId']==sb)]
# A small procedural colour lookup, not a replacement for the existing noise/alpha.
stops=[(0,'AC6AFF'),(.18,'FF66BE'),(.42,'FF875D'),(.57,'FFE18A'),(.69,'82EFBA'),(.83,'64C9F2'),(1,'8265DD')]
colors=[(t,tuple(int(h[i:i+2],16) for i in (0,2,4))) for t,h in stops]
pixels=[]
for x in range(256):
 t=x/255
 for (a,ca),(b,cb) in zip(colors,colors[1:]):
  if a<=t<=b:
   f=(t-a)/(b-a);pixels.extend(round(aa+(bb-aa)*f) for aa,bb in zip(ca,cb));pixels.append(255);break
raw=b''.join(b'\0'+bytes(pixels) for _ in range(4))
def chunk(tag,data):return struct.pack('>I',len(data))+tag+data+struct.pack('>I',zlib.crc32(tag+data)&0xffffffff)
tex=folder/'PortalRainbowRamp_BHard.png';tex.write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',256,4,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(raw))+chunk(b'IEND',b''))
tguid=uid();meta=(root/'VFX/PortalEspacial/Tex/Noise.png.meta').read_text();meta=re.sub(r'^guid: \w+',f'guid: {tguid}',meta,flags=re.M);meta=meta.replace('enableMipMap: 1','enableMipMap: 0');meta=re.sub(r'wrap[UVW]: \d+',lambda m:m[0].split(':')[0]+': 1',meta);tex.with_suffix('.png.meta').write_text(meta)
prop=copy.deepcopy(by['41ee62a3eec04b69b21c8d70e72537e1']);prop['m_ObjectId']=uid();prop['m_Guid']['m_GuidSerialized']=str(uuid.uuid4());prop['m_Name']='Rainbow Ramp';prop['m_DefaultReferenceName']='_RainbowRamp';prop['m_OverrideReferenceName']='_RainbowRamp';prop['m_Value']['m_SerializedTexture']=json.dumps({'texture':{'fileID':2800000,'guid':tguid,'type':3}});objs.append(prop);g['m_Properties'].append({'m_Id':prop['m_ObjectId']})
pn=clone_node('3df86a741db7441db0d47d00ff71dc74',-700,650);pn['m_Property']['m_Id']=prop['m_ObjectId']
sample=clone_node('86d5d97168da4ae6aa2276c9b9beeb50',-420,650)
radial=clone_node('1b86239405414188b6a7a6457aa63d46',-700,950)
blend=clone_node('1b86239405414188b6a7a6457aa63d46',-50,200)
alpha=clone_node('e322725ad52b4fef95513a776b318eea',-1450,50)
edge(pn['m_ObjectId'],0,sample['m_ObjectId'],1);edge('ee8ce689a800416d94cc69ffb08a9c04',1,radial['m_ObjectId'],0);edge(radial['m_ObjectId'],2,sample['m_ObjectId'],2)
# Keep particle opacity while avoiding the old pink particle tint.
disconnect('638cd637781f493a877cf70591113423',0);edge('14b9e5308e5143eaa57f796feea74627',0,alpha['m_ObjectId'],0);edge(alpha['m_ObjectId'],4,'638cd637781f493a877cf70591113423',0)
disconnect('7c567b42506a4696873232a04a964a08',0);edge('1b86239405414188b6a7a6457aa63d46',2,blend['m_ObjectId'],0);edge(sample['m_ObjectId'],0,blend['m_ObjectId'],1);edge(blend['m_ObjectId'],2,'7c567b42506a4696873232a04a964a08',0)
by['182535f29a95460db45230fd66e480e5']['m_Name']='Tint / Glow (HDR)'
sg=folder/'SG_PortalRainbow_BHard.shadergraph';sg.write_text('\n\n'.join(json.dumps(o,indent=4) for o in objs)+'\n');sguid=uid();sg.with_suffix('.shadergraph.meta').write_text((folder/'SG_Portal.shadergraph.meta').read_text().replace('f0ece6ed1f780304b88a88626e60f7da',sguid))
def material(src,name,shader=None,color=None):
 s=(folder/src).read_text();s=re.sub(r'  m_Name: .*',f'  m_Name: {name}',s,count=1)
 if shader:s=s.replace('f0ece6ed1f780304b88a88626e60f7da',shader)
 if color:s=re.sub(r'    - _Color: .*',f'    - _Color: {color}',s)
 p=folder/(name+'.mat');p.write_text(s);guid=uid();p.with_suffix('.mat.meta').write_text('fileFormatVersion: 2\nguid: '+guid+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n');return guid
spiral=material('M_Sprial.mat','M_PortalRainbow_BHard',sguid,'{r: 1.65, g: 1.65, b: 1.65, a: 1}')
ring=material('CircleHollow01.mat','M_PortalRim_BHard',color='{r: 1.3, g: 0.55, b: 1.8, a: 1}')
# The old backing references a missing material; give this scene its own dark centre.
centre=material('M_Portal.mat','M_PortalCentre_BHard',color='{r: 0.002, g: 0.001, b: 0.006, a: 1}')
s=scene.read_text();pat=r'(--- !u!1001 &2108575432\n.*?)(    m_RemovedComponents: \[\])';m=re.search(pat,s,re.S);assert m
mods=''
for renderer,mat in [(4277329074829396435,spiral),(315728224406089637,ring),(1235624873456642676,centre)]:
 mods+=f"    - target: {{fileID: {renderer}, guid: 8b8351c027f776e4583703d0b4d3a6fb, type: 3}}\n      propertyPath: m_Materials.Array.data[0]\n      value: \n      objectReference: {{fileID: 2100000, guid: {mat}, type: 2}}\n"
s=s[:m.start()]+m[1]+mods+m[2]+s[m.end():];scene.write_text(s)
print('Created isolated rainbow graph, ramp, spiral/rim/centre materials; assigned only Game_B_Hard.')
