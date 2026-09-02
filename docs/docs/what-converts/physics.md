---
sidebar_position: 1
---

# Physics

VRChat PhysBones, VRM spring bones and legacy Dynamic Bone all become
[Jiggle Physics](https://github.com/naelstrof/UnityJigglePhysics) rigs, the secondary motion
system Basis ships. Dynamic Bone is an ordinary Unity asset and VRM is a format, not a platform,
so an avatar using either converts whether or not VRChat was ever involved.

This page is about PhysBones and Dynamic Bone. Spring bones have their own page, since the two
VRM formats describe a chain differently: see [VRM](vrm.md).

One rig is written per chain. A PhysBone is one chain, and jiggle physics walks into every child
of the bone it is rooted at, so a PhysBone covering a whole head of hair stays one rig rather than
becoming one per strand. A Dynamic Bone can name several root bones, and each of those becomes a
rig of its own with the component's settings.

## What carries across exactly

- The root bone, the transforms the source ignored, and grab settings.
- Per-bone falloff curves. Both VRChat and jiggle physics evaluate them over the normalised
  distance from the root, so a curve maps onto a curve rather than being flattened to a number.
- Gravity, radius, stretch, and how immobile the root is.
- Colliders: sphere, capsule and plane, with the same three shapes on both sides.

## What is fitted

Two settings do not mean the same thing on both sides and are fits rather than conversions:

- **Stiffness**, from VRChat's pull and stiffness.
- **Drag**, from VRChat's spring.

Both are exposed under **Advanced** in the window, as weights you can adjust before rescanning.
Everything else is a direct mapping, so these are the ones to reach for if the result feels wrong.

Values the source does not determine are taken from the jiggle physics package's own presets, so
they start at values its author tuned. The preset per rig is guessed from the bone's name and can
be changed in the window.

## What does not carry across

- Angle limits wider than jiggle physics can express. No limit is written rather than a tighter
  one, since a bone that is suddenly more constrained is worse than one that is less.
- Polar limits, which are approximated to a single angle.
- Gravity falloff, max squish, endpoint positions, and per-axis limit rotations.
- `Is Animated`, and anything driven by a PhysBone parameter.
- A collider inverted to keep bones inside it.

## Checking the result

Press **Test In Editor** on the `BasisAvatar` component. Jiggle physics only runs on a calibrated
avatar, so plain Play mode shows nothing moving.
