using UnityEngine;

// A single focal-scale fit, not a six-degree pose correction or an accuracy claim.
public static class FloorProjectionCalibration
{
    public struct Observation
    {
        public Matrix4x4 view, projection;
        public Vector2 viewport, pixels;
        public Vector3 cameraPosition;
    }

    public static Matrix4x4 Scaled(Matrix4x4 native, float scale)
    {
        native.m00 *= scale;
        native.m11 *= scale;
        return native;
    }

    public static bool FloorPoint(Observation observation, Plane floor, float scale, out Vector3 point)
    {
        var q = Scaled(observation.projection, scale).inverse *
            new Vector4(observation.viewport.x * 2 - 1, observation.viewport.y * 2 - 1, 1, 1);
        point = default;
        if (Mathf.Abs(q.w) < 1e-6f) return false;
        var local = new Vector3(q.x, q.y, q.z) / q.w;
        var direction = observation.view.inverse.MultiplyVector(local).normalized;
        var ray = new Ray(observation.cameraPosition, direction);
        if (!floor.Raycast(ray, out float distance) || distance < 0.1f || distance > 5f) return false;
        point = ray.GetPoint(distance);
        return true;
    }

    public static float Error(Observation[] first, Observation[] repeat, Plane floor, float scale,
        out Vector3[] points)
    {
        points = new Vector3[3];
        float squared = 0;
        for (int i = 0; i < 3; ++i)
        {
            if (!FloorPoint(first[i], floor, scale, out points[i])) return float.PositiveInfinity;
            var clip = Scaled(repeat[i].projection, scale) * repeat[i].view *
                new Vector4(points[i].x, points[i].y, points[i].z, 1);
            if (clip.w <= 0) return float.PositiveInfinity;
            var uv = new Vector2(clip.x / clip.w + 1, clip.y / clip.w + 1) * 0.5f;
            squared += Vector2.Scale(uv - repeat[i].viewport, repeat[i].pixels).sqrMagnitude;
        }
        return Mathf.Sqrt(squared / 3);
    }

    public static bool Solve(Observation[] first, Observation[] repeat, Plane floor,
        out float scale, out float error, out float baseline, out Vector3[] points)
    {
        baseline = Error(first, repeat, floor, 1, out points);
        scale = 1;
        error = float.PositiveInfinity;
        for (int i = 0; i <= 200; ++i)
        {
            float candidate = 0.5f + i * 0.005f;
            float e = Error(first, repeat, floor, candidate, out _);
            if (e < error) { error = e; scale = candidate; }
        }
        float lo = Mathf.Max(0.5f, scale - 0.01f), hi = Mathf.Min(1.5f, scale + 0.01f);
        for (int i = 0; i < 32; ++i)
        {
            float a = Mathf.Lerp(lo, hi, 1f / 3), b = Mathf.Lerp(lo, hi, 2f / 3);
            if (Error(first, repeat, floor, a, out _) < Error(first, repeat, floor, b, out _)) hi = b;
            else lo = a;
        }
        scale = (lo + hi) * 0.5f;
        error = Error(first, repeat, floor, scale, out points);
        // A low residual alone can hide an unobservable focal scale. Require
        // a 10% zoom change either way to produce a measurable image difference.
        if (Error(first, repeat, floor, scale - 0.1f, out _) - error < 5f ||
            Error(first, repeat, floor, scale + 0.1f, out _) - error < 5f) return false;
        if (baseline <= 8f) { scale = 1; error = baseline; Error(first, repeat, floor, scale, out points); }
        if (float.IsNaN(error) || float.IsInfinity(error) || error > 12 || scale <= 0.52f || scale >= 1.48f) return false;
        if (scale != 1 && error > baseline * 0.75f) return false;
        return Vector3.Cross(points[1] - points[0], points[2] - points[0]).magnitude * 0.5f >= 0.02f;
    }
}
