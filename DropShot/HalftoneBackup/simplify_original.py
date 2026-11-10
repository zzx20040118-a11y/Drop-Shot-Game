import json,re
from pathlib import Path
p=Path(__file__).resolve().parents[1]/'Assets/Shader/HalfTone/SG_HalfTone.shadergraph'
objects=[json.loads(s) for s in re.split(r'\n\s*\n',p.read_text(encoding='utf-8-sig').strip())]
graph=objects[0]
removed=set()
for o in objects:
    if o['m_Type'].endswith('.UniversalLitSubTarget'):
        o.update(m_Type='UnityEditor.Rendering.Universal.ShaderGraph.UniversalUnlitSubTarget',m_SGVersion=2,m_KeepLightingVariants=True,m_DefaultSSAO=False,m_DefaultDecalBlending=False)
    if o['m_Type'].endswith('ShaderProperty') and o.get('m_Name') in ['Metallic','Smoothness','Emission']:
        removed.add(o['m_ObjectId'])
    if o.get('m_SerializedDescriptor') in ['SurfaceDescription.Metallic','SurfaceDescription.Smoothness','SurfaceDescription.Emission','SurfaceDescription.NormalTS','SurfaceDescription.Occlusion']:
        removed.add(o['m_ObjectId'])
for o in objects:
    if o.get('m_Property',{}).get('m_Id') in removed:
        removed.add(o['m_ObjectId'])
for o in objects:
    if o['m_ObjectId'] in removed:
        removed.update(s['m_Id'] for s in o.get('m_Slots',[]))
for o in objects:
    for key in ['m_Properties','m_Nodes','m_ChildObjectList','m_Blocks']:
        if key in o:o[key]=[r for r in o[key] if r['m_Id'] not in removed]
for key in ['m_FragmentContext','m_VertexContext']:
    graph[key]['m_Blocks']=[r for r in graph[key]['m_Blocks'] if r['m_Id'] not in removed]
graph['m_Edges']=[e for e in graph['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id'] not in removed and e['m_OutputSlot']['m_Node']['m_Id'] not in removed]
p.write_text('\n\n'.join(json.dumps(o,indent=4) for o in objects if o['m_ObjectId'] not in removed)+'\n')
print('Original graph set to Unlit; existing normal/dot/color chains preserved.')
