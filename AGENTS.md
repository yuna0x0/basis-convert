# AGENTS.md

Entry point for AI agents and for anyone picking this repo up cold.

## What this is

`basis-convert` is Unity Editor tooling that converts VRChat avatar components into their
BasisVR equivalents. It installs into a Basis project as a UPM package. The first feature is
VRChat PhysBones to BasisVR Jiggle Physics.

Not affiliated with or endorsed by the Basis Project. See "Trademark" below before naming
anything.

## Read this first

`agent/` is the committed knowledge base. Start there, in this order:

1. `agent/README.md` for how the folder works and the hygiene rules.
2. `agent/plans/` for the current design.
3. `agent/decisions/` for anything non-obvious that was settled and why.
4. `agent/worklog/` newest entry, for where the last session stopped.

`agent/research/` holds API inventories and doc extracts. Prefer it over re-deriving facts,
but verify anything that names a file, field or flag before relying on it: Basis is on a
`developer` branch and moves.

## Environment

- Unity `6000.5.10f1`. This is the newest Unity 6.5 release and is what BasisVR targets.
  Do not develop against a different minor.
- The package is developed standalone here and symlinked into a Basis clone so it compiles
  against the real assemblies:
  ```
  ln -s ~/Documents/Projects/basis-convert/Packages/com.yuna0x0.basis.convert \
        ~/Documents/Projects/Basis/Basis/Packages/com.yuna0x0.basis.convert
  ```
  The Basis Unity project root is the `Basis/` subfolder of that clone, not the clone root.
- Work happens on a `basis-convert-dev` branch of the Basis clone, taken from
  `upstream/developer`. **Never commit in the Basis clone.** Local paths used for development
  are listed in that clone's `.git/info/exclude`, not in its `.gitignore`.

## Dev loop

```
unity test ~/Documents/Projects/Basis/Basis --mode EditMode --filter "yuna0x0.Basis.Convert*"
```

Everything in the mapper layer is designed to be testable without opening the editor. Only the
end-to-end check needs a running editor, and it needs the `Basis Avatar` component's
**Test in Editor** button rather than plain Play mode, because jiggle physics only initialises
after avatar calibration.

Close the Unity editor before any git operation on the Basis clone. Switching branches under a
running editor risks corrupting its 25 GB `Library`.

## Tooling available

`unity` (CLI), `vpm`, `openupm`, `gh`, `ilspycmd`.

## Conventions

- C#: 4 space indent, Allman braces, PascalCase public members, `_camelCase` private fields,
  public fields over auto-properties. This matches the Basis codebase so contributions read
  the same on both sides.
- Namespace root is `yuna0x0`, matching the official lowercase spelling of the name.
- Editor menus go under `Tools/<ProductName>/...`, never under Basis's own `Basis/` menu.
  Components use `AddComponentMenu("<ProductName>/...")`. See `agent/decisions/` for why.
- No bare `Debug.Log` in package code; route through `BasisDebug` with a `LogTag`, as the
  Basis style guide requires.

## Trademark

Basis, BasisVR and Basis Framework are trademarks of the Basis Project. Their `TRADEMARK.md`
permits truthful descriptive reference and "Built with Basis", and asks third parties not to
imply affiliation or endorsement. The current package name is provisional for that reason.
