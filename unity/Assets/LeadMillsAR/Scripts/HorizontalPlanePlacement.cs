using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// One 20 cm cube attached to a tracked plane anchor. No persistence/geospatial anchor.
public sealed class HorizontalPlanePlacement : MonoBehaviour
{
    [SerializeField] ARPlaneManager planeManager;
    [SerializeField] ARRaycastManager raycastManager;
    [SerializeField] ARAnchorManager anchorManager;
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

    public void Configure(ARPlaneManager planes, ARRaycastManager raycasts,
        ARAnchorManager anchors, XROrigin xrOrigin, GameObject cube)
    {
        planeManager = planes;
        raycastManager = raycasts;
        anchorManager = anchors;
        origin = xrOrigin;
        cubePrefab = cube;
    }

    void Start()
    {
        if (!planeManager || !raycastManager || !anchorManager || !origin || !cubePrefab)
        {
            Debug.LogError("[Lead Mills Placement] Missing setup reference.");
            enabled = false;
            return;
        }
        Debug.Log("[Lead Mills Placement] Plane-anchor test ready; cube size 0.20 m.");
    }

    void Update()
    {
        trackedPlanes = 0;
        foreach (var plane in planeManager.trackables)
            if (plane.alignment == PlaneAlignment.HorizontalUp &&
                plane.trackingState == TrackingState.Tracking && plane.subsumedBy == null)
                trackedPlanes++;

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
            feedback = "Walk slowly around the cube. Check its floor position.";
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
        feedback = "Tap a blue surface to place the 20 cm cube.";
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
            $"Horizontal planes: {trackedPlanes}  |  {ARSession.state}", labelStyle);
        string instruction = ARSession.state != ARSessionState.SessionTracking
            ? "Tracking is starting or limited. Move slowly in good light."
            : placedCube ? $"Anchor: {(placedAnchor ? placedAnchor.trackingState.ToString() : "Missing")}. {feedback}" : trackedPlanes > 0
                ? "Tap a blue surface to place the 20 cm cube." : feedback;
        GUI.Label(new Rect(hud.x + 12, hud.y + 42, hud.width - 24, 76), instruction, labelStyle);
        GUI.enabled = placedCube != null;
        if (GUI.Button(new Rect(hud.x + 12, hud.y + 126, hud.width - 24, 46),
            "Remove cube and place again", buttonStyle))
            RemoveCube();
        GUI.enabled = true;
        GUI.matrix = previousMatrix;
    }
}
