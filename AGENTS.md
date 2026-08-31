# AGENTS.md

Orientation for AI agents and for anyone picking this repo up cold.

## What this is

Editor tooling for bringing avatars and the content worn on them into Basis. It installs into a
Basis project as a UPM package.

Working today: VRChat PhysBones and colliders, VRM spring bones in both formats, legacy Dynamic
Bone, all six VRChat constraint types, the avatar descriptor, menu toggles, selectors and radial
puppets rebuilt as HVR Vixxy controls, animation that plays on its own rebuilt as authored
motion, and a check of the humanoid rig against what Basis's IK needs. A conversion can be
narrowed to some of those, or to individual items, from the window.

## Picking this up cold

Read `agent/worklog/` newest entry first; it ends with where the last session stopped and what is
next. Then `agent/decisions/`. The short version of how this works:

- **Source data is read from YAML**, because the VRChat SDK cannot be installed into a Basis
  project and its components arrive as missing scripts. Native Unity types, animator controllers
  and clips, are read through the editor API instead.
- **Three stages**: readers produce plain data, mappers are pure and produce plans, writers touch
  Unity objects. Only writers need a scene, which is what keeps the rest testable headlessly.
- **Anything approximated or dropped produces a diagnostic** with a stable code. Roughly a third
  of the source surface has no Basis equivalent, so silence would misrepresent the result.

## Lessons this project keeps relearning

- **Read the code, not the docs.** Basis's documentation has been wrong three times: jiggle
  colliders are not sphere-only, chain splitting is unnecessary, and the twist bone lookup lives
  somewhere other than the type named.
- **Decompile rather than infer.** The animation layer ordering quoted everywhere omits a
  deprecated entry and shifts every layer after it. One `ilspycmd` call settled it after the
  wrong version had already shipped in a report.
- **Read the output, not just the test results.** A wide angle limit clamping to a tighter one,
  and duplicate collider diagnostics, were both found by reading a generated report while every
  test passed.

The scope is wider than what is built. Avatars first, then props and worlds; VRChat first, then
whatever else is worth reading, which now includes VRM. Reading, mapping and writing are separate
stages so that a new source is a new reader and nothing else has to move. Do not narrow the
vocabulary in code or docs to "VRChat avatars", or to social VR platforms: a VRM file is a
format rather than a platform, an avatar carrying nothing but Dynamic Bone belongs to neither,
and all of them are in scope. What decides whether something converts is the components it
carries, not where it came from.

## Read this first

`agent/` is the committed knowledge base:

1. `agent/README.md`, how the folder works
2. `agent/decisions/`, what was decided and why, including what was rejected
3. `agent/worklog/`, newest entry, for where the last session stopped
4. `agent/research/`, API inventories and file format notes
5. `agent/plans/`, the current design

Check `decisions/` before arguing for an approach; it may already have been considered.

Research notes record what was true when written. Verify anything that names a file, field or
flag before relying on it. Basis develops on a `developer` branch, all of its packages are
version `0.0.1`, and several fields this package touches are private or internal and reached
through `SerializedObject`.

## Environment

Setup, test commands and style are in `CONTRIBUTING.md`. Nothing here assumes a particular
checkout location, editor version or user; take the editor version from the Basis project's
`ProjectSettings/ProjectVersion.txt` rather than any version written in prose.

Close the Unity editor before any git operation on the Basis clone, and check that it is
actually closed. Switching branches under a running editor can corrupt its Library, which is
tens of gigabytes and slow to rebuild.

## Constraints that are easy to violate

- **Editor-only.** Basis validates loaded avatars against an allow-list of component types, and
  nothing of ours is on it. Persistent state goes on a GameObject tagged `EditorOnly`, which the
  Basis build pipeline strips. See `agent/decisions/0004`.
- **Report, do not omit.** Anything the converter approximates or cannot carry over produces a
  diagnostic with a stable code.
- **Keep the layers apart.** Readers take text, mappers take plain data, only writers touch
  Unity objects. This is what keeps the bulk of the code testable without an editor.
- **Menus belong to us, not to Basis.** `Tools/<ProductName>/...`, never under Basis's own menu.
  See `agent/decisions/0002`.

## Trademark

Basis, BasisVR and Basis Framework are trademarks of the Basis Project. Their policy permits
descriptive reference and asks third parties not to imply affiliation or endorsement. The product
is named Watari for that reason, with the Basis reference kept descriptive: the display name is
`Watari (Converter for Basis)` and menus are `Tools/Watari/...`. See `agent/decisions/0002`.

The package id and the namespaces still read `com.yuna0x0.basis.convert` and
`yuna0x0.Basis.Convert`, where `basis` is a scope segment rather than a product name. They did
not change with the rename, because an id change means a second OpenUPM entry and orphans what
was published.
