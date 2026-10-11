using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Original indoor surrogate. Meter units; floor-center origin; front is local -Z.
public static class VendingMachineAssetBuilder
{
    const string Root = "Assets/LeadMillsAR/VendingMachine";
    static GameObject model;
    static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

    [MenuItem("Lead Mills/Build and Select Vending Machine")]
    public static void Build()
    {
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Meshes");
        AssetDatabase.Refresh(); materials.Clear();
        model = new GameObject("Lead Mills Vending Machine (1.2 x 0.85 x 2.0 m)");
        var shell = Mat("Graphite", new Color(.055f,.065f,.08f), .55f,.48f);
        var trim = Mat("BrushedSteel", new Color(.42f,.47f,.5f), .8f,.68f);
        var dark = Mat("Interior", new Color(.012f,.017f,.022f), .1f,.25f);
        var blue = Mat("BluePaint", new Color(.035f,.24f,.43f), .45f,.55f);
        var light = Mat("Lighting", new Color(.65f,.9f,1f),0,.4f,true);
        var screen = Mat("Screen", new Color(.03f,.18f,.21f), .15f,.8f,true);
        var glass = Mat("DisplayGlass", new Color(.45f,.75f,.85f,.10f), .05f,.92f,false,true);
        // Body bounds exactly 1.2 x .85 x 2.0; base at y=0.
        Box("Left cabinet",new Vector3(-.568f,1.015f,0),new Vector3(.064f,1.93f,.85f),shell);
        Box("Right cabinet",new Vector3(.568f,1.015f,0),new Vector3(.064f,1.93f,.85f),shell);
        Box("Back",new Vector3(0,1.015f,.397f),new Vector3(1.072f,1.93f,.056f),shell);
        Box("Roof",new Vector3(0,1.98f,0),new Vector3(1.2f,.04f,.85f),shell);
        Box("Base",new Vector3(0,.11f,0),new Vector3(1.16f,.12f,.81f),shell);
        for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
            Box("Rubber foot",new Vector3(x*.48f,.025f,z*.32f),new Vector3(.13f,.05f,.13f),dark);
        Box("Door header",new Vector3(0,1.845f,-.392f),new Vector3(1.072f,.23f,.066f),blue);
        Box("Payment panel",new Vector3(.4f,1.045f,-.378f),new Vector3(.272f,1.35f,.074f),shell);
        Box("Lower door",new Vector3(-.14f,.29f,-.379f),new Vector3(.77f,.26f,.072f),shell);
        Box("Display back",new Vector3(-.14f,1.075f,.07f),new Vector3(.77f,1.28f,.02f),dark);
        Box("Window left trim",new Vector3(-.52f,1.075f,-.405f),new Vector3(.022f,1.31f,.035f),trim);
        Box("Window right trim",new Vector3(.245f,1.075f,-.405f),new Vector3(.022f,1.31f,.035f),trim);
        Box("Window top trim",new Vector3(-.137f,1.72f,-.405f),new Vector3(.79f,.028f,.035f),trim);
        Box("Window bottom trim",new Vector3(-.137f,.435f,-.405f),new Vector3(.79f,.028f,.035f),trim);
        Box("Glass",new Vector3(-.137f,1.078f,-.405f),new Vector3(.745f,1.258f,.008f),glass);
        Box("Display light",new Vector3(-.137f,1.69f,-.32f),new Vector3(.68f,.015f,.025f),light);
        var colors = new[] { new Color(.83f,.15f,.12f), new Color(.95f,.66f,.08f), new Color(.12f,.43f,.75f), new Color(.16f,.62f,.32f) };
        for(int row=0;row<4;row++)
        {
            float y=.55f+row*.28f;
            Box("Shelf",new Vector3(-.137f,y-.055f,-.145f),new Vector3(.73f,.02f,.45f),trim);
            Box("Shelf price strip",new Vector3(-.137f,y-.055f,-.374f),new Vector3(.73f,.035f,.012f),dark);
            for(int col=0;col<4;col++)
            {
                float x=-.414f+col*.184f;
                var wrapper=Mat("Product"+col,colors[col],.15f,.38f);
                if(row<2)
                {
                    Part(PrimitiveType.Cylinder,"Drink can",new Vector3(x,y+.055f,-.17f),new Vector3(.082f,.085f,.082f),wrapper);
                    Part(PrimitiveType.Cylinder,"Can lid",new Vector3(x,y+.142f,-.17f),new Vector3(.082f,.002f,.082f),trim);
                    Box("Can label stripe",new Vector3(x,y+.057f,-.212f),new Vector3(.052f,.028f,.004f),light);
                }
                else
                {
                    Box("Snack pack",new Vector3(x,y+.067f,-.17f),new Vector3(.10f,.19f,.048f),wrapper);
                    Box("Pack label",new Vector3(x,y+.072f,-.197f),new Vector3(.077f,.045f,.003f),light);
                }
                // Low-poly helical dispensing coil in front of each product.
                for(int i=0;i<22;i++)
                {
                    float t=i*Mathf.PI*4/21,tn=(i+1)*Mathf.PI*4/21;
                    Vector3 a=new Vector3(x+Mathf.Cos(t)*.052f,y+.004f+Mathf.Sin(t)*.034f,-.28f+i*.003f);
                    Vector3 b=new Vector3(x+Mathf.Cos(tn)*.052f,y+.004f+Mathf.Sin(tn)*.034f,-.28f+(i+1)*.003f);
                    Rod("Coil",a,b,.004f,trim);
                }
            }
        }
        Box("Collection opening",new Vector3(-.14f,.3f,-.426f+.008f),new Vector3(.62f,.15f,.014f),dark);
        Box("Collection lip",new Vector3(-.14f,.21f,-.405f),new Vector3(.66f,.025f,.035f),trim);
        Box("LCD surround",new Vector3(.4f,1.52f,-.427f+.012f),new Vector3(.215f,.15f,.02f),trim);
        Box("LCD",new Vector3(.4f,1.52f,-.424f),new Vector3(.19f,.123f,.002f),screen);
        Box("Card reader",new Vector3(.4f,1.255f,-.413f),new Vector3(.13f,.18f,.02f),dark);
        Box("Card slot",new Vector3(.4f,1.20f,-.425f+.001f),new Vector3(.092f,.008f,.002f),trim);
        for(int row=0;row<4;row++) for(int col=0;col<3;col++)
            Box("Selection key",new Vector3(.338f+col*.062f,1.07f-row*.05f,-.418f),new Vector3(.047f,.035f,.014f),trim);
        Box("Coin slot",new Vector3(.4f,.77f,-.419f),new Vector3(.012f,.085f,.012f),dark);
        Box("Return button",new Vector3(.46f,.77f,-.418f),new Vector3(.04f,.04f,.014f),trim);
        Box("Door handle",new Vector3(.535f,.95f,-.41f),new Vector3(.018f,.32f,.03f),trim);
        for(int i=0;i<8;i++) Box("Side vent",new Vector3(.6f-.001f,.35f+i*.03f,.18f),new Vector3(.002f,.01f,.28f),dark);
        Text("LEAD MILLS",new Vector3(-.43f,1.88f,-.427f),.057f,Color.white);
        Text("REFRESH",new Vector3(-.43f,1.80f,-.427f),.023f,new Color(.6f,.85f,1));
        Text("SELECT ITEM",new Vector3(.305f,1.535f,-.427f),.018f,Color.cyan);
        Text("TAP / PAY",new Vector3(.31f,1.38f,-.427f),.022f,Color.white);
        Text("PICK UP",new Vector3(-.30f,.35f,-.429f),.025f,new Color(.5f,.55f,.6f));
        CombineOpaque();
        var line=new GameObject("Estimated floor footprint"); line.transform.SetParent(model.transform,false);
        var outline=line.AddComponent<LineRenderer>();outline.useWorldSpace=false;outline.loop=true;outline.widthMultiplier=.006f;
        outline.sharedMaterial=Mat("Footprint",new Color(.1f,.9f,.4f),0,0,true);
        outline.positionCount=4;outline.SetPositions(new[]{new Vector3(-.6f,.002f,-.425f),new Vector3(.6f,.002f,-.425f),new Vector3(.6f,.002f,.425f),new Vector3(-.6f,.002f,.425f)});
        outline.shadowCastingMode=ShadowCastingMode.Off;
        var prefab=PrefabUtility.SaveAsPrefabAsset(model,Root+"/VendingMachine.prefab");
        var placement=Object.FindFirstObjectByType<HorizontalPlanePlacement>();
        if(!placement) throw new System.InvalidOperationException("Open the configured AR POC scene first.");
        placement.SetPlacementAsset(prefab,0f);
        EditorUtility.SetDirty(placement);EditorSceneManager.MarkSceneDirty(placement.gameObject.scene);
        EditorSceneManager.SaveScene(placement.gameObject.scene);AssetDatabase.SaveAssets();
        ExportObj(); Preview();
        Object.DestroyImmediate(model);
        Debug.Log("[Lead Mills Asset] Vending machine selected: 1.2 x 0.85 x 2.0 m; floor-center pivot; front -Z; local/session anchor only.");
    }
    static Material Mat(string name,Color color,float metal,float smooth,bool emissive=false,bool transparent=false)
    {
        if(materials.TryGetValue(name,out var existing)) return existing;
        string path=Root+"/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);
        if(emissive){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.7f);}
        if(transparent){m.SetFloat("_Surface",1);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;}
        EditorUtility.SetDirty(m);materials.Add(name,m);return m;
    }
    static GameObject Part(PrimitiveType type,string name,Vector3 position,Vector3 dimensions,Material material)
    {
        var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(model.transform,false);o.transform.localPosition=position;o.transform.localScale=dimensions;
        Object.DestroyImmediate(o.GetComponent<Collider>());o.GetComponent<MeshRenderer>().sharedMaterial=material;return o;
    }
    static void Box(string name,Vector3 p,Vector3 s,Material m)=>Part(PrimitiveType.Cube,name,p,s,m);
    static void Rod(string name,Vector3 a,Vector3 b,float diameter,Material m)
    {
        var o=Part(PrimitiveType.Cylinder,name,(a+b)*.5f,new Vector3(diameter,(b-a).magnitude*.5f,diameter),m);
        o.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
    }
    static void Text(string text,Vector3 p,float size,Color color)
    {
        var o=new GameObject(text);o.transform.SetParent(model.transform,false);o.transform.localPosition=p;
        var t=o.AddComponent<TextMesh>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text=text;t.fontSize=64;t.characterSize=size*(text == "LEAD MILLS" ? .35f : .3f);t.color=color;
        t.anchor=TextAnchor.MiddleLeft;t.alignment=TextAlignment.Left;o.GetComponent<MeshRenderer>().sharedMaterial=t.font.material;
    }
    static void CombineOpaque()
    {
        var groups=new Dictionary<Material,List<MeshFilter>>();
        foreach(var f in model.GetComponentsInChildren<MeshFilter>())
        {
            var m=f.GetComponent<MeshRenderer>().sharedMaterial;if(m.renderQueue>=3000)continue;
            if(!groups.TryGetValue(m,out var list))groups[m]=list=new List<MeshFilter>();list.Add(f);
        }
        foreach(var pair in groups)
        {
            var c=new CombineInstance[pair.Value.Count];for(int i=0;i<c.Length;i++)c[i]=new CombineInstance{mesh=pair.Value[i].sharedMesh,transform=pair.Value[i].transform.localToWorldMatrix};
            var mesh=new Mesh{name=pair.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(c,true,true);
            string path=Root+"/Meshes/"+pair.Key.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
            var o=new GameObject(pair.Key.name);o.transform.SetParent(model.transform,false);o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=pair.Key;
            foreach(var f in pair.Value)Object.DestroyImmediate(f.gameObject);
        }
    }
    static void ExportObj()
    {
        var text=new StringBuilder("# Original vending machine; meter units; floor-center origin; front -Z. Labels remain in Unity prefab.\nmtllib VendingMachine.mtl\n");int offset=1,triangles=0;
        var bounds=new Bounds();bool first=true;
        foreach(var f in model.GetComponentsInChildren<MeshFilter>())
        {
            var mesh=f.sharedMesh;text.Append("o "+f.name+"\nusemtl "+f.GetComponent<MeshRenderer>().sharedMaterial.name+"\n");
            foreach(var v in mesh.vertices){var w=f.transform.TransformPoint(v);if(first){bounds=new Bounds(w,Vector3.zero);first=false;}else bounds.Encapsulate(w);text.AppendFormat(CultureInfo.InvariantCulture,"v {0:F6} {1:F6} {2:F6}\n",w.x,w.y,w.z);}
            var ix=mesh.triangles;triangles+=ix.Length/3;for(int i=0;i<ix.Length;i+=3)text.Append($"f {ix[i]+offset} {ix[i+1]+offset} {ix[i+2]+offset}\n");offset+=mesh.vertexCount;
        }
        File.WriteAllText(Root+"/VendingMachine.obj",text.ToString());var mtl=new StringBuilder();
        foreach(var pair in materials){var c=pair.Value.GetColor("_BaseColor");mtl.AppendFormat(CultureInfo.InvariantCulture,"newmtl {0}\nKd {1:F4} {2:F4} {3:F4}\nd {4:F4}\n\n",pair.Key,c.r,c.g,c.b,c.a);}
        File.WriteAllText(Root+"/VendingMachine.mtl",mtl.ToString());
        // Front text sits just beyond cabinet envelope; structural meshes are the physical dimensions.
        if(Mathf.Abs(bounds.size.x-1.2f)>.005f||Mathf.Abs(bounds.size.y-2f)>.005f||Mathf.Abs(bounds.size.z-.85f)>.005f||Mathf.Abs(bounds.min.y)>.001f)throw new System.InvalidOperationException("Asset bounds/base validation failed: "+bounds);
        Debug.Log("[Lead Mills Asset] Bounds validated: "+bounds+"; triangles "+triangles+"; mesh renderers "+model.GetComponentsInChildren<MeshFilter>().Length);
    }
    static void Preview()
    {
        DebugManager.instance.Reset();
        var keyObject=new GameObject("Asset preview key");var key=keyObject.AddComponent<Light>();key.type=LightType.Directional;key.intensity=2;key.transform.rotation=Quaternion.Euler(40,-35,0);
        var cameraObject=new GameObject("Asset Preview Camera");var camera=cameraObject.AddComponent<Camera>();
        camera.transform.position=new Vector3(2.8f,2.15f,-3.8f);camera.transform.LookAt(new Vector3(0,1,0));camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.15f,.19f);camera.fieldOfView=35;
        var rt=new RenderTexture(1000,1200,24);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
        var image=new Texture2D(1000,1200,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1000,1200),0,0);image.Apply();RenderTexture.active=previous;
        string dir=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Builds/AssetPreviews");Directory.CreateDirectory(dir);File.WriteAllBytes(dir+"/VendingMachine.png",image.EncodeToPNG());
        Object.DestroyImmediate(image);camera.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(cameraObject);Object.DestroyImmediate(keyObject);
    }
}
