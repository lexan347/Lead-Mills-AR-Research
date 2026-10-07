# Software baseline — October 6, 2026

Status: Mac iPhone POC toolchain configured; first physical-device deployment pending.

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
| AR Foundation / ARKit XR Plugin | Exact package versions not yet recorded | Installed/configured; ARKit enabled for iOS |
| XR Plug-in Management | Exact version not yet recorded | Initialize XR on Startup enabled; Project Validation: **0 issues** |

The first native test is iPhone-only. Additional Android/platform modules and positioning SDKs are not established as installed by this entry. Record `ProjectVersion.txt`, `Packages/manifest.json`, and `Packages/packages-lock.json` when the local project is prepared for source handoff.

## Physical test device

| Component | Recorded state at stopping point |
|---|---|
| Device | Physical iPhone 14 Pro, recognized in Xcode |
| iOS | Updating from 26.6.2 to 26.7.1; update completion not confirmed |
| Signing | Automatically manage signing; Personal Team; bundle ID `com.alexanderangulo.leadmillsarpoc` |
| Deployment | Paused during phone update; successful provisioning, install, launch, camera permission, and live AR camera feed not yet confirmed |

The user is unsure whether iOS 27 is available. Availability is unresolved; no iOS 27 upgrade was selected. The recorded decision is to wait for 26.7.1 and resume physical-device deployment next session. See the [iOS setup and resume checklist](../unity/IOS_SETUP.md).
