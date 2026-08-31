# Changelog

Notable changes to this package. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- A VRM avatar's licence is read and shown before converting: its title, author, who may wear it,
  whether it may be changed or passed on, whether commercial, violent, sexual, political or
  hateful use is allowed, whether credit is required, and the licence it names. A permission the
  format has no field for is left out rather than guessed at. A licence that forbids changing the avatar, or limits who may wear
  it, is a warning rather than a block. Converting changes an avatar and using it on Basis is a
  use, and both are the wearer's to judge.
- A VRM avatar's eye offset becomes the Basis eye position. Both formats record the point the
  camera sits at as an offset from the head bone, the same point VRChat calls the view position,
  so a converted VRM avatar has a correct eye height rather than one Basis has to guess. The
  renderers a VRM hides from its wearer are reported: Basis hides the head bone and everything
  under it, which covers the usual case.
- VRM expressions become Vixxy controls. An expression is a named set of blendshape weights,
  and a control with two choices holds the same: off leaves every shape as the avatar was
  authored, on takes the expression's. The lip sync shapes, blinking and looking around are left
  to Basis, which drives them itself. Both VRM formats are read, including their different weight
  scales.
- VRM spring bones are read and become jiggle physics, in both VRM 0.x and VRM 1.0. UniVRM does
  not need to be installed: a VRM avatar in a Basis project carries its spring bones as missing
  scripts, and the data is read from the prefab file the same way every other source is. VRM 1.0
  carries a parameter per joint, and Basis evaluates its parameters over distance from the chain
  root, so a chain that varies along its length becomes a curve rather than an average. Bones
  hanging off a chain that the spring never named are excluded, since a jiggle rig would
  otherwise swing them.
- Menu controls that share one parameter and each set a different value are rebuilt as a single
  Vixxy control with a choice per value, rather than being reported as untraceable. This covers
  outfit and hairstyle selectors, which are common and were the largest remaining gap.
- Radial puppets become Vixxy sliders, taking the two ends of the blend tree their layer holds
  as the control's choices. A tree with motions between its ends is reported as
  `vixxy.puppetEnds`, because a slider interpolates between its choices in a straight line.
- A toggle whose animator layer also waits on one of VRChat's own parameters is rebuilt rather
  than skipped. `IsLocal`, `InStation` and the rest have no Basis equivalent, so the control
  switches whenever it is used, and the guard it no longer waits for is reported as
  `vixxy.builtinGuard`. A built-in that steers a transition on its own still means the layer
  belongs to it rather than to the menu.
- A menu toggle whose clip animates over time is rebuilt rather than dropped. The animation
  becomes an authored motion and the control switches the component on and off, which Vixxy
  permits for this type.
- Animation that plays without anything switching it on is rebuilt as `BasisAuthoredMotion`.
  Basis carries no animator layers on an avatar, so a swaying tail or a turning accessory had
  nowhere to go. The clip is baked to a `BasisMotionClip` beside the animation it came from, at a
  path a second conversion writes over rather than beside. Unlike the components a conversion
  writes, a baked clip is a project asset and stays after an undo.
- Modular Avatar `Object Toggle` is read. A menu item and an object toggle on the same object
  describe a toggle completely without a merged animator, which is how clothing commonly ships
  a switch.
- VRM 1.0's rotation, aim and roll constraints become Basis constraints. Each drives the object
  it sits on and follows one source, so there is no target to relocate. None of the three is
  exact and all three say so: VRM's rotation constraint copies a delta from the source's rest
  where a Basis one follows the rotation itself, VRM's aim states no up direction, and nothing in
  Basis copies rotation about a single axis the way a roll constraint does.

### Fixed

- **Converting an avatar twice stacked a second Vixxy control and menu item on it** rather than
  replacing the first. Everything Vixxy and every authored motion sits on the avatar root, so the
  rule that protects hand-made components elsewhere, replace only what sits on a transform this
  plan writes to, said nothing there. They are matched by the names the plan is about to write,
  and a control somebody else added is left alone.
- A control started at its first choice rather than at the value the avatar declares its
  parameter defaults to, so clothing authored on would have switched itself off at load.
- A motion a menu switches on could be written without the control that switches it, which would
  have played it permanently. It follows that control's selection now.
- Switching off menu toggles or authored motion left their losses in the report, though narrowing
  a conversion narrows what is reported for every other category.
- Clip paths from a Modular Avatar merged animator did not rebase the rotations they animate, so
  a motion read from clothing resolved against the wrong object.
- A second avatar descriptor, which clothing often carries for previewing, was read and reported
  before being discarded.
- The re-conversion dialog counted every component that was not a jiggle rig as a constraint, and
  said the whole thing was undoable, which a baked motion clip is not.
- Sources past a constraint's sixteenth were dropped without saying so. VRChat keeps those in an
  overflow list this does not read, and the difference is reported now.
- `UniHumanoid.Humanoid`, which UniVRM puts on an imported avatar, was reported as an unknown
  script. It records the humanoid bone mapping Unity's own avatar already holds, so it is named
  and left alone.
- A VRM whose expressions and licence are still inside the `.vrm` file is reported as
  `vrm.objectUnreadable`, naming the import setting that writes them into the project, rather
  than silently converting no expressions.
- A layer where one value of its parameter led to two different states was read as though the
  first transition were the only one. Something other than the parameter decides between them,
  so the layer is left alone and reported instead.
- A Modular Avatar menu item's toggle was only built when the prefab also installed an
  expression menu, so a piece of clothing that installs a menu item alone converted nothing.
- Any list entry in an expression menu asset began a new control, so a radial's `subParameters`
  entry was read as a control of its own. Controls are now recognised by indentation.
- Modular Avatar object paths are resolved against the avatar root, as `AvatarObjectReference`
  does, rather than being rebased onto the component's own object.

## [0.1.2] - 2026-08-31

### Fixed

- Menu toggles that animate one side only came out inverted: turning a toggle named `Tail_OFF`
  on showed the tail instead of hiding it. Which side of a toggle animated an object is now
  recorded rather than inferred from the value, since a clip switching an object off looks
  identical to a side that animated nothing. Blendshapes had the same fault.

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

[Unreleased]: https://github.com/yuna0x0/watari-basis/compare/v0.1.2...HEAD
[0.1.2]: https://github.com/yuna0x0/watari-basis/releases/tag/v0.1.2
[0.1.1]: https://github.com/yuna0x0/watari-basis/releases/tag/v0.1.1
[0.1.0]: https://github.com/yuna0x0/watari-basis/releases/tag/v0.1.0
