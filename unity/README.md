# Unity application starter

The tested **[LeadMills_AR_POC project](LeadMills_AR_POC/README.md)** is now included alongside the provider-neutral starter interfaces in `Assets/LeadMillsAR/Scripts/Core/`. Open the nested POC directory as the Unity project; these are separate projects, not one combined Assets tree.

The October 14, 2026 **0.7.0 build 16** milestone demonstrates the full-size textured coal shed outdoors on grass, following successful local tile-floor placement. Unity is pinned to **6000.3.25f1**, AR Foundation and ARKit to **6.3.5**. See [evidence and limitations](../docs/milestones/2026-10-14-full-size-coal-shed/README.md). Geographic registration, cross-launch persistence and Android parity remain pending. Configure your own signing team before exporting.

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
