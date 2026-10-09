# Session recovery checkpoint

Updated: October 9, 2026, approximately 06:34 EDT. This is a resume guide, not an assertion that physical AR placement is accepted.

## Saved state

- Repository branch: `plane-placement-poc`; draft PR [#1](https://github.com/lexan347/Lead-Mills-AR-Research/pull/1).
- Last implementation: `6de57a9`, measured surface-readiness gate plus XR center-eye diagnostic.
- Last exported/installed build: `Builds/iOS_PlanePlacement_20261008_223833`, Debug, bundle `com.alexanderangulo.leadmillsarpoc`.
- Build log: that export's `readiness-xcodebuild.log`; console log: `readiness-device.log`. These are local files outside Git.
- Source subset: `Assets/LeadMillsAR/Scripts/HorizontalPlanePlacement.cs` and `LiDARMeshPreview.cs`; setup/export menu in `Scripts/Editor/PlanePlacementSetup.cs`.
- Exact local Unity editor/project paths and device identifier are saved in the laptop's private recovery note, outside synced `sources/` and outside the public repository.
- Baseline: Unity 6000.3.25f1, AR Foundation/ARKit 6.3.5, iPhone 14 Pro / iOS 27.0.1.
- Build, install, launch and mesh acquisition verified; physical registration/stable placement FAILED. The readiness gate allowed the user's latest placement but did not fix registration.

## Resume in order

1. Read this checkpoint, the latest daily log and the latest acceptance section of `PLANE_PLACEMENT_TEST.md`. Inspect `git status` before editing; do not replace uncommitted work.
2. Verify Unity is on the correct local project and scene, Xcode is available, and `xcrun devicectl list devices` reports the intended physical phone connected. Resolve device trust/unlock only if needed.
3. Check whether the installed app is running before launching it. Reattach the console if possible; if a fresh launch is necessary, record it as a new session. A new app/session has no saved scan or anchor.
4. Preserve previous logs, then write a new timestamped device log. Device console process/session IDs are ephemeral: rediscover them after an interruption.
5. Continue camera/display/world-registration diagnosis. Do not accept an anchor merely because internal tracking, plane normals or readiness checks pass.
6. Update the daily log and this checkpoint at significant build/deploy/test changes, commit/push the source and observations, and keep the PR draft until physical acceptance passes.

## Interruption boundaries

An agent credit reset does not undo files or commits. Unplugging ends a wired development connection and may end console/debugger access. The development app is installed on the phone; its scanning does not need laptop computation. This POC has session-local geometry and anchors: force-quitting/relaunching it loses that scan/placement. Keep private room recordings and full device logs local.

The safe disconnect and optional wireless workflow will be added after checking current Xcode device state and official guidance.
