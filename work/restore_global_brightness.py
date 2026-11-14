import copy,json,pathlib,uuid
root=pathlib.Path('DropShot/Assets');uid=lambda:uuid.uuid4().hex
guid='129d06ef7a8f40fc9aa47b599a09a173'
(root/'Scripts/HalftoneEnvironmentBrightness.cs.meta').write_text('fileFormatVersion: 2\nguid: '+guid+'\n',encoding='utf-8')
p=root/'Shader/HalfTone/SG_HalfTone.shadergraph';s=p.read_text(encoding='utf-8-sig');d=json.JSONDecoder();a=[];raw={}
while s.strip():
 s=s.lstrip();o,n=d.raw_decode(s);a.append(o);raw[o['m_ObjectId']]=s[:n];s=s[n:]
g=a[0];b={o['m_ObjectId']:o for o in a};prop=next(o for o in a if o.get('m_OverrideReferenceName')=='_EnvironmentBrightness')
prop.update(m_Name='Environment Dim (Global)',m_RefNameGeneratedByDisplayName='Environment Dim (Global)',m_DefaultReferenceName='_DS_EnvironmentDim',m_OverrideReferenceName='_DS_EnvironmentDim',m_GeneratePropertyBlock=False,overrideHLSLDeclaration=True,hlslDeclarationOverride=1,m_Value=0.0)
property_node=next(o for o in a if o.get('m_Property',{}).get('m_Id')==prop['m_ObjectId']);property_slot=b[property_node['m_Slots'][0]['m_Id']]
property_slot.update(m_DisplayName='Environment Dim (Global)',m_Value=0.0,m_DefaultValue=0.0)
original=next(o for o in a if o.get('m_Name')=='One Minus');node=copy.deepcopy(original);node['m_ObjectId']=uid();node['m_Group']={'m_Id':''};node['m_DrawState']['m_Position'].update(x=1750.0,y=-350.0);node['m_PreviewExpanded']=False
added=[node]
for ref in node['m_Slots']:
 slot=copy.deepcopy(b[ref['m_Id']]);slot['m_ObjectId']=uid();ref['m_Id']=slot['m_ObjectId'];added.append(slot)
g['m_Nodes'].append({'m_Id':node['m_ObjectId']})
edge=next(e for e in g['m_Edges'] if e['m_OutputSlot']['m_Node']['m_Id']==property_node['m_ObjectId']);target=copy.deepcopy(edge['m_InputSlot']);edge['m_InputSlot']={'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':0}
g['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':1},'m_InputSlot':target})
changed={prop['m_ObjectId'],property_slot['m_ObjectId']}
p.write_text(json.dumps(g,indent=4)+'\n\n'+'\n\n'.join([json.dumps(o,indent=4) if o['m_ObjectId'] in changed else raw[o['m_ObjectId']] for o in a[1:]]+[json.dumps(o,indent=4) for o in added])+'\n',encoding='utf-8')
block='''--- !u!1 &213850632
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 213850634}
  - component: {fileID: 213850633}
  m_Layer: 0
  m_Name: EnvironmentBrightness
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!114 &213850633
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 213850632}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: SCRIPT_GUID, type: 3}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::HalftoneEnvironmentBrightness
  brightness: 0.9
--- !u!4 &213850634
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 213850632}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
'''.replace('SCRIPT_GUID',guid)
for name in ['Game_Hard.unity','Game_Easy.unity']:
 p=root/'Scenes'/name;s=p.read_text(encoding='utf-8');assert '&213850632' not in s and 'm_Name: EnvironmentBrightness' not in s
 marker='--- !u!1660057539 &9223372036854775807\n';assert marker in s
 s=s.replace(marker,block+marker,1);s+='  - {fileID: 213850634}\n';p.write_text(s,encoding='utf-8')
print('Restored one global brightness control in Hard and Easy at 0.9; no native light or custom HLSL added.')
