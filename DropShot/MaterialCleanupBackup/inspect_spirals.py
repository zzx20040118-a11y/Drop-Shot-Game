import struct,zlib,pathlib

def read(path):
 b=path.read_bytes(); ver=struct.unpack_from('<I',b,23)[0]; wide=ver>=7500; head='<QQQB' if wide else '<IIIB'; hs=25 if wide else 13
 def prop(p):
  t=chr(b[p]);p+=1
  fm={'Y':'h','C':'?','I':'i','F':'f','D':'d','L':'q'}
  if t in fm:
   f='<'+fm[t];return struct.unpack_from(f,b,p)[0],p+struct.calcsize(f)
  if t in 'SR':
   n=struct.unpack_from('<I',b,p)[0];p+=4;return b[p:p+n].decode('utf8','replace'),p+n
  n,enc,size=struct.unpack_from('<III',b,p);p+=12;raw=b[p:p+size];raw=zlib.decompress(raw) if enc else raw
  fmt={'f':'f','d':'d','i':'i','l':'q','b':'?','c':'b'}[t]
  return struct.unpack('<'+str(n)+fmt,raw),p+size
 def node(p):
  end,n,plen,nl=struct.unpack_from(head,b,p)
  if not end:return None,p+hs
  p+=hs;name=b[p:p+nl].decode();p+=nl;ps=[]
  for i in range(n):v,p=prop(p);ps.append(v)
  cs=[]
  while p<end-hs:
   child,p=node(p)
   if child:cs.append(child)
  return (name,ps,cs),end
 nodes=[];p=27
 while p<len(b)-hs:
  nd,p=node(p)
  if not nd:break
  nodes.append(nd)
 def walk(ns):
  for name,ps,cs in ns:
   if name in ('Model','Geometry'):print(name,ps[:3])
   if name=='Vertices':
    a=ps[0];print('bounds',[(min(a[k::3]),max(a[k::3])) for k in range(3)])
   if name=='UV':
    a=ps[0];print('UV',len(a)//2,'range',min(a),max(a))
   if name=='P' and ps[0] in ('Lcl Translation','Lcl Rotation','Lcl Scaling','GeometricTranslation','GeometricRotation','GeometricScaling'):print(ps)
   walk(cs)
 walk(nodes)
for n in ('spiral','spiral2'):
 print('\nFILE',n);read(pathlib.Path('DropShot/Assets/Models/spiral/'+n+'.fbx'))
