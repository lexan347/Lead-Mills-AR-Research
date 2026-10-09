# Horizontal-plane placement test

Status: IMPLEMENTED LOCALLY; physical-device acceptance pending. This advances the next gate after the October 8 camera POC; it does not claim successful plane detection or placement on the phone yet.

## Setup and export

The source is in `Assets/LeadMillsAR/Scripts/HorizontalPlanePlacement.cs` and `Assets/LeadMillsAR/Scripts/Editor/PlanePlacementSetup.cs`. These are a source subset, not a complete Unity project. Use the established Universal 3D / URP POC, Unity **6000.3.25f1**, AR Foundation / Apple ARKit XR Plugin **6.3.5**, and Input System **1.20.0**.

1. Copy these scripts and their `.meta` files into the local project's matching Assets paths. Back up the scene before first setup.
2. Open `Assets/Scenes/LeadMills_AR_POC.unity` and wait for compilation.
3. Run **Lead Mills → Set Up Horizontal Plane Test**. This adds AR Plane Manager and AR Raycast Manager to the existing XR Origin, requests horizontal planes, creates a blue translucent plane prefab and orange 20 cm cube prefab, assigns their materials/references, and saves the scene. It preserves the existing camera and mobile renderer.
4. Confirm the Scene List enables only the AR scene. Run **Lead Mills → Export Plane Test for iOS** for a fresh timestamped `Builds/iOS_PlanePlacement_*` development export. This export explicitly includes the AR scene and reports build failure rather than claiming success.
5. Open the new export's `Unity-iPhone.xcodeproj`, retain the existing Personal Team/bundle ID, select the physical iPhone, and build/run.

Generated materials/prefabs are created by the setup menu. The repository preserves the generating scripts; the complete local scene, Unity project settings/dependency locks, and generated build are not incorporated into the repository by this step.

## Current local build evidence

The saved scene was configured through the setup menu and the scripts compiled in Unity. The corrected development export `Builds/iOS_PlanePlacement_20261008_204644` succeeded. The Xcode Debug build succeeded after the user completed the macOS keychain prompt. The app was installed and launched on the physical iPhone 14 Pro at approximately 20:57 EDT. Runtime output showed the placement component starting and ARKit requesting horizontal Plane Tracking and Raycast, with no unsatisfied requested features. The detected surface, placed cube, stability, and reset still require the user’s physical-phone observations.

## On-device procedure

1. Use a well-lit, textured table or floor. Move the phone slowly over the surface until blue detected-plane geometry appears and the horizontal-plane count is greater than zero with SessionTracking.
2. Tap inside a blue surface. One **orange 20 cm cube** should appear, with its bottom resting at the tapped surface.
3. Move sideways and around the cube for 30–60 seconds. Record whether it stays in the same physical location, floats, drifts, or disappears.
4. Tap **Remove cube and place again** and place it once more. Confirm the reset action does not accidentally place another cube beneath the button.
5. Briefly point away and return to the surface. Record tracking state and recovery; do not label this precise spatial validation.

The app accepts only polygon raycast hits on an upward horizontal plane that is currently tracking and not subsumed. Taps on the status/reset overlay are ignored. Once placed, further surface taps do not move or duplicate the cube. Placement uses session coordinates under XR Origin's trackables parent; it does not create a persistent or geospatial anchor. Restarting the app does not preserve the placement.

## Evidence to record

- Date, device, OS, Unity/AR package versions, export folder and source commit.
- Camera working; blue planes visible; plane count and session state.
- Cube placement and bottom contact; observed stability over the defined movement.
- Reset/replacement and tracking-loss/recovery behavior; screenshot or short recording.
- Console lines starting `[Lead Mills Placement]`, and exact errors if any.

Only mark this gate complete after the physical-phone result is observed. Historical site assets, geospatial providers, Android, and field accuracy remain subsequent work.
