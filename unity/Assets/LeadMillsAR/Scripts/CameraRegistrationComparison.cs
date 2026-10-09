using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

// Temporary device comparison controls; baseline is the existing Input System driver.
// Explicit view-roll hypotheses are opt-in; provider meshes are never modified.
public sealed class CameraRegistrationComparison : MonoBehaviour
{
    readonly List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();
    TrackedPoseDriver driver;
    HorizontalPlanePlacement placement;
    UniversalRenderPipelineAsset pipeline;
    bool originalBatching, useCameraPose, poseAvailable;
    Pose cameraPose;
    int rollMode;
    bool restoreBaselineRotation;
    public bool ViewRollTestActive => rollMode != 0;
    float RollAngle => rollMode == 1 ? 90f : rollMode == 2 ? -90f : 0f;
    string poseSource = "Unavailable";
    float nextDiscovery, nextLog;
    GUIStyle style;

    public void Configure(HorizontalPlanePlacement owner) { placement = owner; }

    void Awake()
    {
        driver = GetComponent<TrackedPoseDriver>();
        pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (pipeline) originalBatching = pipeline.useSRPBatcher;
    }

    void OnEnable() { Application.onBeforeRender += ApplyCameraPose; }
    void OnDisable()
    {
        Application.onBeforeRender -= ApplyCameraPose;
        rollMode = 0;
        restoreBaselineRotation = true;
        ApplyCameraPose();
        if (driver) driver.enabled = true;
        if (pipeline) pipeline.useSRPBatcher = originalBatching;
        GraphicsSettings.useScriptableRenderPipelineBatching = originalBatching;
    }

    void ReadCameraPose()
    {
        if (Time.unscaledTime >= nextDiscovery)
        {
            nextDiscovery = Time.unscaledTime + 2f;
            InputDevices.GetDevices(devices);
        }
        poseAvailable = false;
        foreach (var device in devices)
        {
            if (!device.isValid || (device.characteristics & InputDeviceCharacteristics.TrackedDevice) == 0) continue;
            Vector3 position; Quaternion rotation;
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.colorCameraPosition, out position) &&
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.colorCameraRotation, out rotation))
            {
                cameraPose = new Pose(position, rotation);
                poseSource = device.name + "/colorCamera";
                poseAvailable = true;
                break;
            }
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.centerEyePosition, out position) &&
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.centerEyeRotation, out rotation))
            {
                cameraPose = new Pose(position, rotation);
                poseSource = device.name + "/centerEye";
                poseAvailable = true;
                break;
            }
        }
        if (!poseAvailable) poseSource = "Unavailable";
    }

    void Update()
    {
        ReadCameraPose();
        if (useCameraPose && !poseAvailable)
        {
            useCameraPose = false;
            if (driver) driver.enabled = true;
            if (placement) placement.RestartSurfaceCheck();
            Debug.LogWarning("[Lead Mills Compare] Alternate pose lost; restored Input System driver.");
        }
        if (Time.unscaledTime >= nextLog)
        {
            nextLog = Time.unscaledTime + 2f;
            string difference = poseAvailable ?
                $"rotation difference {Quaternion.Angle(transform.localRotation, cameraPose.rotation):F2} deg; position difference {Vector3.Distance(transform.localPosition, cameraPose.position):F4} m" :
                "no alternate camera pose";
            Debug.Log($"[Lead Mills Compare] Mode {(useCameraPose ? "XR camera" : "Input System")}; source {poseSource}; {difference}; SRP batching {(pipeline ? pipeline.useSRPBatcher.ToString() : "Unavailable")}; view roll {RollAngle:F0} deg; orientation {Screen.orientation}.");
        }
        var touch = Touchscreen.current;
        if (touch == null || !touch.primaryTouch.press.wasPressedThisFrame) return;
        var point = touch.primaryTouch.position.ReadValue();
        point.y = Screen.height - point.y;
        if (PoseRect().Contains(point)) TogglePose();
        else if (BatchRect().Contains(point)) ToggleBatching();
        else if (RollRect().Contains(point)) ToggleRoll();
    }

    void LateUpdate() { ApplyCameraPose(); }
    [BeforeRenderOrder(100)]
    void ApplyCameraPose()
    {
        Quaternion rawRotation;
        if (useCameraPose)
        {
            ReadCameraPose();
            if (!poseAvailable) return;
            transform.localPosition = cameraPose.position;
            rawRotation = cameraPose.rotation;
        }
        else
        {
            if (rollMode == 0 && !restoreBaselineRotation) return;
            var action = driver ? driver.rotationInput.action : null;
            if (action == null || action.controls.Count == 0) return;
            rawRotation = action.ReadValue<Quaternion>();
            if (Quaternion.Dot(rawRotation, rawRotation) < 0.5f) return;
        }
        // Positive camera-local Z roll makes rendered geometry turn clockwise
        // in the image. Recompute from the raw source every time: never accumulate.
        transform.localRotation = rawRotation.normalized * Quaternion.Euler(0, 0, RollAngle);
        restoreBaselineRotation = false;
    }

    public void PrepareViewForRaycast() { ApplyCameraPose(); }

    void ToggleRoll()
    {
        rollMode = (rollMode + 1) % 3;
        restoreBaselineRotation = rollMode == 0;
        ApplyCameraPose();
        if (placement) placement.RestartSurfaceCheck();
        Debug.Log($"[Lead Mills Compare] Opt-in view roll changed to {RollAngle:F0} deg; remove prior placement and restart landmark trial. This is a hypothesis, not an accepted correction.");
    }

    void TogglePose()
    {
        ReadCameraPose();
        if (!driver || (!useCameraPose && !poseAvailable))
        {
            Debug.LogWarning("[Lead Mills Compare] Alternate camera pose unavailable; baseline retained.");
            return;
        }
        useCameraPose = !useCameraPose;
        driver.enabled = !useCameraPose;
        if (placement) placement.RestartSurfaceCheck();
        Debug.Log($"[Lead Mills Compare] Pose mode changed to {(useCameraPose ? "XR camera" : "Input System")}; restart landmark trial.");
    }

    void ToggleBatching()
    {
        if (!pipeline) return;
        pipeline.useSRPBatcher = !pipeline.useSRPBatcher;
        GraphicsSettings.useScriptableRenderPipelineBatching = pipeline.useSRPBatcher;
        Debug.Log($"[Lead Mills Compare] SRP batching changed to {pipeline.useSRPBatcher}; pose mode unchanged.");
    }

    float Scale => Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height) / 600f);
    Rect PanelRect()
    {
        var safe = Screen.safeArea;
        return new Rect(safe.x + 8 * Scale, Screen.height - safe.yMin - 166 * Scale,
            safe.width - 16 * Scale, 158 * Scale);
    }
    Rect PoseRect() { var r = PanelRect(); return new Rect(r.x + 8 * Scale, r.y + 8 * Scale, r.width - 16 * Scale, 42 * Scale); }
    Rect BatchRect() { var r = PoseRect(); r.y += 50 * Scale; return r; }
    Rect RollRect() { var r = BatchRect(); r.y += 50 * Scale; return r; }
    public bool ContainsScreenPoint(Vector2 point)
    {
        point.y = Screen.height - point.y;
        return PanelRect().Contains(point);
    }

    void OnGUI()
    {
        if (style == null) style = new GUIStyle(GUI.skin.button) { fontSize = 20, wordWrap = true };
        var previous = GUI.matrix;
        float scale = Scale;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        var panel = PanelRect();
        GUI.Box(new Rect(panel.x / scale, panel.y / scale, panel.width / scale, panel.height / scale), GUIContent.none);
        var pose = PoseRect(); var batch = BatchRect();
        GUI.enabled = driver && (poseAvailable || useCameraPose);
        if (GUI.Button(new Rect(pose.x / scale, pose.y / scale, pose.width / scale, pose.height / scale),
            poseAvailable ? "Pose: " + (useCameraPose ? "XR camera" : "Input System") : "XR camera pose unavailable", style)) TogglePose();
        GUI.enabled = pipeline;
        if (GUI.Button(new Rect(batch.x / scale, batch.y / scale, batch.width / scale, batch.height / scale),
            "Render test — SRP batching: " + (pipeline && pipeline.useSRPBatcher ? "ON" : "OFF"), style)) ToggleBatching();
        GUI.enabled = driver || (useCameraPose && poseAvailable);
        var roll = RollRect();
        string rollLabel = rollMode == 0 ? "BASELINE" : rollMode == 1 ? "90 deg CLOCKWISE" : "90 deg COUNTERCLOCKWISE";
        if (GUI.Button(new Rect(roll.x / scale, roll.y / scale, roll.width / scale, roll.height / scale),
            "View rotation test: " + rollLabel, style)) ToggleRoll();
        GUI.enabled = true;
        GUI.matrix = previous;
    }
}
