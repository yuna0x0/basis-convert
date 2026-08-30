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

Yes. Select the clothing object and convert that. A conversion replaces only components on the
objects it is about to write to, so anything already converted stays as it is.

### I converted twice by accident.

Converting again offers to replace what the previous conversion wrote on the same objects, and
one undo reverts the whole thing.

### The physics feels wrong compared to the original.

Two settings are fits rather than conversions: stiffness and drag. Both are adjustable under
**Advanced** in the window. See [Physics](what-converts/physics.md).

### A component was reported as an unknown script.

Please [open an issue](https://github.com/yuna0x0/watari-basis/issues) with the code from the
report. Components are identified by their script reference, and that table only grows by people
reporting what it does not yet know.

### Does this work for props and worlds?

Props and clothing convert: they carry physics and constraints like anything else. Worlds are not
supported.
