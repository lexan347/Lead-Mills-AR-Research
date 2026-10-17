# Lead Mills — Blockout-First 3D Georeferencing SOP
Effective 2026-10-17 (America/New_York)

## Production objective
Create many georeferenced **dimension-first** building models quickly. Review in Google Earth Pro first; hand off to Unity after geographic quality assurance. Do not overwrite original assets.

## Each object's mandatory fields
Stable ID LM-###; historical name; source KMZ polygon; documented/estimated length, width, height in **meters**; plan type rectangular/L/U/compound/irregular; story count or unknown; longitude/latitude reference anchor; heading clockwise from north; model base-center origin; datum/ground-offset status; provenance and confidence; QA state and revision.

## Production stages and acceptance gates
1. **Inventory:** preserve original KML and assign stable IDs; separate building candidates from rail, retaining walls, wells, docks, and other infrastructure.
2. **Dimensions:** user provides size for each ID; source and uncertainty recorded. Polygon-derived extents are **provisional map measurements**, not historical dimensions.
3. **Blockout:** massing only, with boxes/compound boxes and supplied dimensions; no required roof, windows, textures, or irregularity details at this stage. Keep separate source model per building.
4. **Geo-package:** COLLADA DAE in meters and KML Model KMZ with explicit anchor, heading, units, and scale. For provisional visualization only, allow relativeToGround altitude=0.
5. **Google Earth QA:** check position, heading, scale, intersections, object visibility and footprint; save screen captures and results. Mark passing objects ACCEPTED-GE.
6. **Iterate batch:** do not block deployment of other structures pending detailed research.
7. **Detailed model:** add shape, roof, levels, openings, subsurface geometry later; retain immutable ID and revision history.
8. **Unity:** port only after Google Earth acceptance; define documented local geospatial origin and verify axis, north alignment, scale, vertical relationship and mobile view. Google Earth's elevation is not Unity's survey datum.
9. **GIS archiving:** preserve 3D geometry plus object register and georeference metadata for QGIS and photogrammetry comparisons.

## Vertical reference HOLD
Historic original grade, common elevation datum, structures with **negative ground offsets**, and relation of old foundations to current fill are explicitly DEFERRED. Do not infer historical elevation from terrain visualization.

## Naming standard
LM-###_ShortName_Blockout_v001.blend, .fbx, .dae, .kmz. Publish each revision separately.

## QA evidence
ID, source citation, planned vs measured dimensions, model extents, center lon/lat, heading, model revision, screenshot, approving reviewer, timestamp, uncertainties. Label a visible object as proof of visualization, not proof of historical truth.
