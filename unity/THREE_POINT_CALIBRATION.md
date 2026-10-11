# Floor calibration: 1–3 point trials

Status: implemented; Unity math validation/export passed. Native deployment and physical calibration acceptance are recorded in the session checkpoint. This is a session-local experimental projection correction, not surveyed calibration or a persistent/geospatial anchor.

## Compact controls (v0.4.3)

The Settings button expands pose/SRP/rotation controls before a trial; these controls close and lock while a trial is active. Choose1/2/3 points before Start; Clear returns to point selection. The bottom strip retains the build/attempt ID, current message and one priority movement cue. Guide expands full instructions; Less restores the compact view. Clear and Undo stay available; the third action changes from Continue to Anchor A to Confirm stable, then Stable saved. After anchoring, Undo becomes Reject. Hidden panels and point selectors no longer block camera-space taps. The top bar shows tracking/floor readiness or anchor state. Development console and render-debug overlays are hidden; logs remain available for diagnosis. This change does not repair the existing native-marker lifecycle risk or establish lower thermal load.

## On the phone (v0.4.3)

1. Keep Portrait / CW90 / SRP ON and the same pose mode throughout a trial. Select **1 point**, **2 points** or **3 points**; changing count clears the current trial. One point is the simpler default; fewer points provide fewer spatial cross-checks.
2. Choose that many distinct **physical floor details**, such as recognizable corners of a pattern. Remember A/B/C order. Two points must be at least 25 cm apart. Three must form a wide triangle. A virtual colored circle is an estimated reference and is not a calibration target.
3. Press **Start**, then tap the real details. View 1 accepted taps create red/yellow/bright-green circles. The app pauses after all selected points are captured. Inspect the circles and press **Undo last** to remove/replace the last accepted point, or **Continue** when satisfied.
4. For View 2, move at least 25 cm sideways and change downward angle. Retap the **same PHYSICAL details**, not the colored circles. Accepted taps create separate purple/orange/bright-blue rings. Inspect them, use Undo if needed, then Continue to fit. Live movement/tilt/distance hints remain available; they do not guarantee a valid fit.
5. A failed fit keeps View 1 and shows the specific reason. Undo can replace the last repeat point; tapping a fresh A begins a new repeat set. Weak-view rejection requests 40 cm movement and suggests 25 degrees tilt change. If careful physical correspondences still fail, investigate the model/tracking rather than blindly retrying.
6. A candidate previews the fitted virtual projection. Move to a third viewpoint (at least 25 cm from View 2), retap the same real details. Test 3 creates white/hot-pink/aqua rings labeled 3A/3B/3C (only the selected point count). Compare 1A/2A/3A against physical A, and likewise B/C; Undo replaces the latest circle. Inspect and Continue for the independent check. If rejected, native projection is restored; Undo reopens the last point for replacement and restores the temporary candidate preview, or Clear abandons the trial. A passed check enables **Anchor A**.
7. Undo also works across completed views and candidate/verified transitions. It removes the last accepted observation, reopens its original view/index, clears later results/placement and rebuilds remaining marker anchors. Multiple Undo presses step backward. Undo/replacement events are logged; replay must honor them instead of simply taking the first N point events.
8. After anchoring, walk/tilt for 30–60 seconds and check real-floor contact from a low angle. Press **Confirm stable** to save your acceptance and keep the cube visible. Press **Reject** if it slips/floats; failure is saved and placement removed for a new Start/retry. After confirmation, **Stable saved** shows completion; you can still Reject later or Clear for another trial. Your physical verdict survives subsequent session invalidation in the saved record. A low pixel residual is not a centimeter accuracy guarantee. The estimated plane is not a surveyed floor datum; world Y=0 is not the physical floor.
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

## Proposed coarse-to-fine interaction

Build0004/a004 reached a recorded independent-view pass and Anchor A after repeated retries. To reduce manual tap difficulty, the next proposed interface separates coarse floor/framing acquisition from fine landmark placement: dim/hide the mesh, magnify the camera detail, adjust a crosshair, then confirm or Undo. The magnifier must map input back to the original camera viewport, and trial results must retain independent-view validation. This proposal is not implemented; a passed pixel check does not establish physical anchor stability.

Test 1/2/3 ring diameters are5/7/9cm, with separated labels to distinguish agreeing observations. Circles visualize accepted projected taps; their overlap alone is not independent proof of physical accuracy. Third-view rings participate in Undo/rebuild/cleanup just like the earlier sets.

## Completed physical verdicts and resets (v0.4.2)

Saved records include a separate `physicalStability` verdict and timestamped decision event. Later invalidations preserve an existing verdict and record their specific cause; earlier build records remain unchanged. After placement, original-plane refinement alone does not clear a still-tracked native anchor. Session/camera failure or anchor loss still invalidates placement; confirmation is a user observation, not a tracking override or surveyed accuracy certification.

## Successful three-point indoor POC — October 11, build0007/a002

User reports success and authorizes finalization. Reviewed six screenshots IMG_7697–7702; all show LMAR_v0.4.3_b0007_a002, Anchor Tracking, Stable saved and the retained orange cube/green footprint plus all Test1/2/3 A/B/C labels from close, oblique and wider viewpoints. The compact camera-space interface is visible without the former diagnostic overlays. Retrieved the matching private trial record and registered the six originals against the exact attempt ID.

Three real landmarks across three views: two rejected fits (21.39px and14.90px) preceded candidate focal scale0.67898, fit RMS11.91px versus native139.56px. Independent third-view holdout RMS13.91px passed the15px gate. Anchor A succeeded15:56:16 EDT; user-confirmed-stable was recorded15:57:55,99.2seconds after anchoring. App pause15:59:06 cleared live geometry while preserving outcome and physicalStability=user-confirmed-stable. Earlier build0007/a001 failed fit14.41px and was manually cleared; preserve it as a failed attempt rather than counting it as success.

Milestone accepted: three-point calibration → native anchor at A → user-confirmed stable indoor placement in the tested iPhone14Pro portrait/CW90/Input System/SRP ON configuration. Evidence combines the user's observation, six accepted-state views and persisted verdict. Pixel residuals are correspondence checks, not centimeter accuracy. This is one successful session-local indoor trial, not persistent/geospatial anchoring, surveyed floor height, repeatability across launches/devices or onsite field validation.

Installed baseline0.4.3/build0007, source12b4b09. No new source changes or reinstall required to process this success. Raw room media, precise pose records and device identifiers remain private. Existing managed-marker cleanup risk and thermal/performance refinement remain open; hiding debug overlays is not an exception repair. Next milestone: one small provenance-documented Lead Mills asset at the tested local anchor, with units/scale/origin recorded. Complete source/package-lock handoff and repeat stability testing alongside that work; geospatial/cross-platform/field acceptance stays open.
