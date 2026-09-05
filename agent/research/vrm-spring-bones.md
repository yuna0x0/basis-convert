# VRM spring bones, as they are actually serialized

Read on 2026-08-31 from UniVRM 0.131.0 in the Unity package cache, from its own sample prefab,
and from a real VRM 1.0 file. Everything here is a fact about the file format, not a guess.

## Two formats, both current

**VRM 0.x** (`com.vrmc.univrm`) puts a whole chain group on one component. **VRM 1.0**
(`com.vrmc.vrm`) puts parameters on each joint and lists the chains on the avatar's
`Vrm10Instance`. VRoid Studio exports 1.0 today; 0.x is what most older avatars are.

## Script identities

Loose `.cs` scripts, so the fileID is always `11500000` and the guid identifies the type. The
guids are unchanged across 0.127.0, 0.128.3 and 0.131.0.

```
00ea06e1753e16f4ca870c39c067c86b  VRMSpringBone                 (0.x)
646b65a4a57afd34d8c4ed557efb46a5  VRMSpringBoneColliderGroup    (0.x)
bfba4ccd3f854e64f868ce83553071a9  Vrm10Instance                 (1.0)
0a942e03b39600e41a1b161e958048f7  VRM10SpringBoneJoint          (1.0)
35bfb658269b2af478e501de243deda6  VRM10SpringBoneCollider       (1.0)
177ea458e237fee41b0902e3006c744b  VRM10SpringBoneColliderGroup  (1.0)
```

## VRM 0.x: `VRMSpringBone`

One component carries the parameters for every chain it names:

```
m_comment, m_stiffnessForce (default 1.0), m_gravityPower, m_gravityDir (default 0,-1,0),
m_dragForce (0..1, default 0.4), m_center (Transform), RootBones (List<Transform>),
m_hitRadius (default 0.02), ColliderGroups (VRMSpringBoneColliderGroup[])
```

`VRMSpringBoneColliderGroup` holds `Colliders`, an array of `{Offset, Radius}`. **Spheres only.**

## VRM 1.0: joints on bones, chains on the instance

`VRM10SpringBoneJoint`, one per bone, serialized exactly as:

```
m_stiffnessForce: 1.2
m_gravityPower: 0
m_gravityDir: {x: 0, y: -1, z: 0}
m_dragForce: 1
m_jointRadius: 0.01
```

Newer versions add `m_anglelimitType`, `m_limitSpaceOffset`, `m_pitch`, `m_yaw`, which older
files do not carry.

`Vrm10Instance` holds the chains under a `SpringBone` block:

```yaml
SpringBone:
  ColliderGroups:
  - {fileID: 1919972564}
  Springs:
  - Name: TailHair
    ColliderGroups:
    - {fileID: 1919972564}
    Joints:
    - {fileID: 1266402392}
    - {fileID: 1288770356}
    Center: {fileID: 20730626558239097}
```

So a chain is an ordered list of joint components, and the bone each sits on is the chain. The
last joint in a spring commonly carries no parameters at all: in the `.vrm` file its entry is
just `{"node": 208}`, which is the tail the chain ends at.

`VRM10SpringBoneCollider` carries `ColliderType`, `Offset`, `Radius`, `Tail`, `Normal`, where the
type is `Sphere, Capsule, Plane, SphereInside, CapsuleInside`. `VRM10SpringBoneColliderGroup`
carries `Name` and `Colliders`.

## What this means for a conversion

- **Parameters are per joint in 1.0.** Jiggle's parameters are curves over normalized distance
  from the chain root, which is the same shape: a chain's joints become the curve's keys rather
  than being averaged. In 0.x there is one value per group, so the curve is flat.
- **A spring names its joints rather than a subtree.** Jiggle simulates everything under the
  root bone, so a bone with children the spring does not list is simulated where VRM left it
  still. Those belong in `excludedTransforms`.
- **`Center` has no equivalent.** It is the transform the simulation is done relative to, which
  is how VRM keeps hair from lagging when the avatar moves.
- **Inside colliders have no equivalent**, the same as VRChat's `insideBounds`.

## Expressions

Both formats keep expressions in assets rather than on the avatar, so the components on the
prefab are followed off disk.

**VRM 0.x**: `VRMBlendShapeProxy` (guid `5b678c1df50cfb547990db24a32856da`) names a
`BlendShapeAvatar` (`329dca3bf78fcdd42b2df941673db76f`) whose `Clips` are `BlendShapeClip` assets
(`37562b39ff933b245ac2f35d87edbcd6`). A clip holds `BlendShapeName`, `Preset`, `Values`
(`{RelativePath, Index, Weight}`), `MaterialValues` and `IsBinary`.

**VRM 1.0**: `Vrm10Instance.Vrm` names a `VRM10Object` (`88684271e27adb843b6570957c9e7637`) whose
`Expression` block has one field per preset plus `CustomClips`, each a `VRM10Expression`
(`8c8b2024ae0d0944eb878d90212bf21b`) holding `MorphTargetBindings`, `MaterialColorBindings`,
`MaterialUVBindings` and `IsBinary`.

**The weight scales differ, and this is the thing to get right.** VRM 1.0 weights run 0 to 1:
`MorphTargetBinding.VRM_TO_UNITY` is 100. VRM 0.x weights are already on Unity's 0 to 100 scale,
which `BlendShapeClipHandler` shows by passing the value straight to `SetBlendShapeWeight`.

**A binding names a blendshape by its index in the mesh**, not by name, so converting one needs
the mesh to translate.

The 0.x `BlendShapePreset` enum, which the asset stores as an int, is: Unknown, Neutral, A, I, U,
E, O, Blink, Joy, Angry, Sorrow, Fun, LookUp, LookDown, LookLeft, LookRight, Blink_L, Blink_R.

## Look at and first person

`VRMFirstPerson` (guid `dedba1309bdf12b42af2362f52eea134`) carries VRM 0.x's `FirstPersonBone`,
`FirstPersonOffset` and a `Renderers` list of `{Renderer, FirstPersonFlag}`. VRM 1.0 keeps the
same two ideas in the `VRM10Object` asset: `LookAt.OffsetFromHead` and `FirstPerson.Renderers`.

`FirstPersonFlag` and `FirstPersonType` share their order in both formats: `Auto, Both,
ThirdPersonOnly, FirstPersonOnly`, so 2 hides a renderer from the wearer and 3 shows it only to
them.

The offset is the useful part: it is the point the camera sits at, measured from the head bone,
which is the same thing VRChat calls the view position and Basis stores as `AvatarEyePosition`
(height and depth relative to the avatar root).

Basis hides the head bone in first person and `BasisHeadChop` scales further transforms to
nothing, skipping the head itself (`BasisLocalAvatarDriver.CollectHeadChopEntries`). A scaled
bone takes its children with it, so the usual VRM cases, a face skinned to the head and hats
parented to it, are already covered. Scaling a skinned renderer's own transform would hide
nothing, so the flags are reported rather than turned into head chop targets.

## The licence

Every VRM carries one. VRM 0.x has a `VRMMeta` component (guid
`690ea0146224b8b4694a1925dddeb352`) naming a `VRMMetaObject` asset
(`63b589176a34b344b9ccbee2b7e7114a`) with `Title`, `Author`, `AllowedUser`, `LicenseType` and
`OtherLicenseUrl`. VRM 1.0 keeps the same in the `VRM10Object` asset's `Meta` block, with
`Authors` as a list and `Modification` as its own field.

`AllowedUser` and `AvatarPermissionType` share their order: `OnlyAuthor`,
`ExplicitlyLicensedPerson` / `onlySeparatelyLicensedPerson`, `Everyone`. VRM 1.0's
`ModificationType` is `prohibited`, `allowModification`, `allowModificationRedistribution`. VRM
0.x says the same through `LicenseType`, where `CC_BY_ND` and `CC_BY_NC_ND` forbid changes.

The rest of the permissions differ between the formats, so a field one has and the other does
not is left unstated rather than guessed:

| Permission | VRM 0.x | VRM 1.0 |
|---|---|---|
| Violence, sexual content | `ViolentUssage`, `SexualUssage` as `UssageLicense` (Disallow, Allow) | `ViolentUsage`, `SexualUsage` as bools |
| Commercial | `CommercialUssage`, allow or not | `CommercialUsage` as `personalNonProfit`, `personalProfit`, `corporation` |
| Political or religious, antisocial or hateful | absent | `PoliticalOrReligiousUsage`, `AntisocialOrHateUsage` |
| Passing it on | absent, implied by the licence type | `Redistribution` |
| Credit | absent | `CreditNotation`: `required`, `unnecessary` |

Converting an avatar changes it, so the licence is read and shown before anything is written.
Nothing is blocked: what the licence permits is the wearer's to judge.

## Checked against a real avatar, 2026-08-31

UniVRM 0.131.2 installed into the Basis project and a real VRM 1.0 imported through it. Four
things came out of that, all of which change what the docs say:

- **UniVRM 0.131.0 does not compile on Unity 6000.5.** 45 `CS0619` errors: `TreeView`,
  `TreeViewItem`, `TreeViewState` and `Object.GetInstanceID()` are obsolete-as-error there, in
  `com.vrmc.gltf`'s editor code and `com.vrmc.vrm`'s runtime. **0.131.2 compiles cleanly** and
  the whole suite passes with it installed. It also resolves alongside `com.unity.cloud.gltfast`,
  which the Basis project already has, without conflict.
- **An imported `.vrm` is binary, so nothing can be read from it directly.** Saving a scene
  instance as a prefab is not enough either: that prefab is a `PrefabInstance` document pointing
  back at the `.vrm`, 2.4 KB of overrides with no components in it. The instance has to be
  **unpacked completely** first, and then the components are materialised into the prefab.
- **With that done, spring bones read correctly.** 49 chains became 49 rigs, 28 colliders
  attached to the chains that name them, and the values match the `.vrm`'s own JSON: stiffness
  0.75, drag 0.05, radius 0.0279333.
- **Expressions, the licence and the eye offset still could not be read**, because
  `Vrm10Instance.Vrm` points at a sub-object inside the binary file. UniVRM's importer has
  **"Extract Meta And Expressions"**, which writes them into the project as assets; that is the
  step that makes them readable. `vrm.objectUnreadable` reports when it has not been done.

`UniHumanoid.Humanoid` (guid `97a39af5b64ede64e86b92b5bf94a0e7`, in `com.vrmc.gltf`) sits on an
imported avatar and was showing up as an unknown script. It records the humanoid bone mapping,
which Unity's own avatar already holds, so it is named and ignored.

## Checked again against extracted assets and a second avatar

Two more avatars, after the first run showed what was missing:

- **UniVRM's own `ModelSetup_SeedSan` sample**, which ships extracted: the `VRM10Object` is a real
  `.asset` and each expression is its own file. Everything read: 9 chains to 9 rigs, 8 colliders,
  18 expressions of which the 5 emotions became controls and 13 were left to Basis, the full
  licence ("Seed-san" by VirtualCast, wearing anyone, changes allowed, credit required), and the
  eye offset `(0, 0.0776, 0.1007)` landing at `(1.41, 0.09)` in the root's space. Its stiffness
  of 4 exercised the clamp.
- **Alicia Solid `vrm-0.51`**, from UniVRM's own test models: 18 chains to 18 rigs, 14 colliders,
  drag 0.7, stiffness 2 clamped, and `vrm.objectUnreadable` correctly raised because it was
  imported in place.

**A current UniVRM migrates a VRM 0.x file to 1.0 components on import.** Alicia's prefab carries
a `Vrm10Instance` and 66 `VRM10SpringBoneJoint`, and no 0.x components at all. So the 0.x reader
covers prefabs authored with an older UniVRM rather than anything a current import produces, and
it is still only verified against the hand-written fixture.

**VRM 1.0 has node constraints** that nothing here reads: `Vrm10RotationConstraint`
(`7a07fbecedce41b4396f286fd7634e1d`), `Vrm10AimConstraint` (`37b0507e4ae49724898ca17cc3db6f1a`)
and `Vrm10RollConstraint` (`1e864293edac89b40b9f79c23e7aa547`), each holding a `Source` transform
and a `Weight`. Basis has equivalents. They are counted and reported as `vrm.constraints` for
now.

## The `.vrm` file itself

A `.vrm` is a glTF binary. `extensions.VRMC_springBone` in the JSON chunk holds the same data
keyed by glTF node index, with `specVersion: "1.0"`. Not read by this package: an avatar has to
be imported into Unity to be a Basis avatar at all, and the import is what turns node indices
into transforms. Recorded because it is the authority on what the parameters mean.

## Checked against the specification and the official samples, 2026-09-06

Read `VRMC_vrm-1.0/expressions.md`, `lookAt.md`, `firstPerson.md`, `meta.md` and
`VRMC_springBone-1.0/README.md` from `vrm-c/vrm-specification`, then ran six sample models from
that repository and UniVRM through the planner: Seed-san, `VRM1_Constraint_Twist_Sample`,
the `isBinary_Overrides` and `isBinary_Overridden` conformance models, the MToon UV animation
test and `AliciaSolid_vrm-0.51`. All import under UniVRM 1.0
(0.51 migrated), all plan, none carries an unrecognised component.

What the spec says that the code did not yet reflect:

- **Expressions are not exclusive.** Each has a value in [0, 1], several may be worn at once, and
  morph targets are accumulated from zero. The selector is a menu representation; the spec is
  quoted in decision 0016. `isBinary` and the three `override*` fields are read now and reported.
- **Blink may move several shapes.** Basis's `BlinkViseme` is an array the blink drivers iterate
  in full (`BasisLocalFacialBlinkDriver`, `BasisRemoteFaceDriver`), so a compound blink is
  written whole. Before, Seed-san's two-shape blink was left unset. Visemes stay one shape each:
  `FaceVisemeMovement` holds one index per viseme.
- **LookAt type.** `lookAt.type` is `bone` or `expression`. Seed-san and both isBinary models use
  `expression` and have no eye bones; reported as `vrm.lookAt.expression`.
- **Material and texture-transform binds** name a material and a property kind (color,
  emissionColor, shadeColor, matcapColor, rimColor, outlineColor; uv scale and offset). Still
  dropped with a diagnostic; an expression made only of them is now reported too. Seed-san's five
  emotions each carry one. Mapping them would mean finding the renderers that use the material
  and writing Vixxy material properties with MToon's shader property names.

What matched: spring joint fields and their meaning, tail joints carrying no parameters,
`center`, sphere and capsule colliders with offset and tail, the extended inside and plane
shapes, node constraints, `offsetFromHeadBone`, first person mesh annotations, and the meta
fields. Alicia 0.51 imports through UniVRM's migration and reads as 1.0 components.

