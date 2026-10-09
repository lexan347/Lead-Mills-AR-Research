# System architecture

## Logical architecture

```mermaid
flowchart TD
    H[Historical sources and reconstruction] --> M[Research master 3D model]
    D[Drone imagery / LiDAR / survey control] --> G[Georeference and site frame]
    M --> O[Mobile optimization and LOD pipeline]
    G --> C[Versioned anchor catalog]
    O --> U[Shared Unity project]
    C --> U
    W[WebAR demonstration] --> F[Field comparison]
    U --> P[Positioning provider interface]
    P --> GPS[GPS / sensor-fusion baseline]
    P --> GGL[Google ARCore Geospatial]
    P --> NIA[Niantic Spatial VPS]
    U --> A[Android build from Ubuntu]
    U --> I[iOS Xcode project]
    I --> X[Xcode build on macOS]
    A --> F
    X --> F
    F --> R[Validation results and research report]
```

## Design principles

### One application, replaceable positioning providers

The scene, interface, assets, logging, and experimental protocol remain shared. Vendor-specific SDK code sits behind `IGeospatialAnchorProvider`. This makes GPS, Google, Niantic, and mock/local placement comparable without rewriting the rest of the application.

### Accuracy is application state

The application must not treat “GPS available” as “model correctly aligned.” Each provider reports:

- tracking state;
- horizontal and vertical accuracy estimates;
- heading accuracy where available;
- localization duration;
- provider and map/site identifiers;
- recoverable failure reason.

The UI gates model visibility using documented thresholds and records when those thresholds are crossed.

### Two model classes

- **Research master:** highest defensible detail, full provenance, retained outside mobile build size constraints.
- **Mobile derivative:** decimated mesh, mobile textures, level-of-detail variants, colliders/occlusion meshes, and immutable provenance link to the master.

### Coordinate chain

```text
Historical/model coordinates
  → surveyed local site frame
  → East-Up-North orientation
  → WGS84 latitude/longitude/ellipsoidal or terrain-relative altitude
  → provider anchor
  → Unity local coordinates
```

Every transformation must be documented. Terrain height, orthometric height, and WGS84 ellipsoid height must not be silently interchanged.

## Implementation status — October 8, 2026

The shared cross-platform architecture remains the target. The local Mac implementation is `LeadMills_AR_POC` (URP, Unity `6000.3.25f1`, AR Foundation / Apple ARKit XR Plugin **6.3.5**), with 0 Project Validation issues. Personal Team signing and deployment work on the iPhone 14 Pro / **iOS 27.0.1**. The first native AR POC camera permission/live feed was verified October 8 after correcting the startup Scene List and making a fresh export. The active camera uses `Mobile_RPAsset` / `Mobile_Renderer` with AR Background Renderer Feature; XR Origin uses Device tracking and Y offset 0. Horizontal-plane rendering and plane-anchor creation now run on the device, but stability relative to a floor landmark failed. The next local trial adds LiDAR triangle feedback before surface selection and anchoring; it uses session-local geometry and does not change the geospatial provider contract. Verify stable floor alignment before site assets. The local project is not yet incorporated into `unity/`; source handoff, Android, provider integration, and field validation remain pending. See [Unity/iOS setup](unity/IOS_SETUP.md).

## Repository boundaries

| Path | Purpose |
|---|---|
| `config/` | Public-safe anchor catalogs and configuration examples |
| `schemas/` | Machine-readable validation rules |
| `unity/` | Unity source that is safe to version |
| `webar/` | WebAR manifests, screenshots, and test notes—not account credentials |
| `iterations/` | Dated plans, logs, evidence indexes, and closeouts |
| `docs/` | Static GitHub Pages site |
| `tools/` and `tests/` | Reproducibility and validation utilities |

Large master models, raw drone imagery, VPS capture video, and signing assets are stored outside Git and referenced through manifests and checksums.
