using UnityEngine;

// Test the mesh surface itself; triangle centers are not coverage measurements.
public static class SurfaceCoverage
{
    public static bool IsLevel(Vector3 a,Vector3 b,Vector3 c,float maximumSlopeDegrees=20f)
    {
        var normal=Vector3.Cross(b-a,c-a);
        return normal.sqrMagnitude>1e-10f && Mathf.Abs(normal.normalized.y)>=Mathf.Cos(maximumSlopeDegrees*Mathf.Deg2Rad);
    }
    public static bool ContainsAtPlane(Vector3 a,Vector3 b,Vector3 c,Vector2 point,float heightTolerance=.05f)
    {
        float denominator=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);
        if(Mathf.Abs(denominator)<1e-8f)return false;
        float u=((b.z-c.z)*(point.x-c.x)+(c.x-b.x)*(point.y-c.z))/denominator;
        float v=((c.z-a.z)*(point.x-c.x)+(a.x-c.x)*(point.y-c.z))/denominator;
        float w=1-u-v;
        return u>=-1e-5f && v>=-1e-5f && w>=-1e-5f && Mathf.Abs(u*a.y+v*b.y+w*c.y)<=heightTolerance;
    }
}
