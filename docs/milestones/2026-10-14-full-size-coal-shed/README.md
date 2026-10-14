# Milestone — full-size coal shed in local iPhone AR

**Reached October 14, 2026. Device build: `LMAR_v0.7.0_b0016` (0.7.0, build 16).**

The user confirmed successful placement after the tile-floor readiness fixes, then successfully tested the full-size textured shed. Seven reviewed photographs document indoor and outdoor grass views. The visible UI reports **Anchor: Tracking**, **Placed**, and **Scan off**. Several outdoor views show anchor a003; this supports the walkthrough demonstration but does not quantify drift.

## Result and scope

- **Demonstrated:** imported FBX, repaired URP texture assignments, full-size 1:1 prefab, front-edge placement, and textured outdoor rendering on grass.
- **User-reported:** tile-floor placement works; shed appears and stays anchored while moving; final full-size test successful.
- **Implemented:** small 24 cm capture target, stationary readiness, target/Place input, moderate rough-ground acceptance, post-placement mesh scanning disabled, 30 FPS render target.
- **Unmeasured:** physical temperature/power savings, absolute size/position error, drift, cross-launch persistence, and performance across terrain/devices. “Scan off” describes app-controlled mesh scanning; camera/inertial AR tracking remains active. It does not prove every internal sensor is off.
- **Pending:** geographic anchoring, surveyed coordinates, true/grid-north alignment, historical footprint validation, Android parity, and formal field validation. These photos do not establish placement at the historical site.

The approximate rendered envelope is **7.82 × 4.26 × 3.51 m**, including overhang/foundation. The provisional historical body footprint remains **7.3152 × 3.6576 m**; neither is a surveyed historical dimension. The front-edge placement prefab has a different origin from the centered master; georeferencing must record which is used.

## Preserved materials

- [Unity source snapshot](../../../unity/LeadMills_AR_POC/README.md), including Assets, .meta files, Packages and ProjectSettings.
- [Device build identity](device-build-identity.json). Its inherited `sourceRevision` predates publication and is **not** the milestone source commit. The release tag identifies the published snapshot.
- [Photo provenance and checksums](evidence-manifest.json). Original HEICs remain local. Public JPEGs have no EXIF/GPS metadata. IMG_7735 was withheld because it contains an unrelated passerby. No visual retouching was performed.
- [Daily log](../../../iterations/2026-10-11/Daily_Log_2026-10-14.md).
- [Next phase: GIS and model registration](../../GEOREFERENCING_NEXT_PHASE.md).

## Photo gallery

### IMG_7729.HEIC

Indoor full-scale check; this is not a grade-accuracy measurement.

![Indoor full-scale check; this is not a grade-accuracy measurement.](photos/IMG_7729.jpg)

### IMG_7730.HEIC

Outdoor grass placement, front view; phone assistant overlay visible.

![Outdoor grass placement, front view; phone assistant overlay visible.](photos/IMG_7730.jpg)

### IMG_7731.HEIC

Outdoor front-corner view, anchor a003 shown tracking.

![Outdoor front-corner view, anchor a003 shown tracking.](photos/IMG_7731.jpg)

### IMG_7732.HEIC

Outdoor long-side view of the full-size shed on grass.

![Outdoor long-side view of the full-size shed on grass.](photos/IMG_7732.jpg)

### IMG_7733.HEIC

Outdoor oblique side/rear view of the placed shed.

![Outdoor oblique side/rear view of the placed shed.](photos/IMG_7733.jpg)

### IMG_7734.HEIC

Outdoor rear-corner view of the shed on grass.

![Outdoor rear-corner view of the shed on grass.](photos/IMG_7734.jpg)

### IMG_7736.HEIC

Outdoor front/side view; placed state and scan-off indicator visible.

![Outdoor front/side view; placed state and scan-off indicator visible.](photos/IMG_7736.jpg)
