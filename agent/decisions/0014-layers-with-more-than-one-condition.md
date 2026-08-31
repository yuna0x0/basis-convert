# 0014: Layers with more than one condition

**Status:** accepted, 2026-08-31

## Decision

A layer is read as a menu parameter's own when exactly one of **the avatar's own** parameters
steers it. VRChat's built-in parameters do not count towards that, on two conditions:

1. Every transition that tests a built-in also tests the menu parameter. A built-in steering a
   transition on its own means the layer belongs to it.
2. No value of the menu parameter leads to two different states.

What the rebuilt control no longer waits for is reported as `vixxy.builtinGuard`.

## Why built-ins are different

`IsLocal`, `InStation`, `Seated`, `GestureLeft` and the rest are driven by VRChat, not by the
avatar, and Basis drives none of them. A layer testing one is not a layer steered by two things
the wearer controls: it is the toggle's layer with a condition attached that cannot survive the
move. Refusing to read it lost the toggle and reported nothing useful; reading it and saying what
was dropped is the same trade the rest of the converter makes.

The behavioural difference is real and worth the diagnostic. A gimmick that only ran for the
wearer now runs for everyone who can see the avatar.

## Why the two conditions

**A gesture layer has a state per gesture.** Its transitions are steered by `GestureLeft` alone,
and reading it as a toggle's would take clips the toggle never selects. Requiring every built-in
condition to sit alongside the menu parameter is what separates a guard from a selector.

**A layer can satisfy that and still not be readable.** A facial expression layer keyed on
`(Face, GestureLeft)` names `Face` in every transition, so the first condition passes, but two
gestures at the same `Face` value lead to different states. The value alone does not decide, so
the layer is refused.

The second condition is a fix as much as a guard: before it, such a layer was read as though the
first transition were the only one, keeping whichever clip happened to come first.

## The table, and what an unknown name means

`VrchatBuiltInParameters` lists the names the SDK drives. A name it does not know is treated as
the avatar's own, which is the safe way round: an unrecognised name makes the layer look steered
by two things and leaves it alone, rather than quietly dropping a condition nobody checked.

## Still not read

A layer steered by two of the avatar's own parameters. One toggle gating another has no single
Vixxy control that expresses it. Vixxy has an aggregator that may cover some of these; it has not
been looked at.
