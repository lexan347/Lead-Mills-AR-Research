# Working iPhone POC — full-size coal shed milestone

Snapshot of the tested local Unity project, October 14, 2026. Unity **6000.3.25f1**, AR Foundation and ARKit XR Plugin **6.3.5**; URP **17.3.0**. Device milestone: **0.7.0 build 16**. Android support has not been tested.

Open this directory as a Unity project with that editor and iOS Build Support. Use `Assets/Scenes/LeadMills_AR_POC.unity`; preserve the saved scene and prepared prefab. Install Git LFS and run `git lfs pull` before opening. The FBX and four textures are included; see their bundled source documentation for reconstruction assumptions.

Select your own Apple development team in Player Settings and a usable bundle identifier. Export with **Lead Mills → Export Coal Shed for iOS**, then build/sign/install the generated Xcode project outside Git. Builds are development installs, not an App Store release. Do not rerun setup tools indiscriminately: they can regenerate scene/prefab configuration.

The public copy differs from the tested source only by clearing the personal signing-team setting and removing the export helper's hard-coded team assignment. The live project is untouched. No certificates/profiles, Library, Builds, Logs, or private device captures are included.

The stored build identity describes the tested app; its inherited sourceRevision is not this release commit. Future exports create a new identity. To continue numbering beyond build 16, initialize ignored `Builds/lead-mills-build-sequence.json` to `{"buildNumber":16}` and write the actual source commit to ignored `Builds/source-revision.txt` before exporting. These are local build records, not runtime GIS coordinates.

[Milestone evidence and limits](../../docs/milestones/2026-10-14-full-size-coal-shed/README.md). The current ARAnchor is session-local; GIS coordinates and persistence are not implemented.
