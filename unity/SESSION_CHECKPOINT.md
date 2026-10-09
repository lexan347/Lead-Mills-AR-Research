# Session recovery checkpoint

Updated: October 9, 2026. User verified CW90 portrait alignment and a cube staying in place; default build installed/launched; fresh trial confirms orientation fix but reports small mesh/cube drift. Older entries below preserve the diagnostic sequence.

## Saved state

- Repository branch: `plane-placement-poc`; draft PR [#1](https://github.com/lexan347/Lead-Mills-AR-Research/pull/1).
- Active implementation: single-handler mobile comparison controls (`4327d6c`), deployed in export `20261009_074548`; default portrait CW90 correction from `2af2717`. User-verified opt-in trial: `78dbaca` / `20261009_070358`. Earlier pose/render comparison: `6a4dd69`; readiness baseline: `6de57a9`.
- Last exported/installed build: `Builds/iOS_PlanePlacement_20261009_074548`, Debug, bundle `com.alexanderangulo.leadmillsarpoc`.
- Current build log: `touch-toggle-xcodebuild.log`; console log: `touch-toggle-device-20261009.log` in the latest export. Previous readiness/recovery logs are preserved in the October 8 export. These are local files outside Git.
- Source subset: `Assets/LeadMillsAR/Scripts/HorizontalPlanePlacement.cs`, `LiDARMeshPreview.cs` and `CameraRegistrationComparison.cs`; setup/export menu in `Scripts/Editor/PlanePlacementSetup.cs`.
- Exact local Unity editor/project paths and device identifier are saved in the laptop's private recovery note, outside synced `sources/` and outside the public repository.
- Baseline: Unity 6000.3.25f1, AR Foundation/ARKit 6.3.5, iPhone 14 Pro / iOS 27.0.1.
- Portrait orientation correction is user-verified. The subsequent default-build trial reports small shared mesh/cube drift with tilt/movement; steady physical registration remains open. Quantitative drift, exact contact, other orientations and field accuracy remain unmeasured.

## Resume in order

1. Read this checkpoint, the latest daily log and the latest acceptance section of `PLANE_PLACEMENT_TEST.md`. Inspect `git status` before editing; do not replace uncommitted work.
2. Verify Unity is on the correct local project and scene, Xcode is available, and `xcrun devicectl list devices` reports the intended physical phone connected. Resolve device trust/unlock only if needed.
3. Check whether the installed app is running before launching it. Reattach the console if possible; if a fresh launch is necessary, record it as a new session. A new app/session has no saved scan or anchor.
4. Preserve previous logs, then write a new timestamped device log. Device console process/session IDs are ephemeral: rediscover them after an interruption.
5. Keep portrait/CW90; diagnose residual shared mesh/cube drift using stationary hold, tilt and translation trials before site-asset import. Judge registration against physical landmarks, not just internal readiness/tracking flags.
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

Latest follow-up: reviewed the 06:37 recording (29.96 s), confirming readiness and placement but not physical alignment. Device Hub screen sharing is stopped; the app and wired connection remain available. Recovery log captured ARKit error 102 “Required sensor failed” with retry/reset, then resumed current frames/mesh acquisition; cause unknown. Orientation later sampled LandscapeRight. Investigate orientation/registration and that separate sensor-reset episode next.

## Active comparison work — October 9

Added `CameraRegistrationComparison.cs` on the AR camera at runtime, with independent pose-source and SRP-batching controls; no guessed rotation correction. Unity export `Builds/iOS_PlanePlacement_20261009_065219` succeeded; Xcode build underway. The previously installed readiness build remains the device baseline until installation is confirmed below. Follow the one-variable comparison procedure in `PLANE_PLACEMENT_TEST.md`. Preserve the new export's `comparison-xcodebuild.log` and forthcoming timestamped device log.

Latest active build: comparison export `20261009_065219` built/installed/launched successfully. Its console confirms ARKit/colorCamera available and matching baseline pose values (0°/0 m in samples). Pose Input System, batching ON, portrait at startup. Next pending user test: fixed-landmark slow pan with batching ON → OFF → ON, leaving pose unchanged. Comparison tool session 71973 is ephemeral; rediscover connection if interrupted. Physical registration remains failed until repeat evidence establishes otherwise.

Active follow-up: user suggests 90° clockwise mesh orientation. Added an opt-in common-view roll test (baseline/CW90/CCW90), with placement ray following the displayed view. Default baseline; prior cube removed on mode change. Unity export `20261009_070358` succeeded; native build/deployment pending. Keep earlier comparison logs. The user has not supplied a completed SRP-batching comparison result.

Latest device checkpoint: roll-test export `20261009_070358` built/installed/launched at 07:09 EDT. Startup Input System, SRP ON, roll 0°, Portrait, alternate ARKit/colorCamera matching. Pending user trial: tap view-rotation once for CW90, pan right/tilt down against a fixed landmark; assess mesh/plane then cube contact/stability. Current console tool session 87242 is ephemeral. No phone screen sharing enabled. Physical acceptance remains failed/pending repeat.

Latest user result October 9: CW90 fixes mesh/plane alignment; cube also stays in place. Current installed test export 20261009_070358 is user-verified in portrait/Input System/SRP ON/CW90. Promoted source default CW90 and explicit Portrait display; fresh export/build/install pending. Preserve earlier failure evidence. Next repeat default on launch and verify floor contact before importing one asset.

## Default correction deployed — October 9, approximately 07:28 EDT

Fresh export **`Builds/iOS_PlanePlacement_20261009_071607`** completed Xcode Debug build, installation and launch on the physical iPhone 14 Pro. Startup console confirms **Input System / SRP batching ON / view roll 90° / Portrait** without a manual mode change. Current frames and matching frame projection were received; the intentional render-time camera/input rotation difference is 90°. Private logs: `portrait-default-xcodebuild.log` and `portrait-default-device-20261009.log` in this export. This is a new AR session; prior scan/placement was cleared. No phone screen sharing was enabled.

The user’s earlier confirmation establishes physical alignment and a cube staying in place in the corrected portrait trial. Fresh-default 30–60-second landmark/contact confirmation is requested separately; runtime startup alone does not establish that repeat. Next, after that check, introduce one simple Lead Mills asset.

## Default-build movement refinement — October 9, 07:29 recording

Reviewed the private `07-29-40` recording, **33.18 s**, using five-second frame samples. CW90/Portrait, Input System and SRP batching ON remain active. Sample 0 shows 13 meshes and Ready 0/1; sample 10 shows Ready 1/1 and the blue surface. Samples 20–30 show the orange cube/green footprint with scan overlays hidden and Anchor Tracking. The recording supports automatic startup correction, qualification and anchored placement. It does not measure drift in centimeters or establish exact floor contact.

The user confirms the orientation fix, but reports that the registered mesh shifts away during tilt/movement and the cube shifts slightly too: “100 times better,” but still needing refinement to stay steady and fixed. This qualifies the earlier stable-cube confirmation: **orientation correction accepted; residual movement stability not accepted**. Keep CW90 and portrait. Do not claim the comparative phrase as a measured 100-fold accuracy improvement. Site-asset import stays behind the stability gate.

Code inspection confirms the cube is already parented to an ARAnchor attached to the hit plane; native mesh patches remain in the tracked world hierarchy. Both shifting suggests a shared tracking/image-registration contribution, but does not establish its cause. Internal tracking status and matching raw pose/projection cannot certify physical registration. A frozen transform or smoothed camera could conceal motion or add lag, so no speculative stabilization change is introduced by this review.

Next isolate a stationary hold, tilt with minimal translation, and sideways translation against the same floor landmark. Record whether displacement recovers when motion stops (possible timing/render registration) or persists (possible tracking/map refinement), then compare the existing Input System/XR camera controls with CW90 and batching unchanged. Switching pose clears placement and requires a fresh scan. These outcomes are hypotheses until tested. Keep room media and full device logs private.

Follow-up: the user confirms the mesh/cube **remain shifted when the phone is held still**. Residual drift is persistent in this trial, not reported as recovering after a few seconds. This does not by itself distinguish world-map drift, calibration error, plane refinement or render registration.

Inspection of the default-build console found two separate placements whose centimeter-rounded anchor positions changed about 1–2 cm vertically in sampled output. Later, a background/foreground transition was followed by SessionInitializing/Anchor Limited and a larger anchor-height change. The log has no synchronized recording timestamps, so neither event is established as the cause of the 07:29 video’s drift. Do not conflate separate placements or the later resume episode. Next instrumentation should record high-precision anchor pose deltas, session transitions and monotonic camera-frame times to correlate against recorded movement.

## Comparison controls: touch double-toggle — October 9

The user clarified the drift observation was in **Input System** mode, then reported that XR camera appeared only while the Pose button was held and reverted on release. Source inspection found two input routes for the same mobile button: Input System touch-down in `Update()` and IMGUI button click on release in `OnGUI()`. This can toggle pose twice during one tap. The same pattern affected SRP and view-registration controls, so prior intended mode comparisons must be verified from the persistent label/log, not the transient pressed label.

Corrected all three controls: mobile Input System touch-down remains the single action handler; IMGUI draws the buttons but only dispatches clicks on non-mobile platforms for desktop/editor mouse input. CW90 portrait remains the startup default. No world tracking/anchor correction is claimed by this input fix. Unity compiled/exported `Builds/iOS_PlanePlacement_20261009_074548`; native deployment and released-button verification are recorded separately.

Touch-control export **`20261009_074548`** completed Xcode Debug build, install and fresh launch on the iPhone at approximately **07:48 EDT**. Console confirms Input System baseline, available ARKit/colorCamera, SRP batching ON and CW90/Portrait. Old-build logs show repeated XR-camera/Input-System mode-change pairs, corroborating the duplicate-input bug. New private logs: `touch-toggle-xcodebuild.log`, `touch-toggle-device-20261009.log`. One released-button tap verification is pending from the user; physical drift remains open. No phone screen sharing was enabled.
