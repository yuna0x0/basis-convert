# 0008: Conversion options filter the plan, they do not steer the reading

**Status:** accepted, 2026-08-30

## Decision

The window can narrow a conversion, by kind (physics, colliders, constraints, avatar descriptor,
menu toggles) and one item at a time. That narrowing is applied between planning and writing:

- `ConversionOptions` sits on the plan, and each planned item carries an `Include` flag.
- The plan exposes `SelectedRigs()`, `SelectedConstraints()`, `SelectedVixxyControls()` and
  `DescriptorSelected` beside the full lists.
- Readers and mappers are unaware of any of it. Every scan reads the whole avatar.

Diagnostics follow the selection: `SelectedDiagnostics()` is what the window and the report
group, `AllDiagnostics()` remains the full picture. What the options leave out is stated as left
out, by the window and in the report's summary, rather than disappearing.

## Why

The counts, the detected source kind and the diagnostics are the reason to trust the result. If
options were pushed down into the readers, unchecking a box would change what the avatar appears
to contain, and a narrowed scan would be a different pass over the source with its own failure
modes to reason about. Filtering a complete plan keeps a narrowed conversion equal to a full one
minus the parts left out.

It is also the shape the plan already had: a list of independent items, each with its own
diagnostics, which is what the per-rig preset dropdown was already editing in place.

## Consequence

- `AvatarConverter.FindReplaceable` follows the selection too, so a re-convert with physics
  switched off leaves an earlier conversion's rigs alone rather than clearing them. Replacement
  is scoped to what is about to be written, which is the rule [0006](0006-one-rig-per-source-component.md)
  and [0007](0007-repeated-conversions.md) already set.
- Per-item choices live on the plan, so they reset when the avatar is rescanned. Category
  choices live on the window and are kept in `EditorPrefs`.
- Scanning an avatar is not cheaper when options are narrowed. It has not been slow enough for
  that to matter; if it becomes so, the readers are where to look, not this.
