import json, re, uuid, copy
from pathlib import Path

root = Path(__file__).resolve().parents[1]
folder = root / 'Assets/Shader/HalfTone'
pkg = root / 'Library/PackageCache/com.unity.shadergraph@aa8306cf39d6/Samples~/NodeReference'
def uid(): return uuid.uuid4().hex
def read(path):
    return [json.loads(s) for s in re.split(r'\n\s*\n',path.read_text(encoding='utf-8-sig').strip())]
old = read(folder/'SG_HalfTone.shadergraph')
sub = read(folder/'GetMainLight.shadersubgraph')
templates = {}
for collection in [old,sub] + [read(pkg/p) for p in ['Artistic/NormalStrength.shadergraph','Math/Saturate.shadergraph','Math/SquareRoot.shadergraph','Math/OneMinus.shadergraph','Math/Length.shadergraph','Math/Fraction.shadergraph','Input/ViewDirection.shadergraph']]:
    lookup = {o['m_ObjectId']:o for o in collection}
    for o in collection:
        if o['m_Type'].endswith('Node') and o['m_Type'] not in templates:
            templates[o['m_Type']] = (o,[lookup[s['m_Id']] for s in o.get('m_Slots',[])])

lighting_guid=uid()
(folder/'ToonLighting.hlsl.meta').write_text('fileFormatVersion: 2\nguid: '+lighting_guid+'\nShaderIncludeImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')

class Graph:
    def __init__(self, name, outline=False):
        self.name=name; self.items=[]; self.nodes=[]; self.props=[]; self.groups=[]; self.edges=[]; self.outline=outline
        self.graph=copy.deepcopy(old[0]); self.graph['m_ObjectId']=uid()
        for k in ['m_Properties','m_Keywords','m_CategoryData','m_Nodes','m_GroupDatas','m_Edges','m_StickyNoteDatas','m_ActiveTargets','m_SubDatas']:
            self.graph[k]=[]
        self.graph['m_Path']='DropShot'; self.graph['m_GraphPrecision']=1
        self.graph['m_VertexContext']['m_Blocks']=[]; self.graph['m_FragmentContext']['m_Blocks']=[]
        self.graph['m_VertexContext']['m_Position']={'x':3800,'y':-300}
        self.graph['m_FragmentContext']['m_Position']={'x':3800,'y':100}
        target=copy.deepcopy(next(o for o in old if o['m_Type'].endswith('.UniversalTarget')))
        target['m_ObjectId']=uid(); subtarget=copy.deepcopy(next(o for o in old if o['m_Type'].endswith('.UniversalLitSubTarget')))
        subtarget.update(m_ObjectId=uid(),m_Type='UnityEditor.Rendering.Universal.ShaderGraph.UniversalUnlitSubTarget',m_SGVersion=2,m_KeepLightingVariants=True,m_DefaultSSAO=False,m_DefaultDecalBlending=False)
        target['m_ActiveSubTarget']={'m_Id':subtarget['m_ObjectId']}
        target['m_RenderFace']=1 if outline else 2
        target['m_SurfaceType']=1 if outline else 0
        target['m_ZWriteControl']=2 if outline else 0
        target['m_CastShadows']=not outline; target['m_ReceiveShadows']=not outline
        self.items += [target,subtarget]; self.graph['m_ActiveTargets']=[{'m_Id':target['m_ObjectId']}]
        self.base=self.block('SurfaceDescription.BaseColor')
        self.pos=self.block('VertexDescription.Position')
        self.block('VertexDescription.Normal'); self.block('VertexDescription.Tangent')
    def block(self,name):
        proto=next(o for o in old if o.get('m_SerializedDescriptor')==name)
        n=self.clone(proto, [next(o for o in old if o['m_ObjectId']==s['m_Id']) for s in proto['m_Slots']])
        context='m_VertexContext' if name.startswith('Vertex') else 'm_FragmentContext'
        self.graph[context]['m_Blocks'].append({'m_Id':n['m_ObjectId']});return n
    def clone(self,proto,slots):
        n=copy.deepcopy(proto);n['m_ObjectId']=uid(); n['m_Group']={'m_Id':''}; n['m_Slots']=[]
        n['_slots']=[]
        for s in slots:
            slot=copy.deepcopy(s);slot['m_ObjectId']=uid();self.items.append(slot)
            n['m_Slots'].append({'m_Id':slot['m_ObjectId']});n['_slots'].append(slot)
        self.nodes.append(n);self.items.append(n);return n
    def group(self,name,x,y):
        g={'m_SGVersion':0,'m_Type':'UnityEditor.ShaderGraph.GroupData','m_ObjectId':uid(),'m_Title':name,'m_Position':{'x':x,'y':y}}
        self.groups.append(g);self.items.append(g);return g
    def node(self,kind,x,y,group=None,**settings):
        proto,slots=templates['UnityEditor.ShaderGraph.'+kind+'Node']
        n=self.clone(proto,slots);n.update(settings)
        n['m_DrawState']['m_Position']={'serializedVersion':'2','x':x,'y':y,'width':210,'height':150}
        n['m_PreviewExpanded']=False
        if group:n['m_Group']={'m_Id':group['m_ObjectId']}
        return n
    def slot(self,n,name):return next(s for s in n['_slots'] if s['m_DisplayName']==name)
    def val(self,n,name,value):
        s=self.slot(n,name);v=s.get('m_Value')
        if isinstance(v,dict):
            if isinstance(value,(list,tuple)):s['m_Value']={k:value[i] if i<len(value) else 0 for i,k in enumerate(v)}
            else:s['m_Value']={k:value for k in v}
        else:s['m_Value']=value
        return n
    def edge(self,a,ap,b,bp):
        self.edges.append({'m_OutputSlot':{'m_Node':{'m_Id':a['m_ObjectId']},'m_SlotId':self.slot(a,ap)['m_Id']},'m_InputSlot':{'m_Node':{'m_Id':b['m_ObjectId']},'m_SlotId':self.slot(b,bp)['m_Id']}})
    def prop(self,name,ref,kind,value,x,y,group=None,limits=None):
        suffix={'float':'Vector1ShaderProperty','color':'ColorShaderProperty','texture':'Texture2DShaderProperty'}[kind]
        p=copy.deepcopy(next(o for o in old if o['m_Type'].endswith('.'+suffix)))
        p.update(m_ObjectId=uid(),m_Name=name,m_Guid={'m_GuidSerialized':str(uuid.uuid4())},m_DefaultReferenceName=ref,m_OverrideReferenceName=ref,m_RefNameGeneratedByDisplayName=name)
        if kind=='float':
            p['m_Value']=value
            if limits:p.update(m_FloatType=1,m_RangeValues={'x':limits[0],'y':limits[1]})
        elif kind=='color':p['m_Value']=dict(zip(['r','g','b','a'],value));p['m_ColorMode']=0
        else:p['m_Value']={'m_SerializedTexture':'','m_Guid':''};p['m_DefaultType']=3 if ref=='_BumpMap' else 0
        self.props.append(p);self.items.append(p)
        n=self.node('Property',x,y,group);n['m_Property']={'m_Id':p['m_ObjectId']}
        s=n['_slots'][0];s['m_DisplayName']=name;s['m_ShaderOutputName']='Out'
        s['m_Type']='UnityEditor.ShaderGraph.'+{'float':'Vector1MaterialSlot','color':'Vector4MaterialSlot','texture':'Texture2DMaterialSlot'}[kind]
        if kind=='texture':s['m_Value']={'m_SerializedTexture':'','m_Guid':''}
        elif kind=='color':s['m_Value']={'x':0,'y':0,'z':0,'w':0}
        else:s['m_Value']=value
        return n
    def out(self,n):
        if n['m_Type'].endswith('PropertyNode'): return n['_slots'][-1]['m_DisplayName']
        if n['m_Type'].endswith('SampleTexture2DNode'): return 'RGBA'
        return 'Out'
    def math(self,kind,a,b,x,y,group):
        n=self.node(kind,x,y,group)
        for source,port in [(a,'A'),(b,'B')]:
            if isinstance(source,dict):self.edge(source,self.out(source),n,port)
            else:self.val(n,port,source)
        return n
    def unary(self,kind,a,x,y,group):
        n=self.node(kind,x,y,group);self.edge(a,self.out(a),n,'In');return n
    def custom(self,name,inputs,outputs,x,y,group):
        proto,_=templates['UnityEditor.ShaderGraph.CustomFunctionNode'];slots=[]
        for direction,definitions in enumerate([inputs,outputs]):
            for field,dimension in definitions:
                vec='Vector3MaterialSlot' if dimension==3 else 'Vector1MaterialSlot'
                slots.append({'m_SGVersion':0,'m_Type':'UnityEditor.ShaderGraph.'+vec,'m_ObjectId':uid(),'m_Id':len(slots),'m_DisplayName':field,'m_SlotType':direction,'m_Hidden':False,'m_ShaderOutputName':field,'m_StageCapability':2,'m_Value':{'x':0,'y':0,'z':0} if dimension==3 else 0,'m_DefaultValue':{'x':0,'y':0,'z':0} if dimension==3 else 0,'m_Labels':[]})
        n=self.clone(proto,slots);n.update(m_Name=name+' (Custom Function)',m_FunctionName=name,m_FunctionSource=lighting_guid,m_Precision=1,m_SourceType=0,m_FunctionSourceUsePragmas=True)
        n['m_DrawState']['m_Position']={'serializedVersion':'2','x':x,'y':y,'width':240,'height':250};n['m_Group']={'m_Id':group['m_ObjectId']};return n
    def lerp(self,a,b,mask,x,y,group):
        n=self.node('Lerp',x,y,group)
        for source,port in [(a,'A'),(b,'B'),(mask,'T')]:
            if isinstance(source,dict):self.edge(source,self.out(source),n,port)
            else:self.val(n,port,source)
        return n
    def save(self):
        self.graph['m_Properties']=[{'m_Id':p['m_ObjectId']} for p in self.props]
        self.graph['m_Nodes']=[{'m_Id':n['m_ObjectId']} for n in self.nodes]
        self.graph['m_GroupDatas']=[{'m_Id':g['m_ObjectId']} for g in self.groups];self.graph['m_Edges']=self.edges
        def clean(o):return {k:v for k,v in o.items() if not k.startswith('_')}
        (folder/(self.name+'.shadergraph')).write_text('\n\n'.join(json.dumps(clean(o),indent=4) for o in [self.graph]+self.items)+'\n')

g=Graph('SG_HalftoneToon')
surf=g.group('01  Base Map and Normal Map',-1900,-800)
tex=g.prop('Base Map','_BaseMap','texture',None,-1900,-800,surf)
basecol=g.prop('Base Color','_BaseColor','color',[1,1,1,1],-1900,-600,surf)
sample=g.node('SampleTexture2D',-1630,-800,surf,m_TextureType=0);g.edge(tex,'Base Map',sample,'Texture')
base=g.math('Multiply',sample,basecol,-1350,-800,surf);g.edges[-2]['m_OutputSlot']['m_SlotId']=g.slot(sample,'RGBA')['m_Id']
normal=g.prop('Normal Map','_BumpMap','texture',None,-1900,-350,surf)
nsample=g.node('SampleTexture2D',-1630,-350,surf,m_TextureType=1,m_NormalMapSpace=0);g.edge(normal,'Normal Map',nsample,'Texture')
strength=g.prop('Normal Strength','_BumpScale','float',0.4,-1900,-100,surf,(0,2))
ns=g.node('NormalStrength',-1350,-350,surf);g.edge(nsample,'RGBA',ns,'In');g.edge(strength,'Normal Strength',ns,'Strength')
nw=g.node('Transform',-1080,-350,surf,m_Conversion={'from':3,'to':2},m_ConversionType=2,m_Normalize=True);g.edge(ns,'Out',nw,'In')

lightgroup=g.group('02  Main Light (World Space)',-1900,350)
pos=g.node('Position',-1900,350,lightgroup,m_Space=2)
light=g.custom('ToonLight',[('Position',3)],[('Direction',3),('Color',3),('Attenuation',1)],-1600,350,lightgroup);g.edge(pos,'Out',light,'Position')
dot=g.node('DotProduct',-1300,350,lightgroup);g.edge(nw,'Out',dot,'A');g.edge(light,'Direction',dot,'B')
sat=g.unary('Saturate',dot,-1050,350,lightgroup)
amount=g.node('Multiply',-800,350,lightgroup);g.edge(sat,'Out',amount,'A');g.edge(light,'Attenuation',amount,'B')

bandgroup=g.group('03  Two Flat Diffuse Colors',-500,-800)
threshold=g.prop('Two Tone Threshold','_LightThreshold','float',0.45,-500,-800,bandgroup,(0.05,0.95))
tint=g.prop('Shadow Tint','_ShadowTint','color',[.45,.48,.55,1],-500,-600,bandgroup)
step=g.node('Step',-230,-800,bandgroup);g.edge(threshold,'Two Tone Threshold',step,'Edge');g.edge(amount,'Out',step,'In')
tints=g.lerp(tint,1,step,30,-800,bandgroup)
flat=g.math('Multiply',base,tints,290,-800,bandgroup)

gridgroup=g.group('04  Screen Pixel Dot Grid',-1900,1000)
screenpos=g.node('ScreenPosition',-1900,1000,gridgroup,m_ScreenSpaceType=0)
screen=g.node('Screen',-1900,1200,gridgroup)
size=g.node('Vector2',-1650,1200,gridgroup);g.edge(screen,'Width',size,'X');g.edge(screen,'Height',size,'Y')
pixels=g.math('Multiply',screenpos,size,-1400,1000,gridgroup)
spacing=g.prop('Dot Spacing (Pixels)','_DotSpacing','float',5,-1650,1450,gridgroup,(2,32))
grid=g.math('Divide',pixels,spacing,-1150,1000,gridgroup)
rot=g.node('Rotate',-900,1000,gridgroup,m_Unit=0);g.edge(grid,'Out',rot,'UV');g.val(rot,'Rotation',0.785398);g.val(rot,'Center',[0,0])
frac=g.unary('Fraction',rot,-650,1000,gridgroup)
center=g.math('Subtract',frac,.5,-400,1000,gridgroup)
distance=g.unary('Length',center,-150,1000,gridgroup)
radius=g.prop('Dot Radius','_DotRadius','float',.36,-400,1300,gridgroup,(0,.48))
softness=g.prop('Dot Edge Softness','_DotSoftness','float',.035,-400,1500,gridgroup,(.001,.15))

def dots(mask,x,y,group):
    sat=g.unary('Saturate',mask,x,y,group)
    sqrt=g.unary('SquareRoot',sat,x+250,y,group)
    r=g.math('Multiply',sqrt,radius,x+500,y,group)
    low=g.math('Subtract',r,softness,x+750,y-80,group)
    high=g.math('Add',r,softness,x+750,y+120,group)
    smooth=g.node('Smoothstep',x+1000,y,group);g.edge(low,'Out',smooth,'Edge1');g.edge(high,'Out',smooth,'Edge2');g.edge(distance,'Out',smooth,'In')
    inverse=g.unary('OneMinus',smooth,x+1250,y,group)
    # Suppress the tiny residual dot when radius is zero.
    gate=g.node('Step',x+1000,y+220,group);g.val(gate,'Edge',.0001);g.edge(sat,'Out',gate,'In')
    return g.math('Multiply',inverse,gate,x+1500,y,group)

shgroup=g.group('05  Shadow Dots',100,1700)
delta=g.math('Subtract',threshold,amount,100,1700,shgroup)
darkness=g.math('Divide',delta,threshold,350,1700,shgroup)
shadowmask=dots(darkness,600,1700,shgroup)
darkcol=g.prop('Shadow Dot Color','_ShadowDotColor','color',[.12,.14,.2,1],1400,2200,shgroup)
darkbase=g.math('Multiply',base,darkcol,1650,2200,shgroup)
shadowed=g.lerp(flat,darkbase,shadowmask,2400,1700,shgroup)

highgroup=g.group('06  Highlight Dots',100,2700)
start=g.prop('Highlight Start','_HighlightStart','float',.72,100,2700,highgroup,(.5,.99))
hd=g.math('Subtract',amount,start,350,2700,highgroup)
remaining=g.unary('OneMinus',start,350,2900,highgroup)
ha=g.math('Divide',hd,remaining,600,2700,highgroup)
highmask=dots(ha,850,2700,highgroup)
highcol=g.prop('Highlight Dot Color','_HighlightColor','color',[1,.95,.8,1],2450,3050,highgroup)

finalgroup=g.group('07  Metallic Reflection and Final Color',2650,-700)
metal=g.prop('Metallic','_Metallic','float',0,2650,-700,finalgroup,(0,1))
smooth=g.prop('Smoothness','_Smoothness','float',.5,2650,-500,finalgroup,(0,1))
view=g.node('ViewDirection',2650,-300,finalgroup,m_Space=2)
refl=g.custom('ToonReflection',[('Position',3),('Normal',3),('View',3),('Base',3),('Metallic',1),('Smoothness',1)],[('Reflection',3),('DiffuseWeight',1)],2900,-700,finalgroup)
for a,ap,bp in [(pos,'Out','Position'),(nw,'Out','Normal'),(view,'Out','View'),(base,'Out','Base'),(metal,'Metallic','Metallic'),(smooth,'Smoothness','Smoothness')]:g.edge(a,ap,refl,bp)
weighted=g.node('Multiply',3200,-700,finalgroup);g.edge(shadowed,'Out',weighted,'A');g.edge(refl,'DiffuseWeight',weighted,'B')
reflected=g.node('Add',3450,-700,finalgroup);g.edge(weighted,'Out',reflected,'A');g.edge(refl,'Reflection',reflected,'B')
highlighted=g.lerp(reflected,highcol,highmask,3200,-200,finalgroup)
emission=g.prop('Emission Color','_EmissionColor','color',[0,0,0,1],2900,150,finalgroup)
final=g.math('Add',highlighted,emission,3450,-100,finalgroup);g.edge(final,'Out',g.base,'Base Color')
g.save()

# Outline remains a separate mesh material, now also editable as a graph.
o=Graph('SG_HalftoneOutline',True)
group=o.group('Outline Shell - Object Space',-1000,0)
position=o.node('Position',-1000,0,group,m_Space=0)
normal=o.node('NormalVector',-1000,200,group,m_Space=0)
width=o.prop('Outline Width (Object Units)','_OutlineWidth','float',.012,-1000,400,group,(0,.08))
offset=o.math('Multiply',normal,width,-700,200,group)
expanded=o.math('Add',position,offset,-400,0,group);o.edge(expanded,'Out',o.pos,'Position')
color=o.prop('Outline Color','_OutlineColor','color',[.025,.03,.05,1],-400,400,group);o.edge(color,'Outline Color',o.base,'Base Color')
o.save()
print('Created two editable Shader Graphs')
