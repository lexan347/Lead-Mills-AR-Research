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
    struct SurfaceSample { public Vector3 center, normal; }
    readonly Dictionary<int, List<SurfaceSample>> samples = new Dictionary<int, List<SurfaceSample>>();
    readonly HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
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

    // Count spatial coverage, not raw vertex density. Samples are triangle centers
    // transformed through their live mesh parent, so origin changes stay consistent.
    public int SupportedCells(ARPlane plane, Vector3 center, float radius)
    {
        occupiedCells.Clear();
        Vector3 planeCenter = plane.transform.InverseTransformPoint(center);
        foreach (var pair in samples)
        {
            if (!previews.TryGetValue(pair.Key, out var filter) || !filter) continue;
            foreach (var sample in pair.Value)
            {
                Vector3 world = filter.transform.TransformPoint(sample.center);
                Vector3 normal = filter.transform.TransformDirection(sample.normal).normalized;
                Vector3 local = plane.transform.InverseTransformPoint(world);
                if (Mathf.Abs(local.y) > 0.05f || Mathf.Abs(Vector3.Dot(normal, plane.transform.up)) < 0.94f)
                    continue;
                Vector2 offset = new Vector2(local.x - planeCenter.x, local.z - planeCenter.z);
                if (offset.sqrMagnitude > radius * radius) continue;
                if (!InsideBoundary(new Vector2(local.x, local.z), plane)) continue;
                occupiedCells.Add(new Vector2Int(Mathf.FloorToInt(offset.x / 0.10f), Mathf.FloorToInt(offset.y / 0.10f)));
            }
        }
        return occupiedCells.Count;
    }

    static bool InsideBoundary(Vector2 point, ARPlane plane)
    {
        var boundary = plane.boundary;
        bool inside = false;
        for (int i = 0, j = boundary.Length - 1; i < boundary.Length; j = i++)
        {
            var a = boundary[i]; var b = boundary[j];
            if ((a.y > point.y) != (b.y > point.y) &&
                point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
        }
        return inside;
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
        var vertices = source.sharedMesh.vertices;
        var triangles = source.sharedMesh.triangles;
        var surfaceSamples = new List<SurfaceSample>(triangles.Length / 3);
        for (int i = 0; i + 2 < triangles.Length; i += 3)
        {
            Vector3 a = vertices[triangles[i]], b = vertices[triangles[i + 1]], c = vertices[triangles[i + 2]];
            Vector3 cross = Vector3.Cross(b - a, c - a);
            if (cross.sqrMagnitude > 0.00000001f)
                surfaceSamples.Add(new SurfaceSample { center = (a + b + c) / 3f, normal = cross.normalized });
        }
        samples[key] = surfaceSamples;
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
        samples.Remove(key);
    }

    void OnDisable()
    {
        if (meshManager) meshManager.meshesChanged -= OnMeshesChanged;
        foreach (int key in new List<int>(previews.Keys)) RemovePreview(key);
    }
}
