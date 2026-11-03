---
tags: [decision, pickaxe, frame]
created: 2026-11-03
updated: 2026-11-03
status: active
supersedes:
---
# 0016: A depth lock keeps the pickaxe inside the frame and above the target

**Context.** With the hoe only raising ([[0015-hoe-only-fills-and-costs-stone]]), digging is done
with the game's pickaxe. The owner wants to lock its depth to the probed height while digging inside
the probed area, and to keep it there.

**Decision.** (owner, 2026-11-03; details by Claude)
- `L` while holding the probe toggles the lock; it pins the frame if it wasn't, and unpinning ends it.
- While locked, a pickaxe hit that reaches into the frame is taken over (`DepthLockPatch` on
  `TerrainOp.Awake`, local player's pickaxe only): it digs each vertex as the game would (same radius,
  depth and falloff), but never below the target and never outside the frame, so the frame's edge
  stays a straight wall. The hit's sound, effects and drops are kept. Hits that miss the frame are the
  game's.
- The pickaxe is two-handed, so the probe can't be held with it; the frame and a short readout show
  while a pickaxe is out and the lock is on.

**Alternatives.** Changing the pickaxe's terrain op: its settings travel as a prefab hash, so a
per-hit change can't reach the terrain's owner. Patching the owner's `RaiseTerrain`: every client
would need to know the frame.

**Consequences.** The dig goes out as single-vertex level ops, one per vertex the hit lowers (about a
dozen). Smoothing, which the game's dig may also do, isn't copied inside the frame. Untested in the
game when written.

Related: [[terrain-and-multiplayer]], [[0015-hoe-only-fills-and-costs-stone]], [[0012-controls]]
