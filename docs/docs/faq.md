---
sidebar_position: 8
---

# Questions

### Do I need the VRChat SDK installed?

No, and you should not install it into a Basis project. VRChat components arrive as missing
scripts, and their data is read from the prefab file, which is what makes this work at all.

### My avatar shows missing scripts everywhere. Is that a problem?

That is the normal state of an imported VRChat avatar in a Basis project, and it is what gets
read. Do not remove them before converting.

### Nothing was found in my avatar.

Check that the object you picked is still linked to its prefab. A prefab that has been unpacked
has no file left to read from. If the avatar was stripped of its components before export, there
is nothing to convert either.

### The jiggle physics does not move in Play mode.

Jiggle physics runs on a calibrated avatar. Press **Test in Editor** on the `BasisAvatar`
component instead.

### Can I convert clothing separately from the avatar?

Yes. Select the clothing object and convert that. A conversion replaces only what it wrote
itself, so anything already converted stays as it is.

### An undo left a file behind.

Baked motion clips are project assets, not components, so an undo does not remove them. Anything
else a conversion writes disappears. The clip sits in a `Watari Motion` folder beside the
animation it was baked from, and converting again writes over it rather than adding another.

### I converted twice by accident.

Converting again offers to replace what the previous conversion wrote, naming what will go, and
one undo reverts the removal and the rewrite together. Components somewhere else on the avatar,
and Vixxy controls you made yourself, are left alone.

### My avatar's idle animation does not play.

Authored motion runs on a calibrated avatar, the same as jiggle physics: press **Test in Editor**
on the `BasisAvatar` component. If nothing was converted at all, check the report: only layers
with no parameter steering them are read as motion, and only the rotation in them is baked.

### The physics feels wrong compared to the original.

Two settings are fits rather than conversions: stiffness and drag. Both are adjustable under
**Advanced** in the window. See [Physics](what-converts/physics.md).

### A component was reported as an unknown script.

Please [open an issue](https://github.com/yuna0x0/watari-basis/issues) with the code from the
report. Components are identified by their script reference, and that table only grows by people
reporting what it does not yet know.

### Does this work for props and worlds?

"Prop" means two different things here, so both answers:

- **An object worn on an avatar**, a piece of clothing, an accessory, a gimmick: yes. It is a
  prefab with physics and constraints like any other, and it converts with the avatar it is
  attached to, or on its own if you select it.
- **A Basis prop**, meaning the `BasisProp` content type that players spawn and that is
  networked: no. Nothing here writes one, and no `BasisProp` component is created.

`BasisAvatar` is the only Basis content type written today. That is not a limit of what the tool
is for: props and worlds are in scope, and the reading and mapping stages are shared. What is
missing for each is not the same thing, though. A prop needs no conversion so much as authoring,
which Basis already has its own validator and fix-ups for. A world's geometry and lighting are
plain Unity and need nothing at all, while its behaviour is Udon, which has no readable form to
convert from.

Physics and constraints do convert on anything you point the window at, avatar or not, so a
world's props or a spawnable object get that much already.
