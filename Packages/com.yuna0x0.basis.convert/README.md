# Basis Convert

Editor tooling for bringing content from other social VR platforms into
[Basis](https://basisvr.org/).

Much of moving an avatar between platforms is a direct re-entry of the same data: the same
bone chains, the same colliders, the same constraint relationships, expressed with a different
set of components. This does that part, reports what it could not carry over, and leaves the
result editable.

## Status

Not usable yet. VRChat PhysBones are read and mapped onto jiggle parameters, falloff curves and
colliders included, but nothing is written into a scene yet, so there is no command to run. That
is the next piece of work.

After that: constraints, avatar descriptors, legacy Dynamic Bone and Magica Cloth, and toggle
systems. Props and worlds are in scope later, and so are source platforms other than VRChat,
which is why reading, mapping and writing are separate stages.

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
