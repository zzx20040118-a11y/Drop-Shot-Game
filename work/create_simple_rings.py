import pathlib,re,uuid
from PIL import Image,ImageDraw
root=pathlib.Path('DropShot/Assets');uid=lambda:uuid.uuid4().hex
texguid=uid();prefabguid=uid();image=Image.new('RGBA',(1536,1536));draw=ImageDraw.Draw(image)
draw.ellipse((48,48,1488,1488),outline=(20,15,24,255),width=36)
draw.ellipse((57,57,1479,1479),outline=(255,255,255,255),width=18)
image.resize((512,512),Image.Resampling.LANCZOS).save(root/'VFX/ShortNoteRing.png')
s=(root/'VFX/ShortNoteBurst.png.meta').read_text();s=re.sub(r'guid: [0-9a-f]{32}','guid: '+texguid,s,count=1);s=re.sub(r'spriteID: [0-9a-f]{32}','spriteID: '+uid(),s);s=s.replace('spritePixelsToUnits: 200','spritePixelsToUnits: 256');(root/'VFX/ShortNoteRing.png.meta').write_text(s,encoding='utf-8')
s=(root/'Prefab/VFX/ShortNoteBurst.prefab').read_text();s=s.replace('m_Name: ShortNoteBurst','m_Name: ShortNoteRing');s=re.sub(r'm_Sprite: \{[^\n]+\}','m_Sprite: {fileID: 21300000, guid: '+texguid+', type: 3}',s);s=re.sub(r'm_Color: \{[^\n]+\}','m_Color: {r: 1, g: 0.72, b: 0.12, a: 1}',s);(root/'Prefab/VFX/ShortNoteRing.prefab').write_text(s,encoding='utf-8')
(root/'Prefab/VFX/ShortNoteRing.prefab.meta').write_text('fileFormatVersion: 2\nguid: '+prefabguid+'\nPrefabImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n',encoding='utf-8')
for name in ['Crash_Collider_NormalNote 1.prefab','Crash_Collider_NormalNote 2.prefab']:
 p=root/'Prefab'/name;s=p.read_text();s=re.sub(r'  burstPrefab: \{[^\n]+\}','  burstPrefab: {fileID: 21200000, guid: '+prefabguid+', type: 3}',s);s=s.replace('  animatorSlot: {fileID: 0}\n','  animatorSlot: {fileID: 0}\n  showTargetRing: 1\n');s=re.sub(r'  burstScale: [^\n]+','  burstScale: 1',s);s=re.sub(r'  burstEndScale: [^\n]+','  burstEndScale: 0.55',s);s=re.sub(r'  shakeAmount: [^\n]+','  shakeAmount: 0',s);p.write_text(s,encoding='utf-8')
print('Added simple ring sprite/prefab; both candies use a stable target ring and contracting outer ring; shake disabled. Original burst assets preserved.')
