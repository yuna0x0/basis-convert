# 0006: One jiggle rig per source component, no chain splitting

**Status:** accepted, 2026-08-30

## Decision

A PhysBone becomes exactly one `JiggleRig`, whatever its bone topology. The converter does not
split a branching chain into one rig per branch.

## Why

The Basis avatar documentation says that paired features such as left and right need a separate
`Jiggle Rig` each, "otherwise only one side will jiggle". Planning assumed that and budgeted a
chain splitting step. The jiggle source in the version Basis ships does not behave that way.

`JigglePhysics.Visit` recurses into every valid child, in both the normal path and the
merge-distance path, adding each returned index to the parent point:

```csharp
for (int i = 0; i < validChildrenCount; i++) {
    var child = children[i];
    Visit(child, ..., newIndex, ..., depth + 1, out int childIndex);
    if (childIndex != -1) {
        AddChildToPoint(points, newIndex, childIndex);
    }
}
```

`JiggleRigData.BuildNormalizedDistanceFromRootList` walks the same way and caches every bone
under the root. So a single rig rooted at a shared parent simulates all of its branches.

`excludeRoot` does not remove the root from the simulation, it pins it: `Visit` and
`UpdateParameters` both replace an excluded root's parameters with elasticity 1. That is the
same behaviour as PhysBone's `multiChildType: Ignore`, where the shared root stays put and each
child chain moves independently, so the two line up directly.

Splitting would also lose parity in the other direction. One PhysBone carries one set of
settings for all of its chains; one rig per PhysBone preserves that, whereas several rigs
invite the branches to drift apart under later editing.

## Known asymmetry

`CreateJiggleTree` computes the back-projected virtual root from the first valid child only:

```csharp
var childPos = jiggleRig.GetValidChild(jiggleRig.rootBone, 0).position;
```

With several branches this biases the virtual parent direction toward the first one. It affects
the root's own motion, which `excludeRoot` pins anyway in the multi-child case, so it is not
worth splitting rigs over. Worth rechecking if a converted multi-branch rig looks wrong at the
root.

## Note

This is the second place the Basis avatar docs describe a limitation the shipped code does not
have; the other is the claim that jiggle colliders are sphere-only, when `JiggleCollider`
supports sphere, capsule and plane. Prefer the source over the docs, and verify.
