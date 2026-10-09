using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[DefaultExecutionOrder(-50)]
public sealed class ThreePointFloorCalibration : MonoBehaviour
{
    enum Step { Idle, First, ReviewFirst, Second, ReviewSecond, Third, ReviewThird, Verified }
    struct Tap { public Step view; public int index; public FloorProjectionCalibration.Observation observation; }
    Step step;
    readonly List<Tap> history = new List<Tap>();
    int landmarkCount = 1, pointIndex, revision;
    HorizontalPlanePlacement placement;
    CameraRegistrationComparison comparison;
    ARCameraManager manager;
    Camera arCamera;
    ARPlane plane;
    Plane floor;
    Vector3 planeStart;
    Matrix4x4 nativeProjection;
    bool hasProjection, repeatRetry, holdoutRejected;
    float frameTime, scale = 1, fitError, holdoutError;
    float movementTarget = 0.25f, tiltTarget = 15;
    readonly FloorProjectionCalibration.Observation[] first = new FloorProjectionCalibration.Observation[3];
    readonly FloorProjectionCalibration.Observation[] second = new FloorProjectionCalibration.Observation[3];
    readonly FloorProjectionCalibration.Observation[] third = new FloorProjectionCalibration.Observation[3];
    Vector3[] points;
    readonly Vector3[] initialPoints = new Vector3[3];
    readonly ARAnchor[] markerAnchors = new ARAnchor[9];
    readonly Material[] markerMaterials = new Material[9];
    readonly LineRenderer[] markerRings = new LineRenderer[9];
    readonly Color[] markerColors = { Color.red, Color.yellow, new Color(0.1f, 1f, 0.05f), new Color(0.75f, 0.15f, 1f), new Color(1f, 0.45f, 0f), new Color(0f, 0.65f, 1f), Color.white, new Color(1f, 0.1f, 0.55f), new Color(0f, 1f, 0.85f) };
    GUIStyle markerStyle, textStyle, buttonStyle;
    string message = "Choose 1, 2 or 3 real floor details. Start with one.";
    TrialTrace trace;
    bool Capturing => step == Step.First || step == Step.Second || step == Step.Third;
    bool Reviewing => step == Step.ReviewFirst || step == Step.ReviewSecond || step == Step.ReviewThird;
    bool Preview => step == Step.Third || (step == Step.ReviewThird && !holdoutRejected) || step == Step.Verified;
    public bool CapturesPlacement => step != Step.Idle && step != Step.Verified;
    public bool ConsumedTouchThisFrame { get; private set; }
    int ViewNumber => step == Step.First || step == Step.ReviewFirst ? 1 : step == Step.Second || step == Step.ReviewSecond ? 2 : 3;
    FloorProjectionCalibration.Observation[] Selected(FloorProjectionCalibration.Observation[] source)
    {
        var result = new FloorProjectionCalibration.Observation[landmarkCount];
        System.Array.Copy(source, result, landmarkCount); return result;
    }
    public void Configure(HorizontalPlanePlacement owner, CameraRegistrationComparison control, Camera camera)
    {
        placement = owner; comparison = control; arCamera = camera;
        manager = camera.GetComponent<ARCameraManager>();
        if (manager) manager.frameReceived += OnFrame;
        Application.onBeforeRender += ApplyProjection;
        RenderPipelineManager.beginCameraRendering += OnRendering;
        Debug.Log("[Lead Mills Calibration Test] Ready: 1/2/3 real landmarks, Undo, review/Continue, independent third view with separate 3A/3B/3C markers and native anchor at A.");
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
        nativeProjection = frame.projectionMatrix.Value; hasProjection = true; frameTime = Time.unscaledTime;
        ApplyProjection();
    }
    void OnRendering(ScriptableRenderContext context, Camera camera) { if (camera == arCamera) ApplyProjection(); }
    [BeforeRenderOrder(200)]
    public void ApplyProjection()
    {
        if (arCamera && hasProjection)
            arCamera.projectionMatrix = Preview ? FloorProjectionCalibration.Scaled(nativeProjection, scale) : nativeProjection;
    }
    public void Cancel(string reason)
    {
        bool active = step != Step.Idle;
        ClearMarkers(); history.Clear(); points = null; repeatRetry = holdoutRejected = false;
        movementTarget = 0.25f; tiltTarget = 15; step = Step.Idle; scale = 1; pointIndex = 0; plane = null;
        message = reason; ApplyProjection();
        if (active)
        {
            trace?.Outcome("cleared: " + reason); trace = null;
            if (placement) placement.RestartSurfaceCheck();
            Debug.Log("[Lead Mills Calibration Test] Cleared: " + reason);
        }
    }
    void Begin()
    {
        Cancel("Starting a new trial.");
        if (!comparison.PortraitCorrectionActive || !Steady())
        { message = "Wait for tracking and select CW90/Portrait first."; return; }
        placement.RestartSurfaceCheck(); revision = comparison.RegistrationRevision;
        try { trace = TrialTrace.Begin(comparison.CurrentConfiguration + "; real landmark count " + landmarkCount); }
        catch (System.Exception e) { message = "Could not save test record. Check storage and retry."; Debug.LogError(e); return; }
        step = Step.First; message = $"View 1: tap {landmarkCount} REAL floor detail(s). Circles show your taps; Undo replaces the last point.";
    }
    bool Steady() => hasProjection && Time.unscaledTime - frameTime < 0.25f &&
        ARSession.state == ARSessionState.SessionTracking && ARSession.notTrackingReason == NotTrackingReason.None;
    Vector3 MeanCamera(FloorProjectionCalibration.Observation[] observations)
    {
        var mean = Vector3.zero;
        for (int i = 0; i < landmarkCount; i++) mean += observations[i].cameraPosition;
        return mean / landmarkCount;
    }
    void Update()
    {
        ConsumedTouchThisFrame = false;
        if (!placement || !comparison) return;
        if (step != Step.Idle && (revision != comparison.RegistrationRevision || !Steady() ||
            (plane && (plane.trackingState != TrackingState.Tracking || plane.subsumedBy != null ||
            Vector3.Distance(plane.transform.position, planeStart) > 0.02f))))
        { Cancel("Tracking/plane/mode changed: restart this trial."); return; }
        var touch = Touchscreen.current;
        bool pressed = touch != null && touch.primaryTouch.press.wasPressedThisFrame;
        Vector2 position = pressed ? touch.primaryTouch.position.ReadValue() : Vector2.zero;
        if (!pressed && !Application.isMobilePlatform && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        { pressed = true; position = Mouse.current.position.ReadValue(); }
        if (!pressed) return;
        var gui = new Vector2(position.x, Screen.height - position.y);
        for (int i = 1; i <= 3; i++)
            if (CountRect(i).Contains(gui))
            {
                ConsumedTouchThisFrame = true;
                if (i != landmarkCount) { Cancel("Point count changed. Press Start for a new trial."); landmarkCount = i; }
                return;
            }
        if (StartRect().Contains(gui)) { ConsumedTouchThisFrame = true; if (step == Step.Idle) Begin(); else Cancel("Trial cleared; native projection restored."); return; }
        if (UndoRect().Contains(gui)) { ConsumedTouchThisFrame = true; Undo(); return; }
        if (NextRect().Contains(gui)) { ConsumedTouchThisFrame = true; if (Reviewing && !holdoutRejected) Continue(); return; }
        if (AnchorRect().Contains(gui))
        {
            ConsumedTouchThisFrame = true;
            if (step == Step.Verified && points != null && plane)
                message = placement.PlaceCalibrationAnchor(plane, points[0]) ? "A anchored. Check real-floor contact and movement." : "A lacks current surface support. Rescan or retry.";
            trace?.Log("anchor-A", message); return;
        }
        if (!Capturing || PanelRect().Contains(gui) || comparison.ContainsScreenPoint(position) || placement.ContainsHudScreenPoint(position)) return;
        ConsumedTouchThisFrame = true; Capture(position);
    }
    void Capture(Vector2 screen)
    {
        comparison.PrepareViewForRaycast(); ApplyProjection();
        if (step == Step.First && pointIndex == 0)
        {
            if (!placement.TryCalibrationPlane(screen, out plane)) { message = "Tap a qualified blue floor surface for A."; return; }
            planeStart = plane.transform.position;
            floor = new Plane(plane.transform.up, plane.transform.TransformPoint(new Vector3(plane.center.x, 0, plane.center.y)));
            trace?.Log("reference-floor", "normal " + floor.normal.ToString("F6") + "; distance " + floor.distance.ToString("F6"));
        }
        var observation = new FloorProjectionCalibration.Observation {
            view = arCamera.worldToCameraMatrix, projection = nativeProjection,
            viewport = new Vector2(screen.x / Screen.width, screen.y / Screen.height),
            pixels = new Vector2(Screen.width, Screen.height), cameraPosition = arCamera.transform.position
        };
        if (!FloorProjectionCalibration.FloorPoint(observation, floor, Preview ? scale : 1, out var floorPoint))
        { message = "Keep the REAL floor detail visible, within five meters."; return; }
        if (step == Step.Second || step == Step.Third)
        {
            var previous = step == Step.Second ? first : second;
            if (Vector3.ProjectOnPlane(observation.cameraPosition - MeanCamera(previous), floor.normal).magnitude < movementTarget)
            { message = $"Move sideways {movementTarget * 100:F0} cm; retap the SAME physical detail(s), not the circles."; return; }
        }
        if (step == Step.First)
        {
            if (landmarkCount == 2 && pointIndex == 1 && Vector3.Distance(initialPoints[0], floorPoint) < 0.25f)
            { message = "B is too close to A. Choose a real detail at least 25 cm away; A saved."; return; }
            if (landmarkCount == 3 && pointIndex == 2 && Vector3.Cross(initialPoints[1] - initialPoints[0], floorPoint - initialPoints[0]).magnitude * 0.5f < 0.02f)
            { message = "C too close to A-B line. Tap farther away; A/B saved."; return; }
            if (!SetMarker(pointIndex, floorPoint)) { message = "Marker anchor unavailable. Wait for tracking and retry."; return; }
            initialPoints[pointIndex] = floorPoint;
        }
        if (step == Step.Second)
        {
            if (repeatRetry && pointIndex == 0)
            { ClearRepeatMarkers(); history.RemoveAll(t => t.view != Step.First); repeatRetry = false; }
            if (!SetMarker(3 + pointIndex, floorPoint)) { message = "View 2 marker unavailable. Wait and retry."; return; }
        }
        if (step == Step.Third && !SetMarker(6 + pointIndex, floorPoint))
        { message = "Test 3 marker unavailable. Wait for tracking and retry."; return; }
        var target = step == Step.First ? first : step == Step.Second ? second : third;
        target[pointIndex] = observation;
        history.Add(new Tap { view = step, index = pointIndex, observation = observation });
        int markerIndex = (step == Step.Third ? 6 : step == Step.Second ? 3 : 0) + pointIndex;
        if (markerRings[markerIndex]) markerRings[markerIndex].widthMultiplier = 0.009f;
        pointIndex++;
        trace?.Log(step + " point " + pointIndex, "viewport " + observation.viewport.ToString("F6") + "; pixels " + observation.pixels.ToString("F0") + "; camera " + observation.cameraPosition.ToString("F5") + "; view " + observation.view.ToString("F5") + "; native projection " + observation.projection.ToString("F5"));
        if (pointIndex < landmarkCount)
            message = $"View {ViewNumber} {(char)('A' + pointIndex - 1)} accepted. Tap REAL {(char)('A' + pointIndex)}; Undo corrects the last point.";
        else
        {
            int view = ViewNumber;
            step = step == Step.First ? Step.ReviewFirst : step == Step.Second ? Step.ReviewSecond : Step.ReviewThird;
            message = $"View {view}: {landmarkCount} tap(s) saved. Inspect circles. Undo replaces the last point; Continue accepts this view.";
        }
    }
    void Continue()
    {
        trace?.Log("continue-view", ViewNumber.ToString());
        if (step == Step.ReviewFirst)
        {
            ResetMarkerWidths(); step = Step.Second; pointIndex = 0;
            message = "View 2: move/tilt, then tap SAME REAL floor detail(s). Do not tap virtual circles.";
        }
        else if (step == Step.ReviewSecond)
        {
            if (!FloorProjectionCalibration.Solve(Selected(first), Selected(second), floor, out scale, out fitError, out float baseline, out points, out var report))
            {
                trace?.Log("fit-rejected", report.ToString());
                Debug.Log("[Lead Mills Calibration Test] Fit rejected: " + report);
                message = RejectionGuide(report); step = Step.Second; pointIndex = 0;
                repeatRetry = true; scale = 1; points = null; ApplyProjection();
                movementTarget = report.rejection == FloorProjectionCalibration.Rejection.WeakView ? 0.4f : 0.25f;
                tiltTarget = report.rejection == FloorProjectionCalibration.Rejection.WeakView ? 25 : 15;
                return;
            }
            trace?.Log("fit-report", report.ToString());
            trace?.Log("candidate", $"count {landmarkCount}; scale {scale:F5}; baseline {baseline:F2}px; fit {fitError:F2}px");
            if (!RebuildMarkers(true)) { Cancel("Candidate marker unavailable. Rescan and retry."); return; }
            ResetMarkerWidths(); movementTarget = 0.25f; tiltTarget = 15; repeatRetry = false;
            step = Step.Third; pointIndex = 0; ApplyProjection();
            message = $"Candidate zoom {scale:F3}; fit {fitError:F1}px. View 3: retap SAME REAL detail(s) from another position.";
        }
        else if (step == Step.ReviewThird)
        {
            holdoutError = FloorProjectionCalibration.Error(Selected(first), Selected(third), floor, scale, out _);
            if (float.IsNaN(holdoutError) || float.IsInfinity(holdoutError) || holdoutError > 15)
            {
                holdoutRejected = true; ApplyProjection();
                message = $"Independent view error {holdoutError:F1}px: native projection restored. Undo to replace the last tap, or Clear.";
                trace?.Log("holdout-rejected", message); return;
            }
            trace?.Log("holdout", $"count {landmarkCount}; RMS {holdoutError:F2}px");
            trace?.Outcome("three-view correspondence passed; landmark count " + landmarkCount + "; physical stability unverified");
            step = Step.Verified; ApplyProjection();
            message = $"{landmarkCount}-point check passed; zoom {scale:F3}, repeat {holdoutError:F1}px. Anchor A and check physical contact.";
        }
    }
    void Undo()
    {
        if (history.Count == 0) { message = "No accepted point to undo yet."; return; }
        var tap = history[history.Count - 1]; history.RemoveAt(history.Count - 1);
        step = tap.view; pointIndex = tap.index; repeatRetry = holdoutRejected = false;
        if (step != Step.Third) { scale = 1; points = null; }
        System.Array.Clear(first, 0, 3); System.Array.Clear(second, 0, 3); System.Array.Clear(third, 0, 3);
        foreach (var saved in history)
        {
            var target = saved.view == Step.First ? first : saved.view == Step.Second ? second : third;
            target[saved.index] = saved.observation;
        }
        if (history.Count == 0) plane = null;
        if (placement) placement.RestartSurfaceCheck();
        ApplyProjection();
        if (!RebuildMarkers(step == Step.Third)) { Cancel("Marker rebuild unavailable after Undo. Rescan."); return; }
        trace?.Outcome("in-progress after undo");
        trace?.Log("undo", $"view {ViewNumber}; replace {(char)('A' + pointIndex)}");
        message = $"Undid View {ViewNumber} {(char)('A' + pointIndex)}. Tap the intended REAL floor detail again; earlier points saved.";
    }
    bool RebuildMarkers(bool candidate)
    {
        ClearMarkers();
        foreach (var tap in history)
        {
            float zoom = candidate ? scale : 1;
            if (!FloorProjectionCalibration.FloorPoint(tap.observation, floor, zoom, out var point)) return false;
            if (tap.view == Step.First) initialPoints[tap.index] = point;
            int marker = (tap.view == Step.Third ? 6 : tap.view == Step.Second ? 3 : 0) + tap.index;
            if (!SetMarker(marker, point)) return false;
        }
        return true;
    }
    void ClearMarkers()
    {
        for (int i = 0; i < markerAnchors.Length; i++)
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
        var ringObject = new GameObject("Calibration View " + (index / 3 + 1) + " " + (char)('A' + index % 3));
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
            float radius = 0.025f + (index / 3) * 0.01f;
            vertices[i] = new Vector3(Mathf.Cos(angle) * radius, 0.004f + (index / 3) * 0.002f, Mathf.Sin(angle) * radius);
        }
        ring.SetPositions(vertices); markerRings[index] = ring;
        trace?.Log("marker-view" + (index / 3 + 1) + "-" + (char)('A' + index % 3), "plane anchor world " + point.ToString("F5"));
        return true;
    }
    void ClearRepeatMarkers()
    {
        for (int i = 3; i < markerAnchors.Length; i++)
        {
            if (markerAnchors[i]) Destroy(markerAnchors[i].gameObject);
            if (markerMaterials[i]) Destroy(markerMaterials[i]);
            markerAnchors[i] = null; markerMaterials[i] = null; markerRings[i] = null;
        }
    }
    string RejectionGuide(FloorProjectionCalibration.FitReport report)
    {
        switch (report.rejection)
        {
            case FloorProjectionCalibration.Rejection.WeakView:
                return "View 2 needs more perspective. Move 40 cm, change down-tilt 25 deg, then retry SAME A/B/C. View 1 saved.";
            case FloorProjectionCalibration.Rejection.Residual:
                return $"View 2 disagrees: RMS {report.rms:F0}px (limit 12); {(char)('A' + report.WorstPoint)} largest ({report.WorstError:F0}px). Retry SAME real A/B/C. Careful repeats still fail? Floor/tracking needs checking.";
            case FloorProjectionCalibration.Rejection.ScaleBoundary:
                return "Fit needs zoom outside the tested range. Retry SAME real marks. If repeated, floor/tracking model needs checking; no correction applied.";
            case FloorProjectionCalibration.Rejection.SmallTriangle:
                return "Fitted landmarks too close. Clear; spread A/B at least 25 cm, or A/B/C in a wide triangle.";
            case FloorProjectionCalibration.Rejection.NoImprovement:
                return "Zoom does not explain the mismatch. Retry real A/B/C; repeated failures need floor/tracking diagnosis.";
            default:
                return "Floor rays invalid. Keep marks in view, move away from the floor edge, and retry A/B/C.";
        }
    }
    void ResetMarkerWidths()
    {
        foreach (var ring in markerRings) if (ring) ring.widthMultiplier = 0.006f;
    }
    float DownTilt(Vector3 forward) => Mathf.Asin(Mathf.Clamp(Vector3.Dot(forward.normalized, -floor.normal), -1, 1)) * Mathf.Rad2Deg;
    string MovementGuide()
    {
        if (step == Step.Idle) return "Tests 1/2/3 each confirm A/B/C. Test 3: white/hot pink/aqua. Retap the SAME real floor details.";
        if (Reviewing) return "Check each new circle against its PHYSICAL target and earlier labeled circles (1A/2A/3A, etc.). Undo to replace; Continue confirms. Never retap virtual circles.";
        if (step == Step.Verified) return "Check A against the real floor from a low angle, then walk around it. Clear if it slips.";
        string next = ((char)('A' + pointIndex)).ToString();
        if (step == Step.First) return "Tap physical " + next + ". " + (landmarkCount == 1 ? "Use one distinct floor detail." : landmarkCount == 2 ? "Spread A/B at least 25 cm apart." : "Spread A/B/C in a wide triangle.") + " Circles are references, not targets.";
        var previous = step == Step.Second ? first : second;
        float moved = Vector3.ProjectOnPlane(arCamera.transform.position - MeanCamera(previous), floor.normal).magnitude;
        Vector3 forward = Vector3.zero;
        for (int i = 0; i < landmarkCount; i++) forward += previous[i].view.inverse.MultiplyVector(Vector3.back);
        float referenceTilt = DownTilt(forward);
        float currentTilt = DownTilt(arCamera.transform.forward);
        float tiltChange = Mathf.Abs(currentTilt - referenceTilt);
        string move = moved < movementTarget ? $"<-- move sideways --> {moved * 100:F0} / {movementTarget * 100:F0} cm" : $"Movement ready: {moved * 100:F0} cm";
        string tilt = tiltChange < tiltTarget ? (referenceTilt <= 45 ? "Tilt more down" : "Tilt more up") + $" (change {tiltChange:F0} / {tiltTarget:F0} deg)" : $"Tilt guide ready: change {tiltChange:F0} deg";
        Vector3 center = Vector3.zero;
        for (int i = 0; i < landmarkCount; i++) center += points != null ? points[i] : initialPoints[i];
        center /= landmarkCount;
        float distance = Vector3.ProjectOnPlane(arCamera.transform.position - center, floor.normal).magnitude;
        string range = distance < 0.6f ? $"Move away: floor distance {distance:F1} m (guide 0.6-2.5 m)" :
            distance > 2.5f ? $"Move closer: floor distance {distance:F1} m (guide 0.6-2.5 m)" : "Tap SAME physical " + next + ", NOT colored circles.";
        return move + "\n" + tilt + "\n" + range;
    }
    void DrawMarkerLabels()
    {
        if (markerStyle == null) markerStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(22 * Scale), fontStyle = FontStyle.Bold };
        for (int i = 0; i < markerAnchors.Length; i++)
        {
            if (!markerAnchors[i]) continue;
            var screen = arCamera.WorldToScreenPoint(markerAnchors[i].transform.position);
            if (screen.z <= 0 || screen.x < 0 || screen.x > Screen.width || screen.y < 0 || screen.y > Screen.height) continue;
            int view = i / 3;
            // Separate label positions when all three observations agree at one physical target.
            float xOffset = view == 0 ? -44 : view == 1 ? 0 : 28;
            float yOffset = view == 1 ? 16 : -44;
            var r = new Rect(screen.x + xOffset * Scale, Screen.height - screen.y + yOffset * Scale, 40 * Scale, 32 * Scale);
            GUI.Box(r, GUIContent.none);
            var oldColor = GUI.color; GUI.color = markerColors[i];
            GUI.Label(r, (i / 3 + 1).ToString() + (char)('A' + i % 3), markerStyle); GUI.color = oldColor;
        }
    }
    float Scale => Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height) / 600f);
    Rect PanelRect() { var safe = Screen.safeArea; return new Rect(safe.x + 8 * Scale, Screen.height - safe.yMin - 478 * Scale, safe.width - 16 * Scale, 304 * Scale); }
    Rect CountRect(int count) { var r = PanelRect(); return new Rect(r.x + (76 + (count - 1) * 108) * Scale, r.y + 30 * Scale, 100 * Scale, 42 * Scale); }
    Rect ActionRect(int index) { var r = PanelRect(); float width = (r.width - 40 * Scale) / 4; return new Rect(r.x + 8 * Scale + index * (width + 8 * Scale), r.y + 252 * Scale, width, 44 * Scale); }
    Rect StartRect() => ActionRect(0);
    Rect UndoRect() => ActionRect(1);
    Rect NextRect() => ActionRect(2);
    Rect AnchorRect() => ActionRect(3);
    public bool ContainsScreenPoint(Vector2 point) => PanelRect().Contains(new Vector2(point.x, Screen.height - point.y));
    void Button(Rect r, string title) => GUI.Button(new Rect(r.x / Scale, r.y / Scale, r.width / Scale, r.height / Scale), title, buttonStyle);
    void OnGUI()
    {
        if (textStyle == null) textStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
        if (buttonStyle == null) buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16 };
        DrawMarkerLabels();
        var previous = GUI.matrix; float s = Scale;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1));
        var r = PanelRect(); GUI.Box(new Rect(r.x/s,r.y/s,r.width/s,r.height/s), GUIContent.none);
        GUI.Label(new Rect(r.x/s+8,r.y/s+4,r.width/s-16,22), TrialTrace.DisplayId, textStyle);
        GUI.Label(new Rect(r.x/s+8,r.y/s+38,64,24), "Points:", textStyle);
        var oldBackground = GUI.backgroundColor;
        for (int i = 1; i <= 3; i++)
        {
            GUI.backgroundColor = i == landmarkCount ? new Color(0.35f, 0.8f, 1f) : oldBackground;
            Button(CountRect(i), i + (i == 1 ? " point" : " points"));
        }
        GUI.backgroundColor = oldBackground;
        GUI.Label(new Rect(r.x/s+8,r.y/s+80,r.width/s-16,76), message, textStyle);
        GUI.Label(new Rect(r.x/s+8,r.y/s+158,r.width/s-16,88), MovementGuide(), textStyle);
        Button(StartRect(), step == Step.Idle ? "Start" : "Clear");
        GUI.enabled = history.Count > 0; Button(UndoRect(), "Undo last");
        GUI.enabled = Reviewing && !holdoutRejected; Button(NextRect(), "Continue");
        GUI.enabled = step == Step.Verified; Button(AnchorRect(), "Anchor A");
        GUI.enabled = true; GUI.matrix = previous;
        // All actions dispatch once in Update; IMGUI is visual only.
    }
}
