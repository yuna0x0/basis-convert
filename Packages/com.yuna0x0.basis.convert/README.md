# Basis Convert

Editor tooling for bringing content from other social VR platforms into
[Basis](https://basisvr.org/).

Much of moving an avatar between platforms is a direct re-entry of the same data: the same
bone chains, the same colliders, the same constraint relationships, expressed with a different
set of components. This does that part, reports what it could not carry over, and leaves the
result editable.

## What it does today

Converts VRChat PhysBones and their colliders into Basis Jiggle Physics rigs, carrying over
per-bone falloff curves, collider shapes, ignored transforms and grab settings. Converts all six
VRChat constraint types into their Basis equivalents.

Early, and the package name may still change. Avatar descriptors, legacy Dynamic Bone and Magica
Cloth, and toggle systems come next. Props and worlds are in scope later, and so
are source platforms other than VRChat, which is why reading, mapping and writing are separate
stages.

## Using it

1. Import your avatar into a Basis project. Its VRChat components will show as missing scripts,
   which is expected and is what this reads.
2. Drag the avatar into a scene.
3. Open **Tools > Basis Convert > Convert VRChat Avatar**, or right-click the avatar in the
   hierarchy and pick the same entry.
4. Read the summary. Nothing is written until you press Convert, and the report lists everything
   that will be approximated or dropped.
5. Convert, then tune the resulting components by hand. One undo reverts the whole conversion.

To check the result, use the `Basis Avatar` component's **Test in Editor** button. Jiggle physics
only starts once an avatar is calibrated, so plain Play mode will not show it moving.

The avatar has to still be linked to its prefab, since that file is where the VRChat data is
read from. Convert before unpacking the prefab.

## Installing

The package needs a Basis project. It reads Basis SDK types and the Jiggle Physics package, both
of which ship with the Basis framework.

Add it through the Unity Package Manager, `Add package from git URL`:

```
https://github.com/yuna0x0/basis-convert.git?path=/Packages/com.yuna0x0.basis.convert
```

That needs no extra tooling. VPM and OpenUPM listings will follow once the package is out of
early development.

The `unity` field in `package.json` is a minimum, not a pin, so the package keeps working when
Basis moves to a newer editor.

## Why the VRChat SDK is not involved

The VRChat SDK is documented as changing project settings irreversibly when installed into a
Basis project, so it is not installed there. Every VRChat component in a Basis project is
therefore a missing script.

The data is still there. Unity keeps a missing script's serialized fields in the asset file, so
this reads the prefab directly and identifies components by their script reference rather than
by type. No VRChat SDK, no second Unity project, no export step.

## Conversion is not lossless

The two systems model physics differently and some settings have no counterpart. PhysBone
`pull` and `spring` onto jiggle `stiffness` and `drag` is a fit rather than a formula, and
VRChat's polar angle limits have no single-cone equivalent.

So conversion runs as an undoable editor action rather than at build time: it produces real
components you can tune by hand and keep, rather than regenerating them and discarding your
tuning on every build. Everything approximated or dropped is reported. Nothing changes
silently.

## Contributing

See [CONTRIBUTING.md](../../CONTRIBUTING.md).

## License

MIT, see [LICENSE](LICENSE).

Basis, BasisVR and Basis Framework are trademarks of the Basis Project. This is an independent
tool, not affiliated with or endorsed by them.
