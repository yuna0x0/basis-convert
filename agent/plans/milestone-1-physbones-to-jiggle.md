# basis-convert: VRChat to BasisVR avatar converter

## Context

The 2026-08-29 research (stored in memory as `basisvr-migration-research`) concluded that
migrating a VRChat avatar to BasisVR is roughly 70% mechanical asset/component remapping and
30% semantic re-authoring, and that the mechanical part is worth building as a Basis-side
editor tool. Nothing was implemented then; the project was parked.

This plan picks that up and scaffolds `basis-convert`: a UPM package installed into a Basis
project that converts an imported VRChat avatar's components into their Basis equivalents,
starting with VRChat PhysBones to BasisVR Jiggle Physics.

The problem it solves: a user exports their avatar from a VRChat project as a `.unitypackage`
(source assets only) and re-imports it into a Basis project. The VRChat SDK cannot be installed
there (it irreversibly mangles project settings), so every VRC component arrives as a missing
script. All of the avatar's dynamics data is sitting in the prefab file, unreadable by hand.
`basis-convert` recovers that data and writes real Basis components from it.

Intended outcome for milestone 1: select an imported avatar, hit Convert, get working
`JiggleRig` components with parameters derived from the original PhysBones, plus an honest
report of everything approximated or dropped.

---

## Established facts (verified in this session, do not re-derive)

**Target environment**
- Unity `6000.5.10f1` — newest 6.5 release, matches what Basis targets, already installed.
  Basis clone: `~/Documents/Projects/Basis`, Unity project root is the `Basis/` subfolder.
- No Unity editor is currently running. The Basis clone sits on `yuna0x0`, which is
  0 commits behind `origin/developer` plus one additive avatar commit, with a working tree
  full of regenerable Unity output. Step 0 below stashes that and moves to a fresh
  `basis-convert-dev` branch off `origin/developer`.
- **Tooling available on this machine, all verified:**
  - `unity` CLI — `unity test <project>` runs EditMode tests headless. **This is the primary
    dev loop**; the whole mapper layer is designed to be testable without opening the editor.
  - `vpm` 0.1.28 (VRChat Package Manager CLI) — use it to pull MA/NDMF into the Basis project
    at pinned versions and to add Haï's unofficial listing, instead of hand-editing
    `manifest.json` or clicking through ALCOM.
  - `openupm` 4.5.2 — for the M7 publish, and for resolving dependency versions before then.
  - `gh` — used above to check MA/NDMF releases; keep using it rather than guessing versions.
  - `ilspycmd` at `~/.dotnet/tools/ilspycmd` — needed at M2 to recover the three
    `VRCConstraint*` fileIDs that the reference project does not use.

**Basis avatar API surface**
- `Basis.Scripts.BasisSdk.BasisAvatar` (asmdef `BasisSDK`) is the VRCAvatarDescriptor
  equivalent. `FaceVisemeMovement` is a 15-int array in *exactly* VRChat's viseme order
  (`sil, PP, FF, TH, DD, kk, CH, SS, nn, RR, aa, E, ih, oh, ou`).
  `AvatarEyePosition` is a `Vector2` == `(viewPosition.y, viewPosition.z)`.
- `AvatarHelper` (`com.basis.sdk/Scripts/Editor/AvatarHelper.cs`) already contains a
  `vrc.v_*` / `vrc.blink` blendshape name table. Basis is already half-aware of VRChat naming.
- `BasisConstraintConversion.TryConvert` / `.ConvertHierarchy`
  (`com.basis.sdk/Scripts/Constraints/BasisConstraintConversion.cs`) already converts Unity
  built-in and Animation Rigging constraints to the 14 `Basis*Constraint` types. **Reuse it**;
  only VRC-native `VRCConstraint*` needs our own mapping.
- `HVR.Vixxy.HVRVixxyControl` (`dev.hai-vr.basis.comms`) is the toggle/menu target. Most of its
  fields are `internal`, so authoring it from outside needs `SerializedObject`.
- NDMF 1.14.8 is vendored (which is also upstream latest, 2026-08-29);
  `HVR.Basis.NDMF.BasisFrameworkPlatform` is the `[NDMFPlatformProvider]`. NDMF apply-on-play
  is force-disabled in Basis because it corrupts JiggleRig rest state.
- **Modular Avatar: use >= 1.18.4.** The Basis clone resolves 1.18.3 only because its
  `manifest.json` points at the `bdunderscore/modular-avatar` git HEAD from before the release.
  **1.18.4 (2026-08-29) is the first version with Unity 6 support** — "[#2015] Experimental
  support for Unity 6.1~6.7" — and 1.18.5 landed 2026-08-30 with a Floor Adjuster fix. Anything
  earlier than 1.18.4 is not Unity 6 capable at all, so pin at or above it. This also means
  upstream MA may no longer need Haï's `-unofficial.basis.1` fork; **verify which of the two to
  target before starting M6** rather than assuming the fork is still required.
  Incidental prior art: 1.18.4's "[#2103] Fixed Modular Avatar editor actions that could not be
  undone or whose prefab-instance changes were not saved" is the same undo/prefab-instance
  problem our destructive converter has to solve — worth reading that fix before writing ours.

**Two hard runtime constraints that shape the design**
1. **Content Police allow-list.** `com.basis.sdk/Settings/AvatarContentPoliceSelector.asset`
   is a 159-entry allow-list of component type names permitted on a loaded avatar. It contains
   `GatorDragonGames.JigglePhysics.JiggleRig`, `BasisAuthoredMotion` and all 14 constraints —
   but obviously nothing of ours. **Our package must never leave a runtime component on the
   avatar.**
2. **`EditorOnly` tag stripping.** `BasisAssetBundlePipeline.DestroyEditorOnlyInAvatar`
   destroys any child tagged `EditorOnly` before the build. This is the sanctioned place for
   converter bookkeeping.

**Jiggle target API** (`com.gator-dragon-games.jigglephysics`, ns `GatorDragonGames.JigglePhysics`)
- `JiggleRig` has exactly two serialized fields, both **private**: `jiggleRigData`
  (`JiggleRigData` struct) and `animatedParameters` (bool). Authoring must go through
  `SerializedObject` with property paths prefixed `jiggleRigData.`.
- `JiggleRigData`: `rootBone`, `excludeRoot`, `lockFromGrabbing`, `maxGrabStretch`,
  `jiggleTreeInputParameters`, `excludedTransforms[]`, `jiggleColliders[]`,
  plus `hasSerializedData` / `serializedVersion` (`"v0.0.2"`) which **must** be set.
- `JiggleTreeInputParameters`: `stiffness`, `angleLimit`, `stretch`, `drag`, `airDrag`,
  `gravity`, `collisionRadius` are `JiggleTreeCurvedFloat` (`value` + `curveEnabled` +
  `AnimationCurve curve`, evaluated over normalized distance from root); `soften`,
  `angleLimitSoften`, `rootStretch`, `ignoreRootMotion` are plain floats; three UI toggles
  `advancedToggle`, `collisionToggle`, `angleLimitToggle`.
- `JiggleCollider.JiggleColliderType` is `Sphere | Capsule | Plane` — the docs claiming
  "sphere only" are **out of date**; the code supports all three, same set as VRChat.

---

## Deliverable 1: the repository skeleton

`~/Documents/Projects/basis-convert` (currently empty) becomes a git repo, developed
standalone and symlinked into the Basis clone so it compiles against the real assemblies.

```
basis-convert/
  Packages/com.yuna0x0.basis.convert/     <- the shipped package
    package.json
    LICENSE                MIT, "Copyright (c) 2026 yuna0x0 <yuna@yuna0x0.com>"
    README.md
    Editor/
      yuna0x0.Basis.Convert.Editor.asmdef
    Tests/Editor/
      yuna0x0.Basis.Convert.Editor.Tests.asmdef
      Fixtures/                           <- sanitised prefab/YAML fixtures
  agent/                                  <- committed AI/agent knowledge base
    README.md            what this folder is, and the no-PII rule
    research/            findings, API inventories, doc extracts
    plans/               design docs and this plan, kept current
    worklog/             YYYY-MM-DD.md, one entry per session
    decisions/           short ADR-style notes for anything non-obvious
  docs/                                   <- Docusaurus site, added after M2
  AGENTS.md                               <- entry point; CLAUDE.md symlinks to it
  .gitignore, .editorconfig, .gitattributes
```

`AGENTS.md` points future sessions at `agent/`, states the Unity/Basis versions, the symlink
dev setup, and the `unity test` command. **Hygiene rules, stated in `agent/README.md` and
backed by `.gitignore`:**
- The only identity that may appear anywhere is `yuna0x0 <yuna@yuna0x0.com>`. Never the login
  email, never a real name, never machine-specific absolute paths in committed docs (write
  `~/Projects/...` or repo-relative).
- No third-party asset source or binaries, ever: not Booth avatars, not the VRChat SDK, not
  Dynamic Bone or Magica Cloth 2. Script GUIDs and field names are facts about a file format
  and are fine to record; the files themselves are not ours to redistribute.
- Fixtures are hand-authored minimal prefab YAML checked into `Tests/Editor/Fixtures`, never
  a real purchased model.

Dev link (created once, not committed):
```
ln -s ~/Documents/Projects/basis-convert/Packages/com.yuna0x0.basis.convert \
      ~/Documents/Projects/Basis/Basis/Packages/com.yuna0x0.basis.convert
```

`package.json`, following the conventions read out of `com.basis.sdk` and
`dev.hai-vr.basis.comms`:
- `name: "com.yuna0x0.basis.convert"`, `displayName: "Basis Convert"`, `version: "0.0.1"`
- `author: { "name": "yuna0x0", "url": "https://github.com/yuna0x0" }`, `license: "MIT"`
- `unity: "6000.5"`
- Basis deps go in `vpmDependencies` (repo convention), **not** `dependencies` — and the
  actual compile-time gating is done by asmdef `versionDefines` + `defineConstraints` so the
  package degrades cleanly when a dependency is absent. Copy the idiom from
  `dev.hai-vr.basis.ndmf/Scripts/HVR.Basis.NDMF.asmdef`.

asmdef `yuna0x0.Basis.Convert.Editor`: `includePlatforms: ["Editor"]`, references `BasisSDK`,
`BasisSDKEditor`, `BasisDebug`, `com.gator-dragon-games.jigglephysics`.

Code style follows the Basis repo: 4-space indent, Allman braces, PascalCase public fields,
`_camelCase` private, public fields over auto-properties, and logging through `BasisDebug`
with a `LogTag` rather than `Debug.Log`.

---

## Deliverable 2: architecture

Three layers, deliberately separated so the mapper can be unit-tested with no Unity scene and
so a second front-end (the VRC-side JSON exporter) and a second back-end (an NDMF pass) can be
added later without touching the middle.

```
  FRONT-ENDS                 INTERMEDIATE MODEL              EMITTERS
  ----------                 ------------------              --------
  PrefabYamlSource   \                                 /  JiggleRigEmitter
  (M1: reads missing  \   SourceAvatar                 /   ConstraintEmitter   (M2)
   scripts from the    >   .PhysBones[]        ----->  <   BasisAvatarEmitter  (M3)
   .prefab by GUID)   /    .PhysBoneColliders[]        \   AuthoredMotionEmitter (M4)
                     /     .Constraints[]               \  VixxyEmitter        (M5)
  ManifestSource    /      .Descriptor
  (M6: JSON written        .DynamicBones[]
   by a VRC-side tool)     .MagicaCloth[]
                                  |
                           ConversionReport
                           (Converted / Approximated / Unsupported / Skipped)
```

**Front-end, M1 — `PrefabYamlSource`.** Unity keeps a missing script's serialized YAML intact
in the `.prefab` file, so the data survives the SDK's absence. **Verified against a real
avatar prefab in `yuna0x0-vrc-avatar`** — a live `VRCPhysBone` block reads:

```yaml
--- !u!114 &5276676806667862006
MonoBehaviour:
  m_GameObject: {fileID: 61254327368223374}
  m_Script: {fileID: 1661641543, guid: 2a2c05204084d904aa4945ccff20d8e5, type: 3}
  version: 1
  integrationType: 0
  rootTransform: {fileID: 0}
  ignoreTransforms: []
  endpointPosition: {x: 0, y: 0, z: 0}
  multiChildType: 0
  pull: 0.2
  pullCurve: {serializedVersion: 2, m_Curve: [], m_PreInfinity: 2, m_PostInfinity: 2, ...}
  ...
```

Two things this proves, both of which shape the reader:
- Every PhysBone field, including the curves, is present in plain text. The approach works.
- VRC SDK scripts live in a **DLL** (`type: 3`), so the identity key is the pair
  `(guid of the assembly, fileID = class-name hash)`, **not** the `fileID: 11500000` used by
  loose `.cs` scripts. `VRCPhysBone` is `(2a2c05204084d904aa4945ccff20d8e5, 1661641543)`.
  Dynamic Bone, by contrast, is a loose script: guid `f9ac8d30c6a0d9642a11e5be4c440740`,
  fileID `11500000`. The table must handle both shapes.

Seed table (all harvested from real files in `yuna0x0-vrc-avatar`, written by VRChat SDK
3.10.3 / Unity 2022.3.22f1):

```
2a2c05204084d904aa4945ccff20d8e5 : 1661641543    VRCPhysBone
2a2c05204084d904aa4945ccff20d8e5 : -1631200402   VRCPhysBoneCollider
58e2f01a24261a14cb82e6d3399e8b16 : 1116338486    VRCPositionConstraint
58e2f01a24261a14cb82e6d3399e8b16 : 1788371120    VRCRotationConstraint
58e2f01a24261a14cb82e6d3399e8b16 : -926596935    VRCAimConstraint
67cc4cb7839cd3741b63733d5adf0442 : 542108242     VRCAvatarDescriptor
67cc4cb7839cd3741b63733d5adf0442 : -340790334    VRCExpressionsMenu
67cc4cb7839cd3741b63733d5adf0442 : -1506855854   VRCExpressionParameters
f9ac8d30c6a0d9642a11e5be4c440740 : 11500000      DynamicBone
baedd976e12657241bf7ff2d1c685342 : 11500000      DynamicBoneCollider
4e535bdf3689369408cc4d078260ef6a : 11500000      DynamicBonePlaneCollider
```

`VRCParentConstraint`, `VRCScaleConstraint` and `VRCLookAtConstraint` are unused in the
reference project so their fileIDs are unknown; get them at M2 by extracting
`VRC.SDK3.Dynamics.Constraint.dll` from a VCC package zip under
`~/.local/share/VRChatCreatorCompanion/Repos/` and running `ilspycmd` (already installed).
Because the table is guesswork-free but incomplete, the reader must **report unknown
`m_Script` identities rather than skip them silently** — that is how the table grows.

The reader:
1. Parses the prefab's YAML documents (`--- !u!114 &<fileID>` MonoBehaviour blocks, plus
   `!u!1` GameObject and `!u!4` Transform blocks to rebuild the hierarchy).
2. Filters MonoBehaviours by their `m_Script` `(guid, fileID)` pair against a table of known
   VRC SDK / Dynamic Bone / Magica Cloth script identities, kept as data in
   `Editor/Sources/KnownScriptIdentities.cs` so a new one is a one-line addition.
3. Resolves each YAML `fileID` back to the live `UnityEngine.Object` by calling
   `AssetDatabase.TryGetGUIDAndLocalFileIdentifier` over `AssetDatabase.LoadAllAssetsAtPath`
   and building a `long -> Object` map. **fileID matching, not name-path matching** — avatars
   routinely have duplicate bone names, and Basis only auto-renames those at build time.
4. Emits plain POCOs. No Unity types beyond `Transform` references and `AnimationCurve`.

Deliberately simple YAML handling: Unity's prefab YAML is a restricted, regular subset, and
the fields we need are flat scalars, `{x:,y:,z:}` vectors, `{fileID:}` references and
`AnimationCurve` blocks. A focused hand-written scanner is more predictable here than pulling
in a general YAML dependency, and keeps the package dependency-free.

**Middle — the model + mapper.** Pure C#, no scene access, fully unit-testable. `PhysBoneData`
mirrors the VRC fields; `JiggleRigPlan` mirrors what will be written. The mapper is
`PhysBoneToJiggleMapper.Map(PhysBoneData, MappingProfile) -> JiggleRigPlan + Diagnostics[]`.

**Back-end, M1 — `JiggleRigEmitter`.** Takes a `JiggleRigPlan`, adds a `JiggleRig` via
`Undo.AddComponent`, writes `jiggleRigData.*` through `SerializedObject`, then
`ApplyModifiedProperties()` (which fires `JiggleRig.OnValidate` -> `JiggleRigData.OnValidate`
-> `RegenerateCacheLookup`, so the cache and the `serializedVersion` upgrade path are handled
for us) and finally `ResampleRestPose()`. Every mutation goes through `Undo` so one Ctrl+Z
reverts the whole conversion; the jiggle package already ships a `JiggleUndoRebuildHook` that
reseeds the simulation on undo during play.

---

## Deliverable 3: milestone 1, PhysBones to Jiggle Physics

### The mapping

VRChat's per-bone `AnimationCurve`s are evaluated over normalized chain distance, and
`JiggleTreeCurvedFloat.curve` is evaluated over `normalizedDistanceFromRoot`. That is the same
domain, so every curved PhysBone parameter maps onto a curved jiggle parameter directly, not
just its scalar value. This is the single most valuable finding for M1.

| PhysBone | JiggleRigData | Confidence |
|---|---|---|
| `rootTransform` (or self) | `rootBone` | exact |
| `ignoreTransforms` | `excludedTransforms` | exact |
| `multiChildType == Ignore` | `excludeRoot = true` | exact |
| `immobile` (+ curve) | `ignoreRootMotion` | exact, same 0..1 sense |
| `gravity` (+ curve) | `gravity` (+ curve) | exact |
| `radius` (+ curve) | `collisionRadius` (+ curve), `collisionToggle = true` | exact |
| `allowGrabbing != None` | `lockFromGrabbing = false` | exact |
| `maxStretch` | `maxGrabStretch` | exact |
| `stretchMotion` | `stretch` | close |
| `limitType` Angle(1)/Hinge(2), `maxAngleX` | `angleLimit = maxAngleX / 90`, `angleLimitToggle` | close |
| `pull` + `stiffness` (+ curves) | `stiffness` (+ curve) | **heuristic, needs tuning** |
| `spring` (Simplified) / momentum (Advanced) | `drag` (inverse relationship) | **heuristic, needs tuning** |
| `colliders[]` -> `shapeType` Sphere/Capsule/Plane | `jiggleColliders[]`, same three types | exact |
| `limitType` Polar(3), `maxAngleX`+`maxAngleZ` | approximated to a single `angleLimit` | approximated, warn |
| `limitRotation` (+ 3 axis curves) | no equivalent | dropped, warn |
| `gravityFalloff` | no equivalent | dropped, warn |
| `maxSquish` | no equivalent | dropped, warn |
| `parameter`, `isAnimated` | no equivalent (Vixxy territory, M5) | dropped, report |
| `allowPosing`, `grabMovement`, `snapToHand` | no equivalent | dropped, warn |
| `endpointPosition` | no equivalent; jiggle derives its own endpoint | dropped, warn if non-zero |

Three traps confirmed in the real data, all of which need explicit test coverage:
- **`integrationType`** is `Simplified(0)` or `Advanced(1)` and they interpret `spring`/`stiffness`
  differently. Both are common in practice (1328 vs 2045 instances in the reference project).
- **`version`** is `0` (PhysBone 1.0) or `1` (1.1), again with different spring semantics, and
  again both are common (1716 vs 917). Key the mapper off `(version, integrationType)`.
- **An empty curve `m_Curve: []` means "no falloff", i.e. constant 1.0** — not zero. Getting
  this backwards would silently flatten every jiggle rig.

VRC collider shapes map onto `JiggleCollider` cleanly, but note `insideBounds` (VRC can invert
a collider to keep bones *inside* it) and `bonesAsSpheres` have no jiggle equivalent; warn.

The two heuristic rows are why conversion is destructive-with-undo rather than a build-time
pass: the user has to be able to open the resulting `JiggleRig` and tune it by hand, and keep
that tuning.

Rather than invent starting values, the mapper **starts from the jiggle package's own preset
prefabs** — `com.gator-dragon-games.jigglephysics/Presets/{JiggleHair,JiggleTail,JiggleBreasts,
JiggleRope}.prefab`, each a `JiggleRig` with values tuned by the physics author. For example
`JiggleHair` is `stiffness 0.6 / soften 0.6 / angleLimit 0.3 / angleLimitSoften 0.5 /
rootStretch 0.1 / ignoreRootMotion 0.25 / stretch 0.1 / drag 0.4 / airDrag 0.1`, while
`JiggleBreasts` is `stiffness 0.75 / soften 0.85 / drag 0.033 / airDrag 0`. The emitter copies
the chosen preset's `jiggleRigData`, then overwrites only the fields the mapping table can
derive from the PhysBone, leaving the rest at author-tuned defaults. Preset choice is per-rig,
defaulted by a name heuristic on the root bone (hair / tail / ear / skirt / breast / rope) and
overridable in the dry-run table. That keeps the guesswork small, visible and revisable.

### Chain splitting

A single PhysBone whose root has several child chains (the standard "one PhysBone on the whole
hair root" setup) must become **one `JiggleRig` per chain** — the Basis docs are explicit that
left and right need separate rigs or only one side moves. The converter walks the chain
topology from `rootTransform`, honours `ignoreTransforms`, and:
- single chain -> one rig on the root;
- multiple chains with `multiChildType == Ignore` -> one rig per chain, `excludeRoot = true`;
- multiple chains with `First` / `Average` -> convert as `Ignore` and warn, since jiggle has no
  equivalent blending of a shared root.

### UI

Menu `Tools/<ProductName>/Convert VRChat Avatar` — **not** under Basis's own `Basis/` menu.
See "Naming, menus and trademark" below for why. An IMGUI `EditorWindow` built with
`BasisEditorUI` (`com.basis.sdk/Scripts/Editor/UI/BasisEditorUI.cs`) so it looks native:
1. Object field for the avatar (scene instance or prefab asset).
2. **Dry run by default** — a table of every PhysBone found, the rigs it will produce, and the
   per-item diagnostics, before anything is written.
3. Mapping profile selector.
4. Convert button. Writes under one `Undo` group; leaves the source data untouched.
5. Post-conversion report, also written to a `ConversionReport` ScriptableObject asset so it
   survives the window closing.

Bookkeeping (which PhysBone produced which rig, so a re-run can update instead of duplicate)
goes on a child GameObject tagged `EditorOnly`, which `DestroyEditorOnlyInAvatar` strips at
build time, keeping us clear of the Content Police allow-list.

### Naming, menus and trademark

Basis's `TRADEMARK.md` is short and specific: MIT covers the code, but the **Basis / BasisVR /
Basis Framework names and the logo remain trademarked and require permission**. It explicitly
asks third parties to "avoid claiming implicit or explicit affiliation or endorsement", while
explicitly permitting descriptive, truthful reference — "Built with Basis" is called out as
fine. There is no third-party plugin authoring guide in `BasisDocs`, so this plus observed
convention is the whole rulebook.

Putting our tool at `Basis/Tools/...` would make it look like a shipped Basis feature. That is
precisely the implied affiliation the policy asks us to avoid, and every piece of evidence
points the same way:

- **Basis owns `Basis/`.** All ~60 first-party menu items live there.
- **Haï does not use it.** `dev.hai-vr.basis.comms` ships *inside* the Basis repo and is about
  as blessed as a third-party package gets, yet it registers zero `MenuItem`s and namespaces
  every component under its own vendor prefix, `AddComponentMenu("HVR.Basis/...")`.
- **The wider ecosystem uses `Tools/<Product>/`.** NDMF uses `Tools/NDM Framework/...` +
  `GameObject/NDM Framework/...`; Modular Avatar uses `Tools/Modular Avatar/...`,
  `GameObject/Modular Avatar/...`, `Assets/Modular Avatar/...`, and
  `AddComponentMenu("Modular Avatar/MA ...")`.
- **Unity says the same.** The Asset Store submission guidelines direct editor extensions to
  nest under an existing menu such as `Window/<PackageName>`, or under `Tools` when nothing
  fits — never a new top-level menu.

So the convention to follow is:

| surface | path |
|---|---|
| tool windows | `Tools/<ProductName>/...` |
| hierarchy context actions | `GameObject/<ProductName>/...` |
| project context actions | `Assets/<ProductName>/...` |
| any component (all `EditorOnly`) | `AddComponentMenu("<ProductName>/...")` |

**This also puts the working name in question.** "basis-convert" uses the trademarked name as
the product name rather than descriptively, which is the shape the policy warns about. The
rename the user already planned should land on a distinct product name that reads as
third-party, with the Basis reference demoted to a descriptive subtitle — `displayName` as the
product name, `description` saying something like "Converts VRChat avatars for use with Basis".
If the preferred name does keep "Basis" in it, ask the Basis project for written permission
first; `TRADEMARK.md` invites exactly that ("Contact us if unsure"). Either way the rDNS stays
`com.yuna0x0.*`, which already signals third-party ownership. Treat the current
`com.yuna0x0.basis.convert` as provisional and settle it before the first public release,
because changing a UPM package name after publication is disruptive.

### Tests

EditMode tests in `Tests/Editor`, run headless with
`unity test ~/Documents/Projects/Basis/Basis --platform EditMode`.
- Mapper tests: pure, no scene. Each mapping-table row gets a case, including the curve
  domain mapping and every diagnostic that should be raised.
- YAML reader tests: hand-written minimal fixture prefabs checked into `Tests/Editor/Fixtures`
  (a two-bone chain, a multi-chain root, one with colliders, one with `ignoreTransforms`) —
  authored by hand, never a real purchased avatar.
- Emitter tests: build a small hierarchy in-memory, emit, read the `JiggleRig` back through
  `SerializedObject`, assert the values and that `Undo.PerformUndo` fully reverts.

---

## Execution order

0. **Prepare the Basis clone.** Confirm no Unity editor is running, then, in
   `~/Documents/Projects/Basis`:
   ```
   git stash push -u -m "unity editor churn pre basis-convert"
   git switch -c basis-convert-dev origin/developer
   ```
   The stash holds only regenerable Unity output (font SDF atlases, `link.xml`,
   `GraphicsSettings`, `ProjectSettings`, `AddressableAssetsData`) and is recoverable with
   `git stash pop`; expect it to reappear whenever Unity opens the project.
   `basis-convert-dev` is pristine upstream, so `Basis/Assets/yuna0x0/` is not present and a
   test avatar has to be imported by hand for step 7 — Unity will reimport `Assets/` on the
   first open after this switch, which is slow against the 25 GB `Library`. **Never commit on
   this branch**; our symlinked package goes in `.git/info/exclude`, not in the repo's
   `.gitignore`, so nothing of ours can leak into a Basis commit.
1. **Spike first.** Create the repo skeleton + package + asmdef, symlink it into the Basis
   clone, and write one EditMode test that resolves fileIDs to Transforms on the real nested
   prefab named in the Risks section. Run it with `unity test`. Everything downstream depends
   on this working; if it does not, the fallback is path matching and the plan is unchanged
   apart from the reader internals.
2. Repo scaffolding proper: `agent/` folder, `AGENTS.md`, `.gitignore`, `LICENSE`, README,
   first worklog entry, initial commit.
3. Intermediate model + `PhysBoneToJiggleMapper` + its unit tests. Pure C#, no scene — this is
   where most of the thinking is, and it is fully testable headless.
4. `PrefabYamlSource` + fixture-based tests.
5. `JiggleRigEmitter` + preset copying + emitter/undo tests.
6. The editor window, dry-run table and report asset.
7. End-to-end pass on a real avatar; record the visual diff notes in `agent/worklog/`.

Note for whoever runs step 7: close Unity before any git operation on the Basis clone, and
never commit into that clone — our work lives entirely behind the symlink.

## Verification

1. `unity test ~/Documents/Projects/Basis/Basis --platform EditMode` — all green.
2. Open `~/Documents/Projects/Basis/Basis` in Unity 6000.5.10f1, import a real avatar
   `.unitypackage` exported from `~/Documents/Projects/yuna0x0-vrc-avatar`, and confirm the
   VRC components show as missing scripts. Primary end-to-end fixture:
   `Assets/Avatars/Shinano/Prefab/Shinano.prefab` — 61 PhysBones with genuinely varied
   settings including `limitType: 3` (Polar), non-zero `endpointPosition`, explicit
   `rootTransform`, non-default `spring`/`immobile`. Density stress test:
   `Assets/Avatars/Deer/Prefab/DPS/Tan FeFi (DPS).prefab` (73 PhysBones).
3. `Basis/Tools/Convert VRChat Avatar` -> dry run lists the avatar's PhysBones and the rigs
   they map to; the counts match what the VRChat project actually has.
4. Convert. Confirm `JiggleRig` components appear with sensible values, then Ctrl+Z once and
   confirm the hierarchy is exactly as before.
5. Press **Test in Editor** on the `BasisAvatar` component (plain Play mode does not calibrate
   the avatar, so jiggle will not run) and confirm hair/tail actually move.
6. Compare side by side against the same avatar in the VRChat project and record the
   discrepancies in `agent/worklog/` — those notes are what drives the preset tuning.

---

## Roadmap after M1

- **M2 — Constraints.** `VRCConstraint*` -> the 14 `Basis*` types, delegating Unity and
  Animation Rigging ones to the existing `BasisConstraintConversion`. Mostly 1:1, with three
  known hazards to design around: VRC's `TargetTransform` lets a constraint drive a *different*
  transform than the one it sits on (Basis constraints, like Unity's, always drive their own
  GameObject, so this needs a relocation step); `Sources` serializes as 16 fixed inline slots
  plus an `overflowList`, where only the first `totalLength` are real; and `SolveInLocalSpace`
  / `FreezeToWorld` / `RebakeOffsetsWhenUnfrozen` have no equivalent anywhere. Add the
  Docusaurus site at this milestone.
- **M3 — Avatar descriptor.** `VRCAvatarDescriptor` -> `BasisAvatar`: view position, visemes
  (identical 15-entry ordering), blink, eye/mouth position. Partly achievable through NDMF's
  `CommonAvatarInfo` round-trip, which `BasisFrameworkPlatform` already implements.
- **M4 — Legacy dynamics.** Dynamic Bone -> `JiggleRig`, reusing the whole M1 pipeline behind a
  new front-end reader. Dynamic Bone is the easy win: its source is checked into the reference
  project (`Assets/External/DynamicBone/Scripts/`) so the field list is exact rather than
  reverse-engineered, its fields map onto jiggle almost directly (`m_Damping`->`drag`,
  `m_Elasticity`+`m_Stiffness`->`stiffness`, `m_Inert`->`ignoreRootMotion`, `m_Radius`->
  `collisionRadius`, `m_Exclusions`->`excludedTransforms`, and every `*Distrib` AnimationCurve
  maps onto the matching `JiggleTreeCurvedFloat.curve`), and `SKYMY_Workshop/.../EMISTIA`
  ships the *same avatar* in both a DynamicBone and a PhysBone prefab — a free A/B oracle for
  checking that both readers converge on the same jiggle result.
  **Magica Cloth 2** is not used anywhere in the reference project, but the purchased package
  is sitting in the local Asset Store cache at
  `~/Library/Unity/Asset Store-5.x/Magica Soft/ScriptingPhysics/Magica Cloth 2.unitypackage`
  (and Dynamic Bone at `.../Will Hong/Editor ExtensionsAnimation/Dynamic Bone.unitypackage`).
  A `.unitypackage` is a gzipped tar of `<guid>/{asset,asset.meta,pathname}`, so both can be
  read straight off disk with `tar` — no Unity, no Asset Store download step. The MC2 entry
  point is `Assets/MagicaCloth2/Scripts/Core/Cloth/MagicaCloth.cs`, guid
  `bdbd3ce05f5b45942b56ede5c9b38364`, whose single serialized payload is a
  `ClothSerializeData`. So MC2 is developable after all; it just needs a hand-authored fixture
  rather than a real avatar. Caveat: these are whatever was last downloaded — MC2 **2.18.1**
  (cached May 2026), Dynamic Bone from May 2024 — so re-download both from the Asset Store
  before starting M4 and re-derive the field table from the current version. Asset GUIDs are
  stable across versions; field layouts are not. UniVRM `VRMSpringBone` (23 instances in the reference project) is
  a cheaper third reader if one is wanted.
  **These are purchased assets: read them for GUIDs and field names, never vendor their source
  into this repo.**
- **M5 — Animation and toggles.** FX layers / expression menus -> `HVRVixxyControl` +
  `HVRVixxyMenuItem`, and ambient loops -> `BasisAuthoredMotion` (with `BasisMotionClip` baking
  via the existing `Basis/Build/Bake Authored Motion Clip` window). This is the largest and
  least mechanical piece; expect assisted authoring, not automatic conversion.
- **M6 — Modular Avatar.** Target MA >= 1.18.4 (see Established facts — first version with
  Unity 6 support; check whether Haï's unofficial fork is still needed at that point). MA has
  zero Basis-specific integration in the Basis repo today. In the reference project the MA
  components that actually matter are, by usage: `BlendshapeSync` (57), `MenuItem` (45),
  `MeshSettings` (23), `MergeArmature` (22), `MenuInstaller` (14), `MergeAnimator` (8),
  `ObjectToggle` (8), `BoneProxy` (7). All use `fileID: 11500000`, so the guid alone identifies
  the type. **`MergeArmature` and `BoneProxy` mean the authored hierarchy is not the built
  hierarchy** — a raw-YAML reader sees clothing armatures as separate roots until NDMF runs,
  which is the strongest argument for eventually offering the NDMF-pass back-end. Decide then
  between converting MA components to native Basis setups and supporting them in place. Note
  MA Shape Changer is broken on Basis (it cannot see Basis's generated LOD meshes).
- **M7 — Publish.** OpenUPM, after the naming decision in "Naming, menus and trademark" is
  settled. Ship a `LICENSE` (MIT, `yuna0x0 <yuna@yuna0x0.com>`) and a README that describes the
  Basis relationship truthfully and without implying endorsement.

## Risks

- **Heuristic jiggle feel.** Mitigated by presets as data, dry run, and undo. Never claimed as
  exact; the report says which values were guessed.
- **VRC SDK script GUIDs and field layouts.** GUIDs are stable in practice but not
  contractual, and field sets drift between SDK versions (`networkIDs` arrived in 3.4; the
  `allowCollision`/`allowGrabbing`/`allowPosing` bools are migrating to a tri-state
  `AdvancedBool`). The reader keys off the component's own `version:` field, tolerates missing
  keys, and reports unknown script identities rather than skipping them. The later JSON
  exporter front-end removes the dependency on GUIDs entirely.
- **Nested prefabs and stripped GameObjects.** Real avatars use them heavily — the prefab
  inspected above has `--- !u!1 &... stripped` GameObjects with `m_PrefabInstance` back
  references, and the PhysBone lives on a prefab *instance*. The MonoBehaviour block itself is
  fully materialised, so the data is readable, but resolving `m_GameObject` back to a live
  `Transform` across a stripped reference is the one part of the reader that needs a spike
  before the rest is built. **Do this first**: a throwaway EditMode test that loads that exact
  prefab, maps every `!u!114` fileID to an object, and prints the resolved bone paths. If
  `AssetDatabase.TryGetGUIDAndLocalFileIdentifier` does not resolve stripped entries, fall back
  to reconstructing hierarchy paths from the YAML and matching by path, reporting ambiguity on
  duplicate names instead of guessing.
- **Basis is a moving target.** `developer` branch, `0.0.1` on every package, private fields
  reached via `SerializedObject`. Pin what is known to work in `agent/decisions/` and keep the
  emitter layer thin so breakage is localised.
