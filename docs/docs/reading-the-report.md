---
sidebar_position: 5
---

# Reading the report

Every conversion produces a report: in the window while you work, and as Markdown through **Copy
report** or **Save report**. It is the record of what came across and what did not.

## Severities

| Heading | Meaning |
|---|---|
| Needs attention | It converts, but not the way it worked before. Check it. |
| Not carried over | The setting has no Basis equivalent and was dropped. |
| Approximated | Fitted onto a Basis setting that does not mean quite the same thing. |
| Mapped directly | Carried across as it was. Listed so the report is complete. |

Entries are grouped by a stable code, because they repeat: an avatar with sixty PhysBones
produces sixty identical notes, which is unreadable one by one and useful as a count.

## Codes you are likely to see

| Code | What it means |
|---|---|
| `physbone.limitType.tooWide` | The angle limit was wider than jiggle physics can express, so no limit was written rather than a tighter one. |
| `physbone.isAnimated` | The PhysBone was marked as animated. Nothing reads that on Basis. |
| `collider.limit` | More colliders were referenced than a jiggle rig can hold. The extras were dropped. |
| `constraint.solveInLocalSpace` | A VRChat constraint setting with no Basis equivalent. |
| `vixxy.notSimple` | A menu toggle that animates over time or drives something a Vixxy control cannot hold. |
| `vixxy.puppetEnds` | A radial puppet blended through motions between its ends. A slider interpolates between its ends in a straight line. |
| `vixxy.builtinGuard` | The toggle's layer also waited on a VRChat parameter such as `IsLocal`. Basis has no equivalent, so the control switches whenever it is used. |
| `motion.baked` | An animator layer that plays on its own was rebuilt as authored motion. |
| `motion.switched` | A menu toggle animated over time, so it was rebuilt as a motion the control switches on. |
| `motion.rotationOnly` | That layer also animates something other than rotation, which a baked motion clip cannot hold. |
| `vrm.stiffness` | A VRM chain's stiffness force was fitted onto jiggle stiffness, which is a narrower scale. |
| `vrm.branchesExcluded` | Bones hanging off a VRM chain that the spring never named were excluded, so they stay as still as VRM left them. |
| `vrm.collider.inside` | A VRM collider held bones inside its shape. Basis only pushes out, so it now pushes the opposite way. |
| `contacts.dropped` | VRChat contacts were found. Basis has no contact system. |
| `modularAvatar.menus` | Modular Avatar menu and animator components were found. See [Modular Avatar](what-converts/modular-avatar.md). |
| `source.unknownScript` | A component whose script this version does not recognise. Please report it. |

## The rig section

Separate from the conversion, the report describes what Basis's full-body IK will make of the
humanoid rig: whether the bone mapping is complete, whether the eye bones are mapped, whether
twist bones are named so Basis finds them, and whether a Jaw bone is mapped that the Basis setup
guide asks to be cleared. These are settings on the model, not things a conversion changes, and
the window offers to clear the Jaw mapping for you.
