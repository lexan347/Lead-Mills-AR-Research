using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CoalShedSetup
{
    const string Source = "Assets/Unity Packages/LeadMills_CoalShed_Draft01/LeadMills_CoalShed_Unity_Draft01.fbx";
    const string Output = "Assets/LeadMillsAR/CoalShed";
    [MenuItem("Lead Mills/Inspect Coal Shed")]
    public static void Inspect()
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Source);
        if (!asset) throw new InvalidOperationException("Coal shed FBX is missing.");
        var instance = UnityEngine.Object.Instantiate(asset);
        try {
            var report = new StringBuilder();
            var renderers = instance.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach(var renderer in renderers) {
                bounds.Encapsulate(renderer.bounds);
                report.AppendLine(renderer.name + " bounds=" + renderer.bounds);
                foreach(var material in renderer.sharedMaterials)
                    report.AppendLine("  material=" + (material ? material.name + " shader=" + material.shader.name + " texture=" + (material.mainTexture ? material.mainTexture.name : "none") : "null"));
            }
            report.AppendLine("TOTAL: " + bounds);
            Directory.CreateDirectory("Builds/CoalShedReports");
            File.WriteAllText("Builds/CoalShedReports/import-inspection.txt", report.ToString());
            Debug.Log("[Coal Shed] " + report);
        } finally { UnityEngine.Object.DestroyImmediate(instance); }
    }
    [MenuItem("Lead Mills/Prepare Coal Shed Prefabs")]
    public static void Prepare()
    {
        Directory.CreateDirectory(Output + "/Materials");
        Directory.CreateDirectory(Output + "/Meshes");
        AssetDatabase.Refresh();
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Source);
        if (!source) throw new InvalidOperationException("Missing FBX.");
        var root = new GameObject("Lead Mills Coal Shed Full Size");
        try {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
            model.transform.SetParent(root.transform, false);
            // Imported doors face -X; placement controls expect the front at -Z.
            model.transform.localRotation = Quaternion.Euler(0,-90,0);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>()) {
                var materials = renderer.sharedMaterials;
                for (int i=0;i<materials.Length;i++) materials[i]=MakeMaterial(materials[i]);
                renderer.sharedMaterials=materials;
            }
            // Correct the two missing procedural mappings on copies of the meshes.
            foreach(var filter in model.GetComponentsInChildren<MeshFilter>()) {
                bool roof=filter.name=="CoalShed_Roof", stone=filter.name=="CoalShed_Foundation";
                if(!roof && !stone)continue;
                var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);
                var vertices=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[vertices.Length];
                for(int i=0;i<vertices.Length;i++){
                    var point=filter.transform.TransformPoint(vertices[i]);
                    var normal=filter.transform.TransformDirection(normals[i]);
                    uv[i]=roof?new Vector2(point.z/2.4f,-Mathf.Abs(point.x)/1.2f):
                        new Vector2((Mathf.Abs(normal.x)>.5f?point.z:point.x)/2.5f,point.y/1.5f);
                }
                mesh.uv=uv;mesh.RecalculateTangents();
                var path=Output+"/Meshes/"+filter.name+".asset";
                var existingMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existingMesh){EditorUtility.CopySerialized(mesh,existingMesh);UnityEngine.Object.DestroyImmediate(mesh);mesh=existingMesh;}
                else AssetDatabase.CreateAsset(mesh,path);
                filter.sharedMesh=mesh;
            }
            var renderers=root.GetComponentsInChildren<Renderer>();
            var bounds=renderers[0].bounds;
            foreach(var r in renderers) bounds.Encapsulate(r.bounds);
            model.transform.localPosition = new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z);
            var footprint = new GameObject("Estimated floor footprint");
            footprint.transform.SetParent(root.transform,false);
            var line=footprint.AddComponent<LineRenderer>();
            line.useWorldSpace=false;line.loop=true;line.positionCount=4;line.widthMultiplier=.015f;
            var marker = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            marker.SetColor("_BaseColor", new Color(.1f,.9f,.4f));
            var markerPath=Output+"/Materials/Footprint.mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(markerPath);
            if(existing){EditorUtility.CopySerialized(marker,existing);UnityEngine.Object.DestroyImmediate(marker);marker=existing;}
            else AssetDatabase.CreateAsset(marker,markerPath);
            line.sharedMaterial=marker;
            float x=bounds.extents.x,z=bounds.extents.z;
            line.SetPositions(new[]{new Vector3(-x,.004f,-z),new Vector3(x,.004f,-z),new Vector3(x,.004f,z),new Vector3(-x,.004f,z)});
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            PrefabUtility.SaveAsPrefabAsset(root,Output+"/CoalShed_FullSize.prefab");
            root.name="Lead Mills Coal Shed Tabletop 1 to 10";
            root.transform.localScale=Vector3.one*.1f;
            PrefabUtility.SaveAsPrefabAsset(root,Output+"/CoalShed_Tabletop.prefab");
            AssetDatabase.SaveAssets();
            File.WriteAllText("Builds/CoalShedReports/prepared.txt", "Full-size oriented dimensions (metres): "+bounds.size+"\nTabletop scale: 0.1\nAll four image textures explicitly assigned; original FBX unchanged.\n");
            Selection.activeObject=AssetDatabase.LoadAssetAtPath<GameObject>(Output+"/CoalShed_Tabletop.prefab");
            Debug.Log("[Coal Shed] Prepared both sizes. Full size dimensions "+bounds.size);
        } finally {UnityEngine.Object.DestroyImmediate(root);}
    }
    static Material MakeMaterial(Material original)
    {
        if(!original)throw new InvalidOperationException("Missing source material.");
        string name=original.name;
        string textureName=null;
        if(name.Contains("Exterior") || name.Contains("DoorTrim"))textureName="Weathered White Wooden Siding";
        else if(name.Contains("Door_Wood"))textureName="Weathered Rustic Wooden Planks";
        else if(name.Contains("Fieldstone"))textureName="Weathered Mossy Stone Wall Texture";
        else if(name.Contains("Roof"))textureName="Weathered Gray-Brown Wooden Shingle Texture";
        bool iron=name.Contains("Iron");
        if(!iron && textureName==null)throw new InvalidOperationException("Unmapped material: "+name);
        string path=Output+"/Materials/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
        material.SetColor("_BaseColor",iron?new Color(.075f,.065f,.055f):Color.white);
        material.SetFloat("_Metallic",iron?.65f:0f);
        material.SetFloat("_Smoothness",iron?.22f:.12f);
        if(textureName!=null){
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Unity Packages/LeadMills_CoalShed_Draft01/Textures/"+textureName+".png");
            if(!texture)throw new InvalidOperationException("Missing texture: "+textureName);
            material.SetTexture("_BaseMap",texture);
        }
        EditorUtility.SetDirty(material);
        return material;
    }
    [MenuItem("Lead Mills/Select Tabletop Coal Shed")]
    public static void SelectTabletop()=>Select("CoalShed_Tabletop.prefab");
    [MenuItem("Lead Mills/Select Full Size Coal Shed")]
    public static void SelectFullSize()
    {
        PrepareFullSizePlacement();
        Select("CoalShed_FullSize_FrontPlacement.prefab");
    }
    static void PrepareFullSizePlacement()
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Output+"/CoalShed_FullSize.prefab");
        if(!source)throw new InvalidOperationException("Prepare the full-size prefab first.");
        var root=new GameObject("Coal Shed Full Size - Front Edge Placement");
        try {
            var model=(GameObject)PrefabUtility.InstantiatePrefab(source);
            model.transform.SetParent(root.transform,false);
            var renderers=model.GetComponentsInChildren<MeshRenderer>();
            if(renderers.Length==0)throw new InvalidOperationException("Full-size shed has no mesh renderers.");
            var bounds=renderers[0].bounds;
            foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            if(Mathf.Abs(bounds.size.z-7.82f)>.03f || Mathf.Abs(bounds.size.x-4.26f)>.03f || Mathf.Abs(bounds.size.y-3.51f)>.03f)
                throw new InvalidOperationException("Unexpected full-size dimensions: "+bounds.size);
            // Local -Z is the door/front. Keep the complete building beyond the
            // small scan target rather than spawning its center around the user.
            model.transform.localPosition=new Vector3(-bounds.center.x,-bounds.min.y,-bounds.min.z);
            PrefabUtility.SaveAsPrefabAsset(root,Output+"/CoalShed_FullSize_FrontPlacement.prefab");
            AssetDatabase.SaveAssets();
            File.WriteAllText("Builds/CoalShedReports/full-size-placement.txt","Dimensions in metres: "+bounds.size+"\nScale 1:1. Front roof edge at capture target; building extends forward.\n");
            Debug.Log("[Coal Shed Validation] Full-size 1:1 dimensions verified: "+bounds.size+"; front-edge placement prepared.");
        } finally {UnityEngine.Object.DestroyImmediate(root);}
    }
    static void Select(string name)
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/LeadMills_AR_POC.unity")throw new InvalidOperationException("Open the AR POC scene first.");
        var placement=UnityEngine.Object.FindFirstObjectByType<HorizontalPlanePlacement>();
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Output+"/"+name);
        if(!placement || !prefab)throw new InvalidOperationException("Missing placement component or prepared prefab.");
        var outline=prefab.GetComponentInChildren<LineRenderer>();
        var scale=prefab.transform.localScale;
        var point=outline.GetPosition(2);
        Undo.RecordObject(placement,"Select Coal Shed");
        placement.SetPlacementAsset(prefab,0,new Vector2(Mathf.Abs(point.x*scale.x),Mathf.Abs(point.z*scale.z)));
        EditorUtility.SetDirty(placement);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Coal Shed] Placement selected: "+prefab.name);
    }

    [MenuItem("Lead Mills/Preview Coal Shed")]
    public static void Preview()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Output+"/CoalShed_Tabletop.prefab");
        var preview=new PreviewRenderUtility();
        try {
            var instance=UnityEngine.Object.Instantiate(prefab);
            preview.AddSingleGO(instance);
            preview.camera.fieldOfView=32;
            preview.camera.nearClipPlane=.01f;preview.camera.farClipPlane=10;
            preview.camera.transform.position=new Vector3(-.95f,.72f,-1.2f);
            preview.camera.transform.LookAt(new Vector3(0,.16f,0));
            preview.camera.clearFlags=CameraClearFlags.Color;
            preview.camera.backgroundColor=new Color(.18f,.2f,.23f);
            preview.lights[0].intensity=1.4f;
            preview.lights[0].transform.rotation=Quaternion.Euler(40,40,0);
            preview.lights[1].intensity=.8f;
            preview.ambientColor=new Color(.5f,.5f,.5f);
            preview.BeginStaticPreview(new Rect(0,0,1000,800));
            preview.Render(true);
            var image=preview.EndStaticPreview();
            File.WriteAllBytes("Builds/CoalShedReports/tabletop-preview.png",image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            Debug.Log("[Coal Shed] Preview saved.");
        } finally {preview.Cleanup();}
    }
    [MenuItem("Lead Mills/Export Coal Shed for iOS")]
    public static void Export()
    {
        SurfaceCoverageValidation.Validate();
        PlayerSettings.iOS.appleEnableAutomaticSigning=true;
        // Select your own development team in Player Settings before exporting.
        PlanePlacementSetup.ExportIOS();
    }

}
