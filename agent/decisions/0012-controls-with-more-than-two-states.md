# 0012: Controls with more than two states, and radials as sliders

**Status:** accepted, 2026-08-31

## Decision

A Vixxy control holds a list of choices with a value per choice, so it is not limited to on and
off. Three menu shapes are read onto it:

- **A toggle**, two choices, off first.
- **A selector**: several menu entries sharing one int parameter, each setting a different value.
  They become one control with a choice per value, named after the entry that selects it.
- **A radial puppet**: one control presented as a slider, with the two ends of its blend tree as
  its choices.

Two-axis and four-axis puppets are not read. They drive two parameters at once, which no single
Vixxy control expresses.

## Why a selector is one control, not several

The menu shows several entries, but they are mutually exclusive by construction: each writes its
own value into a shared parameter, and the animator layer has one state per value. Emitting one
control per entry would produce controls that silently fight over the same objects, and the user
would have to know which ones cancel each other. One control with a choice per value is what the
data actually says, and Vixxy already draws it as a picker.

The consequence is that the control is named after the parameter rather than after any one entry,
because no entry names the whole thing.

## Why a radial keeps only its ends

Vixxy interpolates between a control's choices in a straight line. A blend tree's children sorted
by threshold describe a sweep; the lowest and highest describe the same sweep's endpoints exactly,
and any motion the tree held in between is approximated by the line through them. Keeping the
intermediate motions as extra choices would not help: the choices are what the slider interpolates
between, not stops it passes through, so a third choice changes where the slider's midpoint sits
rather than adding fidelity.

Anything lost this way is reported as `vixxy.puppetEnds`, which is the same rule the rest of the
converter follows.

## What this needed in the readers

Neither half of a radial is found by the path a toggle takes. The menu entry names its parameter
under `subParameters` rather than as its own, and the layer holds a blend tree rather than
transitions between states, so both readers gained a second entry point rather than a wider
version of the first.

## Rejected

- **Baking a radial to a fixed number of steps.** It turns a continuous control into a selector
  with arbitrary granularity, and Vixxy already has the continuous shape.
- **Leaving selectors reported as untraceable.** They were the largest remaining gap: on the
  reference avatar, two of the three parameters that never traced were selectors, and both are
  ordinary outfit and hairstyle pickers rather than anything unusual.
