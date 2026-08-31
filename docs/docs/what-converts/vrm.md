---
sidebar_position: 2
---

# VRM

VRM avatars, including everything VRoid Studio exports, carry their physics as spring bones and
their faces as expressions. Both formats are read: spring bones become Basis jiggle physics, and
expressions become HVR Vixxy controls.

UniVRM does not need to be installed. A VRM avatar imported into a Basis project arrives with its
components as missing scripts, and the data is read from the prefab file either way.

## The two formats

**VRM 0.x** puts one component on the avatar carrying a group of chains and a single set of
parameters for all of them. Each root bone it names becomes a jiggle rig.

**VRM 1.0** puts a joint on each bone and lists which joints make up which chain on the avatar's
own component. Each chain becomes a jiggle rig rooted at its first joint.

## What carries across

| VRM | Basis jiggle |
|---|---|
| Drag force | Drag, on the same 0 to 1 scale |
| Joint radius / hit radius | Collision radius, both in metres |
| Gravity power and direction | Gravity, the downward part of it |
| Collider groups | The rig's colliders |
| Stiffness force | Stiffness, as a fit rather than a conversion |

**Parameters that differ along a chain become a curve.** VRM 1.0 carries a value per joint, and
Basis evaluates its parameters over distance from the chain root, which is the same axis. A chain
that stiffens toward its tip keeps that shape instead of being averaged.

**Bones the chain does not name are excluded.** A VRM spring names the bones it moves; a jiggle
rig simulates everything under its root. An accessory hanging off a hair bone would start
swinging, so it is excluded to leave it as still as VRM left it.

## Expressions

A VRM expression is a named set of blendshape weights. The ones an author added, and the
emotion presets, become Vixxy controls with a menu item each: off leaves every shape at the
weight the avatar was authored with, on takes the expression's.

The lip sync shapes, blinking and looking around do not. Basis drives those itself, and a menu
item would fight it. They are reported as left to Basis.

VRM refers to a blendshape by its position in the mesh rather than by name, so each one is looked
up on the renderer it names. A shape that is no longer there, usually because the mesh changed
after the expression was authored, is reported rather than guessed at.

An expression that also changes material colours or UVs keeps the blendshapes and reports the
rest: VRM names the material to change, while Vixxy acts through a renderer.

## What does not

- **The centre transform.** VRM simulates relative to it so hair does not lag behind a moving
  avatar. Basis has no equivalent, though its own root motion handling covers some of the same
  ground.
- **Inside colliders**, which hold bones within a shape rather than pushing them out. Basis only
  pushes out, so those are written as ordinary colliders and reported: they now push the opposite
  way, and are worth removing if the result looks wrong.
- **Look-at, first-person settings and the avatar's metadata.** Basis drives gaze from the eye
  bones and fills in the `BasisAvatar` component itself when its inspector is first opened.
