using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[DefaultExecutionOrder(-50)]
public sealed class ThreePointFloorCalibration : MonoBehaviour
{
    enum Step { Idle, First, Second, Third, Verified }
    Step step;
    HorizontalPlanePlacement placement;
    CameraRegistrationComparison comparison;
    ARCameraManager manager;
    Camera arCamera;
    ARPlane plane;
    Plane floor;
    Vector3 planeStart;
    Matrix4x4 nativeProjection;
    bool hasProjection;
    float frameTime;
    int pointIndex, revision;
    float scale = 1, fitError, holdoutError;
    readonly FloorProjectionCalibration.Observation[] first = new FloorProjectionCalibration.Observation[3];
    readonly FloorProjectionCalibration.Observation[] second = new FloorProjectionCalibration.Observation[3];
    readonly FloorProjectionCalibration.Observation[] third = new FloorProjectionCalibration.Observation[3];
    Vector3[] points;
    string message = "Three real floor landmarks; recheck from two other views.";
    GUIStyle textStyle, buttonStyle;
    bool Capturing => step == Step.First || step == Step.Second || step == Step.Third;
    public bool CapturesPlacement => Capturing;
    public bool ConsumedTouchThisFrame { get; private set; }

    public void Configure(HorizontalPlanePlacement owner, CameraRegistrationComparison control, Camera camera)
    {
        placement = owner; comparison = control; arCamera = camera;
        manager = camera.GetComponent<ARCameraManager>();
        if (manager) manager.frameReceived += OnFrame;
        Application.onBeforeRender += ApplyProjection;
        RenderPipelineManager.beginCameraRendering += OnRendering;
        Debug.Log("[Lead Mills Calibration Test] Ready: three landmarks, translated/tilted repeat, independent third view, native anchor at A. Native projection retained until a candidate passes fitting.");
    }
    void OnDisable()
    {
        if (manager) manager.frameReceived -= OnFrame;
        Application.onBeforeRender -= ApplyProjection;
        RenderPipelineManager.beginCameraRendering -= OnRendering;
        if (arCamera && hasProjection) arCamera.projectionMatrix = nativeProjection;
    }
    void OnApplicationPause(bool paused) { if (paused) Cancel("App paused: calibration cleared. Rescan on return."); }
    void OnFrame(ARCameraFrameEventArgs frame)
    {
        if (!frame.projectionMatrix.HasValue) return;
        nativeProjection = frame.projectionMatrix.Value;
        hasProjection = true; frameTime = Time.unscaledTime;
        ApplyProjection();
    }
    void OnRendering(ScriptableRenderContext context, Camera camera) { if (camera == arCamera) ApplyProjection(); }
    [BeforeRenderOrder(200)]
    public void ApplyProjection()
    {
        if (!arCamera || !hasProjection) return;
        bool active = step == Step.Third || step == Step.Verified;
        arCamera.projectionMatrix = active ? FloorProjectionCalibration.Scaled(nativeProjection, scale) : nativeProjection;
    }
    public void Cancel(string reason)
    {
        bool hadCalibration = step != Step.Idle;
        step = Step.Idle; scale = 1; pointIndex = 0; plane = null;
        message = reason;
        ApplyProjection();
        if (hadCalibration)
        {
            if (placement) placement.RestartSurfaceCheck();
            Debug.Log("[Lead Mills Calibration Test] Cleared: " + reason);
        }
    }
    void Begin()
    {
        Cancel("Starting a new trial.");
        if (!comparison.PortraitCorrectionActive || !Steady())
        { message = "Wait for tracking and select CW90/Portrait first."; return; }
        placement.RestartSurfaceCheck();
        revision = comparison.RegistrationRevision;
        step = Step.First; pointIndex = 0;
        message = "View 1: tap A on a real floor mark. Then B and C, spread in a triangle.";
    }
    bool Steady() => hasProjection && Time.unscaledTime - frameTime < 0.25f &&
        ARSession.state == ARSessionState.SessionTracking && ARSession.notTrackingReason == NotTrackingReason.None;
    Vector3 MeanCamera(FloorProjectionCalibration.Observation[] observations) =>
        (observations[0].cameraPosition + observations[1].cameraPosition + observations[2].cameraPosition) / 3;
    void Update()
    {
        ConsumedTouchThisFrame = false;
        if (!placement || !comparison) return;
        if (step != Step.Idle && (revision != comparison.RegistrationRevision || !Steady() ||
            (plane && (plane.trackingState != TrackingState.Tracking || plane.subsumedBy != null ||
            Vector3.Distance(plane.transform.position, planeStart) > 0.02f))))
        { Cancel("Tracking/plane/mode changed: restart the three-point trial."); return; }
        var touch = Touchscreen.current;
        bool pressed = touch != null && touch.primaryTouch.press.wasPressedThisFrame;
        Vector2 position = pressed ? touch.primaryTouch.position.ReadValue() : Vector2.zero;
        if (!pressed && !Application.isMobilePlatform && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        { pressed = true; position = Mouse.current.position.ReadValue(); }
        if (!pressed) return;
        var gui = new Vector2(position.x, Screen.height - position.y);
        if (StartRect().Contains(gui)) { if (step == Step.Idle) Begin(); else Cancel("Trial cleared; native projection restored."); return; }
        if (AnchorRect().Contains(gui))
        {
            if (step == Step.Verified && points != null && plane)
                message = placement.PlaceCalibrationAnchor(plane, points[0]) ? "A anchored. Check contact and movement; calibration is session-local." : "A no longer has enough surface support; rescan/retry.";
            return;
        }
        if (!Capturing || PanelRect().Contains(gui) || comparison.ContainsScreenPoint(position) || placement.ContainsHudScreenPoint(position)) return;
        ConsumedTouchThisFrame = true;
        Capture(position);
    }
    void Capture(Vector2 screen)
    {
        comparison.PrepareViewForRaycast(); ApplyProjection();
        if (step == Step.First && pointIndex == 0)
        {
            if (!placement.TryCalibrationPlane(screen, out plane)) { message = "Tap a qualified blue floor surface for A."; return; }
            planeStart = plane.transform.position;
            floor = new Plane(plane.transform.up, plane.transform.TransformPoint(new Vector3(plane.center.x, 0, plane.center.y)));
        }
        var observation = new FloorProjectionCalibration.Observation {
            view = arCamera.worldToCameraMatrix, projection = nativeProjection,
            viewport = new Vector2(screen.x / Screen.width, screen.y / Screen.height),
            pixels = new Vector2(Screen.width, Screen.height), cameraPosition = arCamera.transform.position
        };
        if (!FloorProjectionCalibration.FloorPoint(observation, floor, step == Step.Third ? scale : 1, out _))
        { message = "Keep the real floor mark visible, within five meters."; return; }
        if (step == Step.Second || step == Step.Third)
        {
            var previous = step == Step.Second ? first : second;
            if (Vector3.ProjectOnPlane(observation.cameraPosition - MeanCamera(previous), floor.normal).magnitude < 0.25f)
            { message = "Move sideways at least 25 cm; then tap the same real A, B, C marks."; return; }
        }
        var target = step == Step.First ? first : step == Step.Second ? second : third;
        target[pointIndex++] = observation;
        Debug.Log($"[Lead Mills Calibration Test] {step} point {pointIndex}; t {Time.unscaledTime:F3}; screen {observation.viewport}; camera {observation.cameraPosition.ToString("F5")}.");
        if (pointIndex < 3) { message = $"{step} view: tap {(step == Step.First ? "a different" : "the same")} real {(pointIndex == 1 ? "B" : "C")} floor mark."; return; }
        pointIndex = 0;
        if (step == Step.First)
        {
            FloorProjectionCalibration.Error(first, first, floor, 1, out var initial);
            if (Vector3.Cross(initial[1] - initial[0], initial[2] - initial[0]).magnitude * 0.5f < 0.02f)
            { Cancel("Choose three marks spread in a triangle, not a line; start again."); return; }
            step = Step.Second;
            message = "View 2: move 25 cm sideways and change downward tilt. Tap SAME real A/B/C.";
        }
        else if (step == Step.Second)
        {
            if (!FloorProjectionCalibration.Solve(first, second, floor, out scale, out fitError, out float baseline, out points))
            { Cancel("Fit is uncertain. Restart with clearer marks and a bigger viewpoint/tilt change."); return; }
            step = Step.Third; ApplyProjection();
            message = $"Candidate zoom {scale:F3}; fit {fitError:F1}px. View 3: move/tilt again (25 cm); retap real A/B/C.";
            Debug.Log($"[Lead Mills Calibration Test] Candidate scale {scale:F5}; baseline RMS {baseline:F2}px; fit RMS {fitError:F2}px. Holdout pending.");
        }
        else
        {
            holdoutError = FloorProjectionCalibration.Error(first, third, floor, scale, out _);
            if (float.IsNaN(holdoutError) || float.IsInfinity(holdoutError) || holdoutError > 15)
            { Cancel($"Independent view error {holdoutError:F1}px: correction rejected; native projection restored."); return; }
            step = Step.Verified;
            message = $"Three-view check passed: zoom {scale:F3}, repeat {holdoutError:F1}px. Anchor A, then check movement.";
            Debug.Log($"[Lead Mills Calibration Test] Three-view accepted; scale {scale:F5}; fit RMS {fitError:F2}px; holdout RMS {holdoutError:F2}px. Physical placement acceptance still required.");
        }
    }
    float Scale => Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height) / 600f);
    Rect PanelRect() { var safe = Screen.safeArea; return new Rect(safe.x + 8 * Scale, Screen.height - safe.yMin - 310 * Scale, safe.width - 16 * Scale, 136 * Scale); }
    Rect StartRect() { var r = PanelRect(); return new Rect(r.x + 8 * Scale, r.y + 84 * Scale, (r.width - 24 * Scale) * 0.5f, 44 * Scale); }
    Rect AnchorRect() { var r = StartRect(); r.x += r.width + 8 * Scale; return r; }
    public bool ContainsScreenPoint(Vector2 point) => PanelRect().Contains(new Vector2(point.x, Screen.height - point.y));
    void OnGUI()
    {
        if (textStyle == null) textStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
        if (buttonStyle == null) buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18 };
        var previous = GUI.matrix; float s = Scale;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1));
        var r = PanelRect(); GUI.Box(new Rect(r.x/s,r.y/s,r.width/s,r.height/s), GUIContent.none);
        GUI.Label(new Rect(r.x/s+8,r.y/s+4,r.width/s-16,78), message, textStyle);
        r = StartRect(); GUI.Button(new Rect(r.x/s,r.y/s,r.width/s,r.height/s), step == Step.Idle ? "3-point calibration" : "Clear / restart", buttonStyle);
        GUI.enabled = step == Step.Verified;
        r = AnchorRect(); GUI.Button(new Rect(r.x/s,r.y/s,r.width/s,r.height/s), "Anchor at A", buttonStyle);
        GUI.enabled = true; GUI.matrix = previous;
        // All actions are dispatched once in Update; IMGUI is visual only.
    }
}
