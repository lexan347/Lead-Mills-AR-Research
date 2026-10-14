using System.IO;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public static class PlanePlacementSetup
{
    const string Root = "Assets/LeadMillsAR";

    [MenuItem("Lead Mills/Set Up Horizontal Plane Test")]
    public static void ConfigureScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/LeadMills_AR_POC.unity")
            throw new System.InvalidOperationException("Open LeadMills_AR_POC before running setup.");
        var origin = Object.FindFirstObjectByType<XROrigin>();
        if (!origin) throw new System.InvalidOperationException("XR Origin is missing.");
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Prefabs");
        AssetDatabase.Refresh();

        var planeMaterial = MakeMaterial(Root + "/Materials/DetectedPlane.mat", new Color(0.05f, 0.6f, 1f, 0.28f), true);
        var cubeMaterial = MakeMaterial(Root + "/Materials/TestCube.mat", new Color(1f, 0.42f, 0.06f, 1f), false, true);
        var planeObject = new GameObject("Detected Horizontal Plane");
        planeObject.AddComponent<ARPlane>();
        planeObject.AddComponent<MeshFilter>();
        planeObject.AddComponent<MeshRenderer>().sharedMaterial = planeMaterial;
        planeObject.AddComponent<ARPlaneMeshVisualizer>();
        var planePrefab = PrefabUtility.SaveAsPrefabAsset(planeObject, Root + "/Prefabs/DetectedPlane.prefab");
        Object.DestroyImmediate(planeObject);

        var cubeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubeObject.name = "Test Cube (20 cm)";
        cubeObject.transform.localScale = Vector3.one * 0.2f;
        Object.DestroyImmediate(cubeObject.GetComponent<Collider>());
        cubeObject.GetComponent<MeshRenderer>().sharedMaterial = cubeMaterial;
        // Outline the cube footprint at 2 mm above the estimated plane.
        // It is a virtual contact reference, not a measured physical marker.
        var outlineMaterial = MakeMaterial(Root + "/Materials/ContactOutline.mat",
            new Color(0.1f, 1f, 0.15f, 1f), false);
        var outlineObject = new GameObject("Estimated Surface Footprint");
        outlineObject.transform.SetParent(cubeObject.transform, false);
        var outline = outlineObject.AddComponent<LineRenderer>();
        outline.useWorldSpace = false;
        outline.loop = true;
        outline.widthMultiplier = 0.02f;
        outline.sharedMaterial = outlineMaterial;
        outline.positionCount = 4;
        outline.SetPositions(new[] {
            new Vector3(-0.55f, -0.49f, -0.55f),
            new Vector3(-0.55f, -0.49f, 0.55f),
            new Vector3(0.55f, -0.49f, 0.55f),
            new Vector3(0.55f, -0.49f, -0.55f)
        });
        outline.shadowCastingMode = ShadowCastingMode.Off;
        outline.receiveShadows = false;
        var cubePrefab = PrefabUtility.SaveAsPrefabAsset(cubeObject, Root + "/Prefabs/TestCube.prefab");
        Object.DestroyImmediate(cubeObject);

        var planes = origin.GetComponent<ARPlaneManager>();
        if (!planes) planes = Undo.AddComponent<ARPlaneManager>(origin.gameObject);
        planes.requestedDetectionMode = PlaneDetectionMode.Horizontal;
        planes.planePrefab = planePrefab;
        planes.enabled = true;
        var raycasts = origin.GetComponent<ARRaycastManager>();
        if (!raycasts) raycasts = Undo.AddComponent<ARRaycastManager>(origin.gameObject);
        raycasts.enabled = true;
        var anchors = origin.GetComponent<ARAnchorManager>();
        if (!anchors) anchors = Undo.AddComponent<ARAnchorManager>(origin.gameObject);
        anchors.enabled = true;
        var meshObject = origin.transform.Find("LiDAR Scan Preview");
        if (!meshObject)
        {
            var obj = new GameObject("LiDAR Scan Preview");
            obj.transform.SetParent(origin.transform, false);
            meshObject = obj.transform;
        }
        // Bounding volume for mesh acquisition; generated patches are parented
        // by ARMeshManager under the origin's unit-scale trackables parent.
        meshObject.localScale = Vector3.one * 10f;
        var sourceMesh = new GameObject("LiDAR Source Mesh");
        sourceMesh.AddComponent<MeshFilter>();
        var meshPrefab = PrefabUtility.SaveAsPrefabAsset(sourceMesh, Root + "/Prefabs/LiDARSourceMesh.prefab");
        Object.DestroyImmediate(sourceMesh);
        var meshes = meshObject.GetComponent<ARMeshManager>();
        if (!meshes) meshes = Undo.AddComponent<ARMeshManager>(meshObject.gameObject);
        meshes.meshPrefab = meshPrefab.GetComponent<MeshFilter>();
        meshes.normals = false;
        meshes.enabled = true;
        var preview = meshObject.GetComponent<LiDARMeshPreview>();
        if (!preview) preview = Undo.AddComponent<LiDARMeshPreview>(meshObject.gameObject);
        preview.Configure(meshes, MakeMaterial(Root + "/Materials/LiDARWire.mat",
            new Color(0.15f, 1f, 0.9f, 1f), false));
        var placement = origin.GetComponent<HorizontalPlanePlacement>();
        if (!placement) placement = Undo.AddComponent<HorizontalPlanePlacement>(origin.gameObject);
        placement.Configure(planes, raycasts, anchors, preview, origin, cubePrefab);
        EditorUtility.SetDirty(planes);
        EditorUtility.SetDirty(raycasts);
        EditorUtility.SetDirty(anchors);
        EditorUtility.SetDirty(placement);
        EditorUtility.SetDirty(meshes);
        EditorUtility.SetDirty(preview);
        EditorSceneManager.MarkSceneDirty(scene);
        FloorCalibrationMathValidation.Validate();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Lead Mills Placement] Setup saved: LiDAR triangle scan, horizontal surface selection, plane anchor and contact outline.");
    }

    [MenuItem("Lead Mills/Export Plane Test for iOS")]
    public static void ExportIOS()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
            throw new System.InvalidOperationException("Switch the active build platform to iOS first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/LeadMills_AR_POC.unity" ||
            !Object.FindFirstObjectByType<HorizontalPlanePlacement>())
            throw new System.InvalidOperationException("Set up the LeadMills_AR_POC plane test first.");
        FloorCalibrationMathValidation.Validate();
        string output = BuildTraceability.PrepareExport();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { scene.path },
            locationPathName = output,
            target = BuildTarget.iOS,
            options = BuildOptions.Development
        });
        BuildTraceability.SaveManifest(output);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.InvalidOperationException("Plane test export failed: " + report.summary.result);
        Debug.Log("[Lead Mills Placement] iOS export succeeded: " + Path.GetFullPath(output));
    }

    static Material MakeMaterial(string path, Color color, bool transparent, bool lit = false)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        var shader = Shader.Find(lit ? "Universal Render Pipeline/Lit" : "Universal Render Pipeline/Unlit");
        if (!shader) throw new System.InvalidOperationException("Required URP shader is missing.");
        if (!material)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        material.SetColor("_BaseColor", color);
        if (lit)
        {
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.2f);
        }
        material.SetFloat("_Surface", transparent ? 1f : 0f);
        material.SetFloat("_SrcBlend", (float)(transparent ? BlendMode.SrcAlpha : BlendMode.One));
        material.SetFloat("_DstBlend", (float)(transparent ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
        material.SetFloat("_ZWrite", transparent ? 0f : 1f);
        material.SetFloat("_Cull", (float)CullMode.Off);
        if (transparent) material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        else material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)(transparent ? RenderQueue.Transparent : RenderQueue.Geometry);
        material.SetOverrideTag("RenderType", transparent ? "Transparent" : "Opaque");
        EditorUtility.SetDirty(material);
        return material;
    }
}
