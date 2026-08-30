# 0010: Dependencies are declared to VPM, referenced directly, and not gated

**Status:** accepted, 2026-08-30

## Decision

`yuna0x0.Basis.Convert.Editor` references `BasisSDK`, `BasisSDKEditor`, `BasisDebug`,
`com.gator-dragon-games.jigglephysics` and `HVR.Basis.Comms` as ordinary assembly references,
none of them gated behind a `versionDefines` symbol.

Every package those assemblies come from is declared in `vpmDependencies`:

```json
"vpmDependencies": {
  "com.basis.sdk": "0.0.1",
  "com.gator-dragon-games.jigglephysics": "16.0.0",
  "dev.hai-vr.basis.comms": "0.0.1"
}
```

The three Basis assemblies all belong to `com.basis.sdk`. UPM `dependencies` stays empty.

## Why declare them

They are used, so they are declared. It is also what the surrounding packages do:
`com.basis.framework` lists `com.gator-dragon-games.jigglephysics` in its own `vpmDependencies`,
and `dev.hai-vr.basis.comms` lists `com.basis.sdk`, `com.basis.framework` and `com.basis.server`.
All of those live inside the Basis repository rather than in a listing, and a VPM client treats a
dependency as satisfied by what the project already has, which a Basis project does.

## Why UPM `dependencies` stays empty

That field is resolved against a registry. None of these packages is published to one, so naming
them there would break a git URL or OpenUPM install rather than describe it. `vpmDependencies` is
the field VPM clients read, and it is the right place for packages that arrive with Basis.

## Why nothing is gated

An asmdef reference is all or nothing: if the assembly is absent the whole package fails to
compile, not only the part that uses it. Making one optional means moving the code that touches
it into a second assembly with `"defineConstraints": ["<symbol>"]`, which Unity skips when the
symbol is absent, and giving the core an interface to call into instead of a direct call.

That is real structure to carry for a situation that does not exist: `git ls-files` on a Basis
checkout finds both `Basis/Packages/dev.hai-vr.basis.comms` and
`Basis/Packages/com.gator-dragon-games.jigglephysics` tracked there, so they arrive with the
framework. This package only means anything inside a Basis project.

If Basis ever unbundles Haï's Comms package, the fix is that split: `VixxyWriter`, the toggle
mapper and the planner's toggle path move into an assembly constrained on a symbol declared
through `versionDefines`, and the rest keeps working without toggles. Nothing else has to move,
because Vixxy is only touched in those places.
