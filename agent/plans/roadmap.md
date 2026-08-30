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
window that scans, reports and converts, over as much or as little of the avatar as is ticked.
Re-running replaces its own output rather than stacking a second set. The humanoid rig is checked
against what Basis's IK needs.

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
- **Toggles and animation. Partly done.** Menu toggles are rebuilt as HVR Vixxy controls with
  menu items, covering object switching, blendshapes and material properties. On the reference
  avatar 10 of 26 rebuild; the rest are reported with why. What remains:

  - **Toggles in merged animators.** Only 11 of the reference avatar's 26 toggles are traced to
    an animator layer at all, and 10 of those 11 rebuild. The avatar ships several FX
    controllers and the descriptor names one; Modular Avatar's `MergeAnimator` brings the rest
    in at build time. Reading those is worth more for coverage than anything below it, and is
    the first concrete cost of the authored hierarchy not being the built hierarchy.
  - **Puppets.** A radial puppet maps onto a Vixxy control with several choices and a slider
    presentation. Two and four axis puppets have no equivalent.
  - **Ambient motion onto `BasisAuthoredMotion`.** Untouched. Clips that animate over time are
    currently counted and reported, and this is where they would go.

  Material properties are done. A clip sets one channel at a time, `material._Color.r` and its
  siblings, so the channels are gathered back into one property, typed by how they were named:
  r, g, b, a is a colour, x, y, z, w is a vector, no suffix is a float. Channels neither side of
  the toggle sets are read from the material as authored, the same rule blendshapes already
  used. Vixxy applies these through a `MaterialPropertyBlock`, which covers every material on
  the renderer, so a renderer with more than one material gets a diagnostic saying so.

- **Conversion options in the window. Done.** A conversion can be narrowed rather than being all
  or nothing. Basic is five checkboxes over the kinds of thing a conversion produces: physics,
  the colliders those rigs rest on, constraints, the avatar descriptor and menu toggles. Advanced
  adds a checkbox per rig, per constraint and per toggle, with All and None on each list, and the
  two tuning weights.

  The narrowing is a filter over the plan, not a flag threaded through the readers: every scan
  still reads the whole avatar, so the counts and the detected source kind do not change with
  what is ticked. Diagnostics follow the selection, and what was left out is stated as left out
  in the window and in the report. See [decision 0008](../decisions/0008-conversion-options.md).
- **Several source prefabs. Done.** A conversion reads every prefab the hierarchy is built from,
  not just the avatar's own, because clothing, hair and accessories are prefabs of their own
  carrying their own physics. See [decision 0009](../decisions/0009-several-source-prefabs.md).
- **Modular Avatar. Partly done.** Its components are identified and reported for what they are.
  The hierarchy ones, `MergeArmature`, `BoneProxy`, `MeshSettings`, `BlendshapeSync` and
  `Parameters`, do their job on Basis and are left to it. The ones that target VRChat cannot:
  `MergeAnimator` merges into animator layer slots Basis does not have, and `MenuItem` and
  `MenuInstaller` build an expression menu it does not have.

  A menu item and a merged animator read together describe a toggle completely, so those are
  traced and rebuilt as Vixxy controls, with the merged animator's clip paths rebased onto the
  object it was merged at. What remains:

  - **`ObjectToggle`.** The simplest shape, a toggle with no animator at all. Not implemented,
    because there is no instance of it in the reference library to check against.
  - **Gimmick controllers.** A layer is only read as a toggle when a single parameter steers it,
    which is what keeps false positives out. Gimmick packs commonly combine conditions, so their
    toggles are reported rather than rebuilt.
  - Menu items are read per prefab. A menu installed into a submenu keeps its label but not its
    place in a menu tree, since Vixxy menu items are flat.
- **Props and worlds.** Separate content types, same three stages.

## Backlog, outside this package

- **A shared documentation domain.** The docs site is built for GitHub Pages at
  `yuna0x0.github.io/watari-basis/`, which needs no DNS and no extra infrastructure. Hosting all
  of yuna0x0's project documentation together instead, as `docs.yuna0x0.com/watari-basis/` on
  Cloudflare Pages, would read better and would be reusable across projects, at the cost of a
  site that has to be maintained on its own. Out of scope here: moving later is a change to
  `url`, `baseUrl` and a deployment workflow, and the pages themselves do not care.

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
