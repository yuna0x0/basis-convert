# basis-convert

Editor tooling for bringing content from other social VR platforms into
[Basis](https://basisvr.org/).

The package is in [`Packages/com.yuna0x0.basis.convert`](Packages/com.yuna0x0.basis.convert).
Read [its README](Packages/com.yuna0x0.basis.convert/README.md) to install and use it; this file
covers the repository itself.

## Layout

```
Packages/com.yuna0x0.basis.convert/   the package
  Editor/Sources/                     read foreign formats into the intermediate model
  Editor/Model/                       the intermediate model, plain data
  Editor/Mapping/                     model to Basis component parameters
  Tests/Editor/
agent/                                design notes, decisions, research, worklog
```

Reading, mapping and writing are deliberately separate stages. A new source platform is a new
reader, a new content type is a new mapper and writer, and neither disturbs the other.

## Status

Early: names, APIs and the package id are all still subject to change.

Working: VRChat PhysBones, their colliders and all six VRChat constraint types convert to their
Basis equivalents, from **Tools > Basis Convert > Convert VRChat Avatar**. Nothing is written
until you confirm, and one undo reverts a whole conversion.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). [`agent/`](agent/README.md) holds the design notes and
the reasoning behind past decisions; read it before proposing a change in direction.

## License

MIT, see [LICENSE](Packages/com.yuna0x0.basis.convert/LICENSE).

Basis, BasisVR and Basis Framework are trademarks of the Basis Project. This is an independent
tool, not affiliated with or endorsed by them.
