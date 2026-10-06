# Browser demonstration

## Purpose

The WebAR phase answers a narrow question: can a visitor open a link on an iPhone or Android phone and see an optimized 3D object associated with the Lead Mills location?

It does not establish survey-grade placement or persistence.

## Proposed workflow

1. Create a WorldCAST creator account without storing credentials in this repository.
2. Export a meter-scale GLB test model with baked textures.
3. Create one GeoCAST scene and use provisional coordinates only for testing.
4. Record scene identifier, platform settings, asset checksum, and publication date in an iteration manifest.
5. Open the published link in Safari and Chrome.
6. Allow camera and location access only for the test.
7. Capture device, OS, browser, network, load time, placement observations, and screenshots.
8. Remove or unpublish the scene if it exposes unreviewed site information.

## Acceptance checklist

- [ ] Same asset appears on one supported iPhone and one supported Android phone.
- [ ] Asset scale and orientation are documented.
- [ ] Link and QR entry paths work.
- [ ] Camera/location notices are visible.
- [ ] Failure behavior is recorded when location or camera access is denied.
- [ ] Results are labeled approximate and not presented as precise historical alignment.

## Files retained in Git

- scene manifest without credentials;
- optimized test asset if licensing and size allow;
- screenshots cleared for publication;
- device/test table;
- asset checksum and conversion record.
