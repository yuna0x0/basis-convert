---
sidebar_position: 1
---

# Watari

Watari brings an avatar you already own into [Basis](https://basisvr.org/), with its
physics, constraints, menus and motion intact.

You import the avatar into a Basis project, pick it in the scene, and convert:

- **VRChat PhysBones and legacy Dynamic Bone** become Basis jiggle physics
- **VRChat constraints** become their Basis equivalents
- **The avatar descriptor** becomes a `BasisAvatar` component
- **Menu toggles, selectors and radial puppets** become HVR Vixxy controls
- **Animation that plays on its own** becomes authored motion, baked to a clip

Clothing and accessories convert along with the avatar they are worn on.

Conversion is not lossless, and the tool does not pretend otherwise. Roughly a third of what a
VRChat avatar carries has no Basis equivalent, so anything approximated or dropped is listed with
a reason before anything is written.

## What you need

- A Basis project, opened in the Unity version Basis targets.
- Your avatar imported into it, still linked to its prefab.

The VRChat SDK is not needed and should not be installed. Its components arrive as missing
scripts in a Basis project, and that is exactly what this reads.

## What to read next

- [Installing](installation.md)
- [Converting an avatar](converting-an-avatar.md)
- [What converts](what-converts/physics.md), source by source
- [Limitations](limitations.md), which is worth reading before you rely on the result
