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

### Safe unplug between sessions

1. Finish the movement trial and save the recording/observations. Commit changed source and update this checkpoint before pausing agent work.
2. Finish any build/install first. For a planned break, end the test session deliberately, then unplug. If Xcode owns the debug session, its Stop button ends the app; launch the installed app again from the phone's Home Screen when needed.
3. For untethered scanning, launch the installed app from the Home Screen, or launch it with `xcrun devicectl device process launch --device <device> com.alexanderangulo.leadmillsarpoc` **without `--console`**. The app then runs independently of a host console. Unplugging does not uninstall it or delete the Unity project.
4. The current `devicectl --console` capture forwards catchable signals to the app: do not use Ctrl-C assuming it only detaches. Deliberately quit the phone app/end that test first, let the console end, and relaunch independently for a new untethered trial. A USB transport loss may end log capture; it is not a reason to delete/rebuild the project.
5. On reconnect, unlock if necessary, confirm the device is connected, inspect whether the app is running and resume from the saved build/notes. Record any app relaunch as a new AR session. This POC does not preserve scans or anchors across app restarts; preserving a live scan through arbitrary interruptions is not implemented.

### Optional wireless development

Apple supports running on a paired physical device over Wi-Fi. Keep Mac/phone on the same compatible network; Apple's current Device Hub guidance calls for IPv6-enabled Wi-Fi. Verify the phone remains available after removing the cable before relying on wireless build/install/log capture. Current verification is wired only; no wireless success is claimed. Xcode 27 uses Device Hub, so do not assume an older “Connect via network” checkbox exists. Reconnect the cable if discovery fails; do not unpair or weaken security as a workaround.

See [Apple Device Hub guidance](https://developer.apple.com/documentation/xcode/managing-your-simulated-and-physical-devices-in-device-hub) and [running an app on a wireless device](https://help.apple.com/xcode/mac/current/en.lproj/dev3e2f4ee6d.html).

## Latest recovery verification

October 9: GitHub authentication and push verified; Unity retained the correct scene; latest generated Xcode project opened; phone paired/Developer Mode/wired-connected. Installed app relaunched as a new session and logging restored to `recovery-device-20261009_0637.log`. Runtime again confirms mesh acquisition and plane-anchor placement. Original room registration failure remains unresolved. An old Xcode memory-termination alert was observed; date/build association unknown. Continue diagnosis from the 22:43 recording review, not from an assumption that the readiness gate fixed alignment.
