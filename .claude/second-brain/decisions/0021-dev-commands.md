---
tags: [decision, hoe, cheats]
created: 2026-11-03
updated: 2026-11-03
status: active
supersedes:
---
# 0021: Two dev commands, for the session only

**Context.** The owner wanted a dev command that lets the level hoe skip its stone and one that lets it
remove terrain too, active only in the session the command was used in (2026-11-03).

**Decision.**
- `rcinellevel_nostone [on|off]`: the hoe takes no stone. `rcinellevel_dig [on|off]`: the hoe also
  lowers ground above the target, down to the game's -8 m limit, and paints it like the rest (only
  raising is paid for). Without an argument each toggles.
- Cheat commands (Jotunn `IsCheat`), so they need `devcommands` like the game's own: everyone in a
  single-player world, only admins on a server.
- In memory only, never saved; off again when the player joins another world session (a new `ZNet`),
  but not on death, which only makes a new `Player`.
- The readout says "dev: no stone / dig" while one is on.

**Alternatives.** Config settings: would outlive the session and be synced from the server.

**Consequences.** The game's own no-cost mode keeps skipping the stone as before. Untested in the
game when written.

Related: [[0015-hoe-only-fills-and-costs-stone]], [[0016-pickaxe-depth-lock]]
