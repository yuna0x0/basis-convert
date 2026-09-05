---
sidebar_position: 9
---

# Versions checked

The readers were checked against these releases of each source. A component or field a later
release adds is not read until the converter is checked against it, and shows up as
`source.unknownScript` in the report. The report's first line and the bottom of the window state
the same versions.

| Source | Checked against |
|---|---|
| VRChat SDK | 3.10.5, released 2026-09-04 |
| UniVRM | 0.131.2 |
| Dynamic Bone | 1.3.4 |
| Modular Avatar | 1.18.7, with NDMF 1.14.8 |
| Basis | the `developer` branch as of 2026-09-05 |

## What the VRChat SDK added since 3.8

Every avatar component and field in 3.10.5 was compared with 3.8.0. The descriptor, expression
menus and parameters, and the six constraints did not change. What did:

- **Global PhysBone colliders** (3.10.4) are reported as `collider.global.dropped`.
- **Box-shaped contacts** (3.10.4) join the other contacts under `contacts.dropped`.
- **VRC Raycast** (3.10.3) is reported as `raycast.dropped`.
- **Per-platform overrides** (3.8.1) and impostor settings are reported as `vrchat.buildSettings`.
- **VRC Head Chop** (3.6.0) converts to a Basis Head Chop. See
  [Avatar descriptor](what-converts/avatar-descriptor.md).

## VRM

Checked against the specification text for VRMC_vrm 1.0 (expressions, lookAt, firstPerson, meta)
and VRMC_springBone 1.0, and against the consortium's sample models: Seed-san, the constraint and
twist sample, the two isBinary conformance models, the MToon UV animation test, and Alicia 0.51.
All six read and plan without an unrecognised component.

## Dynamic Bone

1.3.4 differs from 1.3.2 only in when it disables its own multithreading. The serialized
fields, and the script guids the readers match on, are unchanged.

## Modular Avatar

Every component 1.18.7 ships is named. The vertex filters that go with Mesh Cutter, and Move
Independently, are mesh and hierarchy work Modular Avatar does on Basis itself, and are listed
with the rest of that kind.
