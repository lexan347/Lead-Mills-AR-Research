# Decision record

## D-001 — documentation-first repository

**Date:** 2026-10-06<br>
**Status:** Accepted

Follow the DronePi Research pattern: a navigable README, explicit decisions, weekly iterations, evidence indexes, and no unsupported completion claims.

## D-002 — one Unity project for Android and iOS

**Date:** 2026-10-06<br>
**Status:** Accepted

Ubuntu is the primary authoring and Android-build environment. macOS/Xcode performs final iOS compilation, signing, device installation, and distribution. Source moves through Git; generated caches and builds do not.

## D-003 — WebAR before custom application

**Date:** 2026-10-06<br>
**Status:** Accepted

Use a browser-based demonstration to validate communication value and mobile asset performance before committing the field study to a specific native positioning SDK. WebAR success is not evidence of precise geospatial alignment.

## D-004 — positioning-provider abstraction

**Date:** 2026-10-06<br>
**Status:** Accepted

Google, Niantic, GPS, and mock/local positioning will be isolated behind a shared provider interface. This reduces vendor lock-in and supports controlled comparison.

## D-005 — Niantic VPS as leading precision candidate

**Date:** 2026-10-06<br>
**Status:** Provisional

Niantic is the leading candidate because a custom site map can be created where preexisting Street View coverage or stable urban features may be insufficient. Acceptance depends on onsite mapping, localization performance, terms, costs, and device testing.

## D-006 — no precise public anchors before review

**Date:** 2026-10-06<br>
**Status:** Accepted

The public repository begins with null coordinates. Surveyed coordinates are published only after technical validation and review of site-owner, cultural-resource, safety, and privacy considerations.

## Open decisions

- Repository visibility: public or private during development.
- Final repository name and organizational ownership.
- Unity editor and package versions.
- Google versus Niantic primary provider after onsite tests.
- Historical reconstruction source and uncertainty notation.
- License for original code, models, and documentation.
- Long-term hosting and offline behavior after graduation.
