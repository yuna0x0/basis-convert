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
| Approximated, check by eye | Fitted onto a Basis setting that does not mean quite the same thing. |
| Mapped directly | Carried across as it was. Listed so the report is complete. |

Entries are grouped by a stable code, because they repeat: an avatar with sixty PhysBones
produces sixty identical notes, which is unreadable one by one and useful as a count.

## Codes you are likely to see

| Code | What it means |
|---|---|
| `physbone.limitType.tooWide` | The angle limit was wider than jiggle physics can express, so no limit was written rather than a tighter one. |
| `physbone.isAnimated` | The PhysBone was marked as animated. Nothing reads that on Basis. |
| `collider.limit` | More colliders were referenced than a jiggle rig can hold. The extras were dropped. |
| `constraint.solveInLocalSpace.dropped` | A VRChat constraint solved in local space. Basis constraints solve in world space, so the setting was dropped. |
| `vixxy.notSimple` | A menu toggle that animates over time or drives something a Vixxy control cannot hold. |
| `vixxy.puppetEnds` | A radial puppet blended through motions between its ends. A slider interpolates between its ends in a straight line. |
| `vixxy.builtinGuard` | The toggle's layer also waited on a VRChat parameter such as `IsLocal`. Basis has no equivalent, so the control switches whenever it is used. |
| `motion.baked` | An animator layer that plays on its own was rebuilt as authored motion. |
| `motion.switched` | A menu toggle animated over time, so it was rebuilt as a motion the control switches on. |
| `motion.rotationOnly` | That layer also animates something other than rotation, which a baked motion clip cannot hold. |
| `vrm.constraint.rotation` | A VRM rotation constraint copies a delta from rest; a Basis one follows the rotation itself. |
| `vrm.constraint.aim` | A VRM aim constraint states no up direction, so the scene's up is used. |
| `vrm.constraint.roll` | Nothing in Basis copies rotation about one axis, so this became a rotation constraint limited to it. |
| `vrm.objectUnreadable` | The avatar's expressions and licence are still inside the `.vrm` file. Extract them in its import settings. |
| `vrm.licence` | What the avatar's VRM licence says: its title, author and who may wear it. |
| `vrm.licence.restricted` | The licence forbids changing the avatar, or limits who may wear it. |
| `vrm.eyePosition` | The avatar's eye offset became the Basis eye position. |
| `vrm.eyePosition.noRig` | It said where its eyes sit, but the rig is not humanoid with a head mapped. |
| `vrm.firstPerson` | Renderers marked to hide from the wearer. Basis hides the head bone instead. |
| `vrm.stiffness` | A VRM chain's stiffness force was fitted onto jiggle stiffness, which is a narrower scale. |
| `vrm.branchesExcluded` | Bones hanging off a VRM chain that the spring never named were excluded, so they stay as still as VRM left them. |
| `vrm.collider.inside` | A VRM collider held bones inside its shape. Basis only pushes out, so it now pushes the opposite way. |
| `contacts.dropped` | VRChat contacts were found. Basis has no contact system. |
| `modularAvatar.menus` | Modular Avatar menu and animator components were found. See [Modular Avatar](what-converts/modular-avatar.md). |
| `source.notUnpacked` | Nothing was found, and the prefab was saved from an imported model without unpacking. Its components are still in that file. |
| `source.prefabVariant` | The avatar is a prefab variant, so the prefab it inherits from was read as well. Names that base. |
| `source.editorOnlyTool` | Components of an editor-time authoring tool, which carry no runtime behaviour. Nothing to convert, nothing lost. |
| `source.unknownScript` | A component whose script this version does not recognise. Please report it. |

## The rig section

Separate from the conversion, the report describes what Basis's full-body IK will make of the
humanoid rig: whether the bone mapping is complete, whether the eye bones are mapped, whether
twist bones are named so Basis finds them, and whether a Jaw bone is mapped that the Basis setup
guide asks to be cleared. These are settings on the model, not things a conversion changes, and
the window offers to clear the Jaw mapping for you.
