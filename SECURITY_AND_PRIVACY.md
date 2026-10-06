# Security and privacy

## Secrets

Never commit:

- Google Cloud or Maps API keys;
- Niantic application credentials or access tokens;
- Apple certificates, provisioning profiles, or private signing keys;
- Android keystores or passwords;
- analytics secrets;
- private GitHub tokens.

Use platform secret stores, environment variables, local untracked files, and GitHub Actions secrets. Example configuration files contain placeholders only.

## Camera and location data

The application must explain why camera and location data are needed before activating geospatial services. Provider-required notices and privacy policies must be included in the app and field-study consent materials where applicable.

## Field imagery

Before publishing scans or screenshots:

- remove faces, vehicle plates, private residences, and unrelated bystanders where practical;
- check whether images reveal sensitive ecological, archaeological, or infrastructure information;
- record consent and institutional requirements;
- preserve unredacted research originals only in an access-controlled archive when justified.

## Application behavior

- Do not display a model as precisely aligned when tracking is coarse or lost.
- Do not log more device identifiers, imagery, or location history than the study requires.
- Provide an obvious way to stop camera/location use.
- Use HTTPS for downloadable catalogs and assets.
- Verify checksums before loading research assets used in a formal trial.
