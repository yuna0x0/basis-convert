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

**Done.** The whole PhysBone path: reading components out of prefabs whose scripts are missing,
resolving each to the bone that carries it, mapping onto jiggle parameters and colliders, writing
the rigs, and an editor window that scans, reports and converts. See
`../research/physbone-to-jiggle-mapping.md`.

Chain splitting turned out to be unnecessary, see `../decisions/0006`.

**Next, to finish avatar physics.**

1. **Tune the two heuristic mappings** against avatars compared side by side in motion. Nothing
   else here is guesswork; these two are.
2. **Re-running should update rather than duplicate.** Needs bookkeeping tying each rig to the
   component it came from, on an `EditorOnly` GameObject per decision 0004.

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
