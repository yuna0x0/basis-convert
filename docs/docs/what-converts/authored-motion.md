---
sidebar_position: 6
---

# Authored motion

A Basis avatar carries no animator layers of its own, so animation that plays without anything
switching it on has nowhere to go except
[`BasisAuthoredMotion`](https://basisvr.org/), a component that replays baked motion from a
batched job. A tail that sways, ears that twitch, an accessory that turns: on VRChat these are FX
layers with nothing steering them, and they are rebuilt as authored motion.

## Two kinds

**Motion the avatar simply has.** A layer counts when no parameter steers it and its default
state holds a clip that turns transforms over time. That is the shape ambient motion is authored
in, and it plays from the moment the avatar loads.

**Motion a menu switches on.** A toggle whose clip animates rather than switches cannot be held
by a Vixxy control, which stores a value per choice rather than a curve. The animation becomes a
motion of its own and the control enables and disables the component: `BasisAuthoredMotion` is
one of the types a Vixxy activation is permitted to switch. The component starts in whatever
state the control's default choice puts it, so an untouched avatar looks right.

A clip authored to loop keeps looping while the control holds it on. One that was not plays once
each time it is switched on.

## What is written

Each becomes one movement of kind `Sequence`, holding a `BasisMotionClip`: the clip's
rotations sampled at 60 frames a second, one row per bone it turns. Rotations are recorded as
they land on the bone rather than as the curve states them, which is what lets a clip replay
correctly whatever rest pose the avatar has.

The baked clip is written into a `Watari Motion` folder beside the animation it came from, at a
path derived from the clip's name, so converting a second time replaces it rather than leaving a
copy behind.

:::note
The baked clip is a project asset, so unlike everything else a conversion writes, **an undo does
not remove it.** The components disappear; the asset stays on disk.
:::

## What does not carry across

- **Movement and scaling.** A baked Basis motion clip holds rotation only. A clip that also
  moves or scales something keeps the turning and reports the rest as `motion.rotationOnly`.
- **Anything else a clip animates over time.** A toggle whose clip moves, scales or drives
  something other than rotation over time is still reported and left alone, rather than
  converted in part.
