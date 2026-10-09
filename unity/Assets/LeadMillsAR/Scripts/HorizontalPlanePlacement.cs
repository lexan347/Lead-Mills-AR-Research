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
    readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    GameObject placedCube;
    ARAnchor placedAnchor;
    float nextPoseLog;
    string feedback = "Move slowly over a textured table or floor.";
    GUIStyle labelStyle;
    GUIStyle buttonStyle;
    int trackedPlanes;
    ARCameraManager cameraManager;
    Matrix4x4 frameProjection;
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
    }

    void Start()
    {
        if (!planeManager || !raycastManager || !anchorManager || !meshPreview || !origin || !cubePrefab)
        {
            Debug.LogError("[Lead Mills Placement] Missing setup reference.");
            enabled = false;
            return;
        }
        cameraManager = origin.Camera.GetComponent<ARCameraManager>();
        if (cameraManager) cameraManager.frameReceived += OnCameraFrame;
        RenderPipelineManager.beginCameraRendering += OnCameraRendering;
        Debug.Log("[Lead Mills Placement] LiDAR readiness gate: level within 10 deg, 40 cm extent, 8 supported 10 cm cells, 3 s stability, 15 cm viewpoint change; cube size 0.20 m.");
    }

    void OnCameraFrame(ARCameraFrameEventArgs frame)
    {
        lastCameraFrameTime = Time.unscaledTime;
        if (frame.projectionMatrix.HasValue)
        {
            frameProjection = frame.projectionMatrix.Value;
            hasFrameProjection = true;
        }
    }

    void OnCameraRendering(ScriptableRenderContext context, Camera camera)
    {
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
        RenderPipelineManager.beginCameraRendering -= OnCameraRendering;
    }

    void Update()
    {
        meshPreview.SetVisible(placedCube == null);
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
            if (visualizer) visualizer.enabled = placedCube == null && ready;
            // Explicit visibility also covers visualizers disabled before their first Update.
            var renderer = plane.GetComponent<MeshRenderer>();
            if (renderer) renderer.enabled = placedCube == null && ready;
            if (plane.alignment == PlaneAlignment.HorizontalUp &&
                plane.trackingState == TrackingState.Tracking && plane.subsumedBy == null)
                trackedPlanes++;
        }

        // Log camera motion alongside anchor pose so a new device trial can
        // distinguish camera input failure from an evolving AR world estimate.
        if (placedAnchor && Time.unscaledTime >= nextPoseLog)
        {
            nextPoseLog = Time.unscaledTime + 5f;
            Debug.Log($"[Lead Mills Placement] Anchor {placedAnchor.trackingState}; " +
                $"anchor pose {placedAnchor.transform.position}; camera pose {origin.Camera.transform.position}; " +
                $"session {ARSession.state}; reason {ARSession.notTrackingReason}.");
        }

        Vector2 screenPoint;
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            screenPoint = touch.primaryTouch.position.ReadValue();
        else if (Application.isEditor && Mouse.current != null &&
                 Mouse.current.leftButton.wasPressedThisFrame)
            screenPoint = Mouse.current.position.ReadValue();
        else
            return;

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
        if (!raycastManager.Raycast(screenPoint, hits, TrackableType.PlaneWithinPolygon))
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

            if (anchorManager.subsystem == null ||
                !anchorManager.subsystem.subsystemDescriptor.supportsTrackableAttachments)
            {
                feedback = "Plane anchors are unavailable. Placement was not created.";
                Debug.LogWarning("[Lead Mills Placement] Plane anchor attachment unsupported.");
                return;
            }
            placedAnchor = anchorManager.AttachAnchor(plane, hit.pose);
            if (!placedAnchor)
            {
                feedback = "Anchor creation failed. Scan the surface and try again.";
                Debug.LogWarning("[Lead Mills Placement] Plane anchor creation failed.");
                return;
            }
            // Anchor sits on the surface; cube pivot is at its center.
            placedCube = Instantiate(cubePrefab, placedAnchor.transform, false);
            placedCube.transform.localPosition = Vector3.up * 0.10f;
            placedCube.transform.localRotation = Quaternion.identity;
            placedCube.name = "Lead Mills Test Cube (20 cm)";
            feedback = "Keep cube in view. Move 20 cm sideways and back.";
            Debug.Log($"[Lead Mills Placement] Cube attached to plane {hit.trackableId}; " +
                $"anchor {placedAnchor.trackableId}; pose {hit.pose.position}.");
            return;
        }
        feedback = "Wait for a horizontal surface to finish tracking.";
    }

    float UiScale => Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height) / 600f);

    Rect HudRect()
    {
        var safe = Screen.safeArea;
        return new Rect(safe.x + 8 * UiScale, Screen.height - safe.yMax + 8 * UiScale,
            safe.width - 16 * UiScale, 184 * UiScale);
    }

    Rect ResetRect()
    {
        var hud = HudRect();
        return new Rect(hud.x + 12 * UiScale, hud.y + 126 * UiScale,
            hud.width - 24 * UiScale, 46 * UiScale);
    }

    void RemoveCube()
    {
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
        GUI.Label(new Rect(hud.x + 12, hud.y + 8, hud.width - 24, 30),
            $"Meshes: {meshPreview.MeshCount} | Ready: {readyPlanes}/{trackedPlanes} | {ARSession.state}", labelStyle);
        string instruction = ARSession.state != ARSessionState.SessionTracking
            ? "Tracking is starting or limited. Move slowly in good light."
            : placedCube ? $"Anchor: {(placedAnchor ? placedAnchor.trackingState.ToString() : "Missing")}. {feedback}" : scanStatus + (feedback.StartsWith("That spot") ? " " + feedback : "");
        GUI.Label(new Rect(hud.x + 12, hud.y + 42, hud.width - 24, 76), instruction, labelStyle);
        GUI.enabled = placedCube != null;
        if (GUI.Button(new Rect(hud.x + 12, hud.y + 126, hud.width - 24, 46),
            "Remove cube and place again", buttonStyle))
            RemoveCube();
        GUI.enabled = true;
        GUI.matrix = previousMatrix;
    }
}
