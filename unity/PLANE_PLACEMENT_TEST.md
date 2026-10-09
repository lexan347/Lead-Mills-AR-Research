# Horizontal-plane placement test

Status: DEVICE DETECTION / VISIBLE PLACEMENT VERIFIED; both initial and plane-anchor trials failed stability. LiDAR scan-first revision exported; device acceptance pending.

## Setup and export

The source is in `Assets/LeadMillsAR/Scripts/HorizontalPlanePlacement.cs`, `Assets/LeadMillsAR/Scripts/LiDARMeshPreview.cs`, and `Assets/LeadMillsAR/Scripts/Editor/PlanePlacementSetup.cs`. These are a source subset, not a complete Unity project. Use the established Universal 3D / URP POC, Unity **6000.3.25f1**, AR Foundation / Apple ARKit XR Plugin **6.3.5**, and Input System **1.20.0**.

1. Copy these scripts and their `.meta` files into the local project's matching Assets paths. Back up the scene before first setup.
2. Open `Assets/Scenes/LeadMills_AR_POC.unity` and wait for compilation.
3. Run **Lead Mills → Set Up Horizontal Plane Test**. This adds AR Plane Manager, AR Raycast Manager, and AR Anchor Manager to the existing XR Origin, plus AR Mesh Manager and LiDARMeshPreview on a child object. It requests horizontal planes, creates a cyan triangle-edge preview of live LiDAR mesh patches, creates a blue translucent plane prefab and orange 20 cm cube prefab with shaded faces and a green base outline, assigns their materials/references, and saves the scene. It preserves the existing camera and mobile renderer.
4. Confirm the Scene List enables only the AR scene. Run **Lead Mills → Export Plane Test for iOS** for a fresh timestamped `Builds/iOS_PlanePlacement_*` development export. This export explicitly includes the AR scene and reports build failure rather than claiming success.
5. Open the new export's `Unity-iPhone.xcodeproj`, retain the existing Personal Team/bundle ID, select the physical iPhone, and build/run.

Generated materials/prefabs are created by the setup menu. The repository preserves the generating scripts; the complete local scene, Unity project settings/dependency locks, and generated build are not incorporated into the repository by this step.

## Current local build evidence

The saved scene was configured through the setup menu and the scripts compiled in Unity. The corrected development export `Builds/iOS_PlanePlacement_20261008_204644` succeeded. The Xcode Debug build succeeded after the user completed the macOS keychain prompt. The app was installed and launched on the physical iPhone 14 Pro at approximately 20:57 EDT. Runtime output showed the placement component starting and ARKit requesting horizontal Plane Tracking and Raycast, with no unsatisfied requested features. The captured runtime console subsequently recorded **two cube placements with one reset between them**, demonstrating accepted tracked-horizontal-plane raycasts and the placement/reset handlers on the device. Eight user-supplied screenshots (IMG_7545–IMG_7552) confirm live camera video, blue plane meshes, 4–5 horizontal planes with SessionTracking, and an orange cube visible across multiple views. The user subsequently reported that the cube slid, floated, or followed the phone. This fails the stability criterion; exact floor contact and the cause remain unresolved. The room screenshots and raw device logs are not published in this repository.

## On-device procedure

1. Use a well-lit, textured table or floor. Move the phone slowly over the surface until cyan LiDAR triangle edges and blue detected-plane geometry appear. Require a positive mesh-patch count, a positive horizontal-plane count, and SessionTracking. This trial requires the LiDAR-equipped iPhone 14 Pro; no non-LiDAR fallback is implemented.
2. Tap inside a blue surface. One **orange 20 cm cube** should appear. The revised test uses a lit material and a green footprint just above the estimated surface. Cyan triangles and blue plane meshes hide while the cube is placed; mesh acquisition and plane detection continue, and reset restores their visibility. The green outline is a virtual estimate, not a surveyed or physical marker.
3. Keep the entire cube and a fixed floorboard joint visible. Move the phone slowly about 20 cm sideways and back before a longer 30–60-second trial. Compare the green outline against that same joint; record floating, sliding, drift, or disappearance. A room-wide sweep that loses the cube from view cannot establish continuous contact.
4. Tap **Remove cube and place again** and place it once more. Confirm the reset action does not accidentally place another cube beneath the button.
5. Briefly point away and return to the surface. Record tracking state and recovery; do not label this precise spatial validation.

The app accepts only polygon raycast hits on an upward horizontal plane that is currently tracking and not subsumed. Taps on the status/reset overlay are ignored. Once placed, further surface taps do not move or duplicate the cube. The revised placement attaches an AR anchor to the hit plane and parents the cube to that anchor, with a local 0.10 m height offset. It refuses placement if plane attachment is unsupported or anchor creation fails. Reset removes the cube and requests anchor removal. The HUD reports anchor tracking, and five-second console diagnostics include anchor and camera poses plus session tracking reason. This is a session-local plane anchor; it is neither persistent nor geospatial. Restarting the app does not preserve the placement.

## Evidence to record

- Date, device, OS, Unity/AR package versions, export folder and source commit.
- Camera working; cyan triangles and blue planes visible; mesh-patch count, plane count and session state.
- Cube placement and bottom contact; observed stability over the defined movement.
- Reset/replacement and tracking-loss/recovery behavior; screenshot or short recording.
- Console lines starting `[Lead Mills Placement]`, and exact errors if any.

Only mark this gate complete after the physical-phone result is observed. Historical site assets, geospatial providers, Android, and field accuracy remain subsequent work.

## Stability revision

The first build instantiated the cube directly under the trackables parent without an anchor. The revision uses `ARAnchorManager.AttachAnchor` so the provider can update the placement with its tracked plane estimate. Camera pose-driver position/rotation bindings and origin offsets were inspected; no camera-transform fault was established. An anchor is a targeted improvement, not a proven diagnosis or guarantee against drift. Repeat the movement, surface-contact, and reset tests before accepting this gate.

Unity compiled the anchor revision and successfully exported **`Builds/iOS_PlanePlacement_20261008_210614`**. This export result is separate from device stability acceptance.

After the user completed the subsequent signing prompt, the anchor revision’s **Xcode Debug build succeeded** and it was installed/launched on the physical iPhone 14 Pro. The device console confirmed **“Plane-anchor test ready”**, successful cube attachment to a plane anchor, **Anchor Tracking**, and **SessionTracking** with tracking reason **None**. This verifies anchor creation and its reported tracking state; repeat physical stability/surface-contact acceptance is still pending.

**Anchor revision acceptance: FAILED.** On the repeat trial, the user again reported **“Still sliding, floating, or following the phone.”** Runtime samples show changing camera positions and a mostly steady anchor position during SessionTracking; one sample shows Anchor Limited / SessionInitializing with reason Initializing, followed by Tracking recovery. This establishes that anchor attachment alone did not resolve the perceived instability. Camera projection/background handling, pose-driver bindings, and unit origin scales were inspected without establishing a configuration fault. Clarifying the observed movement and testing its relation to a fixed surface landmark remain necessary before choosing another correction.

The user clarified the failure: **“It moves across the screen, but slides relative to the floor landmark.”** Therefore, the remaining issue is physical landmark alignment during movement. Sampled poses alone do not establish the cause; a synchronized visual movement recording would help compare the visible slip against tracking-state changes. Stable placement remains unverified.

A second supplied screenshot sequence, **IMG_7556–IMG_7565**, was reviewed. IMG_7556–IMG_7560 show the pre-placement instructions and 2–3 horizontal planes; IMG_7561–IMG_7565 show the placed orange cube, **Anchor: Tracking**, **SessionTracking**, and 3–4 horizontal planes. Several views crop the cube at an image edge. No captured HUD frame shows limited tracking, but still frames cannot rule out intervening tracking changes or establish continuous landmark alignment. The sequence confirms the anchor revision is visibly running; it does not resolve the user-reported sliding or demonstrate stable floor contact. The images remain local.

## Recording review and clearer contact trial

Reviewed the supplied **ScreenRecording_10-08-2026 21-16-35_1.MP4** (approximately 11.53 seconds) using one-second frame samples. The cube appears between the 1- and 2-second samples, leaves view during a broad room sweep, and returns around 7–9 seconds. Sampled app HUD frames show SessionTracking and, after placement, Anchor Tracking; the plane count changes from 1 to 2. Several samples show motion blur. This does not establish a tracking-state failure during the visible slip or quantify floor alignment drift; the user’s reported instability remains unresolved. The video and extracted room frames are kept local.

A diagnostic presentation revision uses URP Lit for distinguishable cube faces and a green 22 cm footprint at approximately 2 mm above the estimated plane. Blue mesh overlays hide after placement and return on reset, while plane detection and anchor tracking continue. The HUD asks for a small sideways/back motion with the cube in view. These changes improve observation; they are not an established correction for AR registration drift. Compilation, fresh export, signing/deployment, and repeat acceptance are recorded separately.

## Polycam-inspired scan → surface → anchor trial

The user requested a scan-first interaction after supplying Polycam pictures. **IMG_7575–IMG_7577** show live room capture with a dense triangle overlay on floors and furniture. Other supplied Polycam pictures show settings/onboarding; they do not establish a completed scan, exported model, or Unity import. Polycam version and capture accuracy are not established. Images/account details remain local.

The second supplied recording, **ScreenRecording_10-08-2026 21-19-45_1.MP4** (approximately 23.19 seconds), was reviewed with sampled frames. It includes multiple pre-placement/placed states and room pans; these are separate placement trials, not one continuously tracked cube. Sampled app HUD frames show SessionTracking and, when placed, Anchor Tracking. This does not establish stable alignment or identify the cause of the reported sliding.

The clarity-only export **`Builds/iOS_PlanePlacement_20261008_212518`** compiled, built, and installed successfully, but was superseded before a device trial by the user's scan-first request. The new revision uses AR Mesh Manager and renders triangle edges from its live meshes. Placement waits for at least one generated mesh patch, then still requires a tracked upward horizontal-plane polygon hit and successful plane-anchor attachment. A patch count is not a whole-room completion or accuracy score. This is a geometry preview, without Polycam texturing, mesh classification, saved room models, or persistent anchors.

Unity compiled the scan-first scripts and successfully exported **`Builds/iOS_PlanePlacement_20261008_214232`**. Signing/deployment and visual/stability acceptance are recorded separately below. The shaded cube and green footprint remain observation aids, not a proven drift correction.

Unity documents that ARKit meshing requires LiDAR and that enabled plane detection can help flatten reconstructed mesh regions. Meshing and plane detection run together during scanning; the user selects the surface before the app creates an anchor. A triangle preview does not guarantee stable world registration. See [ARKit 6.3 meshing documentation](https://docs.unity3d.com/Packages/com.unity.xr.arkit@6.3/manual/arkit-meshing.html) and [AR Foundation meshing setup](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.3/manual/features/meshing.html).

The scan-first Xcode build reached codesigning without a recorded compile error. macOS opened a signing-key permission prompt; device installation/launch and rendering acceptance remain pending that user action.
