---
tags: [decision, probe, terrain]
created: 2026-11-03
updated: 2026-11-03
status: active
supersedes:
---
# 0002: The probe's neutral point is the cut-and-fill balance

**Context.** The owner asked for a probe that finds "the neutral point" of an area. Three readings
were possible: the ground's original (unmodified) height, the height where digging and raising
cancel out, or the average of the current ground ([[rcinellevel]]).

**Decision.** (owner, 2026-11-03) The **cut-and-fill balance**: the height at which levelling the
area digs away as much ground as it raises.

**Alternatives.**
- *Original ground height*: the least terrain change against the world as generated, but over an
  area that was already edited, or on a slope, it can mean digging a lot and raising little.
- *Average current height*: easy to compute, but only equals the balance for a flat-bottomed square
  area sampled evenly; a mean of samples weighted by area is what the balance needs anyway.

**Consequences.**
- The probe needs a defined area (its shape and size must match what the hoe then levels, or the
  balance is off) and samples heights across it evenly by area. Over a square on a regular grid,
  the balance height is the mean of the samples: everything above it is cut, everything below is
  fill, and they're equal by construction. With 90° walls at the edge (the hoe, [[0003-separate-tools]])
  the area really ends at its border, so nothing is lost to a slope outside it.
- The game's ±8 m dig/raise limit (to check in `TerrainComp`) can make the balance unreachable in
  parts of the area: the probe should say where.
- Whether the game counts material (raising with the hoe costs stone) isn't part of the balance;
  the probe balances ground, not inventory. To check whether digging gives anything back.

Related: [[rcinellevel]], [[0003-separate-tools]]
