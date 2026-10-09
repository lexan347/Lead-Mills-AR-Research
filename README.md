# Lead Mills AR Research

Research and prototype repository for placing historically reconstructed, georeferenced 3D objects at the former Forest River Lead Works site on the Salem–Marblehead boundary in Massachusetts.

The project will evaluate a low-friction WebAR demonstration and a research-grade Unity application for iPhone and Android. The central research problem is not merely displaying a model through a phone camera; it is measuring how accurately and repeatably a full-scale reconstruction can be aligned with its intended real-world location.

## Start here — repository overview

Updated October 8, 2026. This repository follows the documentation-first and evidence-preserving structure used by [DronePi Research](https://github.com/lexan347/DronePi-Research).

**Current stage:** native camera POC confirmed October 8 on iPhone 14 Pro / iOS 27.0.1 after correcting the Build Profiles scene and making a fresh export. On October 9, the user confirmed that clockwise 90° camera-view registration fixed mesh/plane alignment and that the anchored cube stayed in place. This is the first user-verified stable portrait scan → surface → anchor trial. The readiness gate checks level, local LiDAR coverage, temporal stability and viewpoint change. The tested portrait view is now the installed startup default. The fresh trial confirms the orientation fix but reports small residual mesh/cube drift during tilt and movement. Refine steady registration and verify surface contact before a simple Lead Mills asset. Other orientations/devices, persistent/geospatial anchors, surveyed accuracy and field validation remain pending.

| Section | What you will find |
|---|---|
| [Programmatic roadmap](ROADMAP.md) | Demonstration, Unity, VPS, validation, and deployment phases |
| [System architecture](ARCHITECTURE.md) | Shared Unity project, platform builds, geospatial providers, and asset pipeline |
| [Decision record](DECISIONS.md) | Technology choices, rationale, assumptions, and open decisions |
| [Site and anchor data](SITE_DATA.md) | Coordinate policy, reference frames, survey requirements, and public-data rules |
| [Security and privacy](SECURITY_AND_PRIVACY.md) | Camera/location notices, secrets, field data, and publishing controls |
| [Current iteration](iterations/2026-10-04/README.md) | Week of October 4–10, 2026 |
| [Software baseline](docs/SOFTWARE_BASELINE.md) | Linux inventory status, pinned Mac/AR versions, and verified iPhone camera test |
| [Unity starter](unity/README.md) | Cross-platform source layout and Linux/macOS build workflow |
| [Horizontal-plane test](unity/PLANE_PLACEMENT_TEST.md) | LiDAR scan → surface → anchor POC, failed stability trials and acceptance procedure |
| [Unity / iOS setup](unity/IOS_SETUP.md) | Verified POC configuration, clean-build checklist, and troubleshooting |
| [WebAR demonstration](webar/README.md) | Browser-first demonstration procedure and evidence checklist |
| [Research notes](Research_Notes_Template.md) | Source review and independent reasoning template |
| [GitHub Pages dashboard](docs/index.html) | Static public project overview prepared for deployment |

## Research objectives

1. Display an optimized 3D reconstruction through a mobile phone camera at Lead Mills.
2. Compare GPS-only, visual-positioning, and surveyed-marker-assisted placement.
3. Quantify horizontal error, vertical error, heading error, drift, localization time, and repeatability.
4. Maintain one shared Unity codebase that can produce Android and iOS builds.
5. Preserve sufficient configuration, test evidence, and provenance for academic review.

## Development platforms

| Work | Primary machine |
|---|---|
| Unity authoring, model optimization, automated validation, Android builds | Ubuntu workstation |
| iOS signing, device installation, and TestFlight/App Store builds | macOS / MacBook Pro with Xcode |
| WebAR viewing | iPhone Safari or Android Chrome |
| Site scanning and VPS-map capture | Supported iPhone or Android device |

The same Git repository moves between Ubuntu and macOS. Generated Unity caches and platform build folders are intentionally excluded.

## Repository policy

- Record demonstrated facts separately from plans and vendor claims.
- Do not publish API keys, signing certificates, private field notes, or unreviewed personal data.
- Do not publish precise anchor coordinates until they have been surveyed, reviewed, and approved for public release.
- Keep research-quality source models separate from mobile derivatives; record checksums and conversion settings.
- Store large binary assets through Git LFS or an external research-data archive.
- Do not infer historical geometry without recording the source and confidence level.

## Local validation

```bash
python3 tools/validate_anchor_catalog.py config/anchors.example.json
python3 -m unittest discover -s tests -v
```

## Repository and Pages URLs

- Repository: `https://github.com/lexan347/Lead-Mills-AR-Research`
- Project dashboard: `https://lexan347.github.io/Lead-Mills-AR-Research/`

The public repository exists; dashboard deployment was recorded in the prior publication-status commits. The current setup stopping point is also reflected in the dashboard status.

## Licensing status

No open-source license has been assigned to original project materials yet. Third-party software, models, photographs, maps, and historical sources retain their own licenses and attribution requirements.
