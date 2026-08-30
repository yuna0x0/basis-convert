# 0007: Repeated conversions are scoped by the plan, not by stored state

**Status:** accepted, 2026-08-30

## Decision

Converting an avatar that has already been converted removes the components sitting on the
transforms the new conversion is about to write to, after a confirmation dialog, then writes.

Nothing is stored on the avatar between runs. The plan already knows every transform it targets,
so the previous output is found by looking there.

## Why

Every comparable tool re-scans rather than remembering. Basis's own
`BasisDeprecatedComponentUpgrader` enumerates what it will change, confirms with a dialog naming
the consequence, and rewrites; it keeps no record. Modular Avatar and NDMF avoid the question
entirely by generating onto a throwaway copy at build time. Unity's URP converters scan and
convert without leaving markers.

## Rejected: a marker component recording what was created

Considered and prototyped. It would have been more precise: a rig hand-tuned on a bone the
converter also targets would have survived, where the scoped rule replaces it.

It does not work. A `MonoBehaviour` in an editor-only assembly cannot be attached to a
GameObject at all:

```
Can't add script behaviour 'ConversionRecord' because it is an editor script.
To attach a script it needs to be outside the 'Editor' folder.
```

Making it work would mean shipping a runtime assembly whose only purpose is bookkeeping, adding
a GameObject to every converted avatar, and relying on the `EditorOnly` tag to keep it out of
builds. That is a large concession to [0004](0004-editor-only.md) for one edge case, and it
leaves a missing script behind for anyone opening the scene without this package installed.

## Consequence

A rig tuned by hand on a bone the converter also targets is replaced on a re-convert. That is
what re-converting that bone means, and the dialog says which components will go. Preserving
such tuning would need update-in-place, which is a bigger feature and is not planned yet.
