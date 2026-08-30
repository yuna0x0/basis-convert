# Changelog

Notable changes to this package. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed

- Menu toggles that animate one side only came out inverted: turning a toggle named `Tail_OFF`
  on showed the tail instead of hiding it. Which side of a toggle animated an object is now
  recorded rather than inferred from the value, since a clip switching an object off looks
  identical to a side that animated nothing. Blendshapes had the same fault. **Re-convert to pick
  up the corrected toggles.**

## [0.1.1] - 2026-08-31

### Changed

- Renamed to Watari. The menu is now **Tools > Watari > Convert Avatar to Basis**, and the
  repository moved to `yuna0x0/watari-basis`. The package id is unchanged, so an installed copy
  updates in place.

## [0.1.0] - 2026-08-30

First release.

### Added

- **Physics.** VRChat PhysBones and legacy Dynamic Bone, with their colliders, convert to Basis
  jiggle physics. Per-bone falloff curves, collider shapes, ignored transforms and grab settings
  carry across. Dynamic Bone is an ordinary Unity asset, so an avatar using it converts whether
  or not VRChat was involved.
- **Constraints.** All six VRChat constraint types convert to their Basis equivalents. A
  constraint that drove a transform other than its own object is moved onto the transform it
  drives.
- **Avatar descriptor.** View position, the fifteen visemes and blink become a `BasisAvatar`
  component, updated in place on a re-conversion so anything Basis filled in itself survives.
- **Menu toggles.** Toggles are rebuilt as HVR Vixxy controls with menu items, covering object
  switching, blendshapes and material properties.
- **Modular Avatar.** Its hierarchy components are recognised and left to it. Menu items and
  merged animators, which target VRChat structures Basis does not have, are read together and
  rebuilt as Vixxy controls.
- **Whole hierarchies.** A conversion reads every prefab the chosen object is built from, so
  clothing and accessories convert with the avatar they are worn on.
- **Conversion options.** Each kind of thing can be switched off, and under Advanced so can any
  individual prefab, rig, constraint or toggle.
- **Rig check.** Reports the humanoid rig against what Basis's full-body IK expects, and offers
  to clear the Jaw mapping the Basis setup guide asks to be removed.
- **Reporting.** Anything approximated or dropped is listed with a stable code and a reason,
  before anything is written. The report can be copied or saved as Markdown.
- Nothing is written until the conversion is confirmed, one undo reverts a whole conversion, and
  converting again replaces its own output rather than stacking a second set.

### Known limitations

- VRChat contacts, puppets, and animation that plays over time do not convert.
- Two parts of the physics mapping are fits rather than conversions, and are exposed as
  adjustable weights.
- Component data is read from prefab files, so the avatar must still be linked to its prefab,
  and changes made to a prefab instance in a scene rather than to the prefab are not seen.

[Unreleased]: https://github.com/yuna0x0/watari-basis/compare/v0.1.1...HEAD
[0.1.1]: https://github.com/yuna0x0/watari-basis/releases/tag/v0.1.1
[0.1.0]: https://github.com/yuna0x0/watari-basis/releases/tag/v0.1.0
