# Unity application starter

This directory contains provider-neutral interfaces and data objects, not the complete local Unity project. On October 6, the Mac POC `LeadMills_AR_POC` was created in Universal 3D / URP using Unity 6.3 LTS `6000.3.25f1` for Apple Silicon with iOS Build Support. AR Foundation / ARKit are configured, Project Validation has 0 issues, and the minimal AR scene exported successfully to Xcode with no Unity build errors.

Personal Team signing and bundle ID `com.alexanderangulo.leadmillsarpoc` are configured. The physical iPhone 14 Pro is recognized; deployment is paused while it updates from iOS 26.6.2 to 26.7.1. First on-device launch remains pending. See [iOS setup and resume checklist](IOS_SETUP.md) and [software baseline](../docs/SOFTWARE_BASELINE.md). Exact AR package versions, matching Linux editor installation, and cross-platform builds remain unconfirmed.

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
