# Changelog

Notable changes to this package. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Menu toggles that only switch objects are rebuilt as HVR Vixxy controls, each with a menu item.
  Toggles that also set blendshapes or drive material properties are reported rather than half
  built.

- The window names what it detected before converting: VRChat avatar, humanoid avatar with
  Dynamic Bone, prop or clothing, and the signals behind the guess. Warns when something has a
  humanoid rig but nothing convertible on it, which usually means the wrong object was picked.
- Menu toggles are traced to the animator layers behind them, and each side reduced to what it
  does: objects switched, blendshapes set, and how much it does beyond that.

- Avatars that never came from VRChat convert too. Dynamic Bone is an ordinary Unity asset, and
  an avatar using it now gets a `BasisAvatar` component created from its humanoid rig, with the
  report saying what was left for Basis to fill in.

- The expression menu tree and parameter list are read and described: how many controls of each
  kind, across how many menus. Submenus are separate assets and are followed. Nothing is
  converted yet; this is what a Vixxy rebuild has to cover.

- The report now names the systems that do not convert at all: expression menus, expression
  parameters, custom animation layers and VRChat contacts. None of them have a Basis equivalent,
  and a report that stayed silent about them read as though nothing was lost.

- Rig readiness check: reports whether the humanoid mapping is complete, whether a Jaw bone is
  mapped that the Basis setup guide asks to be cleared, whether the eye bones are mapped, and
  which arm bones have a twist child Basis will pick up. Offers to clear the Jaw mapping.

- Legacy Dynamic Bone components convert to jiggle rigs, along with their colliders. Damping and
  inert map straight across, and a component driving several roots becomes one rig per root.

- The VRChat avatar descriptor converts to a `BasisAvatar` component: view position, the fifteen
  viseme blendshapes and the blink blendshape. The component is updated in place on a re-convert
  rather than replaced, so anything Basis filled in itself survives.

- Converting an avatar again replaces the previous output instead of stacking a second set of
  components on it, after a confirmation. Only the bones the conversion writes to are touched, so
  components added by hand elsewhere on the avatar survive.

- VRChat constraints convert to their Basis equivalents, covering all six types. A constraint
  that drove a transform other than its own object is moved onto the transform it drives, since
  Basis constraints always drive their own.

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
- Editor window under Tools > Basis Convert, and a hierarchy context menu entry. Scans an
  avatar, shows what a conversion would produce, converts on confirmation, and writes a report.
- Conversion report grouping diagnostics by code, since they repeat per component.
- End to end pipeline: plan an avatar from its prefab without changing anything, then apply the
  plan to a hierarchy. Transforms are located in the target by sibling-index path, so a plan read
  from a prefab asset applies exactly to a scene instance of it.
- Writer that produces `JiggleRig` components, starting from the jiggle package's preset rigs so
  parameters the source does not determine keep values tuned by that package's author. Every
  mutation is registered with Undo, so one undo reverts a whole conversion.
