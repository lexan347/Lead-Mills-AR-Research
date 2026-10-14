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
    struct SurfaceSample { public Vector3 a, b, c; }
    readonly Dictionary<int, List<SurfaceSample>> samples = new Dictionary<int, List<SurfaceSample>>();
    readonly HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
    bool visible = true;
    float nextLog;
    public int MeshCount => previews.Count;
    public void SetScanning(bool scan)
    {
        if (!meshManager) meshManager=GetComponent<ARMeshManager>();
        if(scan){enabled=true;if(meshManager)meshManager.enabled=true;}
        else {SetVisible(false);if(meshManager)meshManager.enabled=false;enabled=false;}
        Debug.Log("[Lead Mills Scan] Requested "+(scan?"ON":"OFF")+"; mesh subsystem running: "+(meshManager && meshManager.subsystem!=null && meshManager.subsystem.running)+"; camera and anchor tracking retained.");
    }

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

    // Count projected triangle surface coverage, allowing rough facets around the ground estimate.
    // All triangle vertices are transformed through their live mesh parent.
    public int SupportedCells(ARPlane plane, Vector3 center, float radius)
    {
        occupiedCells.Clear();
        Vector3 planeCenter = plane.transform.InverseTransformPoint(center);
        const float cellSize=.05f;
        int limit=Mathf.CeilToInt(radius/cellSize);
        foreach (var pair in samples)
        {
            if (!previews.TryGetValue(pair.Key, out var filter) || !filter) continue;
            var toPlane=plane.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
            foreach (var sample in pair.Value)
            {
                var a=toPlane.MultiplyPoint3x4(sample.a);
                var b=toPlane.MultiplyPoint3x4(sample.b);
                var c=toPlane.MultiplyPoint3x4(sample.c);
                if(!SurfaceCoverage.IsLevel(a,b,c,GroundPlacementPolicy.MaximumFacetSlopeDegrees))continue;
                float minX=Mathf.Min(a.x,Mathf.Min(b.x,c.x))-planeCenter.x;
                float maxX=Mathf.Max(a.x,Mathf.Max(b.x,c.x))-planeCenter.x;
                float minZ=Mathf.Min(a.z,Mathf.Min(b.z,c.z))-planeCenter.z;
                float maxZ=Mathf.Max(a.z,Mathf.Max(b.z,c.z))-planeCenter.z;
                if(maxX < -radius || minX > radius || maxZ < -radius || minZ > radius)continue;
                for(int x=Mathf.Max(-limit,Mathf.FloorToInt(minX/cellSize));x<Mathf.Min(limit,Mathf.CeilToInt(maxX/cellSize));x++)
                for(int z=Mathf.Max(-limit,Mathf.FloorToInt(minZ/cellSize));z<Mathf.Min(limit,Mathf.CeilToInt(maxZ/cellSize));z++)
                {
                    var key=new Vector2Int(x,z);
                    var offset=new Vector2((x+.5f)*cellSize,(z+.5f)*cellSize);
                    if(occupiedCells.Contains(key)||offset.sqrMagnitude>radius*radius)continue;
                    var point=new Vector2(planeCenter.x+offset.x,planeCenter.z+offset.y);
                    if(SurfaceCoverage.ContainsAtPlane(a,b,c,point,GroundPlacementPolicy.HeightTolerance) && InsideBoundary(point,plane))occupiedCells.Add(key);
                }
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
                surfaceSamples.Add(new SurfaceSample { a=a, b=b, c=c });
        }
        samples[key] = surfaceSamples;
        if(!visible){preview.GetComponent<MeshRenderer>().enabled=false;return;}
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
