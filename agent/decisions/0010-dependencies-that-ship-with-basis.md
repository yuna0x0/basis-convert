# 0010: Dependencies that ship with Basis are referenced directly

**Status:** accepted, 2026-08-30

## Decision

`yuna0x0.Basis.Convert.Editor` references `BasisSDK`, `BasisSDKEditor`, `BasisDebug`,
`com.gator-dragon-games.jigglephysics` and `HVR.Basis.Comms` as ordinary assembly references.
None of them is gated behind a `versionDefines` symbol, and `vpmDependencies` in `package.json`
names only `com.basis.sdk`.

## Why

All of them ship inside the Basis repository. `git ls-files` on a Basis checkout finds
`Basis/Packages/dev.hai-vr.basis.comms` and `Basis/Packages/com.gator-dragon-games.jigglephysics`
tracked there, so they arrive with the framework rather than being installed separately. This
package only means anything inside a Basis project, so inside its intended environment every
reference resolves.

Gating would mean more than a define. An asmdef reference is all or nothing: if the assembly is
absent the whole package fails to compile, not only the part that uses it. Making one optional
means moving the code that touches it into a second assembly with
`"defineConstraints": ["<symbol>"]`, which Unity skips when the symbol is absent, and giving the
core an interface to call into instead of a direct call. That is real structure to carry, for a
situation that does not exist.

## Why the dependencies are not declared to VPM

A VPM client resolves `vpmDependencies` against listings. Neither Jiggle Physics nor
`dev.hai-vr.basis.comms` is published in one, because both live inside the Basis repository, so
declaring them would turn a working install into a failed resolve. `com.basis.sdk` stays declared
because it names the thing this package targets.

## If it ever changes

If Basis unbundles Haï's Comms package, the fix is the two-assembly split described above:
`VixxyWriter`, the toggle mapper and the planner's toggle path move into an assembly constrained
on a symbol declared through `versionDefines`, and the rest of the package keeps working without
toggles. Nothing else has to move, because Vixxy is only touched in those places.
