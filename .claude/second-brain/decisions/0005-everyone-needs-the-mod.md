---
tags: [decision, multiplayer]
created: 2026-11-03
updated: 2026-11-03
status: active
supersedes:
---
# 0005: Server and every player need the mod; anyone who can craft the tools may use them

**Context.** The owner asked whether servers need the mod, and wants the tools usable by anyone who
can craft them. What the game does with unknown terrain ops and items is in
[[terrain-and-multiplayer]].

**Decision.** (2026-11-03)
- Anyone who can craft the tools may use them: no admin-only switch. The recipe is the only gate.
- The mod declares that **everyone must have it** (Jotunn's `NetworkCompatibility` with
  `CompatibilityLevel.EveryoneMustHaveMod` and a version check), so a client without it, or with
  another version, can't join a server that has it, and the other way round.
- The Thunderstore README says so plainly: install it on the server and on every client.

**Alternatives.**
- *Clients only*: edits fail without a message wherever a player without the mod owns the terrain,
  and near the world's centre wherever the server does; tools put in chests vanish when a player
  without the mod uses the chest.
- *Our own RPC that carries the settings*, so the owner needs no prefab: the owner still needs our
  code to receive it, so it doesn't remove the need for the mod.

**Consequences.** Admins who want a vanilla-compatible server can't use RcinelLevel. Settings that
matter for fairness (largest area, costs) can be synced from the server through Jotunn's config
sync ([[0004-jotunn]]). To try with a dedicated server and two clients before the first release.

Related: [[terrain-and-multiplayer]], [[rcinellevel]]
