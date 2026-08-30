# 0003: Conversion is a destructive, undoable editor action

**Status:** accepted, 2026-08-30

## Decision

Conversion runs from a menu item, writes real components into the hierarchy, and registers
every mutation with `Undo` so one Ctrl+Z reverts the whole operation. It is not an NDMF
build-time pass.

The mapper itself is a pure library with no scene side effects, so an NDMF back-end can be
added later without touching it.

## Why

PhysBone to jiggle is not a formula. `pull` plus `stiffness` onto jiggle `stiffness`, and
`spring` onto `drag`, are judgement calls, and their meaning changes with both
`integrationType` and the PhysBone `version` field. Whatever the converter produces, someone
will open the resulting `JiggleRig` and tune it by feel. A build-time pass would regenerate from
source every build and throw that tuning away.

Non-destructive is the right default for authoring tools that *add* behaviour, such as Modular
Avatar. This tool *translates* an existing setup once, as a migration. Migrations are
destructive by nature and the output is meant to be edited afterwards.

## Consequences

- A dry run is mandatory before writing anything, and a report records what was approximated
  or dropped.
- Re-running must update rather than duplicate, so the converter needs bookkeeping tying each
  produced component back to the source component it came from.
- That bookkeeping cannot be a normal runtime component. See
  [0004](0004-editor-only.md).
