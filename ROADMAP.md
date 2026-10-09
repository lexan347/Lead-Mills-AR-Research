# Programmatic roadmap

Dates are planning targets, not claims of completed work. Each phase closes only when its acceptance evidence is stored in the repository or linked research archive.

## Phase 0 — repository and governance

**Objective:** establish reproducible source control, documentation, privacy boundaries, and cross-platform handoff.

- Create the repository, workflows, Pages dashboard, decision log, and iteration templates.
- Confirm GitHub ownership and visibility.
- Select a license or explicitly retain all rights.
- Install the same Unity editor version on Ubuntu and macOS before opening a production project.

**Exit evidence:** validation workflow passes; Pages deploys; Ubuntu-to-macOS clone test is documented.

**October 8 status:** public repository and dashboard publication recorded. Linux POC inventory reviewed and Mac inventory/cleanup completed. The Mac toolchain and exact POC editor are recorded in the [software baseline](docs/SOFTWARE_BASELINE.md); matching Ubuntu editor and clone/project-opening evidence are still pending.

## Phase 1 — browser demonstration

**Objective:** show one lightweight 3D object at or near Lead Mills without an installed application.

- Prepare a small GLB test asset with meter units and a documented forward direction.
- Configure a GeoCAST/WebAR scene using provisional coordinates.
- Test on at least one iPhone and one Android phone.
- Record load time, device/OS/browser, approximate placement behavior, screenshots, and limitations.

**Exit evidence:** both device classes render the same asset onsite or in an approved surrogate location. This phase does not claim precise alignment.

## Phase 2 — Unity cross-platform baseline

**Objective:** produce one source project with Android and iOS builds.

- Pin the Unity editor, AR Foundation, Android, and iOS package versions.
- Implement camera permissions, a model viewer, diagnostics, and a mock/local anchor provider.
- Build Android on Ubuntu.
- Generate the iOS Xcode project and sign it on macOS.
- Record build manifests and device compatibility results.

**Exit evidence:** identical anchor catalog and test asset render on both platforms.

**October 8 progress (phase remains open):** the Mac baseline is Unity 6.3 LTS `6000.3.25f1` Apple Silicon with iOS Build Support, AR Foundation **6.3.5**, and Apple ARKit XR Plugin **6.3.5**. `LeadMills_AR_POC` (URP) has ARKit enabled and 0 Project Validation issues. Personal Team signing uses `com.alexanderangulo.leadmillsarpoc`. The physical iPhone 14 Pro updated to **iOS 27.0.1**; the October 7 install/launch worked but displayed the template sky/ground. On October 8, selecting only `Scenes/LeadMills_AR_POC` in the iOS Scene List and exporting to a fresh folder produced a camera permission prompt and working live camera feed: the **first successful native AR proof-of-concept milestone**. See the [October 7 log](iterations/2026-10-04/Daily_Log_2026-10-07.md) and [October 8 log](iterations/2026-10-04/Daily_Log_2026-10-08.md).

**Next gate:** detect a real horizontal surface and place one simple test object. Device screenshots verify horizontal-plane/cube rendering and runtime output verifies plane-anchor creation, but both initial and anchor trials failed physical stability. Following the user’s Polycam reference, the local implementation now adds live LiDAR triangles before horizontal-surface selection and anchor creation, with a shaded 20 cm cube and virtual contact outline. The scan-first recording verifies visible triangles/cube/footprint, but the user reports the mesh and plane fail to align with the floor. The new readiness gate requires level, local LiDAR coverage, three seconds of stable plane geometry and a 15 cm viewpoint change before revealing/selecting blue surfaces. Readiness interaction now passes, while physical registration remains failed. Diagnose the shared camera/geometry path using independent camera-pose and SRP-batching comparisons; physical stability remains unaccepted. See the [test guide](unity/PLANE_PLACEMENT_TEST.md). Then verify placement/tracking before importing Lead Mills site assets. Capture the local project source, package locks, build manifest, and test evidence for reproducibility. Matching Linux editor, Android build, anchor catalog/model rendering on both platforms, and the WebAR demonstration remain pending. The working iPhone camera feed does not close the cross-platform acceptance gate.

## Phase 3 — geospatial-provider integration

**Objective:** keep application behavior independent of a single positioning vendor.

- Implement the provider interface for Google ARCore Geospatial or Niantic Spatial VPS2.
- Retain a GPS/coarse provider for comparison.
- Expose accuracy, tracking state, localization duration, and failure reason in the UI and logs.
- Display content only after configurable accuracy thresholds are satisfied.

**Exit evidence:** provider can localize, place a test object, lose tracking safely, and recover without silently reporting false precision.

## Phase 4 — site mapping and historical asset pipeline

**Objective:** connect the mobile model to defensible real-world control.

- Identify durable visual features and approved scanning paths.
- Establish a surveyed origin, elevation convention, and true/grid-north relationship.
- Record the transformation from the research model coordinate system to WGS84/East-Up-North.
- Produce decimated, textured mobile LODs while preserving the research master separately.
- Record provenance and confidence for reconstructed components.

**Exit evidence:** asset manifest contains source checksum, derivative checksum, units, origin, heading, polygon count, texture sizes, and conversion history.

## Phase 5 — field-validation study

**Objective:** quantify performance rather than relying on visual impressions.

Minimum metrics:

- horizontal and vertical position error;
- heading error;
- time to first coarse and precise localization;
- drift over a defined walking route;
- repeatability across launches and devices;
- failure/recovery rate;
- lighting, weather, foliage, tide, and network conditions.

Compare at least GPS-only, VPS, and a surveyed visual-marker/reference method where permission allows.

**Exit evidence:** preregistered procedure, raw observations, analysis script, results table, and limitations.

## Phase 6 — interpretive experience

**Objective:** transform the validated placement system into a responsible historical interpretation.

- Add narration, labels, time layers, accessibility features, and safety guidance.
- Separate documented facts from hypothetical reconstruction.
- Add offline/failure messaging and a non-AR alternative.
- Obtain site-owner and institutional approvals before public field deployment.

**Exit evidence:** reviewed content, accessibility check, privacy notice, field-safety review, and stakeholder approval.

## Phase 7 — release and preservation

**Objective:** deliver a reproducible academic artifact.

- Publish Android and iOS beta builds or a documented installation package.
- Archive code commit, dependency lock files, models, metadata, results, and build manifests.
- Publish the GitHub Pages dashboard and final report links.
- Document service dependencies and a migration/offline plan.

**Exit evidence:** tagged release, archived research package, deployment instructions, and final limitations statement.
