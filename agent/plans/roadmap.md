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

**Done.** Reading VRChat components out of prefabs whose scripts are missing, resolving each to
the bone that carries it, and mapping PhysBones onto jiggle parameters. See
`../research/physbone-to-jiggle-mapping.md`.

**Next, to finish avatar physics.**

1. **Chain splitting.** One PhysBone whose root has several child chains must become one
   `JiggleRig` per chain. Basis is explicit that a left and right pair needs separate rigs or
   only one side moves. Needs hierarchy, so it sits between the mapper and the writer.
2. **The writer.** Add `JiggleRig` via `Undo.AddComponent`, copy the chosen preset's
   `jiggleRigData`, then overwrite only the fields the source actually determined. Writing goes
   through `SerializedObject` because `jiggleRigData` is private. `ApplyModifiedProperties`
   triggers `OnValidate`, which regenerates the cache; then `ResampleRestPose`.
3. **The window.** Dry run first, always: what was found, what it will produce, and every
   diagnostic, before anything is written. Then convert under one undo group, and keep a report.

## After that

- **Constraints.** VRChat's constraint components onto the 14 Basis types. Basis already ships
  `BasisConstraintConversion` for the Unity and Animation Rigging ones, so only the VRChat-native
  ones need us. Three hazards: VRChat constraints can drive a transform other than their own,
  their source list is 16 fixed slots plus an overflow list where only `totalLength` entries are
  real, and `SolveInLocalSpace` / `FreezeToWorld` have no equivalent anywhere.
- **Avatar descriptor.** Viseme, blink and eye settings onto `BasisAvatar`. The viseme ordering
  is identical, so it is positional. NDMF's `CommonAvatarInfo` already covers part of this.
- **Legacy physics.** Dynamic Bone, whose fields map onto jiggle almost directly, and Magica
  Cloth. Same pipeline, new readers.
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
