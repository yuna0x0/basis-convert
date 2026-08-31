---
sidebar_position: 4
---

# Menu toggles

Basis has no expression menu, and no FX layer. What it has is
[HVR Vixxy](https://github.com/hai-vr/basis-comms), which ships with Basis and holds a control
as a set of choices with a value per choice. Menu toggles are rebuilt as Vixxy controls with a
menu item each.

## How a toggle is found

The expression menu names a parameter. The FX controller has a layer steered by that parameter,
with a clip on each side. Both are read, and the two clips are reduced to what they actually do.

A layer counts only when a single parameter steers it, which keeps gesture layers that mention a
toggle as a secondary condition from being read as that toggle's own.

## What can be rebuilt

- **Objects switched on and off.**
- **Blendshapes**, as a weight per choice.
- **Material properties**, including colours animated one channel at a time.

Where a clip sets something on one side only, the other side keeps the avatar's authored value,
read from the object rather than assumed to be the opposite.

## Controls with more than two states

An expression menu commonly puts several entries on one parameter, each setting a different
value, so that picking one clears the rest. Those entries are grouped by their parameter and
become one Vixxy control with a choice per value, named after the menu entry that selects it.

## Radial puppets

A radial drives a float rather than switching between states: its menu entry names its parameter
under `subParameters`, and its layer holds a blend tree rather than transitions. It becomes a
Vixxy control presented as a slider, with the lowest and highest motions in the tree as its two
choices.

Vixxy interpolates in a straight line between a control's choices, so the two ends carry across
exactly and the shape of the sweep between them does not. A tree holding motions between its ends
is reported as `vixxy.puppetEnds`.

## What cannot

- Anything that animates over time. Vixxy holds a value per choice, not a curve. Motion that
  plays on its own is converted separately, as
  [authored motion](authored-motion.md).
- Two-axis and four-axis puppets.
- Expression parameters as a system. Vixxy controls hold their own state, so there is no
  parameter list to recreate, and anything driven by parameters outside a toggle has to be
  rebuilt by hand.

A toggle that cannot be rebuilt is reported rather than half converted. Emitting the half that
works would leave a control that looks finished and does part of the job.
