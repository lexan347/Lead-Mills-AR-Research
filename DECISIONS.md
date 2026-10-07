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

Use Unity 6.3 LTS `6000.3.25f1` for Apple Silicon with iOS Build Support and the Universal 3D / URP template for `LeadMills_AR_POC`. First verify a minimal AR Foundation / ARKit camera session on the physical iPhone before importing historical models or adding geospatial SDKs. The longer-term shared Android/iOS project remains the objective; matching Linux editor and package-version capture are pending. Do not upgrade the editor merely because a newer release is available; record any required baseline change.

## D-008 — direct physical-device signing for the first test

**Date:** 2026-10-06<br>
**Status:** Configured; deployment pending

Use Xcode automatic signing with the user's Personal Team and bundle ID `com.alexanderangulo.leadmillsarpoc` for the initial iPhone 14 Pro test. Recognition of the device and signing configuration do not establish successful provisioning or installation. The first acceptance gate is install, launch, camera permission, and a live AR camera feed without crashing.

## D-009 — pause for the chosen iOS update

**Date:** 2026-10-06<br>
**Status:** Accepted; update completion pending

Wait for the physical iPhone 14 Pro to update from iOS 26.6.2 to 26.7.1, then resume device setup/deployment next session. The user is unsure whether iOS 27 is available; availability remains unresolved and no iOS 27 upgrade was selected. Record the actual installed iOS version when work resumes.

## Open decisions

- License and any future visibility/ownership changes for the existing public `lexan347/Lead-Mills-AR-Research` repository.
- Matching Linux Unity `6000.3.25f1` baseline and exact Unity package versions.
- Google versus Niantic primary provider after onsite tests.
- Historical reconstruction source and uncertainty notation.
- License for original code, models, and documentation.
- Long-term hosting and offline behavior after graduation.
