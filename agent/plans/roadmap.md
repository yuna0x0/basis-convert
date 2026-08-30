# Roadmap

## Scope

Bring content from other social VR platforms into Basis. Two axes, both open:

- **Content**: avatars first, then props, then worlds.
- **Source platform**: VRChat first, because that is where the content and the demand are.
  Nothing in the architecture assumes it. A different source is a new reader.

The pipeline is three stages, kept apart so those axes stay independent:

```
readers  ->  intermediate model  ->  mappers  ->  writers
(text in)    (plain data)            (pure)       (Unity objects out)
```

Only writers touch Unity objects, which is what keeps the rest testable without an editor open.

## Where things stand

**Done.** VRChat PhysBones and their colliders, legacy Dynamic Bone, all six VRChat constraint
types, and the avatar descriptor. Reading components out of prefabs whose scripts are missing,
resolving each to the transform that carries it, mapping, and writing, driven from an editor
window that scans, reports and converts. Re-running replaces its own output rather than stacking
a second set. The humanoid rig is checked against what Basis's IK needs.

See `../research/physbone-to-jiggle-mapping.md` for the mapping table, and the decisions folder
for why chain splitting is unnecessary (0006) and why repeated conversions carry no stored state
(0007).

**The one thing still open on physics** is tuning. Two mappings are fits rather than conversions,
pull to stiffness and spring to drag, and they need a converted avatar watched in motion next to
the original. Everything else maps directly.

## After that

- **Constraints. Done.** All six VRChat types map onto their Basis equivalents. The three
  hazards were real and are handled: a constraint driving another transform is relocated onto
  that transform, only the first `totalLength` of the 16 inline source slots are read, and
  `SolveInLocalSpace` / `FreezeToWorld` are reported as dropped. Basis's own
  `BasisConstraintConversion` still covers Unity and Animation Rigging constraints, which this
  does not duplicate.
- **Avatar descriptor. Done.** View position, the fifteen visemes and blink map onto
  `BasisAvatar`. The viseme ordering is identical between the two, so it is positional. The
  animator, human scale, renderer list and mouth position are deliberately left for Basis's own
  automatic setup, which fills empty values when its inspector is first opened.
- **Legacy physics. Dynamic Bone done.** Its fields map onto jiggle more directly than
  PhysBones do: damping is drag and inert is ignoreRootMotion, both on the same 0 to 1 scale,
  and the distribution curves are jiggle's curves. One component can drive several roots, which
  becomes one rig each. Magica Cloth is still open; it needs a hand-authored fixture, since no
  reference avatar uses it.

- **Rig readiness for full-body IK. Done.** Full-body tracking itself is not convertible: trackers,
  calibration and the IK solve are all client side, and a VRChat avatar carries no data about
  them. What *is* avatar-side, and does belong here, is whether the rig meets what Basis's IK
  needs. That lives in the FBX importer's humanoid description rather than in components, and
  an editor script can read and write it:

  - Animation Type is Humanoid and the bone mapping is complete, which Basis requires.
  - The Jaw mapping is cleared. The Basis setup docs call it out as usually wrong on imported
    avatars, and it is a one-line fix.
  - Twist and roll bones exist and are named so Basis finds them. Its arm twist support keys off
    child bones whose names contain `twist` or `roll`.
  - Eye bones are mapped, since Basis calibrates gaze from them at load.

  Implemented as a validate-and-offer-to-fix pass reported alongside the conversion diagnostics,
  and skipped entirely for props, which carry physics but no humanoid rig.

  The twist behaviour was checked against the code first, as planned, and the documentation was
  half right. It happens exactly as described, first direct child whose name contains `twist` or
  `roll`, case-insensitively, but in `BasisTransformMapping.FindTwistBone` in `com.basis.common`,
  not in the `BasisFullIKConstraintJob` the constraints page names, which does not exist in the
  shipped source. The IK itself lives in `com.basis.eeriemovement`.
- **Toggles and animation.** Expression menus and FX layers onto HVR Vixxy, ambient motion onto
  `BasisAuthoredMotion`. The least mechanical part by far; expect assisted authoring rather than
  automatic conversion.
- **Modular Avatar.** Works on Basis but has no Basis-specific integration. `MergeArmature` and
  `BoneProxy` mean the authored hierarchy is not the built hierarchy, which is the strongest
  argument for eventually offering a build-time path alongside the destructive one.
- **Props and worlds.** Separate content types, same three stages.

## Constraints that shape all of it

- Everything ships editor-only. See `../decisions/0004-editor-only.md`.
- Conversion is destructive and undoable. See
  `../decisions/0003-destructive-conversion-with-undo.md`.
- Anything approximated or dropped produces a diagnostic. Roughly a third of the PhysBone
  surface has no jiggle equivalent, so silence would be misleading.
- Basis is a moving target: `developer` branch, every package at `0.0.1`, several fields reached
  through `SerializedObject` because they are private. Keep the writer layer thin so breakage
  stays local.

## Open questions

- The display name is provisional, see `../decisions/0002-menu-placement-and-naming.md`.
- The two heuristic mappings need tuning against avatars compared side by side.
- Whether to offer a non-destructive build-time path in addition, and if so whether through NDMF
  or Basis's own build hooks.
