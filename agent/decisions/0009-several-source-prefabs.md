# 0009: A conversion reads every prefab the hierarchy is built from

**Status:** accepted, 2026-08-30

## Decision

Converting reads the whole hierarchy, not one prefab file. The scanned object is the first
source, and every prefab instance under it is another. Each is read from its own file, and each
planned item records which source it came from, because its transforms are in that prefab's
space. At conversion time each source is located in the target by the sibling-index path it was
found at, and items are translated against their own source's root.

## Why

An avatar is rarely one prefab. Clothing, hair and accessories are prefabs of their own carrying
their own physics, dropped onto the avatar in a scene or nested inside its prefab. In the
reference library the avatar prefabs hold 966 PhysBones between them and the clothing prefabs
hold 2206, so reading only the avatar's own file converts less than a third of the physics on an
assembled avatar.

Reading one file was not merely incomplete, it was misreported: the conversion said what it
found in that file and looked complete, which is the failure the report-do-not-omit rule exists
to prevent.

## Why not flatten first

Modular Avatar can bake an assembled avatar into a single hierarchy, and its hierarchy work
(`MergeArmature`, `BoneProxy`) runs on Basis. Requiring that would put a build step and a
dependency between the user and a conversion, and would not help an avatar assembled without
Modular Avatar, which is most of the reference library. Reading several prefabs costs one
discovery pass and a source reference per item.

## Scope and control

A conversion is rooted at the object you pick and covers that object and its children, the way
editor converters usually work. Nothing outside it is read, and nothing is ever written to a
prefab asset: components go onto the hierarchy that was picked, under one undo.

Every prefab found under that root can be unticked, so a prop parented onto an avatar is not
converted with it. The prefab list sits with the other per-item lists under Advanced, and what
is left out is named in the report, per [decision 0008](0008-conversion-options.md).

## Consequences

- Component data is read from prefab files, so a change made to a clothing prefab **instance**
  in the scene, rather than to its prefab, is not seen. Collider assignments made that way are
  the common case, and they surface as `physics.collider.unresolved`.
- A clothing prefab shipped with an avatar descriptor of its own for previewing does not
  displace the avatar's: the first descriptor found wins, and the avatar is read first.
- `AvatarConversionPlanner.Plan(string)` still reads exactly one prefab, as the tests
  and any caller with an asset path use. `Plan(GameObject)` is the hierarchy entry point.
