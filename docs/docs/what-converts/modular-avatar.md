---
sidebar_position: 6
---

# Modular Avatar

[Modular Avatar](https://modular-avatar.nadena.dev/) runs on Basis, and much of what it does
needs no conversion. Its components are read here so the parts that cannot work on Basis are
handled rather than silently doing nothing.

Modular Avatar does not need to be installed for this. Its components are read from the prefab
like every other source, and all of them are named, so a component this does not handle is
reported as what it is rather than as an unknown script.

## Left to Modular Avatar

`Merge Armature`, `Bone Proxy`, `Mesh Settings`, `Blendshape Sync` and `Parameters` rearrange the
hierarchy, which is platform-independent work. They are reported as left alone rather than as
unrecognised, and nothing is written for them.

## Rebuilt

`Menu Item` targets VRChat's expression menu and `Merge Animator` targets its animator layer
slots, neither of which exists on Basis, so clothing that installs a toggle this way does nothing
there.

Read together, those two describe a toggle completely: the menu item names the parameter, the
merged animator holds the layer that implements it. Those are traced and rebuilt as Vixxy
controls, on the same terms as [menu toggles](menu-toggles.md) from the avatar's own menu.

`Object Toggle` needs no animator at all. It switches objects while its own object is active, and
a menu item on that object is what makes it active, so the two together say what a toggle and its
clips say. Objects it does not name keep the state the avatar was authored with, which is the
same rule everywhere else.

Paths inside a merged animator's clips are relative to the object the animator was merged at, and
are rebased before anything is resolved. Paths in an `Object Toggle` are not: Modular Avatar
resolves those against the avatar root, so they are used as written.

## Not covered

- Gimmick controllers whose layers are steered by several parameters at once, which are reported
  rather than guessed at.
- Menu structure. A rebuilt control keeps its label, not its place in a menu tree.
