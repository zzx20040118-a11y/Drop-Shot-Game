from pathlib import Path
import re,shutil
backup=Path('work/before-stage2-beat-glow');backup.mkdir(exist_ok=True)
scene=Path('DropShot/Assets/Scenes/Game_B_Hard.unity');shutil.copy2(scene,backup/scene.name);s=scene.read_text()
configs=[('Macarons',[[f'马卡龙_{i:02}'] for i in range(1,6)]),('Candy',[
['玻璃球内糖果_04','玻璃球内糖果_06','前景红白糖球'],
['玻璃球内糖果_03','玻璃球内糖果_05','玻璃球内糖果_10','玻璃球内糖果_11'],
['玻璃球内糖果_02','玻璃球内糖果_07','出糖口前的蓝白糖球'],
['玻璃球内糖果_01','玻璃球内糖果_08','玻璃球内糖果_09','前景绿白糖球']]),('EggStripes',[
['草莓粉彩蛋 • 波浪糖霜_0','草莓粉彩蛋 • 波浪糖霜_2']+[f'蓝莓彩蛋 • 纵向糖霜_{i}' for i in [0,2,4,6]],
['草莓粉彩蛋 • 波浪糖霜_1']+[f'蓝莓彩蛋 • 纵向糖霜_{i}' for i in [1,3,5,7]]])]
new='';children=''
for index,(label,groups) in enumerate(configs):
 go=2149701100+index*10;tr=go+1;component=go+2
 assert '&'+str(go) not in s
 children+=f'  - {{fileID: {tr}}}\n'
 new+=f'''--- !u!1 &{go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {tr}}}
  - component: {{fileID: {component}}}
  m_Layer: 0
  m_Name: Stage2_{label}Glow
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
  m_Father: {{fileID: 889800021}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &{component}
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
  targetRoot: {{fileID: 889800021}}
  objectGroups:
'''+''.join('  - '+ '|'.join(group)+'\n' for group in groups)+'''  glowStrength: 1
  overrideGlowColor: 1
  glowColor: {r: 1, g: 1, b: 1, a: 1}
  cakeRoot: {fileID: 0}
  decorationMaterial: {fileID: 0}
  lowerBoundary: 2.4
  upperBoundary: 3.7
'''
m=re.search(r'--- !u!4 &889800021\n.*?(?=--- !u!)',s,re.S);b=m[0];b=b.replace('  m_Father: {fileID: 0}',children+'  m_Father: {fileID: 0}');s=s[:m.start()]+b+s[m.end():]
marker='--- !u!1660057539 &9223372036854775807';assert s.count(marker)==1;s=s.replace(marker,new+marker);scene.write_text(s)
# Apply the previously requested slower Miss recovery, without changing shake timing.
for p in Path('DropShot/Assets/Scenes').glob('Game_*.unity'):
 if p!=scene:shutil.copy2(p,backup/p.name)
 raw=p.read_bytes();text=raw.decode('utf-8-sig')
 pattern=r'(--- !u!114 &\d+\r?\n(?:(?!--- !u!).)*?Assembly-CSharp::MissFeedback\r?\n(?:(?!--- !u!).)*?  fadeSeconds: )0\.24'
 text,n=re.subn(pattern,lambda m:m[1]+'0.48',text,flags=re.S);assert n==1
 p.write_bytes((b'\xef\xbb\xbf' if raw.startswith(b'\xef\xbb\xbf') else b'')+text.encode('utf-8'))
print('B Hard Stage2: 5 macaron groups, 4 candy colour groups, 2 alternating egg stripe groups. Miss fade 0.24 -> 0.48 in all four scenes.')
