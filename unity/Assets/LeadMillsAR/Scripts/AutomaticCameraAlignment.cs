using UnityEngine;
using UnityEngine.XR.ARSubsystems;

// ARKit background shader uses row-vector UV * displayMatrix. Derive focal
// extents from those UV derivatives and camera intrinsics, not a stored room fit.
public static class AutomaticCameraAlignment
{
    public static bool TryScale(Matrix4x4 projection, Matrix4x4 display, XRCameraIntrinsics intrinsics, out float scale)
    {
        scale = 1;
        if (intrinsics.resolution.x <= 0 || intrinsics.resolution.y <= 0 ||
            intrinsics.focalLength.x <= 0 || intrinsics.focalLength.y <= 0 ||
            projection.m00 <= 0 || projection.m11 <= 0) return false;
        // This prototype supports the physically tested portrait/CW90 axis swap only.
        if (Mathf.Abs(display.m00) > .02f || Mathf.Abs(display.m11) > .02f ||
            Mathf.Abs(display.m01) < .2f || Mathf.Abs(display.m10) < .2f) return false;
        var u = new Vector2(display.m00 * intrinsics.resolution.x / intrinsics.focalLength.x,
            display.m01 * intrinsics.resolution.y / intrinsics.focalLength.y);
        var v = new Vector2(display.m10 * intrinsics.resolution.x / intrinsics.focalLength.x,
            display.m11 * intrinsics.resolution.y / intrinsics.focalLength.y);
        if (u.magnitude < .01f || v.magnitude < .01f || Mathf.Abs(Vector2.Dot(u.normalized, v.normalized)) > .02f) return false;
        float x = 2f / u.magnitude / projection.m00, y = 2f / v.magnitude / projection.m11;
        if (float.IsNaN(x) || float.IsNaN(y) || float.IsInfinity(x) || float.IsInfinity(y) ||
            x < .5f || x > 1.5f || y < .5f || y > 1.5f || Mathf.Abs(x-y) > .03f * Mathf.Max(x,y)) return false;
        scale = (x+y)*.5f;
        return true;
    }
}
