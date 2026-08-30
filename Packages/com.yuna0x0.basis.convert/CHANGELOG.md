# Changelog

All notable changes to this package are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Unity YAML scanner that reads component data out of prefabs whose scripts are missing.
- Script identity table mapping VRChat SDK, Dynamic Bone and Magica Cloth script references
  onto the component types they were.
- Resolver that ties each component in a prefab file back to the bone that carries it, through
  both local file identifiers and nested prefab source references.
- Reader for VRCPhysBone and VRCPhysBoneCollider, including their falloff curves.
- PhysBone to Jiggle Physics mapper, with a diagnostic for everything it approximated or had to
  drop, and a tunable profile for the two parts of the mapping that are judgement calls.
