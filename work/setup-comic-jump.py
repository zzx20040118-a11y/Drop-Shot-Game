from pathlib import Path
import json,re,uuid,copy,shutil
root=Path('DropShot/Assets');folder=root/'VFX';backup=Path('work/before-comic-jump');backup.mkdir(exist_ok=True)
scene=root/'Scenes/Game_B_Hard.unity';shutil.copy2(scene,backup/scene.name)
def uid():return uuid.uuid4().hex
sguid=uid();mguid=uid();scriptguid=uid()
(root/'Scripts/BallJumpPopVFX.cs.meta').write_text('fileFormatVersion: 2\nguid: '+scriptguid+'\n',encoding='utf-8')
s=(folder/'PortalEspacial/Mat/SG_Portal.shadergraph').read_text(encoding='utf-8-sig');d=json.JSONDecoder();objs=[]
while s.strip():
 o,n=d.raw_decode(s.lstrip());objs.append(o);s=s.lstrip()[n:]
by={o['m_ObjectId']:o for o in objs};g=objs[0]
keep=['0c3143f69294486985696438b763808f','7c567b42506a4696873232a04a964a08','e72d4192cb824073986308ecd65f9f66','3df86a741db7441db0d47d00ff71dc74','86d5d97168da4ae6aa2276c9b9beeb50','14b9e5308e5143eaa57f796feea74627','e322725ad52b4fef95513a776b318eea','418869f42ad34edb99419aa26e1b55f9']
g['m_Nodes']=[{'m_Id':i} for i in keep];g['m_VertexContext']['m_Blocks']=[{'m_Id':keep[0]}];g['m_FragmentContext']['m_Blocks']=[{'m_Id':keep[1]},{'m_Id':keep[2]}];g['m_Properties']=[{'m_Id':'41ee62a3eec04b69b21c8d70e72537e1'}];g['m_CategoryData']=[];g['m_Edges']=[]
def edge(a,sa,b,sb):g['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':a},'m_SlotId':sa},'m_InputSlot':{'m_Node':{'m_Id':b},'m_SlotId':sb}})
edge(keep[3],0,keep[4],1);edge(keep[5],0,keep[1],0);edge(keep[5],0,keep[6],0);edge(keep[4],7,keep[7],0);edge(keep[6],4,keep[7],1);edge(keep[7],2,keep[2],0)
prop=by['41ee62a3eec04b69b21c8d70e72537e1'];prop['m_Value']['m_SerializedTexture']=json.dumps({'texture':{'fileID':2800000,'guid':'66472af148a3d43a795242a238fd72ac','type':3}});prop['isMainTexture']=True
ids=set(keep+[prop['m_ObjectId'],g['m_ObjectId']]);ids.update(ref['m_Id'] for i in keep for ref in by[i].get('m_Slots',[]))
for o in objs:
 if 'Target' in o['m_Type']:
  ids.add(o['m_ObjectId'])
  if 'm_CastShadows' in o:o['m_CastShadows']=False;o['m_ReceiveShadows']=False
p=folder/'SG_ComicJump.shadergraph';assert not p.exists();p.write_text('\n\n'.join(json.dumps(o,indent=4) for o in objs if o['m_ObjectId'] in ids)+'\n',encoding='utf-8');p.with_suffix('.shadergraph.meta').write_text((folder/'PortalEspacial/Mat/SG_Portal.shadergraph.meta').read_text().replace('f0ece6ed1f780304b88a88626e60f7da',sguid))
s=(folder/'PortalEspacial/Mat/M_Sprial.mat').read_text();s=s.replace('f0ece6ed1f780304b88a88626e60f7da',sguid)
s=re.sub(r'(--- !u!21 [\s\S]*?  m_Name: )[^\n]*',lambda m:m[1]+'M_ComicJump',s,count=1);s=s.replace('guid: 4cd57a21b52f00f47a78d6b869064cb9','guid: 66472af148a3d43a795242a238fd72ac')
p=folder/'M_ComicJump.mat';p.write_text(s);p.with_suffix('.mat.meta').write_text('fileFormatVersion: 2\nguid: '+mguid+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
s=scene.read_text(encoding='utf-8-sig')
# Disable only the old trail instances in B Hard, preserving each prefab asset.
count=0
def oldtrail(m):
 global count
 b=m[0]
 for guid,goid in [('3e2897e7c74bce04c89de42f7c248350','5017178436238072501'),('cfbea87fe3365ad4fb0f9742a8bc5934','449500957533937570')]:
  if f'm_SourcePrefab: {{fileID: 100100000, guid: {guid}, type: 3}}' not in b:continue
  pat=rf'(    - target: \{{fileID: {goid}, guid: {guid}, type: 3\}}\n      propertyPath: m_IsActive\n      value: )\d+'
  if re.search(pat,b):b=re.sub(pat,lambda x:x[1]+'0',b)
  else:b=b.replace('    m_RemovedComponents: []',f'    - target: {{fileID: {goid}, guid: {guid}, type: 3}}\n      propertyPath: m_IsActive\n      value: 0\n      objectReference: {{fileID: 0}}\n    m_RemovedComponents: []')
  count+=1
 return b
s=re.sub(r'--- !u!1001 &\d+\n.*?(?=--- !u!|\Z)',oldtrail,s,flags=re.S);assert count==4
m=re.search(r'--- !u!1 &666114097\n.*?(?=--- !u!)',s,re.S);b=m[0];b=b.replace('  m_Layer: 0','  - component: {fileID: 2149801002}\n  m_Layer: 0');s=s[:m.start()]+b+s[m.end():]
block=f'''--- !u!114 &2149801002
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 666114097}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {scriptguid}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::BallJumpPopVFX
  flight: {{fileID: 666114104}}
  particleMaterial: {{fileID: 2100000, guid: {mguid}, type: 2}}
  trailInterval: 0.065
  trailLifetime: 0.24
  dotSize: 0.18
  burstSize: 0.7
  fillColor: {{r: 1, g: 0.87, b: 0.48, a: 1}}
  outlineColor: {{r: 0.2, g: 0.09, b: 0.05, a: 1}}
'''
s=s.replace('--- !u!1660057539 &9223372036854775807',block+'--- !u!1660057539 &9223372036854775807');scene.write_text(s,encoding='utf-8')
print('B Hard configured: 4 old trail instances disabled; new pooled particle feedback attached. Flight scripts unchanged.')
