# Contributing

## Setting up

You need a Basis project to build against: the package references Basis SDK types and the
Jiggle Physics package. Clone [BasisVR/Basis](https://github.com/BasisVR/Basis) and open the
`Basis/` subfolder as the Unity project, not the repository root. Use the editor version in that
project's `ProjectSettings/ProjectVersion.txt`; Basis moves between editor versions and the
package follows it rather than pinning one.

Develop this repository separately and link the package into that project, so the two stay
independent:

```sh
ln -s /path/to/basis-convert/Packages/com.yuna0x0.basis.convert \
      /path/to/Basis/Basis/Packages/com.yuna0x0.basis.convert
```

Add that path to the Basis clone's `.git/info/exclude`, not its `.gitignore`, so nothing of
yours lands in a Basis commit. Work on a branch there and never commit to it.

## Tests

Run the EditMode tests from the Test Runner window, or headlessly if you have a Unity CLI
available:

```sh
unity test /path/to/Basis/Basis --mode EditMode --filter "yuna0x0.Basis.Convert*"
```

Most of the code is deliberately free of scene and AssetDatabase access so it can be tested
without an editor open. Keep it that way: readers take text, mappers take plain data, and only
the writers touch Unity objects.

Some tests need a real VRChat avatar imported into the Basis project. Those assets cannot be
distributed, so the tests skip themselves when the fixture is absent rather than failing. If you
are working on the readers, point the fixture path at an avatar you own.

## Style

Match the surrounding code, which follows the Basis codebase: four space indent, Allman braces,
PascalCase public members, `_camelCase` private fields, plain public fields over properties.
Log through `BasisDebug` with a tag rather than `Debug.Log`.

Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/), as Basis
does. Describe what changed and why; skip tool or process detail.

## Things worth knowing before you change something

- **Everything this package ships is editor-only.** Basis validates avatars against an
  allow-list of component types at load, and ours are not on it. Persistent state belongs on a
  GameObject tagged `EditorOnly`, which the Basis build pipeline strips.
- **Conversion is destructive and undoable, on purpose.** Some of the mapping is a judgement
  call, so the output has to be editable and has to survive rebuilds. See
  `agent/decisions/0003`.
- **Anything the converter cannot carry over must produce a diagnostic**, not a silent omission.
- Do not commit third party assets: no VRChat SDK, no purchased avatars or plugins, in any form.
  Script GUIDs and field names are facts about a file format and are fine to record.

## Reporting a conversion that came out wrong

Include the source component's settings, what the converted result looked like, and what you
expected. If the source avatar is not yours to share, the settings alone are usually enough.
