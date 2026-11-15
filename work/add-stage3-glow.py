from pathlib import Path
import re,json,shutil
root=Path('DropShot/Assets');scene=root/'Scenes/Game_B_Hard.unity';backup=Path('work/before-stage3-beat-glow');backup.mkdir(exist_ok=True);shutil.copy2(scene,backup/scene.name)
# Keep the old RowGlow alongside the scene backup for a targeted rollback.
previous=Path('work/before-stage2-beat-glow/RowGlow.cs')
configs=[
 {'name':'CookieDots','groups':['|'.join(f'奶油饼干_凸糖点_{i:02}' for i in range(row,33,8)) for row in range(8,0,-1)],'filters':[], 'strength':.8},
 {'name':'CupStripes','groups':['奶油甜品_蓝黄连续斜纹厚杯身']*2,'filters':['cyan','yellow'],'strength':.8},
 {'name':'Pancakes','groups':[f'松饼_{i:02}_' for i in range(1,4)],'filters':[],'strength':.8},
 {'name':'Rainbow','groups':[f'彩虹_{i:02}_' for i in range(1,4)],'filters':[],'strength':.8}
]
s=scene.read_text(encoding='utf-8-sig');new='';children=''
for i,c in enumerate(configs):
 go=2149901000+i*10;tr=go+1;comp=go+2;assert not re.search(r'&'+str(go)+r'\b',s);children+=f'  - {{fileID: {tr}}}\n'
 new+=f'''--- !u!1 &{go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {tr}}}
  - component: {{fileID: {comp}}}
  m_Layer: 0
  m_Name: Stage3_{c['name']}Glow
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{tr}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 889800031}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &{comp}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: b69a4274258e40bf9e34dd4ccfe82c15, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::RowGlow
  rowMaterials: []
  beatsPerFlash: 2
  targetRoot: {{fileID: 889800031}}
  objectGroups:
'''+''.join('  - '+x+'\n' for x in c['groups'])
 new+=('  groupMaterialNames:\n'+''.join('  - '+x+'\n' for x in c['filters'])) if c['filters'] else '  groupMaterialNames: []\n'
 new+=f'''  glowStrength: {c['strength']}
  overrideGlowColor: 1
  glowColor: {{r: 1, g: 1, b: 1, a: 1}}
  cakeRoot: {{fileID: 0}}
  decorationMaterial: {{fileID: 0}}
  lowerBoundary: 2.4
  upperBoundary: 3.7
'''
m=re.search(r'--- !u!4 &889800031\n.*?(?=--- !u!)',s,re.S);assert m;b=m[0].replace('  m_Father: {fileID: 0}',children+'  m_Father: {fileID: 0}');s=s[:m.start()]+b+s[m.end():]
marker='--- !u!1660057539 &9223372036854775807';assert s.count(marker)==1;s=s.replace(marker,new+marker);scene.write_text(s,encoding='utf-8');Path('work/stage3-glow-config.json').write_text(json.dumps(configs,ensure_ascii=False,indent=2),encoding='utf-8')
print('Added Stage3: 8 cookie rows, 2 cup stripe colours, 3 pancake layers, 3 rainbow bands. Yellow cookie unchanged. B Hard only.')
