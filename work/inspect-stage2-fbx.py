from pathlib import Path
import struct
p=Path('DropShot/Assets/Models/Stage1/Stage2/Stage2_sweets2.fbx');data=p.read_bytes();version=struct.unpack_from('<I',data,23)[0];wide=version>=7500;fmt='<QQQ' if wide else '<III';hdr=24 if wide else 12
models={};mats={};links=[]
def node(pos,depth=0,parent=''):
 end,count,size=struct.unpack_from(fmt,data,pos);pos+=hdr
 if end==0:return None,pos+1
 ln=data[pos];pos+=1;name=data[pos:pos+ln].decode();pos+=ln;props=[]
 for i in range(count):
  typ=chr(data[pos]);pos+=1
  if typ in 'YCLFDI':
   f={'Y':'h','C':'?','L':'q','F':'f','D':'d','I':'i'}[typ];v=struct.unpack_from('<'+f,data,pos)[0];pos+=struct.calcsize(f);props.append(v)
  elif typ in 'SR':
   n=struct.unpack_from('<I',data,pos)[0];pos+=4;v=data[pos:pos+n];pos+=n;props.append(v.decode('utf-8',errors='replace') if typ=='S' else None)
  elif typ in 'fdlibc':
   n,encoding,length=struct.unpack_from('<III',data,pos);pos+=12+length;props.append(None)
  else:raise ValueError(typ)
 if parent=='Objects' and name=='Model':models[props[0]]=props[1].split('\0')[0]
 if parent=='Objects' and name=='Material':mats[props[0]]=props[1].split('\0')[0]
 if parent=='Connections' and name=='C':links.append(props)
 while pos<end-(25 if wide else 13):
  _,pos=node(pos,depth+1,name)
 return name,end
pos=27
while pos<len(data)-(25 if wide else 13):
 name,pos=node(pos)
 if name is None:break
for id,name in models.items():
 assigned=[mats[c[1]] for c in links if len(c)>2 and c[0]=='OO' and c[2]==id and c[1] in mats]
 if assigned:print(name,':',' / '.join(assigned))
