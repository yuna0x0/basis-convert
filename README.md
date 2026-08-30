# basis-convert

Editor tooling for bringing content from other social VR platforms into
[Basis](https://basisvr.org/). First target: VRChat PhysBones to Jiggle Physics.

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

Not usable yet, and names, APIs and the package id are all still subject to change.

Built so far: reading VRChat component data out of prefabs whose scripts are missing, resolving
each component to the transform that carries it, and mapping PhysBones onto jiggle parameters.
Writing components into a scene is next, and until it exists there is nothing to run.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). [`agent/`](agent/README.md) holds the design notes and
the reasoning behind past decisions; read it before proposing a change in direction.

## License

MIT, see [LICENSE](Packages/com.yuna0x0.basis.convert/LICENSE).

Basis, BasisVR and Basis Framework are trademarks of the Basis Project. This is an independent
tool, not affiliated with or endorsed by them.
