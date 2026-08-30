# Basis Convert

Unity Editor tooling that converts VRChat avatar components into their
[BasisVR](https://basisvr.org/) equivalents.

The first feature is VRChat PhysBones to BasisVR Jiggle Physics. Constraints, the avatar
descriptor, legacy Dynamic Bone and Magica Cloth 2, and toggle systems follow.

## Why

The VRChat SDK cannot be installed into a Basis project: it changes project settings
irreversibly. So when you export an avatar from a VRChat project and import it into a Basis
one, every VRChat component arrives as a missing script. The data is still there in the prefab
file, just unreadable. Basis Convert recovers it and writes real Basis components from it.

## Requirements

- Unity 6000.5 (Unity 6.5), matching what BasisVR targets
- A Basis project with the Basis SDK and the Jiggle Physics package

## Status

Early. Nothing here is stable yet, including the package name.

## Conversion is not lossless

Some VRChat concepts have no Basis equivalent, and some map only approximately. PhysBone
`pull`/`spring` to jiggle `stiffness`/`drag` in particular is a judgement call, not a formula.
The converter therefore runs as an undoable editor action rather than at build time, so you can
tune the result by hand and keep that tuning, and it reports everything it approximated or had
to drop.

## License

MIT. See [LICENSE](LICENSE).

## Trademark

Basis, BasisVR and Basis Framework are trademarks of the Basis Project. This package is an
independent third party tool and is not affiliated with or endorsed by them.
