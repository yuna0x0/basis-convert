# Changelog

Notable changes to this package. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- A VRC Head Chop converts to a Basis Head Chop, naming the same bones with the same scale
  factors. A bone limited to VR or to desktop is reported: `headChop.condition.dropped`.
- VRC Raycast is reported as `raycast.dropped`; per-platform overrides and impostor settings as
  `vrchat.buildSettings`. Neither was recognised before.
- A PhysBone collider marked global, an SDK 3.10.4 setting, is reported:
  `collider.global.dropped`.
- The report, the window and a docs page state the source versions the readers were checked
  against: VRChat SDK 3.10.5, UniVRM 0.131.2, Dynamic Bone 1.3.2, Modular Avatar 1.18.3.
- A plane collider facing anything but its transform's Y axis is reported, since a Basis plane
  always faces that axis: `collider.planeRotation.dropped`, `collider.planeAxis.dropped`,
  `vrm.collider.planeNormal`.

### Fixed

- VRChat and Dynamic Bone capsule colliders were written one diameter too long. Both give a
  capsule's height end to end; Basis measures between the cap centres.
- A VRChat or Dynamic Bone capsule no taller than its diameter is written as a sphere, which is
  how both sources collide it.

## [0.4.0] - 2026-09-04

### Added

- A `.vrm` file converts as it imports: spring bones, node constraints, expressions, licence and
  eye offset are read from its components, so it no longer has to be unpacked and saved as a
  prefab first.
- A VRM's vowel expressions and blink fill the Basis Avatar's visemes. Five of the fifteen, since
  VRM names no consonants: `vrm.visemes` and `vrm.blink`.
- VRM 0.x look at components are reported as `vrm.lookAt` rather than as unknown scripts, along
  with its humanoid description.
- A prefab saved from a `.vrm` without unpacking reads its components from the file:
  `source.modelRead`.
- An avatar whose visemes nothing could fill says so as `descriptor.visemesUnset`.

### Fixed

- An imported `.vrm` converted as though it were empty. It is binary, and only text was read.
- A prefab saved from a `.vrm` without extracting its assets reported `vrm.objectUnreadable`
  instead of reading the expressions and licence from the component.
- The report titled itself "VRChat avatar to Basis" whatever it had read.

## [0.3.3] - 2026-09-03

### Fixed

- Text was cut off rather than wrapped under the conversion result, in the rig and diagnostic
  lists, beside the tuning weights and under the buttons.
- The line describing Advanced left out the checkbox per motion, which it has always added.

## [0.3.2] - 2026-09-02

### Fixed

- Detected, Read from, What to convert and Prefabs cut their text off instead of wrapping when
  the window was narrow.
- Read from named every prefab, so an avatar built from dozens of them filled the window above
  the scroll view. It names six and counts the rest, as the report does.

## [0.3.1] - 2026-09-01

### Changed

- A prefab saved from an imported model without unpacking now says so, naming the model, instead
  of the generic message for finding nothing. Reported as `source.notUnpacked`.

## [0.3.0] - 2026-09-01

### Added

- A prefab variant converts the physics, colliders and constraints it inherits. Its own file
  holds only its overrides, so every prefab above it is read too, reported as
  `source.prefabVariant`.
- Avatar Modify Support is named rather than reported as an unrecognised script. It is
  editor-only and carries nothing to convert, reported as `source.editorOnlyTool`.

### Fixed

- Components a prefab only refers to, rather than defines, were read as though they were its
  own. On a variant this counted colliders twice and reported the copies as unresolved.
- The report guide named `constraint.solveInLocalSpace`; the code is
  `constraint.solveInLocalSpace.dropped`.

## [0.2.0] - 2026-08-31

### Added

- VRM spring bones become jiggle physics, in both VRM 0.x and VRM 1.0. UniVRM is not needed.
- VRM expressions become Vixxy controls. Lip sync, blinking and gaze are left to Basis.
- VRM 1.0 rotation, aim and roll constraints become Basis constraints. None is exact, and all
  three are reported.
- A VRM's eye offset becomes the Basis eye position.
- A VRM's licence is shown before converting: title, author, who may wear it, and every
  permission the file states.
- Menu controls that share a parameter become one Vixxy control with a choice per value. This
  covers outfit and hairstyle selectors.
- Radial puppets become Vixxy sliders, taking the two ends of the blend tree. A tree with
  motions in between is reported as `vixxy.puppetEnds`.
- Toggles guarded by a VRChat parameter such as `IsLocal` are rebuilt rather than skipped. The
  dropped guard is reported as `vixxy.builtinGuard`.
- A menu toggle whose clip animates over time is rebuilt as an authored motion the control
  switches on.
- Animation that plays with nothing switching it on becomes `BasisAuthoredMotion`, baked to a
  `BasisMotionClip` asset. The asset is a project file and survives an undo.
- Modular Avatar `Object Toggle` is read.

### Fixed

- Converting an avatar twice stacked a second Vixxy control and menu item instead of replacing
  the first.
- Controls started at their first choice instead of the parameter's declared default, so
  clothing authored on switched itself off at load.
- A motion a menu switches on could be written without its control, playing it permanently.
- Switching off menu toggles or authored motion left their losses in the report.
- Clip paths from a Modular Avatar merged animator did not rebase the rotations they animate.
- A second avatar descriptor, which clothing often carries, was read and reported before being
  discarded.
- The re-conversion dialog counted every non-jiggle component as a constraint, and called a
  baked motion clip undoable.
- Constraint sources past the sixteenth were dropped silently. VRChat's overflow list is still
  not read, but the difference is reported.
- `UniHumanoid.Humanoid` was reported as an unknown script.
- A VRM whose expressions and licence are still inside the `.vrm` is reported as
  `vrm.objectUnreadable`, naming the import setting that extracts them.
- A layer where one parameter value led to two states was read as if the first transition were
  the only one. The layer is left alone and reported instead.
- A Modular Avatar menu item's toggle was only built when the prefab also installed a menu.
- Any list entry in an expression menu asset began a new control, so a radial's `subParameters`
  was read as a control of its own.
- Modular Avatar object paths are resolved against the avatar root, as `AvatarObjectReference`
  does.

## [0.1.2] - 2026-08-31

### Fixed

- Menu toggles that animate one side only came out inverted: a toggle named `Tail_OFF` showed
  the tail instead of hiding it. Which side animated an object is now recorded rather than
  inferred. Blendshapes had the same fault.

## [0.1.1] - 2026-08-31

### Changed

- Renamed to Watari. The menu is now **Tools > Watari > Convert Avatar to Basis**, and the
  repository moved to `yuna0x0/watari-basis`. The package id is unchanged, so an installed copy
  updates in place.

## [0.1.0] - 2026-08-30

First release.

### Added

- **Physics.** VRChat PhysBones and legacy Dynamic Bone, with their colliders, become Basis
  jiggle physics. Falloff curves, collider shapes, ignored transforms and grab settings carry.
- **Constraints.** All six VRChat constraint types. One driving a transform other than its own
  is moved onto the transform it drives.
- **Avatar descriptor.** View position, the fifteen visemes and blink become a `BasisAvatar`,
  updated in place on a re-conversion.
- **Menu toggles.** Rebuilt as HVR Vixxy controls with menu items, covering object switching,
  blendshapes and material properties.
- **Modular Avatar.** Hierarchy components are left to it. Menu items and merged animators are
  read together and rebuilt as Vixxy controls.
- **Whole hierarchies.** Every prefab the chosen object is built from is read, so clothing
  converts with the avatar.
- **Conversion options.** Each kind can be switched off, and under Advanced so can any
  individual prefab, rig, constraint or toggle.
- **Rig check.** Reports the humanoid rig against what Basis's IK expects, and offers to clear
  the Jaw mapping.
- **Reporting.** Anything approximated or dropped is listed with a stable code and a reason
  before anything is written. Copyable, or saved as Markdown.
- Nothing is written until confirmed, one undo reverts a conversion, and converting again
  replaces its own output.

### Known limitations

- VRChat contacts, puppets, and animation that plays over time do not convert.
- Two physics settings are fits rather than conversions, exposed as adjustable weights.
- Component data is read from prefab files, so the avatar must still be linked to its prefab.

[Unreleased]: https://github.com/yuna0x0/watari-basis/compare/v0.4.0...HEAD
[0.4.0]: https://github.com/yuna0x0/watari-basis/releases/tag/v0.4.0
[0.3.3]: https://github.com/yuna0x0/watari-basis/releases/tag/v0.3.3
[0.3.2]: https://github.com/yuna0x0/watari-basis/releases/tag/v0.3.2
[0.3.1]: https://github.com/yuna0x0/watari-basis/releases/tag/v0.3.1
[0.3.0]: https://github.com/yuna0x0/watari-basis/releases/tag/v0.3.0
[0.2.0]: https://github.com/yuna0x0/watari-basis/releases/tag/v0.2.0
[0.1.2]: https://github.com/yuna0x0/watari-basis/releases/tag/v0.1.2
[0.1.1]: https://github.com/yuna0x0/watari-basis/releases/tag/v0.1.1
[0.1.0]: https://github.com/yuna0x0/watari-basis/releases/tag/v0.1.0
