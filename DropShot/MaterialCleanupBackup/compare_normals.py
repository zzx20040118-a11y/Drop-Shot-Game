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
 def analyze(ns):
  for name,ps,cs in ns:
   if name=='Geometry':
    d={n:p for n,p,c in cs}; verts=d['Vertices'][0]; polys=d['PolygonVertexIndex'][0]
    uvnode=next((c for n,p,c in cs if n=='LayerElementUV'),None)
    matnode=next((c for n,p,c in cs if n=='LayerElementMaterial'),None)
    if not uvnode or not matnode:continue
    ud={n:p for n,p,c in uvnode}; md={n:p for n,p,c in matnode}
    uv=ud['UV'][0]; ui=ud.get('UVIndex',[None])[0]; mats=md['Materials'][0]
    nn=next((c for n,p,c in cs if n=='LayerElementNormal'),[]); nd={n:p for n,p,c in nn}; normals=nd.get('Normals',[[]])[0]
    stats={};face=[];corners=[];fi=0
    for ci,index in enumerate(polys):
     vi=-index-1 if index<0 else index;face.append(vi);corners.append(ci)
     if index<0:
      mi=mats[fi] if len(mats)>1 else mats[0];s=stats.setdefault(mi,[0,0,0.0,0.0])
      for k in range(1,len(face)-1):
       ids=[0,k,k+1]; xyz=[verts[face[j]*3:face[j]*3+3] for j in ids]
       uvs=[uv[(ui[corners[j]] if ui else corners[j])*2:(ui[corners[j]] if ui else corners[j])*2+2] for j in ids]
       area=abs((uvs[1][0]-uvs[0][0])*(uvs[2][1]-uvs[0][1])-(uvs[1][1]-uvs[0][1])*(uvs[2][0]-uvs[0][0]))/2
       s[0]+=1;s[1]+=area<1e-12;s[2]+=area
       if normals:
        e=[xyz[1][j]-xyz[0][j] for j in range(3)];f=[xyz[2][j]-xyz[0][j] for j in range(3)]
        cross=[e[1]*f[2]-e[2]*f[1],e[2]*f[0]-e[0]*f[2],e[0]*f[1]-e[1]*f[0]]
        normal=normals[corners[0]*3:corners[0]*3+3]
        if len(normal)==3:s[3]+=sum(cross[j]*normal[j] for j in range(3))<0
      face=[];corners=[];fi+=1
    print('UV statistics per material (triangles, zero UV area, UV area):',stats)
   analyze(cs)
 analyze(nodes)
 walk(nodes)
for n in ('spiral','spiral2'):
 print('\nFILE',n);read(pathlib.Path('DropShot/Assets/Models/spiral/'+n+'.fbx'))



