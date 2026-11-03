---
tags: [decision, controls, persistence]
created: 2026-11-03
updated: 2026-11-03
status: active
supersedes:
---
# 0020: Controls in the game's key hint bar; the frame is kept per world with the character

**Context.** The controls were a line in the readout above the player, while the game lists controls
in its key hint bar at the bottom, and with the level hoe that bar showed the build hammer's hints
(snapping, copy, build menu), which don't apply. The owner also wants the probed frame back after
leaving and joining the same world (2026-11-03).

**Decision.**
- `ToolHints` (postfix on `KeyHints.UpdateHints`) shows the tools' controls in the key hint bar
  instead of the build or combat hints while the probe or the level hoe is held, built from the game's
  own hint templates (`BuildHints/Keyboard/Place`, `Copy`); the readout keeps the measurements only.
  Jotunn's key hint manager wasn't usable: it only takes a left-hand item that is a weapon.
- The frame (shape, size, centre, pinned, target, height by hand, depth lock) is kept in the
  character's custom data (`Player.m_customData`, saved with the character), under
  `RcinelLevel.Frame.<world uid>`. Clients get the world's uid from the server (`ZNet.World`), so it
  works on servers too.

**Alternatives.** A file under BepInEx per world: lost when the character is played on another PC,
and world names aren't unique. The world's own save: belongs to the server, and every player has
their own frame.

**Consequences.** The frame goes wherever the character goes, one per world. A character played
without the mod keeps the entry untouched (the game saves custom data it doesn't know). Untested in
the game when written.

Related: [[0012-controls]], [[0014-hoe-levels-a-brush-inside-the-frame]]
