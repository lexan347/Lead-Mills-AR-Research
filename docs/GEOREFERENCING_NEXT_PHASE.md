# Next phase — site coordinates to a registered 3D model

**Planned after the October 14, 2026 full-size local AR milestone.**

Use **QGIS as the coordinate/control master** and **Blender with BlenderGIS for terrain and model assembly**. Keep the successful phone build as a tagged baseline while developing a separate georeferencing derivative. Neither GIS tool by itself makes the current session-local ARAnchor geographic or persistent.

## 1. Inventory the source data

Locate the existing `.blend` master, site maps/1890 plan, orthophoto, elevation raster/point cloud, any QGIS project, and approved control-point survey. Record provenance, license, date, horizontal CRS, units, vertical datum/geoid, and uncertainty. Missing CRS or altitude references must be resolved before alignment. Historical map features are interpretive evidence; they are not automatically surveyed control.

**First concrete deliverable:** a source inventory and a QGIS GeoPackage with an empty, documented control-point layer. Keep unknown coordinates null. Existing public anchor policy in SITE_DATA.md applies.

## 2. Establish the site frame in QGIS

Choose a projected metric CRS after checking the source data and survey requirements. Massachusetts Mainland State Plane NAD83 (EPSG:26986) is a candidate, not an approved choice; do not assign it to data whose CRS is unknown. Transform between known CRSs rather than relabeling coordinates. Record height reference separately.

Prepare clipped terrain/orthophoto layers and georeference the historical plan against defensible features. Choose a transformation appropriate to map distortion. Save control points, residuals, source/target CRS, transformation settings, and independent check points. Low fit residuals alone do not establish absolute accuracy.

Control-point fields: ID, description, easting, northing, elevation, horizontal CRS, height datum, acquisition method/date, uncertainty, control/check role, photo/reference, publication approval. Select durable approved features, distributed across the modeled area; vegetation is not permanent control. Keep precise unapproved coordinates outside public Git.

## 3. Assemble the site in BlenderGIS

Import terrain and control geometry prepared in QGIS. BlenderGIS can establish a scene georeference from imported GIS data; verify it rather than assuming a mesh export carries CRS information. Use a local metric origin near the study area to keep working coordinates small, and store its exact projected offset in metadata.

Bring in the existing shed at 1:1 meter scale. Preserve the historical master and document its provisional dimensions. Use the centered grade-origin master for registration, or record the exact offset if the phone's front-edge placement prefab is used. Record translation, rotation, scale, forward direction, vertical axis, and true/grid-north relationship. Avoid adjusting size to make an uncertain historical map appear to fit.

Compare transformed model points to independent site check points and record errors. If reconstructing a 3D rigid alignment from paired control points, use non-collinear well-distributed points with redundancy; keep validation points out of the fit. Select the actual survey/check design once terrain and reference data are known.

## 4. Export a mobile derivative and explicit transform

Export an optimized model plus a sidecar manifest: source/derivative SHA-256, units, model origin, projected CRS, site-origin easting/northing, vertical datum, model-to-site transform, heading convention, axis/handedness conversion to Unity, and uncertainty. Verify known distances and orientation after Unity import. Do not assume FBX/GLB preserves the GIS coordinate system.

## 5. Connect the phone to the same site frame

Implement a separate registration method, initially a surveyed visual marker or approved measured reference; evaluate a geospatial/VPS provider later. A single position does not resolve heading. Account for any model-origin offset and the phone's local AR frame. Preserve the current local-placement mode as a fallback and comparison.

Acceptance: check model corners/origin against independent references; measure horizontal, vertical, heading error, drift, relaunch repeatability, and scan/thermal behavior on a defined route. Choose acceptance thresholds before collecting the validation data. Only then claim geographic placement accuracy.

## Official references

- [QGIS Georeferencer](https://documentation.qgis.org/3.44/en/docs/user_manual/managing_data_source/georeferencer.html): control points, transformation settings and errors.
- [BlenderGIS project](https://github.com/domlysz/BlenderGIS) and [shapefile import workflow](https://github.com/domlysz/BlenderGIS/wiki/Shapefile-import): GIS preparation, scene georeferencing, and elevation attributes.

These sources describe tool capabilities. The proposed workflow above is a project recommendation; site registration has not yet been performed.
