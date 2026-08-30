# agent/

Committed knowledge base for AI agents and humans picking this repo up cold. The point is that
a future session should not have to re-derive what an earlier one already established.

## Layout

| folder | holds | lifetime |
|---|---|---|
| `research/` | API inventories, file format notes, extracts from other projects' docs and source | updated when re-verified |
| `plans/` | design documents for the current and upcoming milestones | living |
| `decisions/` | short ADR-style notes: what was decided, why, what was rejected | append only |
| `worklog/` | `YYYY-MM-DD.md`, one per working session: what happened, what broke, where it stopped | append only |

## How to use it

Read `decisions/` and the newest `worklog/` entry before changing direction on anything. If you
are about to argue for an approach, check whether it was already considered and rejected.

Research notes record what was true when written. **Verify before relying on anything that
names a file, field or flag.** BasisVR develops on a `developer` branch, every one of its
packages is version `0.0.1`, and several fields this package touches are `private` or
`internal` and reached through `SerializedObject`. Things move.

When a research note turns out to be wrong, fix the note in place rather than adding a
correction elsewhere. When a decision is reversed, add a new decision that supersedes the old
one and say so in both.

## Hygiene rules

These are not optional. This repo is going to be public.

1. **Identity.** The only identity that appears anywhere is `yuna0x0 <yuna@yuna0x0.com>`. Never
   a login address, never a real name, never a machine-specific absolute path. Write `~/...`
   or repo-relative paths.
2. **No third party assets.** Never commit the VRChat SDK, Dynamic Bone, Magica Cloth 2, or any
   avatar bought from Booth or elsewhere, in any form, including inside a `.unitypackage`.
   Script guids, fileIDs and field names are facts about a file format and are fine to record
   here. The files themselves are not ours to redistribute.
3. **Fixtures are hand authored.** Test fixtures are minimal prefab YAML written by hand and
   checked into the package's `Tests/Editor/Fixtures`. Real avatars are used for local
   end-to-end checks only, from outside the repo, and their findings are recorded here as
   prose and numbers rather than as files.
4. **No secrets.** No tokens, no passwords, no `.BEE` passwords, no server config.
