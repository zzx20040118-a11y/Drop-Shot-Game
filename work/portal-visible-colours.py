from pathlib import Path
import re,uuid,shutil
folder=Path('DropShot/Assets/VFX/PortalEspacial/Mat');scene=Path('DropShot/Assets/Scenes/Game_B_Hard.unity');backup=Path('work/before-portal-visible-colours');backup.mkdir(exist_ok=True)
shutil.copy2(scene,backup/scene.name);shutil.copy2(folder/'M_PortalRainbow_BHard.mat',backup/'M_PortalRainbow_BHard.mat')
p=folder/'M_PortalRainbow_BHard.mat';s=p.read_text();s=re.sub(r'    - _Brightness: .*','    - _Brightness: 2.2',s);s=re.sub(r'    - _ColourSpread: .*','    - _ColourSpread: 2',s);p.write_text(s)
# Same texture and opacity as the original outer ring, now with the isolated colour graph.
s=re.sub(r'  m_Name: .*','  m_Name: M_PortalRingColour_BHard',s,count=1)
s=s.replace('guid: 4cd57a21b52f00f47a78d6b869064cb9','guid: 98364bb3367523a44997a4c4327aec90')
s=re.sub(r'    - _MainTexSpeed: .*','    - _MainTexSpeed: {r: 0, g: 0, b: 0, a: 0}',s)
s=s.replace('    - _Brightness: 2.2','    - _Brightness: 2.6')
p=folder/'M_PortalRingColour_BHard.mat';assert not p.exists();p.write_text(s);guid=uuid.uuid4().hex
p.with_suffix('.mat.meta').write_text('fileFormatVersion: 2\nguid: '+guid+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
s=scene.read_text();pattern=r'(--- !u!1001 &2108575432\n.*?)(    m_RemovedComponents: \[\])';m=re.search(pattern,s,re.S);assert m;b=m[1]
def mod(target,prop,value='',reference='{fileID: 0}'):
 global b
 pat=rf"    - target: \{{fileID: {target}, guid: 8b8351c027f776e4583703d0b4d3a6fb, type: 3\}}\n      propertyPath: '?{re.escape(prop)}'?\n      value: .*\n      objectReference: .*\n"
 entry=f'    - target: {{fileID: {target}, guid: 8b8351c027f776e4583703d0b4d3a6fb, type: 3}}\n      propertyPath: {prop}\n      value: {value}\n      objectReference: {reference}\n'
 if re.search(pat,b):b=re.sub(pat,entry,b)
 else:b+=entry
mod(315728224406089637,'m_Materials.Array.data[0]',reference=f'{{fileID: 2100000, guid: {guid}, type: 2}}')
for particle in [3154600911134928196,4150897840218093903]:
 for channel in ['r','g','b','a']:mod(particle,'InitialModule.startColor.maxColor.'+channel,'1')
s=s[:m.start()]+b+m[2]+s[m.end():];scene.write_text(s)
print('B Hard: original ring texture + colour graph; brightened spiral/rim; removed particle tint attenuation.')
