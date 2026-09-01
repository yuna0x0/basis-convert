---
sidebar_position: 6
---

# Authored motion

A Basis avatar has no animator layers, so animation that plays on its own is rebuilt as
`BasisAuthoredMotion`, a component that replays baked motion. A tail that sways, ears that
twitch, an accessory that turns: on VRChat these are FX layers with nothing steering them.

## Two kinds

**Motion the avatar has.** A layer with no parameter steering it, whose state holds a
clip that turns transforms over time. It plays from the moment the avatar loads.

**Motion a menu switches on.** A Vixxy control stores a value per choice, not a curve, so a
toggle whose clip animates cannot hold it. The animation becomes a motion of its own and the
control enables and disables the component, which Vixxy permits for this type. It starts in the
state the control's default choice puts it in.

A clip authored to loop keeps looping while the control holds it on. One that was not plays once
each time it is switched on.

## What is written

Each becomes one movement of kind `Sequence`, holding a `BasisMotionClip`: the clip's
rotations sampled at 60 frames a second, one row per bone it turns. Rotations are recorded as
they land on the bone rather than as the curve states them, so a clip replays correctly whatever
rest pose the avatar has.

The baked clip is written into a `Watari Motion` folder beside the animation it came from, at a
path derived from the clip's name, so converting a second time replaces it rather than leaving a
copy behind.

:::note
**An undo does not remove the baked clip.** It is a project asset, not a component, so the
components disappear and the clip stays on disk.
:::

## What does not carry across

- **Movement and scaling.** A baked Basis motion clip holds rotation only. A clip that also
  moves or scales something keeps the turning and reports the rest as `motion.rotationOnly`.
- **Anything else a clip animates over time.** A toggle whose clip moves, scales or drives
  something other than rotation is reported and left alone.
