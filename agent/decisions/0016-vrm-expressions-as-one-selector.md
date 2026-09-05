# 0016: A VRM avatar's expressions become one selector

**Status:** accepted, 2026-09-05

## Decision

The expressions an author added and the emotion presets become one Vixxy control named
Expression, with Neutral as the first choice and one choice per expression. Every blendshape
any of them touches is written at every choice: the expression's weight where it sets the shape,
zero everywhere else. Visemes, blink and look-at are left to Basis as before. A Neutral
expression that carries weights of its own fills the first choice.

## Why

0.2.0 through 0.5.1 wrote a two-choice toggle per expression. Two things were wrong with it. Two
expressions could be on at once, layering Happy onto Angry, which VRM never does: an avatar
wears one expression at a time and the application picks it. And the menu held one entry per
emotion, five or more for a stock avatar, where one entry with a choice each is what the data
describes.

## Rejected

- **A toggle per expression**, the previous design. Layers expressions and floods the menu.
- **A slider per expression.** VRM 1.0 expressions are continuous unless `isBinary`, but a slider
  per emotion has the same two faults and adds a hold-to-blend interaction the wearer does not
  want for a face. Continuous weights are not lost: each choice carries the expression's own
  weights.
- **Emotions on the selector, custom expressions as toggles.** VRM does not distinguish the two
  in use; a custom Wink is worn instead of Happy, not with it. One rule is simpler and the
  wearer can still pick any of them.

## Consequences

`VrmExpressionToVixxyMapper.MapSelector` replaces the per-expression `Map`. The report line
`vrm.expressionsRebuilt` counts the choices on the one control.
