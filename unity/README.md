# Unity application starter

This directory contains provider-neutral interfaces and data objects, not the complete local Unity project. The Mac POC `LeadMills_AR_POC` was created October 6 in Universal 3D / URP with Unity 6.3 LTS `6000.3.25f1` Apple Silicon and iOS Build Support. AR Foundation **6.3.5** and Apple ARKit XR Plugin **6.3.5** are installed; ARKit is enabled and Project Validation has 0 issues.

The **first successful native AR POC** was confirmed October 8 on the physical iPhone 14 Pro / iOS **27.0.1**, using Personal Team signing and `com.alexanderangulo.leadmillsarpoc`. Camera permission and the live feed worked after selecting the actual AR scene instead of `SampleScene` and making a fresh iOS export. See [iOS setup and troubleshooting](IOS_SETUP.md) and [software baseline](../docs/SOFTWARE_BASELINE.md). Next: horizontal-plane detection and simple test-object placement before site-asset import. Local Unity source/package locks, matching Linux editor, and cross-platform builds remain pending.

## Horizontal-plane test implementation

The next gate is implemented as a source subset in the local POC: live cyan LiDAR triangles → blue horizontal-surface selection → plane anchor with a shaded orange 20 cm cube and green footprint. Reset restores scan overlays. Earlier device trials verify plane/cube rendering and anchor creation but failed stability relative to a floor landmark. The scan-first revision requires LiDAR and has verified scan rendering but failed physical floor alignment; a stronger level/coverage/stability/viewpoint gate is implemented before blue-plane selection. Verify this gate and diagnose physical mesh registration next. See the [test setup and acceptance procedure](PLANE_PLACEMENT_TEST.md).

## Planned project layout

```text
Assets/LeadMillsAR/
  Scripts/Core/          provider-neutral contracts and diagnostics
  Scripts/Providers/     GPS, Google, Niantic, and mock implementations
  Scenes/                bootstrap, diagnostics, and field experience
  Prefabs/               UI and anchor content
  Settings/              public-safe runtime configuration
Packages/                pinned Unity and SDK dependencies
ProjectSettings/         text-serialized project settings
```

## Cross-platform workflow

1. Establish the production project and verify Unity `6000.3.25f1` on both machines. The initial Mac iPhone POC exists locally; Ubuntu parity and source handoff remain pending.
2. Commit `Assets`, `Packages`, `ProjectSettings`, and all `.meta` files.
3. Build Android APK/AAB on Ubuntu.
4. Push source changes and pull them on the MacBook Pro.
5. Open with the exact same Unity version.
6. Switch to iOS and generate an Xcode project outside the repository.
7. Sign and install through Xcode or distribute through TestFlight.

Do not commit `Library`, `Temp`, `Logs`, `obj`, platform builds, credentials, keystores, provisioning profiles, or generated Xcode projects.

## Provider contract

`IGeospatialAnchorProvider` separates positioning from presentation. A provider must report tracking and accuracy, place a requested anchor, and stop cleanly. The application decides whether accuracy is sufficient to display the reconstruction.

## Definition of a baseline build

- permission and privacy notices;
- visible provider/tracking/accuracy diagnostics;
- anchor catalog loading;
- test model placement through the mock provider;
- structured session log export;
- Android and iOS builds from the same commit.

The current diagnostic revision adds `CameraRegistrationComparison.cs`: independently compare Input System versus available tracked XR camera pose, and SRP batching ON/OFF, using a fixed physical landmark. These controls test hypotheses; they are not an accepted alignment fix. See the comparison procedure in [the test guide](PLANE_PLACEMENT_TEST.md).
