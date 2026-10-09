using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;

// Live ARKit geometry preview. Mesh patches are not planes or persistent anchors.
public sealed class LiDARMeshPreview : MonoBehaviour
{
    [SerializeField] ARMeshManager meshManager;
    [SerializeField] Material wireMaterial;
    readonly Dictionary<int, MeshFilter> previews = new Dictionary<int, MeshFilter>();
    bool visible = true;
    float nextLog;
    public int MeshCount => previews.Count;

    public void Configure(ARMeshManager manager, Material material)
    {
        meshManager = manager;
        wireMaterial = material;
    }

    void OnEnable()
    {
        if (!meshManager) meshManager = GetComponent<ARMeshManager>();
        if (meshManager) meshManager.meshesChanged += OnMeshesChanged;
    }

    void Update()
    {
        if (MeshCount > 0 && Time.unscaledTime >= nextLog)
        {
            nextLog = Time.unscaledTime + 5f;
            Debug.Log($"[Lead Mills Mesh] Live mesh patches: {MeshCount}; preview visible: {visible}.");
        }
    }

    public void SetVisible(bool show)
    {
        if (visible == show) return;
        visible = show;
        foreach (var preview in previews.Values)
            if (preview) preview.GetComponent<MeshRenderer>().enabled = show;
    }

    void OnMeshesChanged(ARMeshesChangedEventArgs args)
    {
        foreach (var filter in args.removed) RemovePreview(filter.GetInstanceID());
        foreach (var filter in args.added) RebuildPreview(filter);
        foreach (var filter in args.updated) RebuildPreview(filter);
    }

    void RebuildPreview(MeshFilter source)
    {
        if (!source || !source.sharedMesh || source.sharedMesh.vertexCount == 0 || !wireMaterial) return;
        int key = source.GetInstanceID();
        if (!previews.TryGetValue(key, out var preview) || !preview)
        {
            var obj = new GameObject("LiDAR Triangle Edges");
            obj.transform.SetParent(source.transform, false);
            preview = obj.AddComponent<MeshFilter>();
            preview.sharedMesh = new Mesh { name = "Live LiDAR Triangle Edges", indexFormat = IndexFormat.UInt32 };
            preview.sharedMesh.MarkDynamic();
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = wireMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            previews[key] = preview;
        }
        var triangles = source.sharedMesh.triangles;
        var lines = new int[triangles.Length * 2];
        for (int i = 0, j = 0; i + 2 < triangles.Length; i += 3)
        {
            int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
            lines[j++] = a; lines[j++] = b;
            lines[j++] = b; lines[j++] = c;
            lines[j++] = c; lines[j++] = a;
        }
        var mesh = preview.sharedMesh;
        mesh.Clear();
        mesh.vertices = source.sharedMesh.vertices;
        mesh.SetIndices(lines, MeshTopology.Lines, 0);
        preview.GetComponent<MeshRenderer>().enabled = visible;
    }

    void RemovePreview(int key)
    {
        if (!previews.TryGetValue(key, out var preview)) return;
        if (preview)
        {
            Destroy(preview.sharedMesh);
            Destroy(preview.gameObject);
        }
        previews.Remove(key);
    }

    void OnDisable()
    {
        if (meshManager) meshManager.meshesChanged -= OnMeshesChanged;
        foreach (int key in new List<int>(previews.Keys)) RemovePreview(key);
    }
}
