# 0002: Menu placement, and why the name is provisional

**Status:** accepted, 2026-08-30

## Decision

Editor surfaces go under our own product name, never under Basis's menus:

| surface | path |
|---|---|
| tool windows | `Tools/<ProductName>/...` |
| hierarchy context actions | `GameObject/<ProductName>/...` |
| project context actions | `Assets/<ProductName>/...` |
| components, all `EditorOnly` | `AddComponentMenu("<ProductName>/...")` |

The display name `Basis Convert` is **provisional** and must be settled before the first public
release, because renaming a published UPM package is disruptive.

## Why

Basis's `TRADEMARK.md`: MIT covers the code, but the Basis, BasisVR and Basis Framework names
and the logo are trademarked and require permission. It asks third parties to avoid implying
affiliation or endorsement, and explicitly permits truthful descriptive reference, calling out
"Built with Basis" as fine.

An entry under `Basis/Tools/...` would read as a shipped Basis feature. That is the implied
affiliation the policy asks us to avoid. Every other signal agrees:

- Basis owns the `Basis/` menu; roughly 60 first-party items live there.
- Haï's `dev.hai-vr.basis.comms` ships **inside** the Basis repo and still registers zero
  `MenuItem`s, namespacing components as `AddComponentMenu("HVR.Basis/...")`.
- NDMF uses `Tools/NDM Framework/...`; Modular Avatar uses `Tools/Modular Avatar/...`,
  `GameObject/Modular Avatar/...` and `AddComponentMenu("Modular Avatar/MA ...")`.
- Unity's Asset Store submission guidelines say editor extensions nest under an existing menu
  such as `Window/<PackageName>`, or under `Tools` when nothing fits, never a new top-level
  menu.

There is no third-party plugin authoring guide in BasisDocs, so the trademark policy plus this
observed convention is the whole rulebook.

## On the name

`basis` as a scope segment in a package id follows Haï's precedent and reads as "our
Basis-related things". A display name of "Basis Convert" is closer to using the mark as the
product name, which is the shape the policy warns about. Either pick a distinct product name
and demote Basis to the description, or ask the Basis project for permission first, which
`TRADEMARK.md` invites ("Contact us if unsure").
