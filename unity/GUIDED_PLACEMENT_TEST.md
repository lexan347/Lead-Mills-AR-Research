# Guided placement prototype — v0.6.0

## Phone flow

Start in **Simple** mode. Point the center crosshair at a textured, level floor spot, in good light. Move slowly sideways, then hold the footprint steady. The1.2×0.85m footprint appears amber while collecting evidence and green when ready. Press **Place** once. NoA/B/C landmark taps or manual fitting are required in this mode. The vending machine faces the viewer initially and stays attached to a native plane anchor.

Walk around and check real floor contact/registration. **Stable** saves your physical verdict; **Move** removes placement and starts another scan. Tracking limitation hides the object and asks you to hold still/look at the same area; it is not silently presented as stable. App pause clears live placement and preserves any saved verdict. Every placement attempt receives a build/attempt ID and privateJSON record.

**Advanced** opens the existing1/2/3-point diagnostic workflow and settings. **Simple** returns to guided mode after Clear. Mode switching clears placement; it does not reuse a fitted room correction. Keep Advanced available while the guided prototype is being validated.

## Automatic optical calculation

`AutomaticCameraAlignment` follows the installed ARKit background shader's row-vectorUV×display-matrix convention. It converts screen-UV derivatives into normalized sensor-ray derivatives using live camera focal lengths and sensor resolution. It compares the implied screen focal scales with the provider projection and applies a common scale to the virtual projection. The live background image, camera world pose/CW90 and native principal/depth terms are retained. No previous room fit or hard-coded0.75 correction is loaded.

Only the tested portrait/CW90 axis swap is supported. Invalid/missing data, skewed/unsupported transforms, out-of-range focal ratios or inconsistent axes prevent readiness. Metadata must remain stable for1second. The existing surface gate retains level/extent/LiDAR-coverage/3second plane stability/15cm viewpoint change; the center point needs supported cells and a0.6second target hold. Place rechecks current tracking/surface/target. Green reports internal readiness, not surveyed accuracy or independently proven image registration.

## Checks and acceptance boundary

Math checks reproduce the recorded1920×1440/1326.749px/0.61502display example's approximately0.75ratio, preserve an already matched projection and reject unsupported transforms/inconsistent axes. Existing manual fit/holdout/rejection checks remain. These test the algebra, not physical camera convention correctness or a universally valid correction. Principal-point/image-convention audit, other devices/orientations, saved-world-map recovery and broad reliability remain open. The editor Play screenshot was unavailable; no visual editor or phone usability acceptance is inferred.

Required phone trial: a fresh start, Simple scan, green→Place, physical landmark/contact check and30–60second observation, then Stable if it holds. Repeat from another viewpoint and another fresh launch. If image-relative slipping remains, capture the build/attempt and recording; use Advanced for comparison. Do not relax guards or promote guided mode to audience release solely because synthetic checks/Tracking flags pass.

Guided build **0.6.0/build0010** signed native Debug build succeeded; codesign verification, embedded version/source and physical-phone inventory0.6.0/10 verified. Installed/launched19:20EDT. Runtime identifies VendingMachine,0.000m floor offset, guided intrinsics/display derivation and green→Place workflow; Portrait/CW90/Input System/SRP ON confirmed. Signed product and native/device logs preserved with the build. Private trial records copied before installation. Startup verifies deployment only: guided physical alignment/readiness usability and fresh-launch repeatability still require the user trial.
