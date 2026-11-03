---
tags: [decision, hoe, balance]
created: 2026-11-03
updated: 2026-11-03
status: active
supersedes:
---
# 0015: The level hoe only raises, and pays for it in stone

**Context.** After the second test the owner found removing ground with the level hoe "kinda cheaty",
and wants it to cost stone like the game's own hoe.

**Decision.** (owner, 2026-11-03; details by Claude)
- A stroke only **raises** the vertices of the brush that lie below the target, inside the frame;
  anything above stays. Digging down to the target is the pickaxe's job ([[0016-pickaxe-depth-lock]]).
- It costs **stone for the ground it raises**, at the rate the game's own Raise ground costs: its
  stone divided by the volume one use raises, measured from the game's piece at start-up (the log
  says what it found). A server setting `Stone cost` multiplies it (0 makes it free). Without enough
  stone the stroke doesn't happen and says how much it needs; the readout shows the next stroke's
  fill and price. The game's no-cost mode skips the price.
- Nothing above the game's ±8 m limit is raised or paid for.

**Alternatives.** A fixed price per stroke as a piece requirement: cheap for deep holes, dear for
shallow ones, and a 5 × 5 m brush can raise far more than Raise ground. Raising for free: the cheat
the owner wanted gone.

**Consequences.** The rate is only as good as the reading of Raise ground (radius, height and falloff
summed on the 1 m grid, the way `RaiseTerrain` applies them); to check against the game in a test. The
build menu shows no requirement for the piece, since the price depends on the ground.

Related: [[0014-hoe-levels-a-brush-inside-the-frame]], [[0016-pickaxe-depth-lock]]

**Cheaper 2026-11-03**, after the owner's test: the price grew too fast and evening out almost flat
ground should be free. Now each spot's first metre of raising is free (`Free fill depth`, server
setting) and only the height above it is paid, at a quarter of Raise ground's rate (`Stone cost`
default 0.25). A per-spot allowance rather than "free if the stroke is shallow", so the price grows
smoothly instead of jumping at 1 m.

**Free depth 0.25 m (2026-11-03)**: a metre was too generous (owner); `Free fill depth` defaults to 0.25.
