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
    readonly Vector3[] initialPoints = new Vector3[3];
    readonly ARAnchor[] markerAnchors = new ARAnchor[3];
    readonly Material[] markerMaterials = new Material[3];
    readonly LineRenderer[] markerRings = new LineRenderer[3];
    readonly Color[] markerColors = { Color.red, Color.yellow, new Color(0.1f, 1f, 0.05f) };
    int initialPointCount;
    GUIStyle markerStyle;

    string message = "Three real floor landmarks; recheck from two other views.";
    GUIStyle textStyle, buttonStyle;
    TrialTrace trace;
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
        ClearMarkers();
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
        ClearMarkers(); initialPointCount = 0; points = null;
        step = Step.Idle; scale = 1; pointIndex = 0; plane = null;
        message = reason;
        ApplyProjection();
        if (hadCalibration)
        {
            if (trace != null) trace.Outcome("cleared: " + reason);
            trace = null;
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
        try { trace = TrialTrace.Begin(comparison.CurrentConfiguration); }
        catch (System.Exception e) { message = "Could not save test record. Restart after checking storage."; Debug.LogError(e); return; }
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
            trace?.Log("anchor-A", message);
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
        if (!FloorProjectionCalibration.FloorPoint(observation, floor, step == Step.Third ? scale : 1, out var floorPoint))
        { message = "Keep the real floor mark visible, within five meters."; return; }
        if (step == Step.Second || step == Step.Third)
        {
            var previous = step == Step.Second ? first : second;
            if (Vector3.ProjectOnPlane(observation.cameraPosition - MeanCamera(previous), floor.normal).magnitude < 0.25f)
            { message = "Move sideways at least 25 cm; then tap the same real A, B, C marks."; return; }
        }
        var target = step == Step.First ? first : step == Step.Second ? second : third;
        if (step == Step.First)
        {
            if (!SetMarker(pointIndex, floorPoint))
            { message = "Could not anchor the marker. Wait for plane tracking and retry."; return; }
            initialPoints[pointIndex] = floorPoint;
            initialPointCount = pointIndex + 1;
        }
        if (markerRings[pointIndex]) markerRings[pointIndex].widthMultiplier = 0.009f;
        target[pointIndex++] = observation;
        trace?.Log(step + " point " + pointIndex, "viewport " + observation.viewport + "; camera " + observation.cameraPosition.ToString("F5") + "; view " + observation.view.ToString("F5") + "; native projection " + observation.projection.ToString("F5"));
        Debug.Log($"[Lead Mills Calibration Test] {step} point {pointIndex}; t {Time.unscaledTime:F3}; screen {observation.viewport}; camera {observation.cameraPosition.ToString("F5")}.");
        if (pointIndex < 3) { message = $"{step} view: tap {(step == Step.First ? "a different" : "the same")} real {(pointIndex == 1 ? "B" : "C")} floor mark."; return; }
        pointIndex = 0;
        if (step == Step.First)
        {
            FloorProjectionCalibration.Error(first, first, floor, 1, out var initial);
            if (Vector3.Cross(initial[1] - initial[0], initial[2] - initial[0]).magnitude * 0.5f < 0.02f)
            { Cancel("Choose three marks spread in a triangle, not a line; start again."); return; }
            ResetMarkerWidths();
            step = Step.Second;
            message = "View 2: move 25 cm sideways and change downward tilt. Tap SAME real A/B/C.";
        }
        else if (step == Step.Second)
        {
            if (!FloorProjectionCalibration.Solve(first, second, floor, out scale, out fitError, out float baseline, out points))
            { Cancel("Fit is uncertain. Restart with clearer marks and a bigger viewpoint/tilt change."); return; }
            trace?.Log("candidate", $"scale {scale:F5}; baseline {baseline:F2}px; fit {fitError:F2}px");
            // Show the candidate's reconstructed locations, not repeat-touch screen coordinates.
            for (int i = 0; i < 3; i++)
                if (!SetMarker(i, points[i]))
                { Cancel("Candidate marker anchor unavailable. Rescan and retry."); return; }
            trace?.Log("candidate-markers", "A/B/C native plane anchors rebuilt at fitted world points; physical contact unverified");
            ResetMarkerWidths();
            step = Step.Third; ApplyProjection();
            message = $"Candidate zoom {scale:F3}; fit {fitError:F1}px. View 3: move/tilt again (25 cm); retap real A/B/C.";
            Debug.Log($"[Lead Mills Calibration Test] Candidate scale {scale:F5}; baseline RMS {baseline:F2}px; fit RMS {fitError:F2}px. Holdout pending.");
        }
        else
        {
            holdoutError = FloorProjectionCalibration.Error(first, third, floor, scale, out _);
            if (float.IsNaN(holdoutError) || float.IsInfinity(holdoutError) || holdoutError > 15)
            { Cancel($"Independent view error {holdoutError:F1}px: correction rejected; native projection restored."); return; }
            trace?.Log("holdout", $"RMS {holdoutError:F2}px");
            trace?.Outcome("three-view correspondence passed; physical stability unverified");
            step = Step.Verified;
            message = $"Three-view check passed: zoom {scale:F3}, repeat {holdoutError:F1}px. Anchor A, then check movement.";
            Debug.Log($"[Lead Mills Calibration Test] Three-view accepted; scale {scale:F5}; fit RMS {fitError:F2}px; holdout RMS {holdoutError:F2}px. Physical placement acceptance still required.");
        }
    }
    void ClearMarkers()
    {
        for (int i = 0; i < 3; i++)
        {
            if (markerAnchors[i]) Destroy(markerAnchors[i].gameObject);
            if (markerMaterials[i]) Destroy(markerMaterials[i]);
            markerMaterials[i] = null; markerAnchors[i] = null; markerRings[i] = null;
        }
    }
    bool SetMarker(int index, Vector3 point)
    {
        var anchor = placement.CreateCalibrationMarkerAnchor(plane, point);
        if (!anchor) return false;
        if (markerAnchors[index]) Destroy(markerAnchors[index].gameObject);
        if (markerMaterials[index]) Destroy(markerMaterials[index]);
        markerAnchors[index] = anchor;
        var ringObject = new GameObject("Calibration " + (char)('A' + index));
        ringObject.transform.SetParent(anchor.transform, false);
        var ring = ringObject.AddComponent<LineRenderer>();
        ring.useWorldSpace = false; ring.loop = true; ring.positionCount = 40;
        ring.widthMultiplier = 0.006f;
        // URP Unlit need not consume LineRenderer vertex colors; tint each owned material.
        var material = new Material(placement.CalibrationMarkerMaterial);
        material.SetColor("_BaseColor", markerColors[index]);
        material.color = markerColors[index];
        markerMaterials[index] = material; ring.sharedMaterial = material;
        ring.startColor = ring.endColor = Color.white;
        ring.shadowCastingMode = ShadowCastingMode.Off; ring.receiveShadows = false;
        var vertices = new Vector3[40];
        for (int i = 0; i < vertices.Length; i++)
        {
            float angle = i * Mathf.PI * 2 / vertices.Length;
            vertices[i] = new Vector3(Mathf.Cos(angle) * 0.025f, 0.004f, Mathf.Sin(angle) * 0.025f);
        }
        ring.SetPositions(vertices); markerRings[index] = ring;
        trace?.Log("marker-" + (char)('A' + index), "plane anchor world " + point.ToString("F5"));
        return true;
    }
    void ResetMarkerWidths()
    {
        foreach (var ring in markerRings) if (ring) ring.widthMultiplier = 0.006f;
    }
    float DownTilt(Vector3 forward) => Mathf.Asin(Mathf.Clamp(Vector3.Dot(forward.normalized, -floor.normal), -1, 1)) * Mathf.Rad2Deg;
    string MovementGuide()
    {
        if (step == Step.Idle) return "A red | B yellow | C bright green. Markers appear after each accepted tap.";
        if (step == Step.Verified) return "Check A against the real floor from a low angle, then walk around it. Clear if it slips.";
        string next = ((char)('A' + pointIndex)).ToString();
        if (step == Step.First) return "Tap real " + next + ". Spread A/B/C widely in a triangle; circles mark estimated floor positions.";
        var previous = step == Step.Second ? first : second;
        float moved = Vector3.ProjectOnPlane(arCamera.transform.position - MeanCamera(previous), floor.normal).magnitude;
        Vector3 forward = Vector3.zero;
        foreach (var observation in previous) forward += observation.view.inverse.MultiplyVector(Vector3.back);
        float referenceTilt = DownTilt(forward);
        float currentTilt = DownTilt(arCamera.transform.forward);
        float tiltChange = Mathf.Abs(currentTilt - referenceTilt);
        string move = moved < 0.25f ? $"<-- move sideways --> {moved * 100:F0} / 25 cm" : $"Movement ready: {moved * 100:F0} cm";
        string tilt = tiltChange < 15 ? (referenceTilt <= 50 ? "Tilt more down" : "Tilt more up") + $" (change {tiltChange:F0} / 15 deg)" : $"Tilt guide ready: change {tiltChange:F0} deg";
        Vector3 center = (initialPoints[0] + initialPoints[1] + initialPoints[2]) / 3;
        if (points != null) center = (points[0] + points[1] + points[2]) / 3;
        float distance = Vector3.ProjectOnPlane(arCamera.transform.position - center, floor.normal).magnitude;
        string range = distance < 0.6f ? $"Move away: floor distance {distance:F1} m (guide 0.6-2.5 m)" :
            distance > 2.5f ? $"Move closer: floor distance {distance:F1} m (guide 0.6-2.5 m)" : "Keep all REAL marks visible; tap " + next + ".";
        return move + "\n" + tilt + "\n" + range;
    }
    void DrawMarkerLabels()
    {
        if (markerStyle == null) markerStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(22 * Scale), fontStyle = FontStyle.Bold };
        for (int i = 0; i < initialPointCount; i++)
        {
            if (!markerAnchors[i]) continue;
            var screen = arCamera.WorldToScreenPoint(markerAnchors[i].transform.position);
            if (screen.z <= 0 || screen.x < 0 || screen.x > Screen.width || screen.y < 0 || screen.y > Screen.height) continue;
            var r = new Rect(screen.x - 18 * Scale, Screen.height - screen.y - 42 * Scale, 36 * Scale, 32 * Scale);
            GUI.Box(r, GUIContent.none);
            var oldColor = GUI.color; GUI.color = markerColors[i];
            GUI.Label(r, ((char)('A' + i)).ToString(), markerStyle); GUI.color = oldColor;
        }
    }
    float Scale => Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height) / 600f);
    Rect PanelRect() { var safe = Screen.safeArea; return new Rect(safe.x + 8 * Scale, Screen.height - safe.yMin - 392 * Scale, safe.width - 16 * Scale, 218 * Scale); }
    Rect StartRect() { var r = PanelRect(); return new Rect(r.x + 8 * Scale, r.y + 166 * Scale, (r.width - 24 * Scale) * 0.5f, 44 * Scale); }
    Rect AnchorRect() { var r = StartRect(); r.x += r.width + 8 * Scale; return r; }
    public bool ContainsScreenPoint(Vector2 point) => PanelRect().Contains(new Vector2(point.x, Screen.height - point.y));
    void OnGUI()
    {
        if (textStyle == null) textStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
        if (buttonStyle == null) buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18 };
        DrawMarkerLabels();
        var previous = GUI.matrix; float s = Scale;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1));
        var r = PanelRect(); GUI.Box(new Rect(r.x/s,r.y/s,r.width/s,r.height/s), GUIContent.none);
        GUI.Label(new Rect(r.x/s+8,r.y/s+4,r.width/s-16,22), TrialTrace.DisplayId, textStyle);
        GUI.Label(new Rect(r.x/s+8,r.y/s+26,r.width/s-16,56), message, textStyle);
        GUI.Label(new Rect(r.x/s+8,r.y/s+84,r.width/s-16,78), MovementGuide(), textStyle);
        r = StartRect(); GUI.Button(new Rect(r.x/s,r.y/s,r.width/s,r.height/s), step == Step.Idle ? "3-point calibration" : "Clear / restart", buttonStyle);
        GUI.enabled = step == Step.Verified;
        r = AnchorRect(); GUI.Button(new Rect(r.x/s,r.y/s,r.width/s,r.height/s), "Anchor at A", buttonStyle);
        GUI.enabled = true; GUI.matrix = previous;
        // All actions are dispatched once in Update; IMGUI is visual only.
    }
}
