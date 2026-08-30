# 0004: This package must never leave a runtime component on an avatar

**Status:** accepted, 2026-08-30

## Decision

Everything this package ships is editor-only. Any bookkeeping the converter needs to persist
lives on a child GameObject tagged `EditorOnly`.

## Why

Basis runs a component allow-list, the "Content Police":
`com.basis.sdk/Settings/AvatarContentPoliceSelector.asset`, 159 entries at time of writing. It
lists what may exist on a loaded avatar. `GatorDragonGames.JigglePhysics.JiggleRig`,
`BasisAuthoredMotion` and all 14 `Basis*Constraint` types are on it. Nothing of ours is, and
nothing of ours can be without being upstreamed into Basis.

So a runtime component of ours on an avatar is not merely useless, it is stripped or blocked
at load.

`EditorOnly` is the sanctioned escape hatch:
`BasisAssetBundlePipeline.DestroyEditorOnlyInAvatar` walks the avatar before the build and
destroys any child whose tag is `EditorOnly`, recursively. Bookkeeping parked there is visible
while authoring and gone before the avatar is packed.

## Consequences

- The conversion report is a `ScriptableObject` asset in the project, not a component.
- The assembly is `includePlatforms: ["Editor"]`.
- Basis's style guide asks that editor-only code refuse to run at runtime, loudly. Follow it.
