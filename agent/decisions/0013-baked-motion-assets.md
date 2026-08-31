# 0013: Baked motion clips are assets, and where they go

**Status:** accepted, 2026-08-31

## Decision

An animator layer with nothing steering it becomes a `BasisAuthoredMotion` movement of kind
`Sequence`, holding a `BasisMotionClip` baked at conversion time. The baked clip is written to
`<folder of the source animation>/Watari Motion/<clip name>.asset`.

This is the only thing a conversion writes that is not a component on the avatar, and the only
thing an undo does not remove.

## Why an asset at all

`BasisMotionClip` is a flat buffer of rotations at a fixed rate, not curves: the runtime job
interpolates a blittable array rather than touching a managed `AnimationCurve`. There is no
in-component form of it, and one asset is shared by every instance of an avatar. So a converted
motion needs a file, and the conversion has to produce one.

## Why beside the source animation

The alternatives were a folder next to the avatar prefab, a folder chosen by the user, and a
fixed folder under `Assets`. Beside the source animation wins on two counts: the clip lands with
the assets it was derived from, wherever the user keeps the avatar, and the path is a pure
function of the source, so converting twice overwrites rather than accumulating copies. That is
the same rule the components follow, where a re-conversion replaces what the plan targets.

The asset is overwritten in place with `CopySerialized` rather than deleted and recreated, so
anything already referencing it keeps its reference.

## The undo hole, stated rather than closed

Everything else a conversion writes goes through `Undo`, so one Ctrl+Z reverts it. An asset does
not, and `AssetDatabase` operations are not undoable in a way that would be safe to bundle with
component edits. Rather than pretend otherwise, the window says the clip stays, the report says
it, and the docs say it.

## Rotation only

The bake records `localRotation` per frame, following Basis's own `BasisMotionClipBaker`;
`BasisMotionClip` has position and scale arrays reserved but unused, and the runtime writes
rotation. A clip that also moves or scales something keeps the turning and reports the rest as
`motion.rotationOnly`, rather than silently dropping half of what the layer did.

Rotations are recorded as they land on the bone rather than as the curve states them. That is
what lets a clip authored against a different rest pose replay correctly, and it is why the bake
needs the live hierarchy being converted rather than the prefab the plan was read from.

## Motion a menu switches on

A toggle whose clip animates rather than switches becomes a motion of its own, and the control
enables and disables the component. This needs no new mechanism: `HVR_VixxyPermitted` lists
`BasisAuthoredMotion` among the types a Vixxy activation may toggle, an activation holds a
`Component` rather than a GameObject, and the component raises `EnabledStateChanged` so a system
that has already registered it picks the change up without polling.

Two consequences worth knowing:

- **Motions are written before controls**, because a control has to hold a component that does
  not exist until it is written. An activation names its motion by index into the control's own
  motion list rather than by a path, since there is no object to look up.
- **The component starts in whatever state the control's default choice puts it**, so an avatar
  nobody has touched looks right before Vixxy initialises.

A toggle is still dropped whole when its clip animates something other than rotation, or drives
something a control cannot hold at all. Converting the half that works would leave a control that
looks finished and does part of the job.
