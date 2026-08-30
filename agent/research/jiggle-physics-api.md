# Jiggle Physics API (conversion target)

Verified 2026-08-30 against the copy vendored in the Basis clone.

Package `com.gator-dragon-games.jigglephysics`, namespace `GatorDragonGames.JigglePhysics`,
assemblies `com.gator-dragon-games.jigglephysics` and `...jigglephysics.editor`.
A fork of [naelstrof/UnityJigglePhysics](https://github.com/naelstrof/UnityJigglePhysics).

There is **no** Basis-side wrapper component. Basis consumes `JiggleRig` directly;
`BasisAvatarFactory.StoreJiggleRigs` collects them off the avatar at load.

## JiggleRig

Only two serialized fields, and **both are private**:

```csharp
[SerializeField] private JiggleRigData jiggleRigData;
[SerializeField] private bool animatedParameters;
```

So authoring from another assembly means `SerializedObject` with property paths prefixed
`jiggleRigData.`. `SetInputParameters()` exists but only reaches `jiggleTreeInputParameters`,
not `rootBone`, `excludedTransforms` or `jiggleColliders`.

`OnValidate` calls `JiggleRigData.OnValidate`, which handles the `serializedVersion` upgrade
path and `RegenerateCacheLookup()`. So `ApplyModifiedProperties()` is enough; then call
`ResampleRestPose()`.

## JiggleRigData

```csharp
public bool hasSerializedData;          // must be set, OnValidate bails without it
public string serializedVersion;        // currently "v0.0.2"
public Transform rootBone;
public bool excludeRoot;                // the inspector's "Motionless Root"
public bool lockFromGrabbing;           // inverted sense on purpose: absent -> false -> grabbable
public float maxGrabStretch;            // 0 means "use the shipped default" (1f)
public JiggleTreeInputParameters jiggleTreeInputParameters;
public Transform[] excludedTransforms;
[HideInInspector] public JiggleTransformCachedData[] transformCachedData;
public JiggleColliderSerializable[] jiggleColliders;
```

`public const int MaxRuntimeJiggleColliders = 32;`

## JiggleTreeInputParameters

Curved (`JiggleTreeCurvedFloat` = `value` + `curveEnabled` + `AnimationCurve curve`, the curve
evaluated over normalized distance from root): `stiffness`, `angleLimit`, `stretch`, `drag`,
`airDrag`, `gravity`, `collisionRadius`.

Plain floats: `soften`, `angleLimitSoften`, `rootStretch`, `ignoreRootMotion`.

UI toggles: `advancedToggle`, `collisionToggle`, `angleLimitToggle`.

Defaults: `stiffness 0.8, angleLimit 0.5, stretch 0.1, rootStretch 0, drag 0.1, airDrag 0,
ignoreRootMotion 0, gravity 1, collisionRadius 0.1, soften 0, angleLimitSoften 0`.

**The curve domain matches VRChat's.** VRChat PhysBone curves are evaluated over normalized
chain distance and so are these, so a PhysBone curve maps onto a jiggle curve directly rather
than being flattened to its scalar. This is the single most useful fact for the mapper.

## Colliders

```csharp
public struct JiggleColliderSerializable { public Transform transform; public JiggleCollider collider; }
public struct JiggleCollider {
    public enum JiggleColliderType { Sphere, Capsule, Plane }
    public enum CapsuleAxis { X, Y, Z }
    public JiggleColliderType type;
    public float radius;
    public float height;
    public CapsuleAxis capsuleAxis;
    public float3 localOffset;
}
```

**The Basis docs are out of date here.** They say only spheres are supported. The code supports
sphere, capsule and plane, which is the same set VRChat has, so collider shape maps 1:1.

## Preset prefabs

`Presets/{JiggleHair,JiggleTail,JiggleBreasts,JiggleRope}.prefab`, each a `JiggleRig` tuned by
the physics author. Use these as the base for a converted rig and overwrite only what the
PhysBone actually determines, rather than inventing starting values.

| preset | stiffness | soften | angleLimit | angleLimitSoften | rootStretch | ignoreRootMotion | stretch | drag | airDrag |
|---|---|---|---|---|---|---|---|---|---|
| Hair | 0.6 | 0.6 | 0.3 | 0.5 | 0.1 | 0.25 | 0.1 | 0.4 | 0.1 |
| Tail | 0.35 | 0.125 | 0.3 (off) | 0.9 | 0.1 | 0.25 | 0.2 | 0.25 | 0.1 |
| Breasts | 0.75 | 0.85 | 0.65 | 1.0 | 0 | 0.462 | 0.55 | 0.033 | 0 |

## Editor niceties already provided

- `JiggleUndoRebuildHook` reseeds the simulation on undo/redo during play, so an undoable
  converter composes with it for free.
- `JiggleRigDataPropertyDrawer` warns when a rig's root bone is a descendant of another rig's
  root. Worth mirroring in the dry run, since chain splitting can produce exactly that.
