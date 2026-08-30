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

The product name is **Watari**, so those read `Tools/Watari/...`. See "On the name" below.

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

## On the name, resolved 2026-08-31

The product is **Watari** (渡り, a crossing), with "Converter for Basis" as the descriptive half:
`displayName` is `Watari (Converter for Basis)`, menus are `Tools/Watari/`, and the window is
titled Watari.

"Basis Convert" was the shape the policy warns about, a bare `Basis <Word>` used as a product
name. Descriptive reference is separately permitted, and "Converter for Basis" is exactly that,
so the mark stays where the policy allows it and out of the name itself. No permission is needed
and none was asked for.

The ecosystem agrees on the pattern. Haï's package is `HVR Basis Comms`, leading with his own
prefix; towneh's is `Video Area Light`, with no mark in the name at all. A bare `Basis <Word>`
from a third party does not appear anywhere.

The package id stays `com.yuna0x0.basis.convert`. It is vendor-scoped, `basis` is a scope
segment meaning "our Basis-related things" rather than a product name, and it matches
`dev.hai-vr.basis.comms`. The repository name, the C# namespaces and the assembly names stay as
they are for the same reason: none of them is what a user reads, and changing them would break
release URLs and the published package for no gain.
