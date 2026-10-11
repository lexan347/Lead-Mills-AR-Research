# Indoor vending-machine asset test

Original modern surrogate, **1.2m wide ×0.85m deep ×2.0m high**, meter units, floor-center pivot, front local−Z. Cabinet, shelves, cans/snack packs, coils, glass, keypad/card controls, collection opening and side vents use URP materials. Opaque parts combine by material:30,400 structural triangles and11 mesh renderers, plus five TextMesh labels and a floor-reference line. This is an original test object, not a reconstruction of a historical Lead Mills building. The supplied site plan/KMZ remain references for later work; no coordinates are used or published.

![Editor asset preview](../docs/images/vending-machine-preview.png)

## Build and placement

On the configured local AR POC scene, run **Lead Mills → Build and Select Vending Machine**, then the existing versioned iOS export. The builder creates the prefab/materials/combined meshes and OBJ/MTL, validates bounds and base, selects it in the placement component and saves the scene. Running original horizontal-plane setup restores the20cm cube; rerun the vending builder to select this asset. The asset's pivot offset is0; original cube setup restores0.10m. Camera image calibration/anchor logic is retained. Renderer-debug state resets to ordinary lighting at startup; debug overlays remain hidden and logs retained.

Use the existing compact1/2/3-point procedure. Choose physical A at the intended **center of the machine's base**, with B/C outside its footprint where they remain visible. After the independent-view check press Anchor A. The machine faces the viewer at initial placement and then retains its anchored orientation. The green1.2×0.85m outline is an estimated contact reference. Walk around and check size, floor contact and image-relative stability; Confirm stable or Reject records the result. Leave room to see the full2m height; this app does not currently offer asset resizing/rotation controls. Asset scale is fixed1:1.

## Evidence boundary

Editor bounds/render inspection and successful signed deployment do not establish physical full-size registration. Record the exact build/attempt ID, physical size/contact observations and30–60second movement verdict. Full-footprint floor support is not independently surveyed by the center anchor. Persistent/geospatial placement, room occlusion/contact shadows, historical building dimensions and onsite testing are separate work. Existing marker lifecycle/thermal issues remain tracked.

Portable geometry: `Assets/LeadMillsAR/VendingMachine/VendingMachine.obj` with matchingMTL; Unity prefab retains text, PBR materials and contact outline. Asset manifest records dimensions, mesh count and OBJ checksum. Rebuild after the scene setup with the same pinned editor to reproduce the original asset.
