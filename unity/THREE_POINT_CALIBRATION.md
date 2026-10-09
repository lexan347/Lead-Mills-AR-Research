# Three-point floor calibration trial

Status: implemented; Unity math validation/export passed. Native deployment and physical calibration acceptance are recorded in the session checkpoint. This is a session-local experimental projection correction, not surveyed calibration or a persistent/geospatial anchor.

## On the phone

1. Keep **Portrait / CW90 / SRP ON**. Choose either Input System or XR camera and keep that mode throughout a trial. Both previously showed the same residual drift.
2. Scan one textured, level floor area. Choose three unambiguous physical marks **A, B, C**, spread widely in a triangle (for example three distinct rug corners/details). Remember their order. Do not use a line of points or changing objects.
3. Tap **3-point calibration**, then tap the real A, B and C marks in the live image. A must be on a qualified blue surface with local LiDAR support. The app collects the tracked camera pose and native projection for each tap; do not tap virtual triangle edges instead of the physical marks.
4. Move at least **25 cm sideways**, change your downward viewing angle substantially, and retap the same real A, B, C in order. A translation with almost unchanged angle may not contain enough calibration information. Follow the on-screen instruction if the fit is rejected.
5. The app temporarily previews a fitted common focal scale. Move/tilt to a **third view**, at least 25 cm from the second-view camera position, and retap the same real A, B, C. This independent check must pass before calibration is accepted. Candidate appearance alone is insufficient.
6. If the check passes, tap **Anchor at A**. This creates a native plane anchor at the reconstructed A point and places the 20 cm cube with its bottom on the estimated surface. It requires current plane tracking and local LiDAR support; rescan if A lacks coverage.
7. Walk/tilt against the physical A landmark for 30–60 seconds. Record cube contact, mesh registration before placement, displacement after stopping and the on-screen scale/error. A low pixel residual is a correspondence check, not a centimeter accuracy guarantee. Include a low-angle view to check the bottom/footprint against the real floor; this projection fit does not estimate independent floor height, and world Y=0 is not a physical floor datum.
8. **Clear / restart** restores the original projection and removes placement. Repeat with clearer/wider points or a stronger viewpoint change. App pause, tracking loss, a changed pose/render/view mode or more than 2 cm movement of the reference plane also clears calibration. Reopening the app starts a fresh scan and calibration.

## What is fitted

`FloorProjectionCalibration` reconstructs each first-view landmark by intersecting its camera ray with the selected provider floor plane. It varies one common focal multiplier on the native projection's `m00`/`m11` and minimizes second-view pixel reprojection error. Native principal point, depth terms, provider camera pose, world gravity, mesh geometry and CW90 are retained. Search range is 0.5–1.5; boundary solutions are rejected. It can address a common image/projection zoom mismatch; it cannot correct arbitrary tracking drift, lens distortion, pose offsets or an incorrect floor estimate.

The observed approximately 4/3 projection/intrinsics-crop ratio is a diagnostic lead, not a hard-coded 0.75 correction. Each trial fits its own value. There is no correction by default.

Acceptance guards: first/candidate triangle area at least 0.02 m²; 0.1–5 m floor ray distance; at least 25 cm translated repeat; both ±0.1 focal changes must worsen RMS error by at least 5 pixels (rejects weak/ambiguous data); fit RMS at most 12 pixels; at least 25% improvement over baseline when correcting. A baseline within 8 pixels keeps scale 1 after the observability check. Third-view RMS must be at most 15 pixels. These are POC heuristics requiring device validation.

## Verification and evidence

Unity executes synthetic checks before export: recover a known 0.75 focal scale, reconstruct known world points, pass an independent holdout, reject incorrect correspondences and a displaced landmark, preserve an already correct native projection, and reject weak-view calibration. Earlier synthetic weak geometry exposed the observability problem; the solver now rejects that case. Math success does not establish real-room stability.

Private device lines `[Lead Mills Calibration Test]` record the view/point sequence, trial times, fitted scale, baseline/fit/holdout residuals, rejection and anchor operations. `[Lead Mills Calibration]` preserves image calibration data; precise anchor logs distinguish provider pose changes from image-relative slipping. Keep raw room media/logs local. Publish only summarized outcomes in the daily log and checkpoint.

## Version, build and attempt IDs

New numbering starts with **v0.3.0 / build 0001**; older timestamp-only exports retain their original names. Edit `Assets/LeadMillsAR/TestConfiguration.json` to set the numeric version and test protocol/default configuration. Each export consumes the next local build number (including a failed export, so IDs are never reused), updates the app version/build fields and embeds a generated identity. The exporter reads `Builds/source-revision.txt` for the recorded Git source revision.

Example build directory: `Builds/LMAR_v0.3.0_b0001_<UTC timestamp>`; its manifest is `LMAR_v0.3.0_b0001_build-manifest.json`. Every successful start of a calibration trial advances the attempt counter, preserved across app restarts for that build. The app shows `LMAR_v0.3.0_b0001_a001`. Private phone records use that ID plus a UTC timestamp and `.json`; they include actual selected pose/view/SRP configuration, point observations, candidate/holdout errors and outcome. Records/counters use atomic file replacement. A passed correspondence test still records physical stability as unverified.

After reconnecting, copy private trial records from the phone app's `Documents/TestRuns` using `devicectl device copy from --domain-type appDataContainer --domain-identifier com.alexanderangulo.leadmillsarpoc --source Documents/TestRuns --destination <local-build-folder>/TestRuns --device <paired-device>`. Transfer success must be checked; room records stay outside this public repository.

iOS controls its original screen-recording filename. When the visible/logged ID confirms which trial produced a recording, use `python3 tools/register_trial_evidence.py --build-dir <local-build-folder> --test-id LMAR_v0.3.0_b0001_a001 --media <recording-or-photo>` from the repository. The helper creates a labeled local evidence file and a private mapping to the untouched original. It refuses a mismatched build ID or overwriting an existing evidence filename. Do not guess an old recording's attempt number.
