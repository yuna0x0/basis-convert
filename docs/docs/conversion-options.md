---
sidebar_position: 4
---

# Conversion options

A conversion covers the object you pick and its children. What it writes can be narrowed, either
by kind or one item at a time.

## Basic

Under **What to convert**, each row is a kind of thing a conversion produces, with a count beside
it:

| Option | What it writes |
|---|---|
| Physics | Jiggle physics rigs, from PhysBones and from Dynamic Bone |
| Constraints | Basis constraints, from VRChat constraints |
| Avatar descriptor | The `BasisAvatar` component: view position, visemes, blink |
| Menu toggles | HVR Vixxy controls and their menu items |
| Authored motion | `BasisAuthoredMotion`, with a clip baked from each always-on animator layer |

A row is greyed out when the avatar has nothing of that kind, so an empty checkbox always means a
choice rather than an absence.

Authored motion is the one row that writes an asset into the project, the baked clip, which stays
there if you undo the conversion. See [Authored motion](what-converts/authored-motion.md).

{/*
  IMAGE PLACEHOLDER: the What to convert section with its checkboxes and counts.
  Save as docs/static/img/options-basic.png, then replace this comment with:
  ![The basic options](/img/options-basic.png)
*/}

## Advanced

**Advanced** adds:

- **Colliders**, under Physics. Rigs are still written without them, and their bones pass through
  the body instead of resting on it.
- **A checkbox per prefab**, so an accessory parented onto an avatar is not converted with it. Prefabs
  that hold nothing convertible are summarised rather than listed.
- **A checkbox per rig, constraint, toggle and motion**, each with what it affects.
- **The tuning weights** described in [Physics](what-converts/physics.md).
- **Remove the components it read from**, off by default. See below.

{/*
  IMAGE PLACEHOLDER: the advanced view with the prefab list and per-item checkboxes expanded.
  Save as docs/static/img/options-advanced.png, then replace this comment with:
  ![The advanced options](/img/options-advanced.png)
*/}

## Removing the components it read from

VRChat and VRM components arrive as missing scripts, and Unity refuses to save a prefab holding
one. So a converted avatar cannot be saved as a prefab while they are still on it. This option
removes them once the conversion has written its own components.

It is off by default, and unlike everything else here it cannot be undone. The prefab the avatar
came from still holds them, so converting it again means starting from that prefab. The report
says what was removed as `apply.sourceRemoved`.

Whether a build needs this depends on the project. Basis ships the NDMF integration, and when
NDMF itself is also installed the build strips missing scripts before it stages the prefab it
bundles. NDMF is not part of Basis, so without it nothing strips them.

## What narrowing does not change

The scan always reads the whole avatar, so the counts and the detected source kind do not move
with what is ticked. What changes is what gets written, and what is reported: diagnostics follow
the selection, and whatever you left out is named as left out in the report.

Choices about kinds are remembered between sessions. Choices about individual items reset when
the avatar is rescanned.
