# Floor calibration: 1–3 point trials

Status: implemented; Unity math validation/export passed. Native deployment and physical calibration acceptance are recorded in the session checkpoint. This is a session-local experimental projection correction, not surveyed calibration or a persistent/geospatial anchor.

## On the phone (v0.4.0)

1. Keep Portrait / CW90 / SRP ON and the same pose mode throughout a trial. Select **1 point**, **2 points** or **3 points**; changing count clears the current trial. One point is the simpler default; fewer points provide fewer spatial cross-checks.
2. Choose that many distinct **physical floor details**, such as recognizable corners of a pattern. Remember A/B/C order. Two points must be at least 25 cm apart. Three must form a wide triangle. A virtual colored circle is an estimated reference and is not a calibration target.
3. Press **Start**, then tap the real details. View 1 accepted taps create red/yellow/bright-green circles. The app pauses after all selected points are captured. Inspect the circles and press **Undo last** to remove/replace the last accepted point, or **Continue** when satisfied.
4. For View 2, move at least 25 cm sideways and change downward angle. Retap the **same PHYSICAL details**, not the colored circles. Accepted taps create separate purple/orange/bright-blue rings. Inspect them, use Undo if needed, then Continue to fit. Live movement/tilt/distance hints remain available; they do not guarantee a valid fit.
5. A failed fit keeps View 1 and shows the specific reason. Undo can replace the last repeat point; tapping a fresh A begins a new repeat set. Weak-view rejection requests 40 cm movement and suggests 25 degrees tilt change. If careful physical correspondences still fail, investigate the model/tracking rather than blindly retrying.
6. A candidate previews the fitted virtual projection. Move to a third viewpoint (at least 25 cm from View 2), retap the same real details, inspect and Continue for the independent check. If rejected, native projection is restored; Undo reopens the last point for replacement and restores the temporary candidate preview, or Clear abandons the trial. A passed check enables **Anchor A**.
7. Undo also works across completed views and candidate/verified transitions. It removes the last accepted observation, reopens its original view/index, clears later results/placement and rebuilds remaining marker anchors. Multiple Undo presses step backward. Undo/replacement events are logged; replay must honor them instead of simply taking the first N point events.
8. After anchoring, walk/tilt for 30–60 seconds and check real-floor contact from a low angle. A low pixel residual is not a centimeter accuracy guarantee. The estimated plane is not a surveyed floor datum; world Y=0 is not the physical floor.
9. Clear, tracking/plane/mode changes or app pause invalidate the session calibration. Reopening starts a fresh scan. Attempt numbering and records persist separately from session-local geometry.

## What is fitted

`FloorProjectionCalibration` reconstructs each first-view landmark by intersecting its camera ray with the selected provider floor plane. It varies one common focal multiplier on the native projection's `m00`/`m11` and minimizes second-view pixel reprojection error. Native principal point, depth terms, provider camera pose, world gravity, mesh geometry and CW90 are retained. Search range is 0.5–1.5; boundary solutions are rejected. It can address a common image/projection zoom mismatch; it cannot correct arbitrary tracking drift, lens distortion, pose offsets or an incorrect floor estimate.

The observed approximately 4/3 projection/intrinsics-crop ratio is a diagnostic lead, not a hard-coded 0.75 correction. Each trial fits its own value. There is no correction by default.

Acceptance guards: first/candidate triangle area at least 0.02 m²; 0.1–5 m floor ray distance; at least 25 cm translated repeat; both ±0.1 focal changes must worsen RMS error by at least 5 pixels (rejects weak/ambiguous data); fit RMS at most 12 pixels; at least 25% improvement over baseline when correcting. A baseline within 8 pixels keeps scale 1 after the observability check. Third-view RMS must be at most 15 pixels. These are POC heuristics requiring device validation.

## Verification and evidence

Unity executes synthetic checks before export: recover a known 0.75 focal scale, reconstruct known world points, pass an independent holdout, reject incorrect correspondences and a displaced landmark, preserve an already correct native projection, and reject weak-view calibration. Reduced one-/two-point fits and independent holdout/displaced-landmark rejection are also checked. Earlier synthetic weak geometry exposed the observability problem; the solver now rejects that case. Math success does not establish real-room stability.

Private device lines `[Lead Mills Calibration Test]` record the view/point sequence, trial times, fitted scale, baseline/fit/holdout residuals, rejection and anchor operations. `[Lead Mills Calibration]` preserves image calibration data; precise anchor logs distinguish provider pose changes from image-relative slipping. Keep raw room media/logs local. Publish only summarized outcomes in the daily log and checkpoint.

## Version, build and attempt IDs

New numbering starts with **v0.3.0 / build 0001**; older timestamp-only exports retain their original names. Edit `Assets/LeadMillsAR/TestConfiguration.json` to set the numeric version and test protocol/default configuration. Each export consumes the next local build number (including a failed export, so IDs are never reused), updates the app version/build fields and embeds a generated identity. The exporter reads `Builds/source-revision.txt` for the recorded Git source revision.

Example build directory: `Builds/LMAR_v0.3.0_b0001_<UTC timestamp>`; its manifest is `LMAR_v0.3.0_b0001_build-manifest.json`. Every successful start of a calibration trial advances the attempt counter, preserved across app restarts for that build. The app shows `LMAR_v0.3.0_b0001_a001`. Private phone records use that ID plus a UTC timestamp and `.json`; they include actual selected pose/view/SRP configuration, point observations, candidate/holdout errors and outcome. Records/counters use atomic file replacement. A passed correspondence test still records physical stability as unverified.

After reconnecting, copy private trial records from the phone app's `Documents/TestRuns` using `devicectl device copy from --domain-type appDataContainer --domain-identifier com.alexanderangulo.leadmillsarpoc --source Documents/TestRuns --destination <local-build-folder>/TestRuns --device <paired-device>`. Transfer success must be checked; room records stay outside this public repository.

iOS controls its original screen-recording filename. When the visible/logged ID confirms which trial produced a recording, use `python3 tools/register_trial_evidence.py --build-dir <local-build-folder> --test-id LMAR_v0.3.0_b0001_a001 --media <recording-or-photo>` from the repository. The helper creates a labeled local evidence file and a private mapping to the untouched original. It refuses a mismatched build ID or overwriting an existing evidence filename. Do not guess an old recording's attempt number.

## Specific fit feedback (v0.3.2)

Records now include the exact rejection class, fitted/baseline RMS, per-point residuals, scale sensitivity and triangle area. Touch UVs use six decimal places, with pixel dimensions and reference floor equation recorded for replay. Invalid rays, excessive residual, scale bounds, weak perspective, insufficient improvement and small triangles receive distinct advice. Residual disagreement takes message priority over simultaneous scale-boundary/weak-view symptoms. Numerical acceptance thresholds remain in place; retrying does not apply a rejected correction.

## Camera framing and correspondence clarification

User clarified that earlier repeats tapped the colored circles. Those trials are not independent physical-landmark calibration evidence and cannot establish the proposed zoom/floor-height cause. The installed ARKit background shader samples the camera through the provider display transform; this revision preserves that live-image mapping. The fitted focal scale affects virtual projection, not optical camera zoom. Start with one physical detail to avoid requiring a whole wide triangle in the image. A wider live camera view would require its own background/projection-consistent implementation and validation.
