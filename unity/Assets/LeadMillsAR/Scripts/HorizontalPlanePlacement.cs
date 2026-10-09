using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// One 20 cm cube placed in session space. No persistent/geospatial anchor is claimed.
public sealed class HorizontalPlanePlacement : MonoBehaviour
{
    [SerializeField] ARPlaneManager planeManager;
    [SerializeField] ARRaycastManager raycastManager;
    [SerializeField] XROrigin origin;
    [SerializeField] GameObject cubePrefab;
    readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    GameObject placedCube;
    string feedback = "Move slowly over a textured table or floor.";
    GUIStyle labelStyle;
    GUIStyle buttonStyle;
    int trackedPlanes;

    public void Configure(ARPlaneManager planes, ARRaycastManager raycasts,
        XROrigin xrOrigin, GameObject cube)
    {
        planeManager = planes;
        raycastManager = raycasts;
        origin = xrOrigin;
        cubePrefab = cube;
    }

    void Start()
    {
        if (!planeManager || !raycastManager || !origin || !cubePrefab)
        {
            Debug.LogError("[Lead Mills Placement] Missing setup reference.");
            enabled = false;
            return;
        }
        Debug.Log("[Lead Mills Placement] Horizontal-plane test ready; cube size 0.20 m.");
    }

    void Update()
    {
        trackedPlanes = 0;
        foreach (var plane in planeManager.trackables)
            if (plane.alignment == PlaneAlignment.HorizontalUp &&
                plane.trackingState == TrackingState.Tracking && plane.subsumedBy == null)
                trackedPlanes++;

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

            // Cube pivot is at its center. Lift by half its 20 cm height.
            var pose = hit.pose;
            placedCube = Instantiate(cubePrefab,
                pose.position + pose.rotation * Vector3.up * 0.10f,
                pose.rotation, origin.TrackablesParent);
            placedCube.name = "Lead Mills Test Cube (20 cm)";
            feedback = "Walk slowly around the cube. It should stay on the surface.";
            Debug.Log($"[Lead Mills Placement] Cube placed on plane {hit.trackableId}; pose {pose.position}.");
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
            : placedCube ? feedback : trackedPlanes > 0
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
