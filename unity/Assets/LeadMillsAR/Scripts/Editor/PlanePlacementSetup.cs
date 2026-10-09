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
        var cubeMaterial = MakeMaterial(Root + "/Materials/TestCube.mat", new Color(1f, 0.42f, 0.06f, 1f), false);
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
        var placement = origin.GetComponent<HorizontalPlanePlacement>();
        if (!placement) placement = Undo.AddComponent<HorizontalPlanePlacement>(origin.gameObject);
        placement.Configure(planes, raycasts, anchors, origin, cubePrefab);
        EditorUtility.SetDirty(planes);
        EditorUtility.SetDirty(raycasts);
        EditorUtility.SetDirty(anchors);
        EditorUtility.SetDirty(placement);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Lead Mills Placement] Setup saved: horizontal planes, blue surface visualization, tap to anchor one orange 20 cm cube to a plane.");
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
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        string output = "Builds/iOS_PlanePlacement_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { scene.path },
            locationPathName = output,
            target = BuildTarget.iOS,
            options = BuildOptions.Development
        });
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.InvalidOperationException("Plane test export failed: " + report.summary.result);
        Debug.Log("[Lead Mills Placement] iOS export succeeded: " + Path.GetFullPath(output));
    }

    static Material MakeMaterial(string path, Color color, bool transparent)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (!shader) throw new System.InvalidOperationException("URP Unlit shader is missing.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
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
