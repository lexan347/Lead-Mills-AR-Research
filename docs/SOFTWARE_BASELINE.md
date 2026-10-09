# Software baseline — October 8, 2026

Status: first native AR POC verified on the physical iPhone 14 Pro; camera permission and live feed confirmed October 8.

This entry records the user's completed setup and the [Plan software downloads conversation](https://chatgpt.com/c/6ac57c84-40c0-83ea-b2bc-0246fbbf10f3). Versions are session records, not claims about the latest available releases. Raw inventories, screenshots, Unity project files, package locks, and generated builds are not archived in this repository by this update.

## Linux workstation

The Linux POC baseline inventory was reviewed and the environment was reported ready for the current iPhone-first POC. Linux remains the primary GIS/model-preparation and planned Android-build machine. Exact Linux OS, hardware, software versions, and matching Unity installation are not established by this entry; retain/recover the saved `lead_mills_software_inventory.txt` before claiming a reproducible cross-platform baseline. Android builds and the Ubuntu-to-Mac Unity handoff remain pending.

## MacBook Pro

The Mac inventory and storage audit/cleanup were completed before toolchain installation. No measured storage-recovery total or itemized deletion list is claimed here. The reviewed inventory identified an Apple Silicon M1 Pro MacBook Pro with 16 GB RAM and macOS 26.6.2. That macOS version is separate from the phone's iOS update.

| Component | Recorded baseline | Status |
|---|---|---|
| Xcode | 27.0, build 27A266a | Installed; active developer path `/Applications/Xcode.app/Contents/Developer` |
| Homebrew | 7.0.8 | Installed; shell PATH configured |
| Git LFS | 3.8.0 | Installed; `git lfs install` completed (initialized) |
| GitHub CLI | 2.102.0 | Installed; installation alone does not establish authentication |
| Unity Hub | 3.22.2 | Installed |
| Unity Editor | Unity 6.3 LTS, `6000.3.25f1`, Apple Silicon | Installed and selected for this POC |
| Unity module | iOS Build Support | Installed and loaded after editor restart |
| Unity project | `LeadMills_AR_POC`, Universal 3D / URP | Created locally on Mac |
| AR Foundation | 6.3.5 | Installed; version confirmed October 7 |
| Apple ARKit XR Plugin | 6.3.5 | Installed; version confirmed October 7; ARKit enabled for iOS |
| URP assets | `Mobile_RPAsset` / `Mobile_Renderer` | iOS quality pipeline and camera renderer configured; AR Background Renderer Feature added October 7 |
| XR Plug-in Management | Exact version not yet recorded | Initialize XR on Startup enabled; Project Validation: **0 issues** |

The first native test is iPhone-only. Additional Android/platform modules and positioning SDKs are not established as installed by this entry. Record `ProjectVersion.txt`, `Packages/manifest.json`, and `Packages/packages-lock.json` when the local project is prepared for source handoff.

## Physical test device

| Component | Recorded state as of October 8 |
|---|---|
| Device | Physical iPhone 14 Pro, selected in Xcode |
| iOS | **27.0.1**, user confirmed October 7 at 00:52 EDT; supersedes the October 6 planned 26.7.1 update |
| Signing | Automatically manage signing; Alexander Angulo (Personal Team); bundle ID `com.alexanderangulo.leadmillsarpoc`; developer certificate trusted |
| Deployment | Build/install/launch succeeded October 7; camera permission and live camera feed succeeded October 8 after Scene List correction and clean export |
| Startup scene | Only `Assets/Scenes/LeadMills_AR_POC.unity` enabled in iOS Build Profiles; `SampleScene` unchecked |
| Export procedure | New folder such as `Builds/iOS_Diagnostic`; open its generated `Unity-iPhone.xcodeproj` |

The earlier iOS 26.7.1 target is retained in the October 6 historical log; the actual installed device baseline is iOS 27.0.1. This is a session-recorded camera/AR pipeline success, not plane-placement, geospatial, or cross-platform validation. See the [iOS setup and troubleshooting](../unity/IOS_SETUP.md).

## Phone scanning reference

Polycam is present on the iPhone: October 8 user pictures show live room capture with a triangle mesh overlay. Its exact version/license and a completed scan/export are not established. It is an interaction reference for the local Unity LiDAR scan-first trial, not a Unity dependency or imported room asset. The Unity trial uses the existing AR Foundation / ARKit 6.3.5 baseline; mesh-preview rendering is verified; physical alignment failed.
