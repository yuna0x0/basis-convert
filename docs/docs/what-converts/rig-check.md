---
sidebar_position: 8
---

# Rig check

Alongside the conversion, the humanoid rig is checked against what Basis's full-body IK expects.
Nothing here is converted: these are settings on the model, and the report says which ones will
give Basis trouble.

- **The bone mapping is complete**, and the Animation Type is Humanoid, which Basis requires.
- **The Jaw bone is not mapped.** The Basis setup guide asks for it to be cleared on imported
  avatars, and the window offers to do it. That edits the model's import settings and reimports
  it, so it is confirmed separately and is not covered by undo.
- **Eye bones are mapped**, since Basis calibrates gaze from them when the avatar loads.
- **Twist bones exist and are named so Basis finds them.** Basis picks up the first direct child
  of an arm bone whose name contains `twist` or `roll`, case-insensitively.

Props and clothing carry physics but no humanoid rig, so this is skipped for them.
