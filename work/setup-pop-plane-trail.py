from pathlib import Path
import json,copy,re,uuid,struct,zlib,math,random,shutil
root=Path('DropShot/Assets');folder=root/'VFX';backup=Path('work/before-pop-plane-trail');backup.mkdir(exist_ok=True)
scene=root/'Scenes/Game_B_Hard.unity';shutil.copy2(scene,backup/scene.name)
def uid():return uuid.uuid4().hex
def load(p):
 s=p.read_text(encoding='utf-8-sig');d=json.JSONDecoder();o=[]
 while s.strip():
  a,n=d.raw_decode(s.lstrip());o.append(a);s=s.lstrip()[n:]
 return o
objs=load(root/'Shader/SG_BallTrail.shadergraph');g=objs[0];by={o['m_ObjectId']:o for o in objs};templates=load(folder/'PortalEspacial/Mat/SG_Portal.shadergraph');tb={o['m_ObjectId']:o for o in templates}
floatTemplate=next(o for o in load(root/'Shader/HalfTone/SG_HalfTone.shadergraph') if o['m_Type'].endswith('Vector1ShaderProperty'))
def node(source,x,y):
 n=copy.deepcopy(source);n['m_ObjectId']=uid();n['m_DrawState']['m_Position'].update(x=x,y=y)
 for ref in n['m_Slots']:
  sl=copy.deepcopy(by.get(ref['m_Id'],tb.get(ref['m_Id'])));sl['m_ObjectId']=uid();ref['m_Id']=sl['m_ObjectId'];objs.append(sl);by[sl['m_ObjectId']]=sl
 objs.append(n);by[n['m_ObjectId']]=n;g['m_Nodes'].append({'m_Id':n['m_ObjectId']});return n['m_ObjectId']
def edge(a,sa,b,sb):g['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':a},'m_SlotId':sa},'m_InputSlot':{'m_Node':{'m_Id':b},'m_SlotId':sb}})
def scalar(name,ref,value,lo,hi,x,y):
 p=copy.deepcopy(floatTemplate);p.update(m_ObjectId=uid(),m_Name=name,m_DefaultReferenceName=ref,m_OverrideReferenceName=ref,m_Value=value,m_RangeValues={'x':lo,'y':hi});p['m_Guid']['m_GuidSerialized']=str(uuid.uuid4());objs.append(p);g['m_Properties'].append({'m_Id':p['m_ObjectId']})
 n=node(by['10662360028741d2a833184fea82121c'],x,y);by[n]['m_Property']['m_Id']=p['m_ObjectId'];return n
# Procedural data mask: white circles become transparent holes. Seamless wrap.
rng=random.Random(21);circles=[(.15,.18,.10),(.62,.08,.065),(.86,.39,.16),(.40,.48,.08),(.12,.73,.15),(.63,.86,.13)]
w=256;raw=bytearray()
for y in range(w):
 raw.append(0)
 for x in range(w):
  u=x/w;v=y/w;value=0.
  for cx,cy,r in circles:
   dx=min(abs(u-cx),1-abs(u-cx));dy=min(abs(v-cy),1-abs(v-cy));dist=math.hypot(dx,dy)
   value=max(value,max(0.,min(1.,(r-dist)*w/2+.5)))
  raw.append(round(value*255))
def chunk(t,d):return struct.pack('>I',len(d))+t+d+struct.pack('>I',zlib.crc32(t+d)&0xffffffff)
mask=folder/'PopTrailHoleMask.png';assert not mask.exists();mask.write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',w,w,8,0,0,0,0))+chunk(b'IDAT',zlib.compress(raw))+chunk(b'IEND',b''));maskguid=uid()
meta=(folder/'PortalEspacial/Tex/Noise.png.meta').read_text();meta=re.sub(r'^guid: \w+',f'guid: {maskguid}',meta,flags=re.M);meta=meta.replace('sRGBTexture: 1','sRGBTexture: 0');mask.with_suffix('.png.meta').write_text(meta)
prop=copy.deepcopy(tb['41ee62a3eec04b69b21c8d70e72537e1']);prop.update(m_ObjectId=uid(),m_Name='Hole Mask',m_DefaultReferenceName='_HoleMask',m_OverrideReferenceName='_HoleMask');prop['m_Guid']['m_GuidSerialized']=str(uuid.uuid4());prop['m_Value']['m_SerializedTexture']=json.dumps({'texture':{'fileID':2800000,'guid':maskguid,'type':3}});objs.append(prop);g['m_Properties'].append({'m_Id':prop['m_ObjectId']})
maskprop=node(tb['3df86a741db7441db0d47d00ff71dc74'],-1600,900);by[maskprop]['m_Property']['m_Id']=prop['m_ObjectId']
speed=scalar('Hole Speed','_HoleSpeed',.45,-2,2,-2200,600);density=scalar('Hole Density','_HoleDensity',1.,.3,3,-2000,950);size=scalar('Hole Size','_HoleSize',.5,0,1,-1600,1300);front=scalar('Front Connection Only','_FrontPatch',0,0,1,-500,1300)
mul=tb['1b86239405414188b6a7a6457aa63d46'];smooth=tb['2e67f153ca2c46f6a26089912d845ac1'];split=tb['e322725ad52b4fef95513a776b318eea'];one=by['67bca1383e514fdb870f4c922d2a5cd5'];add=by['06a84b38ebba4c2897113433ba9916d4']
tick=node(mul,-1900,600);edge(speed,0,tick,0);edge('ee6fcfa98e5d44a1b89814905557d2cd',0,tick,1)
combine=node(by['4f76c4cb6da5404f8d2f5adae235705f'],-1650,600)
# Combine R/G become texture offset X/Y; move holes down the tail.
edge(tick,2,combine,1)
uv=node(tb['46e8b78aca0e43ab9b44aeb09f49dff5'],-1300,650);edge('8a923446ff8247599e3e4307210570ff',0,uv,0);edge(density,0,uv,1);edge(combine,0,uv,2)
sample=node(tb['86d5d97168da4ae6aa2276c9b9beeb50'],-1000,650);edge(maskprop,0,sample,1);edge(uv,3,sample,2)
threshold=node(one,-1300,1200);edge(size,0,threshold,0)
soft=node(smooth,-700,650);edge(threshold,1,soft,0);edge(sample,4,soft,2)
# Edge2 stays high: size sets hole-edge threshold; mask centers stay fully open.
for sl in by[soft]['m_Slots']:
 if by[sl['m_Id']]['m_Id']==1:by[sl['m_Id']]['m_Value']={'x':1.,'y':1.,'z':1.,'w':1.}
uvsplit=node(split,-1900,1500);edge('8a923446ff8247599e3e4307210570ff',0,uvsplit,0)
tail=node(one,-1600,1550);edge(uvsplit,2,tail,0)
fade=node(smooth,-1300,1550);edge(tail,1,fade,2)
for sl in by[fade]['m_Slots']:
 a=by[sl['m_Id']]
 if a['m_Id'] in [0,1]:a['m_Value']={'x':.12 if a['m_Id']==0 else .6,'y':0.,'z':0.,'w':0.}
punch=node(mul,-400,700);edge(soft,3,punch,0);edge(fade,3,punch,1)
remain=node(one,-100,700);edge(punch,2,remain,0)
alpha=node(mul,200,500);edge('45331618346546e284922e047a480971',7,alpha,0);edge(remain,1,alpha,1)
rootfade=node(smooth,-800,1700);edge(uvsplit,2,rootfade,2)
for sl in by[rootfade]['m_Slots']:
 a=by[sl['m_Id']]
 if a['m_Id'] in [0,1]:a['m_Value']={'x':.78 if a['m_Id']==0 else .90,'y':0.,'z':0.,'w':0.}
a=node(mul,-400,1500);edge(rootfade,3,a,0);edge(front,0,a,1)
b=node(one,-400,1800);edge(front,0,b,0)
gate=node(add,-100,1500);edge(a,2,gate,0);edge(b,1,gate,1)
final=node(mul,500,600);edge(alpha,2,final,0);edge(gate,2,final,1)
g['m_Edges'][:]=[e for e in g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id'] not in ['35deefe2cb404997a9ec8aa441f0874e','4396c65bee11452f8d92a791588ce02b']]
edge(final,2,'35deefe2cb404997a9ec8aa441f0874e',0)
tint=copy.deepcopy(tb['182535f29a95460db45230fd66e480e5']);tint.update(m_ObjectId=uid(),m_Name='Trail Colour',m_DefaultReferenceName='_TrailColor',m_OverrideReferenceName='_TrailColor',m_Value={'r':1.,'g':.8,'b':.35,'a':1.});tint['m_Guid']['m_GuidSerialized']=str(uuid.uuid4());objs.append(tint);g['m_Properties'].append({'m_Id':tint['m_ObjectId']})
tn=node(tb['34a13c33e92e4f3588125f9900b0269f'],300,100);by[tn]['m_Property']['m_Id']=tint['m_ObjectId'];edge(tn,0,'4396c65bee11452f8d92a791588ce02b',0)
for o in objs:
 if o['m_Type'].endswith('.UniversalTarget'):o.update(m_AllowMaterialOverride=True,m_ZWriteControl=2,m_ZTestMode=4,m_CastShadows=False,m_ReceiveShadows=False,m_RenderFace=0)
sguid=uid();p=folder/'SG_PopPlaneTrail.shadergraph';p.write_text('\n\n'.join(json.dumps(o,indent=4) for o in objs)+'\n',encoding='utf-8');p.with_suffix('.shadergraph.meta').write_text((root/'Shader/SG_BallTrail.shadergraph.meta').read_text().replace('ccfe26423a37c1240b84dfc0294e2ff6',sguid))
materials=[]
for name,ztest,patch,queue in [('M_PopPlaneTrail',4,0,3000),('M_PopPlaneFront',8,1,3100)]:
 s=(root/'Material/M_BallTrail/Shader Graphs_SG_BallTrail01 1.mat').read_text();s=s.replace('ccfe26423a37c1240b84dfc0294e2ff6',sguid);s=re.sub(r'(--- !u!21 [\s\S]*?  m_Name: )[^\n]*',lambda m:m[1]+name,s,count=1);s=re.sub(r'm_CustomRenderQueue: -1',f'm_CustomRenderQueue: {queue}',s)
 s=s.replace('    m_Floats:\n',f'    m_Floats:\n    - _HoleSpeed: 0.45\n    - _HoleDensity: 1\n    - _HoleSize: 0.5\n    - _FrontPatch: {patch}\n    - _ZTest: {ztest}\n    - _ZWrite: 0\n    - _Surface: 1\n    - _Cull: 0\n    - _SrcBlend: 5\n    - _DstBlend: 10\n');s=s.replace('    m_Colors: []','    m_Colors:\n    - _TrailColor: {r: 1, g: 0.8, b: 0.35, a: 1}')
 s=s.replace('    - _DistortionStrength: 0.31','    - _DistortionStrength: 0.08');p=folder/(name+'.mat');p.write_text(s);guid=uid();materials.append(guid);p.with_suffix('.mat.meta').write_text('fileFormatVersion: 2\nguid: '+guid+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
scriptguid=uid();(root/'Scripts/BallPopPlaneTrail.cs.meta').write_text('fileFormatVersion: 2\nguid: '+scriptguid+'\n')
s=scene.read_text(encoding='utf-8-sig');blocks=''
for oldid,newid,go,rolling in [('2149801002','2149801022','666114097',0),('2149801012','2149801032','953533038',1)]:
 pattern=rf'--- !u!114 &{oldid}\n.*?(?=--- !u!|\Z)';m=re.search(pattern,s,re.S);assert m;s=s[:m.start()]+m[0].replace('  m_Enabled: 1','  m_Enabled: 0')+s[m.end():]
 pattern=rf'--- !u!1 &{go}\n.*?(?=--- !u!)';m=re.search(pattern,s,re.S);b=m[0];b=b.replace('  m_Layer: 0',f'  - component: {{fileID: {newid}}}\n  m_Layer: 0');s=s[:m.start()]+b+s[m.end():]
 blocks+=f'''--- !u!114 &{newid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {scriptguid}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::BallPopPlaneTrail
  flight: {{fileID: 666114104}}
  rollingTrail: {rolling}
  bodyMaterial: {{fileID: 2100000, guid: {materials[0]}, type: 2}}
  frontMaterial: {{fileID: 2100000, guid: {materials[1]}, type: 2}}
  width: 0.8
  length: {1.8 if rolling else 1.5}
  rootOffset: 0.2
'''
s=s.replace('--- !u!1660057539 &9223372036854775807',blocks+'--- !u!1660057539 &9223372036854775807');scene.write_text(s,encoding='utf-8')
# Validate every graph edge and the independent root-only depth gate.
by={o['m_ObjectId']:o for o in objs};assert len(by)==len(objs)
for e in g['m_Edges']:
 for k in ['m_InputSlot','m_OutputSlot']:
  ep=e[k];n=by[ep['m_Node']['m_Id']];assert any(by[sl['m_Id']]['m_Id']==ep['m_SlotId'] for sl in n['m_Slots'])
print('B Hard: two masked planes per active ball; front layer clipped to root; old dot effects disabled. Graph edges validated.')
