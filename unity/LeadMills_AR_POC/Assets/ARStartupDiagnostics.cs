using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;
using UnityEngine.XR.ARSubsystems;

public class ARStartupDiagnostics : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== Lead Mills AR Startup Diagnostics ===");

        var loader = LoaderUtility.GetActiveLoader();

        if (loader == null)
        {
            Debug.LogError("XR active loader is NULL.");
            return;
        }

        Debug.Log("Active XR Loader: " + loader.name);

        var cameraSubsystem =
            loader.GetLoadedSubsystem<XRCameraSubsystem>();

        if (cameraSubsystem == null)
        {
            Debug.LogError("XRCameraSubsystem is NOT loaded.");
        }
        else
        {
            Debug.Log("XRCameraSubsystem loaded: "
                      + cameraSubsystem.GetType().FullName);

            Debug.Log("Camera subsystem running: "
                      + cameraSubsystem.running);
        }

        Debug.Log("ARSession.state: " + ARSession.state);
    }
}