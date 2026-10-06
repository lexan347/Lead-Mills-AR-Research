# Unity application starter

This directory contains provider-neutral interfaces and data objects. It is not yet a complete Unity project and intentionally does not pin an editor or SDK version until the same supported version is installed and verified on Ubuntu and macOS.

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

1. Create/pin the production Unity project on Ubuntu.
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
