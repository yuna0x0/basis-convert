# 0001: Package and namespace layout

**Status:** accepted, 2026-08-30

## Decision

- Package id: `com.yuna0x0.basis.convert`
- Assembly definitions: `yuna0x0.Basis.Convert.Editor`, `yuna0x0.Basis.Convert.Editor.Tests`
- Namespace root: `yuna0x0`, then `Basis`, then the product

## Why

`basis` is a scope segment under our vendor root, not the product name, so sibling packages can
be added later without renaming anything: `com.yuna0x0.basis.common`,
`com.yuna0x0.basis.<whatever>`. If shared code needs extracting from this package it has an
obvious home.

This mirrors what Haï does, which is the closest thing to an established convention in this
ecosystem: packages `dev.hai-vr.basis.comms` and `dev.hai-vr.basis.ndmf`, assemblies
`HVR.Basis.Comms` and `HVR.Basis.NDMF`.

The namespace root is `yuna0x0`, lowercase, because that is the official spelling of the name.
C# permits a lowercase namespace identifier; PascalCase is a convention, not a rule, and brand
casing wins over it.

## Rejected

- `com.yuna0x0.basis-convert` with namespace `yuna0x0.BasisConvert`. Treats "BasisConvert" as
  one atom, leaving nowhere for sibling packages and no natural split point for shared code.
- `Yuna0x0` as the namespace root. Only worth it if C# required an uppercase first character,
  which it does not.

## Note

The display name is still provisional. See [0002](0002-menu-placement-and-naming.md).
