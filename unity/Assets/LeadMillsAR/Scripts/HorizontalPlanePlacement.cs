using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// One 20 cm cube attached to a tracked plane anchor. No persistence/geospatial anchor.
public sealed class HorizontalPlanePlacement : MonoBehaviour
{
    [SerializeField] ARPlaneManager planeManager;
    [SerializeField] ARRaycastManager raycastManager;
    [SerializeField] ARAnchorManager anchorManager;
    [SerializeField] LiDARMeshPreview meshPreview;
    [SerializeField] XROrigin origin;
    [SerializeField] GameObject cubePrefab;
    [SerializeField] float assetPivotHeight = 0.10f;
    public void SetPlacementAsset(GameObject prefab, float pivotHeight) { cubePrefab = prefab; assetPivotHeight = pivotHeight; }
    readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    GameObject placedCube;
    public bool HasPlacedCube => placedCube != null;
    public bool CalibrationAnchorTracking => placedAnchor && placedAnchor.trackingState == TrackingState.Tracking;
    public string CalibrationAnchorState => placedAnchor ? placedAnchor.trackingState.ToString() : "Missing";
    ARAnchor placedAnchor;
    public bool GuidedMode { get; private set; } = true;
    bool automaticValid, targetReady;
    float automaticScale = 1, automaticSince, targetSince, nextTargetCheck, guidedPlacedAt;
    ARPlane targetPlane;
    Vector3 targetPoint, previousTarget;
    LineRenderer targetOutline;
    Material targetMaterial;
    TrialTrace guidedTrace;
    string guidedMessage = "Point at the floor where you want the machine. Move slowly sideways.";
    float nextPoseLog;
    Vector3 anchorStartLocalPosition;
    Quaternion anchorStartLocalRotation;
    string feedback = "Move slowly over a textured table or floor.";
    GUIStyle labelStyle;
    GUIStyle buttonStyle;
    int trackedPlanes;
    ARCameraManager cameraManager;
    CameraRegistrationComparison comparison;
    ThreePointFloorCalibration calibration;
    Matrix4x4 frameProjection, frameDisplay;
    bool hasFrameDisplay;
    long frameTimestampNs;
    bool hasFrameProjection;
    float lastCameraFrameTime;
    float nextAlignmentLog;
    sealed class SurfaceReadiness
    {
        public Vector3 position, normal, cameraStart;
        public Vector2 size;
        public float since;
        public int cells;
        public bool ready, moved;
    }
    readonly Dictionary<TrackableId, SurfaceReadiness> surfaces = new Dictionary<TrackableId, SurfaceReadiness>();
    readonly HashSet<TrackableId> livePlanes = new HashSet<TrackableId>();
    float nextSurfaceCheck;
    int readyPlanes;
    string scanStatus = "Scan slowly over a textured surface.";

    void CheckSurfaces()
    {
        bool tracking = ARSession.state == ARSessionState.SessionTracking &&
            ARSession.notTrackingReason == NotTrackingReason.None && hasFrameProjection &&
            Time.unscaledTime - lastCameraFrameTime < 0.5f;
        livePlanes.Clear();
        readyPlanes = 0;
        scanStatus = tracking ? "Scan a level area at least 40 cm across." : "Waiting for steady camera tracking.";
        foreach (var plane in planeManager.trackables)
        {
            livePlanes.Add(plane.trackableId);
            if (!surfaces.TryGetValue(plane.trackableId, out var state))
                surfaces[plane.trackableId] = state = new SurfaceReadiness();
            bool eligible = tracking && plane.trackingState == TrackingState.Tracking &&
                plane.subsumedBy == null && plane.alignment == PlaneAlignment.HorizontalUp &&
                Vector3.Dot(plane.transform.up, Vector3.up) >= 0.985f &&
                plane.size.x >= 0.4f && plane.size.y >= 0.4f;
            state.cells = eligible ? meshPreview.SupportedCells(plane,
                plane.center, 0.3f) : 0;
            state.ready = false;
            if (!eligible || state.cells < 8)
            {
                state.since = 0;
                if (eligible) scanStatus = $"Collecting LiDAR coverage: {state.cells}/8 cells.";
                continue;
            }
            if (state.since == 0 || Vector3.Distance(state.position, plane.transform.position) > 0.03f ||
                Vector3.Angle(state.normal, plane.transform.up) > 3f || Vector2.Distance(state.size, plane.size) > 0.10f)
            {
                state.since = Time.unscaledTime;
                state.position = plane.transform.position;
                state.normal = plane.transform.up;
                state.size = plane.size;
                state.cameraStart = origin.Camera.transform.position;
                state.moved = false;
            }
            float elapsed = Time.unscaledTime - state.since;
            state.moved |= Vector3.Distance(state.cameraStart, origin.Camera.transform.position) >= 0.15f;
            state.ready = elapsed >= 3f && state.moved;
            if (state.ready) readyPlanes++;
            else scanStatus = elapsed < 3f ? $"Checking surface stability: {Mathf.Min(elapsed, 3f):F1}/3 s." :
                "Move 15 cm sideways slowly to check another view.";
        }
        foreach (var id in new List<TrackableId>(surfaces.Keys))
            if (!livePlanes.Contains(id)) surfaces.Remove(id);
        if (readyPlanes > 0) scanStatus = "Blue surface ready. Check it matches the room, then tap.";
    }

    public void Configure(ARPlaneManager planes, ARRaycastManager raycasts,
        ARAnchorManager anchors, LiDARMeshPreview preview, XROrigin xrOrigin, GameObject cube)
    {
        planeManager = planes;
        raycastManager = raycasts;
        anchorManager = anchors;
        meshPreview = preview;
        origin = xrOrigin;
        cubePrefab = cube;
        assetPivotHeight = .10f; // Original setup selects the center-pivot 20 cm cube.
    }

    void Start()
    {
        if (!planeManager || !raycastManager || !anchorManager || !meshPreview || !origin || !cubePrefab)
        {
            Debug.LogError("[Lead Mills Placement] Missing setup reference.");
            enabled = false;
            return;
        }
        Debug.Log("[Lead Mills Asset] Active prefab: " + cubePrefab.name + "; floor pivot offset " + assetPivotHeight.ToString("F3") + " m.");
        comparison = origin.Camera.GetComponent<CameraRegistrationComparison>();
        if (!comparison) comparison = origin.Camera.gameObject.AddComponent<CameraRegistrationComparison>();
        comparison.Configure(this);
        cameraManager = origin.Camera.GetComponent<ARCameraManager>();
        if (cameraManager) cameraManager.frameReceived += OnCameraFrame;
        calibration = gameObject.AddComponent<ThreePointFloorCalibration>();
        calibration.Configure(this, comparison, origin.Camera);
        comparison.SetFloorCalibration(calibration);
        Debug.Log("[Lead Mills Guided] Ready: live intrinsics/display focal derivation, center target, green outline -> Place; Advanced retains manual diagnostics.");
        RenderPipelineManager.beginCameraRendering += OnCameraRendering;
        Debug.Log("[Lead Mills Placement] LiDAR readiness gate: level within 10 deg, 40 cm extent, 8 supported 10 cm cells, 3 s stability, 15 cm viewpoint change.");
    }

    void OnCameraFrame(ARCameraFrameEventArgs frame)
    {
        lastCameraFrameTime = Time.unscaledTime;
        frameTimestampNs = frame.timestampNs ?? -1;
        hasFrameDisplay = frame.displayMatrix.HasValue;
        if (hasFrameDisplay) frameDisplay = frame.displayMatrix.Value;
        if (frame.projectionMatrix.HasValue)
        {
            frameProjection = frame.projectionMatrix.Value;
            hasFrameProjection = true;
        }
        bool valid = hasFrameDisplay && hasFrameProjection && comparison && comparison.PortraitCorrectionActive &&
            cameraManager.TryGetIntrinsics(out var intrinsics) && AutomaticCameraAlignment.TryScale(frameProjection, frameDisplay, intrinsics, out _);
        if (valid)
        {
            // Recompute on each frame. Stable metadata is readiness, not proof of physical accuracy.
            cameraManager.TryGetIntrinsics(out var current);
            AutomaticCameraAlignment.TryScale(frameProjection, frameDisplay, current, out float value);
            if (!automaticValid || Mathf.Abs(value-automaticScale) > .006f) automaticSince = Time.unscaledTime;
            automaticScale = value;
        }
        else automaticSince = Time.unscaledTime;
        automaticValid = valid;
        ApplyAutomaticProjection();
    }

    public void ApplyAutomaticProjection()
    {
        if (GuidedMode && hasFrameProjection && origin && origin.Camera)
            origin.Camera.projectionMatrix = automaticValid ? FloorProjectionCalibration.Scaled(frameProjection, automaticScale) : frameProjection;
    }

    void OnCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (origin && camera == origin.Camera) ApplyAutomaticProjection();
        if (!enabled || !origin || camera != origin.Camera || Time.unscaledTime < nextAlignmentLog) return;
        nextAlignmentLog = Time.unscaledTime + 2f;
        var device = InputSystem.GetDevice<HandheldARInputDevice>();
        string poseComparison = device == null ? "AR input missing" :
            $"position delta {Vector3.Distance(camera.transform.localPosition, device.devicePosition.ReadValue()):F4} m; " +
            $"rotation delta {Quaternion.Angle(camera.transform.localRotation, device.deviceRotation.ReadValue()):F2} deg";
        float projectionDelta = 0f;
        if (hasFrameProjection)
            for (int i = 0; i < 16; i++)
                projectionDelta = Mathf.Max(projectionDelta, Mathf.Abs(camera.projectionMatrix[i] - frameProjection[i]));
        var xrDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.CenterEye);
        string xrComparison = xrDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.centerEyeRotation, out var xrRotation)
            ? $"XR center-eye rotation difference {Quaternion.Angle(camera.transform.localRotation, xrRotation):F2} deg"
            : "XR center-eye rotation unavailable";
        string planeComparison = "no tracked plane";
        foreach (var plane in planeManager.trackables)
            if (plane.alignment == PlaneAlignment.HorizontalUp && plane.trackingState == TrackingState.Tracking)
            {
                planeComparison = $"plane up {plane.transform.up}; up dot {Vector3.Dot(plane.transform.up, Vector3.up):F3}; scale {plane.transform.lossyScale}";
                break;
            }
        var driver = camera.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
        string rotationControl = driver && driver.rotationInput.action != null
            ? driver.rotationInput.action.activeControl?.path ?? "None" : "Missing";
        string intrinsics = cameraManager && cameraManager.TryGetIntrinsics(out var calibration)
            ? $"sensor {calibration.resolution}; focal {calibration.focalLength.ToString("F3")}; principal {calibration.principalPoint.ToString("F3")}" : "intrinsics unavailable";
        // Provider pose agreement does not audit the image crop/projection.
        // Keep the full matrices and frame timestamp local for calibration review.
        Debug.Log($"[Lead Mills Calibration] t {Time.unscaledTime:F3}; frame ns {frameTimestampNs}; " +
            $"screen {Screen.width}x{Screen.height}; pixel rect {camera.pixelRect}; {intrinsics}; " +
            $"projection {frameProjection.ToString("F5")}; display received {hasFrameDisplay}; " +
            $"display {frameDisplay.ToString("F5")}; camera world position {camera.transform.position.ToString("F5")}; " +
            $"camera world rotation {camera.transform.rotation.ToString("F5")}.");
        Debug.Log($"[Lead Mills Alignment] {poseComparison}; projection received {hasFrameProjection}; " +
            $"projection delta {projectionDelta:F5}; camera frame age {Time.unscaledTime - lastCameraFrameTime:F3} s; " +
            $"camera local rotation {camera.transform.localEulerAngles}; origin scale {origin.transform.lossyScale}; " +
            $"camera parent scale {camera.transform.parent.lossyScale}; orientation {Screen.orientation}; " +
            $"background {(cameraManager ? cameraManager.currentRenderingMode.ToString() : "Missing")}; " +
            $"rotation control {rotationControl}; {xrComparison}; {planeComparison}.");
    }

    void OnDisable()
    {
        if (cameraManager) cameraManager.frameReceived -= OnCameraFrame;
        if (targetOutline) Destroy(targetOutline.gameObject);
        if (targetMaterial) Destroy(targetMaterial);
        RenderPipelineManager.beginCameraRendering -= OnCameraRendering;
    }

    void Update()
    {
        meshPreview.SetVisible(!GuidedMode && placedCube == null);
        if (Time.unscaledTime >= nextSurfaceCheck)
        {
            nextSurfaceCheck = Time.unscaledTime + 0.5f;
            CheckSurfaces();
        }
        trackedPlanes = 0;
        foreach (var plane in planeManager.trackables)
        {
            // ARPlaneMeshVisualizer owns renderer visibility each frame.
            // Disable that visualizer while inspecting placement; the plane
            // manager still tracks the surface and anchors remain active.
            var visualizer = plane.GetComponent<ARPlaneMeshVisualizer>();
            bool ready = surfaces.TryGetValue(plane.trackableId, out var readiness) && readiness.ready &&
                ARSession.state == ARSessionState.SessionTracking && ARSession.notTrackingReason == NotTrackingReason.None;
            if (visualizer) visualizer.enabled = !GuidedMode && placedCube == null && ready;
            // Explicit visibility also covers visualizers disabled before their first Update.
            var renderer = plane.GetComponent<MeshRenderer>();
            if (renderer) renderer.enabled = !GuidedMode && placedCube == null && ready;
            if (plane.alignment == PlaneAlignment.HorizontalUp &&
                plane.trackingState == TrackingState.Tracking && plane.subsumedBy == null)
                trackedPlanes++;
        }

        // Log camera motion alongside anchor pose so a new device trial can
        // distinguish camera input failure from an evolving AR world estimate.
        if (placedAnchor && Time.unscaledTime >= nextPoseLog)
        {
            nextPoseLog = Time.unscaledTime + 1f;
            Debug.Log($"[Lead Mills Placement] Anchor {placedAnchor.trackingState}; " +
                $"t {Time.unscaledTime:F3}; frame ns {frameTimestampNs}; anchor {placedAnchor.trackableId}; " +
                $"anchor pose {placedAnchor.transform.position.ToString("F5")}; " +
                $"anchor local {placedAnchor.transform.localPosition.ToString("F5")}; " +
                $"local delta {Vector3.Distance(anchorStartLocalPosition, placedAnchor.transform.localPosition):F5} m; " +
                $"local rotation delta {Quaternion.Angle(anchorStartLocalRotation, placedAnchor.transform.localRotation):F4} deg; " +
                $"camera pose {origin.Camera.transform.position.ToString("F5")}; " +
                $"session {ARSession.state}; reason {ARSession.notTrackingReason}.");
        }

        if (GuidedMode)
        {
            UpdateGuidedTarget();
            ReadGuidedControls();
            return;
        }
        if (ReadModeControl()) return;
        if (calibration && (calibration.CapturesPlacement || calibration.ConsumedTouchThisFrame)) return;
        Vector2 screenPoint;
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            screenPoint = touch.primaryTouch.position.ReadValue();
        else if (Application.isEditor && Mouse.current != null &&
                 Mouse.current.leftButton.wasPressedThisFrame)
            screenPoint = Mouse.current.position.ReadValue();
        else
            return;

        if (comparison && comparison.ContainsScreenPoint(screenPoint)) return;

        // IMGUI uses top-left coordinates; touch input uses bottom-left.
        var guiPoint = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
        if (HudRect().Contains(guiPoint))
        {
            // Runtime IMGUI receives no input with Input System-only handling.
            // Read the reset touch through Input System, just like placement.
            if (placedCube && ResetRect().Contains(guiPoint)) RemoveCube();
            return;
        }
        if (placedCube || ARSession.state != ARSessionState.SessionTracking)
            return;

        if (readyPlanes == 0)
        {
            feedback = scanStatus;
            return;
        }
        if (calibration && calibration.ContainsScreenPoint(screenPoint)) return;
        // The native viewport ray uses the provider's unmodified camera view.
        // During the opt-in roll test use the displayed camera's world ray so
        // the hit matches the visible surface rather than an unrotated viewport.
        if (comparison) comparison.PrepareViewForRaycast();
        bool didHit = comparison && comparison.ViewRollTestActive
            ? raycastManager.Raycast(origin.Camera.ScreenPointToRay(screenPoint), hits, TrackableType.PlaneWithinPolygon)
            : raycastManager.Raycast(screenPoint, hits, TrackableType.PlaneWithinPolygon);
        if (!didHit)
        {
            feedback = "Tap inside a detected blue surface.";
            return;
        }
        foreach (var hit in hits)
        {
            var plane = planeManager.GetPlane(hit.trackableId);
            if (plane == null || plane.alignment != PlaneAlignment.HorizontalUp ||
                plane.trackingState != TrackingState.Tracking || plane.subsumedBy != null)
                continue;
            // Recheck the plane and the tap area at placement time, not just a
            // previously qualified center. No anchor on unsupported geometry.
            CheckSurfaces();
            if (!surfaces.TryGetValue(plane.trackableId, out var readiness) || !readiness.ready ||
                meshPreview.SupportedCells(plane, hit.pose.position, 0.2f) < 4)
            {
                feedback = "That spot needs more LiDAR coverage. Scan it slowly.";
                return;
            }

            CreatePlacement(plane, hit.pose);
            return;
        }
        feedback = "Wait for a horizontal surface to finish tracking.";
    }

    bool CreatePlacement(ARPlane plane, Pose pose)
    {
        if (anchorManager.subsystem == null ||
            !anchorManager.subsystem.subsystemDescriptor.supportsTrackableAttachments)
        {
            feedback = "Plane anchors are unavailable. Placement was not created.";
            Debug.LogWarning("[Lead Mills Placement] Plane anchor attachment unsupported.");
            return false;
        }
        placedAnchor = anchorManager.AttachAnchor(plane, pose);
        if (!placedAnchor)
        {
            feedback = "Anchor creation failed. Scan the surface and try again.";
            Debug.LogWarning("[Lead Mills Placement] Plane anchor creation failed.");
            return false;
        }
        anchorStartLocalPosition = placedAnchor.transform.localPosition;
        anchorStartLocalRotation = placedAnchor.transform.localRotation;
        // Cube uses a center pivot; the vending asset uses a floor-center pivot.
        placedCube = Instantiate(cubePrefab, placedAnchor.transform, false);
        placedCube.transform.localPosition = Vector3.up * assetPivotHeight;
        var forward = Vector3.ProjectOnPlane(pose.position - origin.Camera.transform.position, plane.transform.up);
        placedCube.transform.rotation = forward.sqrMagnitude > .001f ? Quaternion.LookRotation(forward, plane.transform.up) : placedAnchor.transform.rotation;
        placedCube.name = cubePrefab.name;
        feedback = "Keep the object in view. Move sideways and back.";
        Debug.Log($"[Lead Mills Placement] Cube attached to plane {plane.trackableId}; " +
            $"anchor {placedAnchor.trackableId}; pose {pose.position}.");
        return true;
    }

    public bool ContainsHudScreenPoint(Vector2 point)
    {
        var gui = new Vector2(point.x, Screen.height-point.y);
        return HudRect().Contains(gui) || ModeRect().Contains(gui) || (GuidedMode && GuidedRect().Contains(gui));
    }

    public bool TryCalibrationPlane(Vector2 screen, out ARPlane plane)
    {
        plane = null;
        CheckSurfaces();
        comparison.PrepareViewForRaycast();
        if (!raycastManager.Raycast(origin.Camera.ScreenPointToRay(screen), hits, TrackableType.PlaneWithinPolygon)) return false;
        foreach (var hit in hits)
        {
            var candidate = planeManager.GetPlane(hit.trackableId);
            if (candidate && surfaces.TryGetValue(candidate.trackableId, out var state) && state.ready &&
                meshPreview.SupportedCells(candidate, hit.pose.position, 0.2f) >= 4)
            { plane = candidate; return true; }
        }
        return false;
    }

    public Material CalibrationMarkerMaterial => cubePrefab.GetComponentInChildren<LineRenderer>().sharedMaterial;

    // Owned by the calibration wizard; no cube placement or reset side effects.
    public ARAnchor CreateCalibrationMarkerAnchor(ARPlane surface, Vector3 point)
    {
        if (!surface || surface.trackingState != TrackingState.Tracking || surface.subsumedBy != null ||
            anchorManager.subsystem == null || !anchorManager.subsystem.subsystemDescriptor.supportsTrackableAttachments)
            return null;
        return anchorManager.AttachAnchor(surface, new Pose(point, surface.transform.rotation));
    }

    public bool PlaceCalibrationAnchor(ARPlane plane, Vector3 point)
    {
        if (!plane || plane.trackingState != TrackingState.Tracking || plane.subsumedBy != null ||
            ARSession.state != ARSessionState.SessionTracking || ARSession.notTrackingReason != NotTrackingReason.None ||
            Vector3.Dot(plane.transform.up, Vector3.up) < 0.985f ||
            meshPreview.SupportedCells(plane, point, 0.2f) < 4) return false;
        RemoveCube();
        return CreatePlacement(plane, new Pose(point, plane.transform.rotation));
    }

    void OnApplicationPause(bool paused)
    {
        if (paused && GuidedMode) { RemoveCube(); if (targetOutline) targetOutline.enabled = false; }
    }
    bool GuidedTracking => ARSession.state == ARSessionState.SessionTracking && ARSession.notTrackingReason == NotTrackingReason.None &&
        automaticValid && Time.unscaledTime-lastCameraFrameTime < .25f && comparison && comparison.PortraitCorrectionActive;
    bool PointerPress(out Vector2 gui)
    {
        var t = Touchscreen.current;
        bool pressed = t != null && t.primaryTouch.press.wasPressedThisFrame;
        var p = pressed ? t.primaryTouch.position.ReadValue() : Vector2.zero;
        if (!pressed && Application.isEditor && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        { pressed = true; p = Mouse.current.position.ReadValue(); }
        gui = new Vector2(p.x, Screen.height-p.y); return pressed;
    }
    Rect ModeRect() { var s=Screen.safeArea;return new Rect(s.xMax-108*UiScale,Screen.height-s.yMax+8*UiScale,100*UiScale,40*UiScale); }
    Rect GuidedRect() { var s=Screen.safeArea;return new Rect(s.x+8*UiScale,Screen.height-s.yMin-144*UiScale,s.width-16*UiScale,136*UiScale); }
    Rect GuidedAction(int i) { var r=GuidedRect();float w=(r.width-24*UiScale)/2;return new Rect(r.x+8*UiScale+i*(w+8*UiScale),r.yMax-52*UiScale,w,44*UiScale); }
    bool ReadModeControl()
    {
        if (!PointerPress(out var gui) || !ModeRect().Contains(gui)) return false;
        if (calibration && calibration.CapturesPlacement) return true;
        calibration.Cancel("Mode changed. Start a new trial.");
        RemoveCube(); GuidedMode = !GuidedMode; calibration.DiagnosticsVisible = !GuidedMode;
        targetSince=0;targetReady=false;if(targetOutline)targetOutline.enabled=false;
        Debug.Log("[Lead Mills Guided] Mode " + (GuidedMode ? "guided automatic" : "advanced manual"));
        return true;
    }
    void ReadGuidedControls()
    {
        if (ReadModeControl() || !PointerPress(out var gui)) return;
        if (GuidedAction(0).Contains(gui))
        {
            if (placedCube) { RemoveCube(); return; }
            // Recheck the center candidate on this frame; never place a stale or unsupported target.
            CheckSurfaces(); nextTargetCheck=0; UpdateGuidedTarget();
            if (!targetReady || !targetPlane || !GuidedTracking) return;
            TrialTrace trial;
            try { trial=TrialTrace.Begin(comparison.CurrentConfiguration + "; guided automatic intrinsics/display; no manual landmark taps"); }
            catch (System.Exception e) { Debug.LogError(e);guidedMessage="Could not save trial. Check storage.";return; }
            trial.Log("automatic-alignment", "derived focal scale " + automaticScale.ToString("F6") + "; native principal/depth preserved; physical alignment unverified");
            if (PlaceCalibrationAnchor(targetPlane,targetPoint))
            {
                guidedTrace=trial;
                guidedPlacedAt=Time.unscaledTime;
                guidedTrace.Log("anchor-A", "Guided center target; native plane attachment; automatic scale " + automaticScale.ToString("F6"));
                guidedTrace.Outcome("guided placement; physical stability unverified");
                if(targetOutline)targetOutline.enabled=false;
            }
            else { trial.Outcome("guided placement failed");guidedTrace=null; }
        }
        else if (GuidedAction(1).Contains(gui) && placedCube && GuidedTracking && CalibrationAnchorTracking && guidedTrace != null && !guidedTrace.HasPhysicalVerdict)
            guidedTrace.PhysicalVerdict(true,"Guided user observation; elapsed " + (Time.unscaledTime-guidedPlacedAt).ToString("F1") + " s; no surveyed accuracy claim");
    }
    void UpdateGuidedTarget()
    {
        if (placedCube)
        {
            bool visible=GuidedTracking && CalibrationAnchorTracking;
            foreach(var renderer in placedCube.GetComponentsInChildren<Renderer>()) renderer.enabled=visible;
            guidedMessage=visible ? (guidedTrace != null && guidedTrace.HasPhysicalVerdict ? "Stable saved. Move starts another placement." : "Placed. Walk around; check floor contact. Tap Stable if it holds.") : "Tracking limited. Hold still and look at the same floor area.";
            targetReady=false;if(targetOutline)targetOutline.enabled=false;return;
        }
        if (Time.unscaledTime < nextTargetCheck) return;
        nextTargetCheck=Time.unscaledTime+.15f;targetReady=false;targetPlane=null;
        if (!GuidedTracking || Time.unscaledTime-automaticSince < 1f)
        { guidedMessage="Point at a textured floor in good light. Move slowly sideways.";targetSince=0;if(targetOutline)targetOutline.enabled=false;return; }
        comparison.PrepareViewForRaycast();
        if(!raycastManager.Raycast(origin.Camera.ScreenPointToRay(new Vector2(Screen.width*.5f,Screen.height*.5f)),hits,TrackableType.PlaneWithinPolygon))
        {guidedMessage="Aim the center at the floor. Tilt down; move slowly left and right.";targetSince=0;if(targetOutline)targetOutline.enabled=false;return;}
        foreach(var hit in hits)
        {
            var plane=planeManager.GetPlane(hit.trackableId);
            if(!plane || plane.alignment!=PlaneAlignment.HorizontalUp || plane.trackingState!=TrackingState.Tracking || plane.subsumedBy!=null)continue;
            targetPlane=plane;targetPoint=hit.pose.position;
            bool supported=surfaces.TryGetValue(plane.trackableId,out var state) && state.ready && meshPreview.SupportedCells(plane,targetPoint,.2f)>=4;
            if(!supported || Vector3.Distance(targetPoint,previousTarget)>.08f)targetSince=Time.unscaledTime;
            previousTarget=targetPoint;
            targetReady=supported && targetSince>0 && Time.unscaledTime-targetSince>=.6f;
            if(!targetOutline)
            {
                var o=new GameObject("Guided placement outline");targetOutline=o.AddComponent<LineRenderer>();
                targetOutline.useWorldSpace=true;targetOutline.loop=true;targetOutline.positionCount=4;targetOutline.widthMultiplier=.008f;
                targetMaterial=new Material(CalibrationMarkerMaterial);targetOutline.sharedMaterial=targetMaterial;
                targetOutline.shadowCastingMode=ShadowCastingMode.Off;targetOutline.receiveShadows=false;
            }
            // Same footprint/orientation as the placed vending machine; no preview asset workload.
            var forward=Vector3.ProjectOnPlane(targetPoint-origin.Camera.transform.position,plane.transform.up).normalized;
            if(forward.sqrMagnitude < .001f){targetReady=false;targetOutline.enabled=false;guidedMessage="Tilt slightly forward so the footprint has a facing direction.";return;}
            var right=Vector3.Cross(plane.transform.up,forward).normalized;var c=targetPoint+plane.transform.up*.004f;
            targetOutline.SetPositions(new[]{c-right*.6f-forward*.425f,c+right*.6f-forward*.425f,c+right*.6f+forward*.425f,c-right*.6f+forward*.425f});
            var color=targetReady?new Color(.1f,1f,.3f):new Color(1f,.7f,.1f);
            targetMaterial.SetColor("_BaseColor",color);if(targetMaterial.HasProperty("_EmissionColor"))targetMaterial.SetColor("_EmissionColor",color*.7f);
            targetOutline.enabled=true;
            guidedMessage=targetReady?"Green outline ready. Tap Place.":"Scan this floor spot slowly sideways, then hold the outline still.";
            return;
        }
        targetSince=0;if(targetOutline)targetOutline.enabled=false;guidedMessage="Find a level floor area. Move slowly sideways.";
    }
    void DrawModeControl()
    {
        float s=UiScale;var previous=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(s,s,1));var r=ModeRect();
        GUI.enabled=!calibration || !calibration.CapturesPlacement;
        GUI.Button(new Rect(r.x/s,r.y/s,r.width/s,r.height/s),GuidedMode?"Advanced":"Simple");GUI.enabled=true;GUI.matrix=previous;
    }
    void DrawGuidedUI()
    {
        if(labelStyle==null){labelStyle=new GUIStyle(GUI.skin.label){fontSize=18,wordWrap=true};buttonStyle=new GUIStyle(GUI.skin.button){fontSize=20};}
        float s=UiScale;var previous=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(s,s,1));
        var safe=Screen.safeArea;GUI.Label(new Rect(safe.x/s+8,(Screen.height-safe.yMax)/s+10,safe.width/s-130,36),placedCube?"Anchor: "+CalibrationAnchorState:"Scan floor",labelStyle);
        if(!placedCube)GUI.Label(new Rect(Screen.width/s*.5f-12,Screen.height/s*.5f-16,28,32),"+",buttonStyle);
        var r=GuidedRect();GUI.Box(new Rect(r.x/s,r.y/s,r.width/s,r.height/s),GUIContent.none);
        GUI.Label(new Rect(r.x/s+8,r.y/s+4,r.width/s-16,22),TrialTrace.DisplayId,labelStyle);
        GUI.Label(new Rect(r.x/s+8,r.y/s+30,r.width/s-16,48),guidedMessage,labelStyle);
        var a=GuidedAction(0);GUI.enabled=placedCube || targetReady;GUI.Button(new Rect(a.x/s,a.y/s,a.width/s,a.height/s),placedCube?"Move":"Place",buttonStyle);
        a=GuidedAction(1);GUI.enabled=placedCube && GuidedTracking && CalibrationAnchorTracking && guidedTrace!=null && !guidedTrace.HasPhysicalVerdict;
        GUI.Button(new Rect(a.x/s,a.y/s,a.width/s,a.height/s),guidedTrace!=null && guidedTrace.HasPhysicalVerdict?"Stable saved":"Stable",buttonStyle);
        GUI.enabled=true;GUI.matrix=previous;
    }

    float UiScale => Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height) / 600f);

    Rect HudRect()
    {
        var safe = Screen.safeArea;
        return new Rect(safe.x + 8 * UiScale, Screen.height - safe.yMax + 8 * UiScale,
            safe.width - 124 * UiScale, (calibration && calibration.CapturesPlacement ? 44 : placedCube ? 148 : 98) * UiScale);
    }

    Rect ResetRect()
    {
        var hud = HudRect();
        return new Rect(hud.x + 12 * UiScale, hud.y + 96 * UiScale,
            hud.width - 24 * UiScale, 46 * UiScale);
    }

    public void RestartSurfaceCheck()
    {
        RemoveCube();
        nextSurfaceCheck = 0;
    }

    void RemoveCube()
    {
        if (guidedTrace != null)
        {
            guidedTrace.Log("session-invalidated", "Placement removed or app paused.");
            if (!guidedTrace.HasPhysicalVerdict) guidedTrace.Outcome("cleared: guided placement removed");
            guidedTrace = null;
        }
        targetSince = 0; targetReady = false;
        Destroy(placedCube);
        if (placedAnchor && !anchorManager.TryRemoveAnchor(placedAnchor))
            Debug.LogWarning("[Lead Mills Placement] Provider did not remove the anchor.");
        placedAnchor = null;
        placedCube = null;
        surfaces.Clear();
        readyPlanes = 0;
        feedback = "Scan again to verify the surface before placing.";
        Debug.Log("[Lead Mills Placement] Cube removed for another placement.");
    }

    void OnGUI()
    {
        if (!enabled) return;
        DrawModeControl();
        if (GuidedMode) { DrawGuidedUI(); return; }
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 21, wordWrap = true };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 23 };
        }
        var previousMatrix = GUI.matrix;
        float scale = UiScale;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        var hud = HudRect();
        hud = new Rect(hud.x / scale, hud.y / scale, hud.width / scale, hud.height / scale);
        GUI.Box(hud, GUIContent.none);
        GUI.Label(new Rect(hud.x + 8, hud.y + 6, hud.width - 16, 32),
            placedCube ? "Anchor: " + CalibrationAnchorState : $"{ARSession.state} | Floor {readyPlanes}/{trackedPlanes}", labelStyle);
        if (calibration && calibration.CapturesPlacement) { GUI.matrix = previousMatrix; return; }
        string instruction = ARSession.state != ARSessionState.SessionTracking
            ? "Tracking is starting or limited. Move slowly in good light."
            : placedCube ? $"Anchor: {(placedAnchor ? placedAnchor.trackingState.ToString() : "Missing")}. {feedback}" : scanStatus + (feedback.StartsWith("That spot") ? " " + feedback : "");
        GUI.Label(new Rect(hud.x + 12, hud.y + 42, hud.width - 24, 50), instruction, labelStyle);
        if (!placedCube) { GUI.matrix = previousMatrix; return; }
        GUI.enabled = placedCube != null;
        if (GUI.Button(new Rect(hud.x + 12, hud.y + 96, hud.width - 24, 46),
            "Remove object", buttonStyle))
            RemoveCube();
        GUI.enabled = true;
        GUI.matrix = previousMatrix;
    }
}
