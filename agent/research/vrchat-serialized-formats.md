# VRChat serialized formats, as seen without the SDK

Verified 2026-08-30 by reading real prefabs written by VRChat SDK 3.10.3 on Unity 2022.3.22f1.

The VRChat SDK cannot be installed into a Basis project, so in a Basis project every VRChat
component is a missing script. Its serialized data survives intact in the `.prefab` file, which
is what makes this package possible.

## Identifying a component whose script is missing

A `MonoBehaviour` document carries `m_Script: {fileID: F, guid: G, type: T}`. Two shapes:

- **Loose `.cs` script**: `fileID` is always `11500000` and `guid` identifies the script.
  Dynamic Bone is like this.
- **Type inside a DLL**, which is how the VRChat SDK ships: `guid` identifies the *assembly*
  and `fileID` is a hash of the class name, so one guid covers many types. `type: 3`.

So the identity key is the `(guid, fileID)` pair, not the guid alone.

```
2a2c05204084d904aa4945ccff20d8e5 : 1661641543    VRCPhysBone
2a2c05204084d904aa4945ccff20d8e5 : -1631200402   VRCPhysBoneCollider
58e2f01a24261a14cb82e6d3399e8b16 : 1116338486    VRCPositionConstraint
58e2f01a24261a14cb82e6d3399e8b16 : 1788371120    VRCRotationConstraint
58e2f01a24261a14cb82e6d3399e8b16 : -926596935    VRCAimConstraint
67cc4cb7839cd3741b63733d5adf0442 : 542108242     VRCAvatarDescriptor
67cc4cb7839cd3741b63733d5adf0442 : -340790334    VRCExpressionsMenu
67cc4cb7839cd3741b63733d5adf0442 : -1506855854   VRCExpressionParameters
4ecd63eff847044b68db9453ce219299 : -1427037861   PipelineManager
f9ac8d30c6a0d9642a11e5be4c440740 : 11500000      DynamicBone
baedd976e12657241bf7ff2d1c685342 : 11500000      DynamicBoneCollider
4e535bdf3689369408cc4d078260ef6a : 11500000      DynamicBonePlaneCollider
```

`VRCParentConstraint`, `VRCScaleConstraint` and `VRCLookAtConstraint` fileIDs are still unknown,
because the reference avatars do not use them. Recover them by extracting
`VRC.SDK3.Dynamics.Constraint.dll` from a VCC package zip and running `ilspycmd`.

Guids are stable in practice but are not a contract, which is why the reader **reports**
unrecognised identities rather than skipping them.

## VRCPhysBone serialized field order

Editor foldout state serializes first and is noise: `foldout_transforms`, `foldout_forces`,
`foldout_collision`, `foldout_stretchsquish`, `foldout_limits`, `foldout_grabpose`,
`foldout_options`, `foldout_gizmos`.

Then: `version`, `integrationType`, `rootTransform`, `ignoreTransforms`, `endpointPosition`,
`multiChildType`, `pull`(+Curve), `spring`(+Curve), `stiffness`(+Curve), `gravity`(+Curve),
`gravityFalloff`(+Curve), `immobileType`, `immobile`(+Curve), `allowCollision`,
`collisionFilter`, `radius`(+Curve), `colliders`, `limitType`, `maxAngleX`(+Curve),
`maxAngleZ`(+Curve), `limitRotation`, `limitRotationX/Y/ZCurve`, `allowGrabbing`, `grabFilter`,
`allowPosing`, `poseFilter`, `snapToHand`, `grabMovement`, `maxStretch`(+Curve),
`maxSquish`(+Curve), `stretchMotion`(+Curve), `isAnimated`, `resetWhenDisabled`, `parameter`,
`showGizmos`, `boneOpacity`, `limitOpacity`.

Enums: `IntegrationType { Simplified=0, Advanced=1 }`, `MultiChildType { Ignore=0, First=1,
Average=2 }`, `LimitType { None=0, Angle=1, Hinge=2, Polar=3 }`, `ImmobileType { AllMotion=0,
WorldMotion=1 }`.

### Three traps

1. **An empty curve, `m_Curve: []`, means "no falloff", that is constant 1.0.** Reading it as
   zero would silently flatten every converted rig.
2. `integrationType` changes what `spring` and `stiffness` mean. Both values are common in the
   wild: 1328 Simplified against 2045 Advanced across the reference project.
3. `version` is `0` (PhysBone 1.0) or `1` (1.1) with different spring semantics, and both are
   common: 1716 against 917. Key the mapper off the `(version, integrationType)` pair.

## VRCPhysBoneCollider

`rootTransform`, `shapeType` (`Sphere=0, Capsule=1, Plane=2`), `insideBounds`, `radius`,
`height`, `position`, `rotation`, `bonesAsSpheres`.

`insideBounds` (keep bones *inside* the collider) and `bonesAsSpheres` have no jiggle
equivalent.

## Other formats worth knowing

- `VRCAvatarDescriptor.VisemeBlendShapes` is a `string[15]` in the same order Basis uses for
  `FaceVisemeMovement`, so viseme mapping is positional.
- `customEyeLookSettings.eyelidsBlendshapes` is an `int[]` that Unity writes as a **hex byte
  blob**, for example `1d000000ffffffffffffffff` meaning `{29, -1, -1}`, little-endian int32.
  It is not a string.
- `baseAnimationLayers` / `specialAnimationLayers` entries carry a `type` field. **The ordering
  usually quoted is wrong.** Decompiled from `VRCSDK3A.dll`
  (`VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType`) it is:

  ```
  Base = 0, Deprecated0 = 1, Additive = 2, Gesture = 3,
  Action = 4, FX = 5, Sitting = 6, TPose = 7, IKPose = 8
  ```

  The commonly cited version omits `Deprecated0` and shifts everything after it by one. Using it,
  a real avatar's layers read as Base, Action, FX, Sitting when they are actually Base, Gesture,
  Action, FX. Plausible enough to go unnoticed, and it makes the FX layer look like Sitting.
  Key off `type`, never off array position.
- VRC constraints serialize `Sources` as **16 fixed inline slots plus an `overflowList`**, where
  only the first `totalLength` entries are meaningful. Slots past that are defaults.
- VRC constraints have a `TargetTransform` that lets them drive a transform other than their
  own. Unity's and Basis's constraints always drive their own GameObject. This is the biggest
  hazard in constraint conversion.
