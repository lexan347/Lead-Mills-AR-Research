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
        points = new Vector3[first.Length];
        if (first.Length < 1 || first.Length > 3 || repeat.Length != first.Length) return float.PositiveInfinity;
        float squared = 0;
        for (int i = 0; i < first.Length; ++i)
        {
            if (!FloorPoint(first[i], floor, scale, out points[i])) return float.PositiveInfinity;
            var clip = Scaled(repeat[i].projection, scale) * repeat[i].view *
                new Vector4(points[i].x, points[i].y, points[i].z, 1);
            if (clip.w <= 0) return float.PositiveInfinity;
            var uv = new Vector2(clip.x / clip.w + 1, clip.y / clip.w + 1) * 0.5f;
            squared += Vector2.Scale(uv - repeat[i].viewport, repeat[i].pixels).sqrMagnitude;
        }
        return Mathf.Sqrt(squared / first.Length);
    }

    public enum Rejection { None, InvalidGeometry, ScaleBoundary, WeakView, Residual, NoImprovement, SmallTriangle }
    public struct FitReport
    {
        public Rejection rejection;
        public float scale, rms, baseline, sensitivityMinus, sensitivityPlus, area;
        public float errorA, errorB, errorC;
        public int WorstPoint => errorA >= errorB && errorA >= errorC ? 0 : errorB >= errorC ? 1 : 2;
        public float WorstError => Mathf.Max(errorA, Mathf.Max(errorB, errorC));
        public override string ToString() => $"reason {rejection}; scale {scale:F5}; RMS {rms:F2}px; baseline {baseline:F2}px; sensitivity {sensitivityMinus:F2}/{sensitivityPlus:F2}px; area {area:F5}m2; A/B/C {errorA:F2}/{errorB:F2}/{errorC:F2}px";
    }
    public static float PointError(Observation observation, Vector3 point, float scale)
    {
        var clip = Scaled(observation.projection, scale) * observation.view * new Vector4(point.x, point.y, point.z, 1);
        if (clip.w <= 0) return float.PositiveInfinity;
        var uv = new Vector2(clip.x / clip.w + 1, clip.y / clip.w + 1) * 0.5f;
        return Vector2.Scale(uv - observation.viewport, observation.pixels).magnitude;
    }
    public static bool Solve(Observation[] first, Observation[] repeat, Plane floor,
        out float scale, out float error, out float baseline, out Vector3[] points) =>
        Solve(first, repeat, floor, out scale, out error, out baseline, out points, out _);

    public static bool Solve(Observation[] first, Observation[] repeat, Plane floor,
        out float scale, out float error, out float baseline, out Vector3[] points, out FitReport report)
    {
        if (first.Length < 1 || first.Length > 3 || repeat.Length != first.Length)
        {
            scale = 1; error = baseline = float.PositiveInfinity; points = new Vector3[first.Length];
            report = new FitReport { rejection = Rejection.InvalidGeometry }; return false;
        }
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
        report = new FitReport {
            scale = scale, rms = error, baseline = baseline,
            sensitivityMinus = Error(first, repeat, floor, scale - 0.1f, out _) - error,
            sensitivityPlus = Error(first, repeat, floor, scale + 0.1f, out _) - error,
            area = points.Length == 3 ? Vector3.Cross(points[1] - points[0], points[2] - points[0]).magnitude * 0.5f : 0,
            errorA = PointError(repeat[0], points[0], scale),
            errorB = points.Length > 1 ? PointError(repeat[1], points[1], scale) : 0,
            errorC = points.Length > 2 ? PointError(repeat[2], points[2], scale) : 0
        };
        if (float.IsNaN(error) || float.IsInfinity(error)) report.rejection = Rejection.InvalidGeometry;
        else if (error > 12) report.rejection = Rejection.Residual;
        else if (scale <= 0.52f || scale >= 1.48f) report.rejection = Rejection.ScaleBoundary;
        else if (report.sensitivityMinus < 5 || report.sensitivityPlus < 5) report.rejection = Rejection.WeakView;
        else if (baseline > 8 && error > baseline * 0.75f) report.rejection = Rejection.NoImprovement;
        else
        {
            if (baseline <= 8) { scale = 1; error = baseline; Error(first, repeat, floor, scale, out points); }
            report.area = points.Length == 3 ? Vector3.Cross(points[1] - points[0], points[2] - points[0]).magnitude * 0.5f : 0;
            if ((points.Length == 3 && report.area < 0.02f) || (points.Length == 2 && Vector3.Distance(points[0], points[1]) < 0.25f)) report.rejection = Rejection.SmallTriangle;
        }
        return report.rejection == Rejection.None;
    }
}
