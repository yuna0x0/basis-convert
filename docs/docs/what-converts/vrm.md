---
sidebar_position: 2
---

# VRM

VRM avatars, including everything VRoid Studio exports, carry their physics as spring bones and
their faces as expressions. Both formats are read: spring bones become Basis jiggle physics, and
expressions become HVR Vixxy controls.

UniVRM does not need to be installed. A VRM avatar imported into a Basis project arrives with its
components as missing scripts, and the data is read from the prefab file either way.

## Bringing in a `.vrm` file

Install [UniVRM](https://github.com/vrm-c/UniVRM) and drag the file into the project. Use 0.131.2
or newer: 0.131.0 does not compile on the Unity version Basis targets. Then two steps that are
easy to miss:

1. **Drag the imported avatar into a scene, unpack it completely** (right click, Prefab, Unpack
   Completely) **and save it as a prefab.** An imported `.vrm` is a binary file, and a prefab
   saved without unpacking only points back at it, so there is nothing to read.
2. **In the `.vrm`'s import settings, press "Extract Meta And Expressions".** Expressions, the
   licence and the eye offset live inside the binary until you do. Spring bones do not need this;
   they are components on the prefab.

Convert the prefab as you would any other avatar. UniVRM's own components are not on the Basis
allow-list, so Basis strips them when the avatar loads and the converted jiggle physics takes
over.

If the expressions and licence are missing after converting, the report says so as
`vrm.objectUnreadable`, which means step 2 was skipped.

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

## The avatar's licence

Every VRM states who may wear it and what may be done with it. The window shows all of it before
you convert: the title, the author, who may wear it, whether it may be changed or passed on,
whether commercial, violent, sexual, political or hateful use is allowed, whether credit is
required, and the licence it names.

A permission the format has no field for is left out rather than guessed at. VRM 0.x has no
political or antisocial fields, and no redistribution or credit fields; VRM 1.0 has all of them.

The licence is a warning when it forbids changing the avatar or limits who may wear it. Nothing
is blocked: converting changes an avatar and using it on Basis is a use, and both are yours to
judge.

## Where the eyes sit

Both formats record the point the camera sits at as an offset from the head bone. Basis stores
the same point as the avatar's eye position, so it carries across: a converted VRM avatar has a
correct eye height instead of one Basis has to guess.

A VRM also marks renderers to hide from the wearer or show only to them. Basis hides the head
bone and everything under it in first person, which covers a face skinned to the head and hats
parented to it. The flags are reported rather than converted. If something still blocks the
camera, add a Basis Head Chop naming it.

## What does not

- **The centre transform.** VRM simulates relative to it so hair does not lag behind a moving
  avatar. Basis has no equivalent, though its own root motion handling covers some of the same
  ground.
- **Inside colliders**, which hold bones within a shape rather than pushing them out. Basis only
  pushes out, so those are written as ordinary colliders and reported: they now push the opposite
  way, and are worth removing if the result looks wrong.
- **Where the avatar looks.** VRM aims the eyes with curves or an expression per direction;
  Basis drives gaze from the eye bones. Only the eye offset carries across, not the aiming.
- **The avatar's metadata**: its title, author and permissions.
