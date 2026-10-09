using UnityEditor;
using UnityEngine;

public static class FloorCalibrationMathValidation
{
    static FloorProjectionCalibration.Observation Observe(Vector3 camera, Vector3 point, Matrix4x4 native, float trueScale)
    {
        var pose = Matrix4x4.TRS(camera, Quaternion.LookRotation(new Vector3(0, 0, 1) - camera), Vector3.one);
        var view = Matrix4x4.Scale(new Vector3(1, 1, -1)) * pose.inverse;
        var clip = FloorProjectionCalibration.Scaled(native, trueScale) * view * new Vector4(point.x, point.y, point.z, 1);
        return new FloorProjectionCalibration.Observation {
            cameraPosition = camera, view = view, projection = native,
            viewport = new Vector2(clip.x / clip.w + 1, clip.y / clip.w + 1) * 0.5f,
            pixels = new Vector2(1179, 2556)
        };
    }
    static void Require(bool condition, string message)
    { if (!condition) throw new System.InvalidOperationException("Floor calibration validation: " + message); }

    [MenuItem("Lead Mills/Validate Three Point Calibration Math")]
    public static void Validate()
    {
        var native = Matrix4x4.Perspective(45, 1179f / 2556, 0.1f, 20);
        var floor = new Plane(Vector3.up, Vector3.zero);
        var points = new[] { new Vector3(-0.3f, 0, 0.8f), new Vector3(0.3f, 0, 0.8f), new Vector3(0, 0, 1.5f) };
        var first = new FloorProjectionCalibration.Observation[3];
        var second = new FloorProjectionCalibration.Observation[3];
        var third = new FloorProjectionCalibration.Observation[3];
        for (int i = 0; i < 3; ++i)
        {
            first[i] = Observe(new Vector3(0, 1, -1), points[i], native, 0.75f);
            second[i] = Observe(new Vector3(0.45f, 1, 0.7f), points[i], native, 0.75f);
            third[i] = Observe(new Vector3(-0.45f, 1.1f, 0.5f), points[i], native, 0.75f);
        }
        bool solved = FloorProjectionCalibration.Solve(first, second, floor, out float scale, out float error, out float baseline, out var recovered);
        Require(solved, "known focal mismatch rejected: scale=" + scale + ", error=" + error + ", baseline=" + baseline + ", minus=" + FloorProjectionCalibration.Error(first, second, floor, scale - 0.1f, out _) + ", plus=" + FloorProjectionCalibration.Error(first, second, floor, scale + 0.1f, out _));
        Require(Mathf.Abs(scale - 0.75f) < 0.001f && error < 0.1f && baseline > 12, "known focal scale not recovered: scale=" + scale + ", error=" + error + ", baseline=" + baseline);
        for (int i = 0; i < 3; ++i) Require(Vector3.Distance(points[i], recovered[i]) < 0.001f, "floor landmark reconstruction");
        Require(FloorProjectionCalibration.Error(first, third, floor, scale, out _) < 0.1f, "independent holdout projection");
        for (int count = 1; count <= 2; count++)
        {
            var reducedFirst = new FloorProjectionCalibration.Observation[count];
            var reducedSecond = new FloorProjectionCalibration.Observation[count];
            var reducedThird = new FloorProjectionCalibration.Observation[count];
            System.Array.Copy(first, reducedFirst, count); System.Array.Copy(second, reducedSecond, count); System.Array.Copy(third, reducedThird, count);
            Require(FloorProjectionCalibration.Solve(reducedFirst, reducedSecond, floor, out var reducedScale, out var reducedError, out _, out var reducedPoints), count + "-point fit rejected");
            Require(reducedPoints.Length == count && Mathf.Abs(reducedScale - 0.75f) < 0.001f && reducedError < 0.1f, count + "-point scale recovery");
            Require(FloorProjectionCalibration.Error(reducedFirst, reducedThird, floor, reducedScale, out _) < 0.1f, count + "-point independent holdout");
            reducedThird[0] = Observe(new Vector3(-0.45f, 1.1f, 0.5f), points[0] + Vector3.right * 0.2f, native, 0.75f);
            Require(FloorProjectionCalibration.Error(reducedFirst, reducedThird, floor, reducedScale, out _) > 15, count + "-point displaced holdout accepted");
        }
        var swap = second[1]; second[1] = second[2]; second[2] = swap;
        Require(!FloorProjectionCalibration.Solve(first, second, floor, out _, out _, out _, out _, out var mismatch) && mismatch.rejection == FloorProjectionCalibration.Rejection.Residual && mismatch.WorstError > 12, "wrong correspondence not identified as a residual mismatch");
        for (int i = 0; i < 3; ++i)
        {
            first[i] = Observe(new Vector3(0, 1, -1), points[i], native, 1);
            second[i] = Observe(new Vector3(0.45f, 1, 0.7f), points[i], native, 1);
        }
        Require(FloorProjectionCalibration.Solve(first, second, floor, out scale, out error, out _, out _) && scale == 1 && error < 0.1f, "correct native projection changed");
        second[0] = Observe(new Vector3(0.45f, 1, 0.7f), points[0] + Vector3.right * 0.2f, native, 1);
        Require(FloorProjectionCalibration.Error(first, second, floor, 1, out _) > 15, "holdout failed to detect displaced landmark");
        for (int i = 0; i < 3; ++i)
        {
            first[i] = Observe(new Vector3(0, 1, -1), points[i], native, 0.75f);
            second[i] = Observe(new Vector3(0.45f, 1, -1), points[i], native, 0.75f);
        }
        Require(!FloorProjectionCalibration.Solve(first, second, floor, out _, out _, out _, out _, out var weak) && weak.rejection == FloorProjectionCalibration.Rejection.WeakView, "weak viewpoint not identified for specific movement guidance");
        Debug.Log("[Lead Mills Calibration Test] Synthetic validation PASSED: 1/2/3-point fits and independent holdouts, known zoom, world points, independent holdout, wrong correspondence, native no-op displaced landmark and weak-view rejection.");
    }
}
