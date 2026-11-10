import json,re,copy,uuid
from pathlib import Path
root=Path(__file__).resolve().parents[1];p=root/'Assets/Shader/HalfTone/SG_HalfTone.shadergraph'
objects=[json.loads(s) for s in re.split(r'\n\s*\n',p.read_text(encoding='utf-8-sig').strip())]
before=copy.deepcopy(objects);g=objects[0]
backup=root/'HalftoneBackup/SG_HalfTone_BeforeGrouping.shadergraph'
if not backup.exists():backup.write_bytes(p.read_bytes())
nodes=[o for o in objects if o['m_ObjectId'] in {r['m_Id'] for r in g['m_Nodes']}]
def find(prefix):return next(n for n in nodes if n['m_ObjectId'].startswith(prefix))
assigned=set()
objects=[o for o in objects if not o['m_Type'].endswith('.GroupData')]
g['m_GroupDatas']=[]
def group(title,x,y,entries):
    gid=uuid.uuid4().hex
    data={'m_SGVersion':0,'m_Type':'UnityEditor.ShaderGraph.GroupData','m_ObjectId':gid,'m_Title':title,'m_Position':{'x':x-35,'y':y-65}}
    objects.append(data);g['m_GroupDatas'].append({'m_Id':gid})
    for prefix,col,row in entries:
        n=find(prefix);assigned.add(n['m_ObjectId']);n['m_Group']={'m_Id':gid}
        n['m_DrawState']['m_Position']={'serializedVersion':'2','x':x+col*250,'y':y+row*230,'width':210,'height':150}
        n['m_PreviewExpanded']=False

group('01  底色 / Base Color',0,0,[('9ceed',0,0),('c5f3a',0,1),('6a34d',1,0),('71745',2,0)])
group('02  法線貼圖 / Normal Map',0,650,[('722ed',0,0),('050ca',1,0),('6df9d',2,0),('bc015',0,1)])
group('03  主光源 / Main Light',900,650,[('9ac84',0,0),('77db9',1,0),('97282',2,0),('a5cbf',3,0)])
group('04  陰影顏色 / Shadow Color',900,0,[('322a7',0,0),('b2a3e',1,0),('42a55',1,1),('ede4d',2,1),('183c3',3,0),('587a1',4,0),('623d6',4,1),('84704',5,0)])
group('05  波點座標與網格 / Dot Grid',0,1350,[('ee0cd',0,0),('97e0c',1,0),('3fbe8',2,0),('59dcc',0,1),('8f5eb',1,1),('b12ec',2,1),('0e7e3',3,0),('9ce8a',3,1),('d7b2d',4,0),('3bae7',4,1),('1e065',5,0),('4562c',5,1),('d5f20',6,0),('cd142',7,0)])
group('06  陰影波點 / Shadow Dots',2100,650,[('cb92d',0,1),('61459',0,2),('e6ec7',1,1),('1648f',2,1),('fa953',0,0),('5b82b',1,2),('47d61',3,0),('67dd1',3,1),('50544',3,2),('a9566',4,0),('f5a22',4,1),('c8c66',5,0),('7be9c',5,2),('7f5df',6,1),('40b31',7,0)])
group('07  亮部波點 / Highlight Dots',2400,1650,[('e6d6b',0,0),('8ebaa',0,1),('7357d',1,0),('6598f',1,1),('5e852',2,0),('85e7a',3,0),('869d0',3,1)])
group('08  波點顏色混合 / Dot Colors',4400,650,[('6fc74',0,0),('b3914',1,0)])
group('09  簡化反光 / Specular',4400,0,[('43698',0,0),('59e99',0,1),('232f0',1,0),('cbaef',2,0)])
group('10  黑邊與輸出 / Outline',5300,650,[('4af89',0,1),('7c230',1,1),('f5d89',1,0),('ccfa7',2,0),('83de4',2,1),('70ae6',3,0)])
for n in nodes:
    if n['m_Type'].endswith('.BlockNode'):continue
    assert n['m_ObjectId'] in assigned, 'Unassigned node: '+n['m_Name']
g['m_VertexContext']['m_Position']={'x':6500,'y':400}
g['m_FragmentContext']['m_Position']={'x':6500,'y':700}

# Ensure the edit changes layout only: all nodes, settings, slots and wires match.
def semantic(collection):
    result={}
    for item in collection:
        if item['m_Type'].endswith('.GroupData'):continue
        o=copy.deepcopy(item)
        for key in ['m_Group','m_DrawState','m_PreviewExpanded','m_GroupDatas']:o.pop(key,None)
        if o['m_Type'].endswith('.GraphData'):
            for key in ['m_VertexContext','m_FragmentContext']:o[key].pop('m_Position',None)
        result[o['m_ObjectId']]=o
    return result
assert semantic(before)==semantic(objects), 'Unexpected non-layout change'
p.write_text('\n\n'.join(json.dumps(o,indent=4,ensure_ascii=False) for o in objects)+'\n',encoding='utf-8')
print(f'Grouped {len(assigned)} nodes into 10 groups. Node settings and connections unchanged.')
