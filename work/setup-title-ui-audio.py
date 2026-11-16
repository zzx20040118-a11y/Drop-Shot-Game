from pathlib import Path
import re,uuid,shutil,json,wave
root=Path('DropShot/Assets');scene=root/'Scenes/TitleScene.unity';backup=Path('work/before-title-ui-audio');backup.mkdir(exist_ok=False);shutil.copy2(scene,backup/scene.name)
def uid():return uuid.uuid4().hex
folder=root/'Audio/TitleUI';folder.mkdir(exist_ok=False);folder.with_suffix('.meta').write_text('fileFormatVersion: 2\nguid: '+uid()+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
sounds=Path(r'C:/Users/helen/Downloads/Cute & Cozy UI Audio Free Sample/Cute & Cozy UI Audio Free Sample/Sounds');clips={}
template=(root/'DropShotUI/Audio/env_land_thud.wav.meta').read_text()
for name in ['Highlight','Clicked_In','Success']:
 p=folder/(name+'.wav');shutil.copy2(sounds/p.name,p);guid=uid();clips[name]=guid;meta=re.sub(r'^guid: \w+','guid: '+guid,template,flags=re.M).replace('preloadAudioData: 0','preloadAudioData: 1').replace('normalize: 1','normalize: 0').replace('  3D: 1','  3D: 0');p.with_suffix('.wav.meta').write_text(meta)
scriptguids={}
for n in ['TitleMenuAudio','TitleButtonAudio']:
 g=uid();scriptguids[n]=g;(root/'Scripts/TitlePage'/(n+'.cs.meta')).write_text('fileFormatVersion: 2\nguid: '+g+'\n')
s=scene.read_text(encoding='utf-8-sig');bs={m[1]:m[0] for m in re.finditer(r'--- !u!\d+ &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)',s,re.S)}
new=''
def component(id,go,script,fields):
 return f'''--- !u!114 &{id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {scriptguids[script]}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::{script}
'''+fields
manager='2149951002';controller='693978102'
new+=component(manager,controller,'TitleMenuAudio',f'''  highlightClip: {{fileID: 8300000, guid: {clips['Highlight']}, type: 3}}
  clickClip: {{fileID: 8300000, guid: {clips['Clicked_In']}, type: 3}}
  successClip: {{fileID: 8300000, guid: {clips['Success']}, type: 3}}
  volume: 0.6
  hoverVolume: 0.4
''')
mods={controller:manager};count=0
for k,b in bs.items():
 if 'm_OnClick:' not in b:continue
 count+=1;go=re.search(r'm_GameObject: \{fileID: (\d+)',b)[1];id=str(2149951100+count);mods[go]=id;start=int('m_MethodName: OnStartButtonClicked' in b)
 new+=component(id,go,'TitleButtonAudio',f'  menuAudio: {{fileID: {manager}}}\n  startButton: {start}\n')
assert count==11
for go,id in mods.items():
 assert id not in bs
 old=bs[go];b=old.replace('  m_Layer:',f'  - component: {{fileID: {id}}}\n  m_Layer:',1);assert b!=old;s=s.replace(old,b,1)
marker='--- !u!1660057539 &9223372036854775807';assert s.count(marker)==1;s=s.replace(marker,new+marker);scene.write_text(s,encoding='utf-8')
print('Imported 3 clips; attached audio to 11 buttons and TitleController. Start uses Success only; normal clicks use Clicked_In.')
# Create the existing full runtime compilation check with the two new scripts.
rsp=Path('work/SceneReveal.check.rsp').read_text().replace('../work/MissFeedback.check.dll','../work/TitleAudio.check.dll');Path('work/TitleAudio.check.rsp').write_text(rsp+'\nAssets/Scripts/TitlePage/TitleMenuAudio.cs\nAssets/Scripts/TitlePage/TitleButtonAudio.cs\n')
