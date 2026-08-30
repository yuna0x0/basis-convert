# basis-convert

Unity Editor tooling that converts VRChat avatar components into their
[BasisVR](https://basisvr.org/) equivalents.

The package lives in [`Packages/com.yuna0x0.basis.convert`](Packages/com.yuna0x0.basis.convert),
and its README is the one to read if you just want to use it.

## Repository layout

```
Packages/com.yuna0x0.basis.convert/   the UPM package
agent/                                design notes, decisions, research, worklog
```

`agent/` is a committed knowledge base so that work can be picked up between sessions without
re-deriving what is already known. Start at [`AGENTS.md`](AGENTS.md).

## Status

Early, and nothing is stable yet, including the name.

Working so far: reading VRChat component data out of prefabs whose scripts are missing, which
is what the whole tool depends on, and tying each component back to the bone that carries it.

## License

MIT, see [LICENSE](Packages/com.yuna0x0.basis.convert/LICENSE).

Basis, BasisVR and Basis Framework are trademarks of the Basis Project. This is an independent
third party tool, not affiliated with or endorsed by them.
