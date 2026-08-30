# Changelog

Notable changes to this package. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Reader for Unity YAML that recovers component data from assets whose scripts are missing,
  which is the case for every VRChat component in a Basis project.
- Script identity table covering the VRChat SDK, Dynamic Bone and Magica Cloth, keyed on the
  script reference so components can be identified without their types being present.
- Resolver tying each component in a prefab back to the transform that carries it, through both
  local file identifiers and nested prefab source references.
- Reader for VRCPhysBone and VRCPhysBoneCollider, including per-bone falloff curves.
- PhysBone to Jiggle Physics mapping, with a diagnostic for everything approximated or dropped,
  and a profile for the parts of the mapping that are judgement calls.
- Collider mapping, covering sphere, capsule and plane. A rotated capsule is snapped to the
  nearest axis, since jiggle orients capsules by axis rather than by rotation.
- End to end pipeline: plan an avatar from its prefab without changing anything, then apply the
  plan to a hierarchy. Transforms are located in the target by sibling-index path, so a plan read
  from a prefab asset applies exactly to a scene instance of it.
- Writer that produces `JiggleRig` components, starting from the jiggle package's preset rigs so
  parameters the source does not determine keep values tuned by that package's author. Every
  mutation is registered with Undo, so one undo reverts a whole conversion.
