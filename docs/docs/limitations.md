---
sidebar_position: 7
---

# Limitations

Worth reading before you rely on a conversion. Everything here is reported by the tool as well;
this is the same information in one place.

## Not converted at all

- **VRChat contacts.** Basis has no contact system, so anything driven by touch is dropped.
- **Two-axis and four-axis puppets.**
- **Animation that moves or scales something over time.** Only rotation is baked, so a toggle
  or a layer that animates anything else over time is reported rather than half converted. See
  [Authored motion](what-converts/authored-motion.md).
- **Expression parameters as a system.** Vixxy controls hold their own state, so there is no
  parameter list to recreate.
- **Custom animation layers**, gestures, sitting and IK poses.

## Converted with a caveat

- **Two physics settings are fits**, not conversions. See [Physics](what-converts/physics.md).
- **Wide angle limits are dropped** rather than clamped to something tighter.
- **Material properties are applied through a property block**, which covers every material on a
  renderer. A renderer with more than one material is reported.
- **Modular Avatar toggles** are rebuilt only when a single parameter of the avatar's own steers
  their layer.
- **A toggle that waited on a VRChat parameter no longer waits.** `IsLocal`, `InStation`,
  `Seated` and the rest have no Basis equivalent, so a control guarded by one switches whenever
  it is used. The report names the guard that was dropped.
- **Authored motion carries rotation only**, which is what a baked Basis motion clip holds. A
  clip that also moves or scales something keeps the turning and reports the rest.
- **A baked motion clip is a project asset**, so an undo removes the components a conversion
  wrote but leaves the clip on disk.
- **A radial puppet becomes a slider between the two ends of its blend tree.** Vixxy interpolates
  in a straight line between choices, so motions the tree held in between are approximated by
  that line.

## Where the data comes from

Component data is read from prefab files, because in a Basis project the VRChat components are
missing scripts and only the file still holds their values. Two things follow from that:

- **The avatar has to still be linked to its prefab.** If the prefab was unpacked, there is
  nothing left to read.
- **A change made to a prefab instance in the scene, rather than to the prefab, is not seen.**
  Collider assignments are commonly made that way, and show up as an unresolved collider
  reference in the report.

## Things a conversion does not touch

Materials and shaders, meshes, the avatar's animator, and anything Basis fills in itself when the
`BasisAvatar` inspector is first opened.
