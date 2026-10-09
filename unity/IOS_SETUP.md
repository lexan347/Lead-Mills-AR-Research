# Unity / iOS POC setup — October 8, 2026

Status: FIRST SUCCESSFUL NATIVE AR PROOF OF CONCEPT. Camera permission and live camera feed verified on the physical iPhone 14 Pro.

## Completed setup

- Mac environment: Xcode **27.0 (27A266a)**, Homebrew **7.0.8**, Git LFS **3.8.0**, GitHub CLI **2.102.0**, Unity Hub **3.22.2**.
- Unity **6.3 LTS `6000.3.25f1` Apple Silicon**, with iOS Build Support loaded after restarting the editor.
- Local `LeadMills_AR_POC` Universal 3D / URP project; iOS active in Build Profiles.
- AR Foundation **6.3.5** and Apple ARKit XR Plugin **6.3.5**; ARKit enabled for iOS, Initialize XR on Startup enabled, ARKit Requirement Required, Face Tracking off.
- Run In Background and camera usage description configured; Project Validation: **0 issues**.
- Xcode automatic signing with **Alexander Angulo (Personal Team)** and bundle ID **`com.alexanderangulo.leadmillsarpoc`**.
- Physical iPhone 14 Pro updated to **iOS 27.0.1**, recognized/selected in Xcode, and developer certificate trusted. Build/install/launch succeeded October 7; camera permission/live feed succeeded October 8.

Camera usage wording selected during setup:

> Camera access is required to place and view the Lead Mills historical reconstruction in augmented reality.

## Scene and renderer configuration

Saved scene: `Assets/Scenes/LeadMills_AR_POC.unity`.

```text
LeadMills_AR_POC
├── Directional Light
├── Global Volume
├── AR Session
└── XR Origin
    └── Camera Offset
        └── Main Camera
```

- AR Session: enabled **AR Session** and **AR Input Manager** components. The diagnostic component belongs here; a Unity Camera component does not.
- XR Origin: Origin Base GameObject = XR Origin, Camera Floor Offset GameObject = Camera Offset, Camera = Main Camera, **Tracking Origin Mode = Device**, **Camera Y Offset = 0**.
- Main Camera: Camera, Audio Listener, **AR Camera Manager**, **AR Camera Background**, Tracked Pose Driver, and Universal Additional Camera Data. Only one camera, under XR Origin.
- iOS quality Render Pipeline Asset = **Mobile_RPAsset**. Main Camera Renderer = **Default Renderer (Mobile_Renderer)**.
- `Assets/Settings/Mobile_Renderer`: **AR Background Renderer Feature** present and enabled. The quality level's name may still be PC; check the assigned pipeline asset rather than relying on the label.

## Save and export without stale generated files

1. Open **File → Build Profiles → iOS → Scene List**. Add/check **`Assets/Scenes/LeadMills_AR_POC.unity`** (shown as `Scenes/LeadMills_AR_POC`); uncheck **SampleScene**. For this POC, enable only the AR scene.
2. Save the scene with **File → Save** and save settings with **File → Save Project**. Check that Unity has no red compile errors.
3. Build into a **new export folder**, such as **`Builds/iOS_Diagnostic`**, rather than overwriting the old `Builds/iOS`. If the diagnostic folder already exists, use another fresh folder.
4. Open **that export's** `Unity-iPhone.xcodeproj`, for example `Builds/iOS_Diagnostic/Unity-iPhone.xcodeproj`.
5. Select the physical iPhone 14 Pro (iOS 27.0.1), recheck automatic signing, Personal Team, and bundle ID, and press Xcode's **▶ Run** button.
6. Trust the development certificate under **Settings → General → VPN & Device Management** if requested. Complete device trust/Developer Mode prompts if Xcode requires them.
7. Allow camera access when prompted and verify the live camera feed. If access was previously denied, inspect the app's Camera permission in iOS settings.

Keep the bundle ID and camera usage description in Unity Player → iOS settings so later exports retain them. Generated Xcode projects and signing files remain outside source control.

## Troubleshooting and lessons learned

### Blue sky / gray ground, no camera prompt

The October 7 app compiled, installed, and launched, but displayed the default Unity sky/horizon/gray ground. AR camera components, AR Session + AR Input Manager, XR Origin wiring, Device tracking/zero offset, and the mobile URP renderer were checked or corrected. Rebuilding after renderer changes alone did not resolve the symptom.

The crucial October 8 finding was that **Build Profiles still built SampleScene**, rather than the saved `LeadMills_AR_POC` scene. Consequently the edited AR scene and startup diagnostic script never ran, and camera permission never triggered. Adding/checking the AR scene, unchecking SampleScene, saving, and making a fresh export led to the permission prompt and working live feed. Stale generated output was guarded against with a new export folder; it was not independently proven to be a separate defect.

Check the enabled build scene before further package/rendering changes. A correct editor hierarchy and 0 Project Validation issues do not prove that the exported app contains that scene.

### Startup diagnostics

`ARStartupDiagnostics.cs` was created locally October 7. Its class name must match the filename (`ARStartupDiagnostics`), and it must be attached to the **AR Session GameObject**; selecting a script asset does not execute it. The initial class-name mismatch was corrected. When no external editor was configured, the script was edited as plain text with TextEdit.

The script logs the marker `=== Lead Mills AR Startup Diagnostics ===`, active XR loader, loaded `XRCameraSubsystem`, camera subsystem running state, and `ARSession.state`. The marker was absent from the failing October 8 runtime log, which prompted the Scene List check. No post-fix subsystem values are archived here; the confirmed outcome is the user's camera-permission/live-feed report. This repository does not contain the local diagnostic source or claim an independently reproduced runtime test.

### CrashReporter and Xcode warnings

The earlier output ended with **`CrashReporter: No pending report exists`**. The conversation identified this as crash-reporting initialization, **not an actual crash**; the application continued running.

The recorded Xcode warnings (including generated Unity/ARKit debug symbols and missing `.pcm` paths, deprecated APIs, run-script warnings, missing App Store icon, and recommended settings) were **non-blocking for this build/install/launch**. The CoreMotion preference warning also did not stop the app. Document/monitor warnings rather than treating their count as a build failure. The blanket **Update to recommended settings** action was deferred; keep Unity authoritative over generated project settings. This does not establish release readiness or resolve every warning.

## Next milestone and evidence boundary

Subsequent device trials confirmed horizontal-plane rendering, visible cube placement, and plane-anchor creation, but both trials failed stability relative to a floor landmark. Following the user’s Polycam reference, the next trial is **LiDAR triangles → horizontal-surface selection → plane anchor**. See [the setup and acceptance guide](PLANE_PLACEMENT_TEST.md). Verify scan rendering, reset, floor contact and stable alignment before importing Lead Mills site assets. Historical models, Android, geospatial localization, and field validation remain pending.

This setup is recorded from the [Plan software downloads conversation](https://chatgpt.com/c/6ac57c84-40c0-83ea-b2bc-0246fbbf10f3). Dates follow America/New_York (EDT): October 6 setup, October 7 early-morning deployment/debugging, October 8 evening Scene List fix and success. The local Unity project, screenshots, dependency locks, build manifests, and generated Xcode output are not archived by this documentation update. Capture those for reproducibility before source handoff.
