# 0015: A source whose file is not text is read from its components

**Status:** accepted, 2026-09-03

## Decision

When a source's file holds no Unity YAML, its components are read through the object API instead
of being treated as empty. Only VRM arrives that way today: a `.vrm` is binary glTF behind a
ScriptedImporter.

The reader produces the same plain data the text reader does, keyed by the same identifiers, so
mappers, the resolver and the writers are untouched.

## Why

An imported `.vrm` dropped into a scene converted as though it carried nothing: no spring bones,
no expressions, no licence, no eye offset. The rule that source data is read from YAML holds
because the VRChat SDK cannot be installed into a Basis project, so its components arrive as
missing scripts that only the file describes. VRM is the opposite case. UniVRM has to be
installed for the file to import at all, so the components are real types with readable fields,
and the file itself has no text to read.

The workaround was to unpack the avatar, save it as a prefab, and press "Extract Meta And
Expressions" in the import settings. Two steps that produce a second copy of the avatar and are
easy to get wrong, to work around a format the tool could read directly.

## How identifiers stay shared

`VrmSpringChainData` and the rest name objects by file id, and `PrefabObjectResolver` maps a file
id to a live object through `AssetDatabase.TryGetGUIDAndLocalFileIdentifier`. Objects inside an
imported asset have local file ids like any other, so the component reader takes each object's
own id and nothing downstream can tell the two readers apart. No synthetic identifiers, and no
second resolver.

Components are recognised by the guid of the script behind them, read with
`MonoScript.FromMonoBehaviour`, against the same table the text path uses. One table, one set of
recognised components.

## What was rejected

**Saving a temporary prefab and reading its text.** Writes into the project during a scan, which
is meant to change nothing, and leaves the expressions unreadable anyway: they are sub-assets of
the binary file, so a saved prefab still points into it.

**Generating YAML from a `SerializedObject`.** Would let every existing reader work unchanged,
but the text readers depend on Unity's exact layout, down to indentation, and a generated
approximation that drifts fails silently.

**Referencing UniVRM types directly.** It is not a dependency and must not become one. Fields are
read by name through `SerializedObject`, which is how private Basis fields are already reached.

## What this does not change

Readers still hand plain data to pure mappers, and only writers touch the scene. A reader that
takes objects rather than text is still a reader; animator controllers and clips have always been
read that way. What decides which reader runs is the file, not the platform the avatar came from.

## Addendum, 2026-09-03: VRM 0.x arrives as text

UniVRM 0.x has no ScriptedImporter. An `AssetPostprocessor` turns a `.vrm` into a real `.prefab`
beside it as it lands, so a 0.x avatar is Unity YAML and the text reader handles it. The component
path still recognises 0.x components, and was checked against a real 0.x avatar, but nothing in a
normal project reaches it. It is a fallback, not the route.

This does not change the decision. What decides which reader runs is whether the file holds text,
and for 0.x the answer is yes.
