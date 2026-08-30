# AGENTS.md

Orientation for AI agents and for anyone picking this repo up cold.

## What this is

Editor tooling for bringing content from other social VR platforms into Basis. It installs into
a Basis project as a UPM package.

The scope is wider than what is built. Avatars first, then props and worlds; VRChat first, then
whatever else is worth reading. Reading, mapping and writing are separate stages so that a new
source platform is a new reader and nothing else has to move. Do not narrow the vocabulary in
code or docs to "VRChat avatars" as though that were the whole of it.

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
descriptive reference and asks third parties not to imply affiliation or endorsement. The
package's display name is provisional for that reason.
