# Unity / iOS POC setup — October 6, 2026

Status: PAUSED at physical-device setup/deployment while the iPhone updates.

## Completed setup

- Installed Unity Hub 3.22.2 and Unity 6.3 LTS `6000.3.25f1` for Apple Silicon, with iOS Build Support.
- Created `LeadMills_AR_POC` using the Universal 3D / URP template; iOS is the active build platform.
- Installed/configured AR Foundation and ARKit XR Plugin. ARKit is enabled for iOS, Initialize XR on Startup is enabled, ARKit Requirement is Required, and Face Tracking is off.
- Resolved project-validation items, including Run In Background and the camera usage description. Project Validation reached **0 issues**.
- Created the minimal AR scene with AR Session and XR Origin / AR camera in place of the template's ordinary camera.
- Generated the iOS Xcode project successfully, including `Unity-iPhone.xcodeproj`, with **no Unity build errors**.
- Configured Automatically manage signing with a Personal Team and bundle ID `com.alexanderangulo.leadmillsarpoc` in Xcode 27.0 (27A266a).
- Xcode recognized the physical iPhone 14 Pro. Device setup/deployment paused because it is updating from iOS 26.6.2 to 26.7.1.

Camera usage wording selected during setup:

> Camera access is required to place and view the Lead Mills historical reconstruction in augmented reality.

The local Unity project and generated Xcode output are not added to this research repository by this documentation update. Exact AR package versions and build manifests still need to be captured. Unity export success is not confirmation of a successful Xcode compilation or on-device launch.

## Stopping point and next session

Wait for the iOS 26.7.1 update to finish. The user is unsure whether iOS 27 is available; that question remains unresolved and does not change today's chosen update target.

1. Confirm the update completed and record the actual iOS version on the iPhone.
2. Reconnect/unlock the physical iPhone 14 Pro and confirm trust/pairing and Developer Mode as needed. Do not mark these complete until checked.
3. Open the generated `Unity-iPhone.xcodeproj` and select the updated physical iPhone in Xcode's device selector.
4. Recheck the Unity-iPhone target's Personal Team, automatic signing, bundle ID, and provisioning/device readiness; resolve any remaining messages.
5. Run from Xcode. Record build/install results and any exact error message.
6. Verify that the app installs, launches, requests camera permission, and displays the live AR camera feed without crashing. Save device/OS/package versions and test evidence in the next daily log.

No successful device installation, camera feed, historical-model placement, Android build, geospatial localization, or field validation is claimed at this stopping point. Importing the Lead Mills model follows the basic on-device camera/AR test.
