---
sidebar_position: 4
---

# Avatar descriptor

The VRChat avatar descriptor becomes a `BasisAvatar`, which is what Basis loads an avatar
through.

## What carries across

- **View position**, as the avatar's eye position.
- **Visemes**, all fifteen. Both platforms order them the same way, so they map by position
  rather than by name.
- **Blink**, taken from the eyelid blendshape the descriptor names.

## What Basis fills in itself

Opening the `BasisAvatar` inspector for the first time makes Basis populate the animator, the
human scale, the renderer list and the mouth position. Those are left to it rather than guessed,
and a re-conversion updates the component in place instead of replacing it, so nothing Basis
filled in is thrown away.

## What does not carry across

Eye look settings, lip sync mode, the collider layout VRChat uses for its own contacts, and the
expression menu and parameters, which are a separate subject: see
[Menu toggles](menu-toggles.md).
