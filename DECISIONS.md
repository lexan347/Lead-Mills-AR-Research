# Decision record

## D-001 — documentation-first repository

**Date:** 2026-10-06<br>
**Status:** Accepted

Follow the DronePi Research pattern: a navigable README, explicit decisions, weekly iterations, evidence indexes, and no unsupported completion claims.

## D-002 — one Unity project for Android and iOS

**Date:** 2026-10-06<br>
**Status:** Accepted

Ubuntu is the primary authoring and Android-build environment. macOS/Xcode performs final iOS compilation, signing, device installation, and distribution. Source moves through Git; generated caches and builds do not.

## D-003 — WebAR before custom application

**Date:** 2026-10-06<br>
**Status:** Accepted

Use a browser-based demonstration to validate communication value and mobile asset performance before committing the field study to a specific native positioning SDK. WebAR success is not evidence of precise geospatial alignment.

## D-004 — positioning-provider abstraction

**Date:** 2026-10-06<br>
**Status:** Accepted

Google, Niantic, GPS, and mock/local positioning will be isolated behind a shared provider interface. This reduces vendor lock-in and supports controlled comparison.

## D-005 — Niantic VPS as leading precision candidate

**Date:** 2026-10-06<br>
**Status:** Provisional

Niantic is the leading candidate because a custom site map can be created where preexisting Street View coverage or stable urban features may be insufficient. Acceptance depends on onsite mapping, localization performance, terms, costs, and device testing.

## D-006 — no precise public anchors before review

**Date:** 2026-10-06<br>
**Status:** Accepted

The public repository begins with null coordinates. Surveyed coordinates are published only after technical validation and review of site-owner, cultural-resource, safety, and privacy considerations.

## D-007 — pinned iPhone-first setup baseline

**Date:** 2026-10-06<br>
**Status:** Accepted for POC

Use Unity 6.3 LTS `6000.3.25f1` for Apple Silicon with iOS Build Support and the Universal 3D / URP template for `LeadMills_AR_POC`. First verify a minimal AR Foundation / ARKit camera session on the physical iPhone before importing historical models or adding geospatial SDKs. The longer-term shared Android/iOS project remains the objective; matching Linux editor and source/package-lock capture are pending. AR Foundation and Apple ARKit XR Plugin **6.3.5** were confirmed October 7. Do not upgrade the editor merely because a newer release is available; record any required baseline change.

## D-008 — direct physical-device signing for the first test

**Date:** 2026-10-06<br>
**Status:** Verified for initial POC (October 8)

Use Xcode automatic signing with the user's Personal Team and bundle ID `com.alexanderangulo.leadmillsarpoc` for the initial iPhone 14 Pro test. Provisioning/build/install/launch succeeded October 7 after device update and certificate trust. Camera permission and the live camera feed were confirmed October 8, completing the first camera POC gate; release distribution remains outside this result.

## D-009 — pause for the chosen iOS update

**Date:** 2026-10-06<br>
**Status:** Superseded by observed device version (October 7)

Wait for the physical iPhone 14 Pro to update from iOS 26.6.2 to 26.7.1, then resume device setup/deployment next session. The user is unsure whether iOS 27 is available; availability remains unresolved and no iOS 27 upgrade was selected. When work resumed October 7 at 00:52 EDT, the user confirmed **iOS 27.0.1**, which is now the actual device baseline. The preceding text preserves the October 6 decision rather than asserting 26.7.1 was installed.

## D-010 — verify the exported startup scene and use a fresh export

**Date:** 2026-10-08<br>
**Status:** Accepted; camera test successful

Enable only `Assets/Scenes/LeadMills_AR_POC.unity` in iOS Build Profiles and uncheck SampleScene. Save scene/project and export into a new folder such as `Builds/iOS_Diagnostic`, then open that export's Xcode project. The missing diagnostic marker and default sky/ground exposed the wrong startup scene; correcting it and making a fresh export yielded camera permission/live feed. Fresh output guards against stale generated files without claiming a separate stale-file defect was proven.

## D-011 — horizontal-plane placement before historical assets

**Date:** 2026-10-08<br>
**Status:** Accepted; implementation pending

With the native camera pipeline working, next detect a real horizontal surface and place one simple test object. Establish stable placement/tracking before importing Lead Mills site assets or integrating geospatial providers. This camera milestone does not close Android, WebAR, or field-validation gates.

## D-012 — session-space plane placement for the next POC

**Date:** 2026-10-08<br>
**Status:** Implemented locally; device acceptance pending

Use AR Plane Manager in horizontal mode and AR Raycast Manager on the existing XR Origin. Accept only hits inside an upward horizontal plane's polygon while it is tracking; place one orange 20 cm cube with its bottom at the hit surface. Keep placement in session coordinates under the trackables parent. Do not claim persistent/geospatial anchoring from this test. Visualize detected planes in blue, display tracking feedback, and provide a reset control. Read both placement and reset touches through the existing Input System-only configuration. Preserve a scene backup and use a fresh iOS development export for testing. Source/setup are in [the plane test guide](unity/PLANE_PLACEMENT_TEST.md).

## Open decisions

- License and any future visibility/ownership changes for the existing public `lexan347/Lead-Mills-AR-Research` repository.
- Matching Linux Unity `6000.3.25f1` baseline and source/package-lock capture; AR Foundation / ARKit 6.3.5 are session-confirmed.
- Google versus Niantic primary provider after onsite tests.
- Historical reconstruction source and uncertainty notation.
- License for original code, models, and documentation.
- Long-term hosting and offline behavior after graduation.
