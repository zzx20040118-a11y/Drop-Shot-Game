import copy,json,math,pathlib,re,uuid
from PIL import Image,ImageDraw
root=pathlib.Path('DropShot/Assets'); uid=lambda:uuid.uuid4().hex
script_guid=uid(); burst_guid=uid(); texture_guid=uid()
(root/'Scripts/ShortNoteVFX.cs.meta').write_text('fileFormatVersion: 2\nguid: '+script_guid+'\n',encoding='utf-8')
path=root/'Shader/HalfTone/SG_HalfTone.shadergraph'
s=path.read_text(encoding='utf-8-sig'); decoder=json.JSONDecoder(); objs=[]; raw=[]
while s.strip():
 s=s.lstrip(); obj,n=decoder.raw_decode(s); objs.append(obj); raw.append(s[:n]); s=s[n:]
graph=objs[0]; by={o['m_ObjectId']:o for o in objs}; added=[]
def clone_node(name,x,y):
 original=next(o for o in objs if o.get('m_Name')==name)
 node=copy.deepcopy(original); node['m_ObjectId']=uid(); node['m_Group']={'m_Id':''}
 node['m_DrawState']['m_Position'].update(x=x,y=y)
 slots=[]
 for ref in node['m_Slots']:
  slot=copy.deepcopy(by[ref['m_Id']]); slot['m_ObjectId']=uid(); slots.append(slot); ref['m_Id']=slot['m_ObjectId']
 added.extend([node]+slots); graph['m_Nodes'].append({'m_Id':node['m_ObjectId']})
 return node,slots
prop=copy.deepcopy(next(o for o in objs if o.get('m_DefaultReferenceName')=='_GlowColor'))
prop.update(m_ObjectId=uid(),m_Type='UnityEditor.ShaderGraph.Internal.Vector3ShaderProperty',m_Name='Note Visual Offset',m_DefaultReferenceName='_NoteVisualOffset',m_OverrideReferenceName='_NoteVisualOffset',m_RefNameGeneratedByDisplayName='Note Visual Offset',m_Value={'x':0.0,'y':0.0,'z':0.0,'w':0.0})
prop['m_Guid']={'m_GuidSerialized':str(uuid.uuid4())}; prop.pop('isMainColor',None);prop.pop('m_ColorMode',None)
graph['m_Properties'].append({'m_Id':prop['m_ObjectId']});added.append(prop)
pos,_=clone_node('Position',-700,-650); pos['m_Space']=0
pnode,pslots=clone_node('Property',-700,-450);pnode['m_Property']={'m_Id':prop['m_ObjectId']}
pslots[0].update(m_Type='UnityEditor.ShaderGraph.Vector3MaterialSlot',m_DisplayName='Note Visual Offset',m_Value={'x':0.0,'y':0.0,'z':0.0},m_DefaultValue={'x':0.0,'y':0.0,'z':0.0})
add,_=clone_node('Add',-400,-600)
vertex=next(o for o in objs if o.get('m_Name')=='VertexDescription.Position')
assert not any(e['m_InputSlot']['m_Node']['m_Id']==vertex['m_ObjectId'] for e in graph['m_Edges'])
def edge(a,ai,b,bi):
 graph['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':a['m_ObjectId']},'m_SlotId':ai},'m_InputSlot':{'m_Node':{'m_Id':b['m_ObjectId']},'m_SlotId':bi}})
edge(pos,0,add,0);edge(pnode,0,add,1);edge(add,2,vertex,0)
# Keep existing objects verbatim so the diff contains only the new property and nodes.
path.write_text(json.dumps(graph,indent=4)+'\n\n'+'\n\n'.join(raw[1:]+[json.dumps(o,indent=4) for o in added])+'\n',encoding='utf-8')
# A replaceable, hollow comic burst with a transparent centre.
size=1536; image=Image.new('RGBA',(size,size));draw=ImageDraw.Draw(image)
points=[]
for i in range(32):
 angle=math.pi*2*i/32-math.pi/2
 radius=size*(0.46 if i%2==0 else 0.355)
 points.append((size/2+math.cos(angle)*radius,size/2+math.sin(angle)*radius))
draw.line(points+[points[0]],fill=(255,255,255,255),width=18,joint='curve')
image.resize((512,512),Image.Resampling.LANCZOS).save(root/'VFX/ShortNoteBurst.png')
meta=(root/'Image/TitlePage/start.png.meta').read_text(encoding='utf-8')
meta=re.sub(r'guid: [0-9a-f]{32}', 'guid: '+texture_guid, meta,count=1)
meta=meta.replace('spritePixelsToUnits: 100','spritePixelsToUnits: 200').replace('spriteMeshType: 1','spriteMeshType: 0').replace('spriteGenerateFallbackPhysicsShape: 1','spriteGenerateFallbackPhysicsShape: 0')
meta=re.sub(r'spriteID: [0-9a-f]{32}','spriteID: '+uid(),meta)
(root/'VFX/ShortNoteBurst.png.meta').write_text(meta,encoding='utf-8')
# Reuse Unity's serialized SpriteRenderer layout and its working material.
source=(root/'Prefab/VFX/LongGoodHitEffect.prefab').read_text(encoding='utf-8')
renderer=next(block for block in re.split(r'(?=--- !u!)',source) if '\nSpriteRenderer:\n' in block)
renderer=re.sub(r'--- !u!212 &-?\d+','--- !u!212 &21200000',renderer,count=1)
renderer=re.sub(r'm_GameObject: \{fileID: -?\d+\}','m_GameObject: {fileID: 100000}',renderer,count=1)
renderer=re.sub(r'm_Sprite: \{[^\n]+\}', 'm_Sprite: {fileID: 21300000, guid: '+texture_guid+', type: 3}',renderer)
renderer=re.sub(r'm_Color: \{[^\n]+\}', 'm_Color: {r: 1, g: 0.24, b: 0.12, a: 1}',renderer)
renderer=re.sub(r'm_SortingOrder: -?\d+','m_SortingOrder: 0',renderer)
prefab='''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &100000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 400000}
  - component: {fileID: 21200000}
  m_Layer: 0
  m_Name: ShortNoteBurst
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &400000
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100000}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
'''+renderer
(root/'Prefab/VFX/ShortNoteBurst.prefab').write_text(prefab,encoding='utf-8')
(root/'Prefab/VFX/ShortNoteBurst.prefab.meta').write_text('fileFormatVersion: 2\nguid: '+burst_guid+'\nPrefabImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
for name in ['Crash_Collider_NormalNote 1.prefab','Crash_Collider_NormalNote 2.prefab']:
 path=root/'Prefab'/name;s=path.read_text(encoding='utf-8')
 game_id=re.search(r'--- !u!1 &(-?\d+)',s)[1]
 renderer_id=re.search(r'--- !u!23 &(-?\d+)',s)[1]
 s=s.replace('  m_Layer: 0','  - component: {fileID: 114800001}\n  m_Layer: 0',1)
 s+='''--- !u!114 &114800001
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: GAME_ID}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: SCRIPT_GUID, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::ShortNoteVFX
  candy: {fileID: RENDERER_ID}
  burstPrefab: {fileID: 21200000, guid: BURST_GUID, type: 3}
  animatorSlot: {fileID: 0}
  previewTime: 0.6
  shakeAmount: 0.015
'''.replace('GAME_ID',game_id).replace('SCRIPT_GUID',script_guid).replace('RENDERER_ID',renderer_id).replace('BURST_GUID',burst_guid)
 path.write_text(s,encoding='utf-8')
print('Added short-note VFX, burst sprite/prefab and zero-default shader vertex offset.')
print('Burst material:', re.search(r'm_Materials:\n([^\n]+)',renderer)[1])
