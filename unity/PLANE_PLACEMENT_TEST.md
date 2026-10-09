# Horizontal-plane placement test

Status: DEVICE DETECTION / VISIBLE PLACEMENT VERIFIED; stability failed in the first trial. A plane-anchor revision is prepared for retesting.

## Setup and export

The source is in `Assets/LeadMillsAR/Scripts/HorizontalPlanePlacement.cs` and `Assets/LeadMillsAR/Scripts/Editor/PlanePlacementSetup.cs`. These are a source subset, not a complete Unity project. Use the established Universal 3D / URP POC, Unity **6000.3.25f1**, AR Foundation / Apple ARKit XR Plugin **6.3.5**, and Input System **1.20.0**.

1. Copy these scripts and their `.meta` files into the local project's matching Assets paths. Back up the scene before first setup.
2. Open `Assets/Scenes/LeadMills_AR_POC.unity` and wait for compilation.
3. Run **Lead Mills → Set Up Horizontal Plane Test**. This adds AR Plane Manager, AR Raycast Manager, and AR Anchor Manager to the existing XR Origin, requests horizontal planes, creates a blue translucent plane prefab and orange 20 cm cube prefab, assigns their materials/references, and saves the scene. It preserves the existing camera and mobile renderer.
4. Confirm the Scene List enables only the AR scene. Run **Lead Mills → Export Plane Test for iOS** for a fresh timestamped `Builds/iOS_PlanePlacement_*` development export. This export explicitly includes the AR scene and reports build failure rather than claiming success.
5. Open the new export's `Unity-iPhone.xcodeproj`, retain the existing Personal Team/bundle ID, select the physical iPhone, and build/run.

Generated materials/prefabs are created by the setup menu. The repository preserves the generating scripts; the complete local scene, Unity project settings/dependency locks, and generated build are not incorporated into the repository by this step.

## Current local build evidence

The saved scene was configured through the setup menu and the scripts compiled in Unity. The corrected development export `Builds/iOS_PlanePlacement_20261008_204644` succeeded. The Xcode Debug build succeeded after the user completed the macOS keychain prompt. The app was installed and launched on the physical iPhone 14 Pro at approximately 20:57 EDT. Runtime output showed the placement component starting and ARKit requesting horizontal Plane Tracking and Raycast, with no unsatisfied requested features. The captured runtime console subsequently recorded **two cube placements with one reset between them**, demonstrating accepted tracked-horizontal-plane raycasts and the placement/reset handlers on the device. Eight user-supplied screenshots (IMG_7545–IMG_7552) confirm live camera video, blue plane meshes, 4–5 horizontal planes with SessionTracking, and an orange cube visible across multiple views. The user subsequently reported that the cube slid, floated, or followed the phone. This fails the stability criterion; exact floor contact and the cause remain unresolved. The room screenshots and raw device logs are not published in this repository.

## On-device procedure

1. Use a well-lit, textured table or floor. Move the phone slowly over the surface until blue detected-plane geometry appears and the horizontal-plane count is greater than zero with SessionTracking.
2. Tap inside a blue surface. One **orange 20 cm cube** should appear, with its bottom resting at the tapped surface.
3. Move sideways and around the cube for 30–60 seconds. Record whether it stays in the same physical location, floats, drifts, or disappears.
4. Tap **Remove cube and place again** and place it once more. Confirm the reset action does not accidentally place another cube beneath the button.
5. Briefly point away and return to the surface. Record tracking state and recovery; do not label this precise spatial validation.

The app accepts only polygon raycast hits on an upward horizontal plane that is currently tracking and not subsumed. Taps on the status/reset overlay are ignored. Once placed, further surface taps do not move or duplicate the cube. The revised placement attaches an AR anchor to the hit plane and parents the cube to that anchor, with a local 0.10 m height offset. It refuses placement if plane attachment is unsupported or anchor creation fails. Reset removes the cube and requests anchor removal. The HUD reports anchor tracking, and five-second console diagnostics include anchor and camera poses plus session tracking reason. This is a session-local plane anchor; it is neither persistent nor geospatial. Restarting the app does not preserve the placement.

## Evidence to record

- Date, device, OS, Unity/AR package versions, export folder and source commit.
- Camera working; blue planes visible; plane count and session state.
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
