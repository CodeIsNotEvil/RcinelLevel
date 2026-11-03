---
tags: [decision, probe, hoe, controls]
created: 2026-11-03
updated: 2026-11-03
status: active
supersedes:
---
# 0010: Circle, square or rectangle, resized with the scroll wheel; height also by hand

**Context.** [[0006-probe-area-set-by-player]] left open how the player sets the area and whether
the height can only come from the probe.

**Decision.** (owner, 2026-11-03)
- The player picks the frame's shape: **circle**, **square** or **rectangle**.
- **The scroll wheel resizes the frame.** Circle and square grow and shrink evenly. The rectangle
  resizes only in the direction the player looks, snapped to the nearest cardinal direction (north,
  east, south, west).
  - Read as (2026-11-03, to confirm when it can be tried): the edge on the side the player faces
    moves; the opposite edge stays. Looking north and scrolling up pushes the north edge out.
    The other reading, both edges of that axis moving together, is a small change if the first one
    feels wrong.
- **The target height can be set by hand** as well as taken from the probe's cut-and-fill result.
  The controls for that are still to design.

**Alternatives.** Marking two corners with the probe; typing width and depth.

**Consequences.**
- Everything stays on the world's 1 m grid and its axes ([[terrain-and-multiplayer]]): a circle's
  wall becomes a staircase of 1 m steps, and a rectangle's sides run north–south and east–west,
  which is also why its edges move along the cardinal directions.
- The scroll wheel already rotates pieces when building and zooms the camera; which modifier key,
  if any, keeps them apart is to settle with the controls.
- The probe computes the balance over exactly the frame the hoe will level, so both tools share one
  frame (shape, size, position).

Related: [[0006-probe-area-set-by-player]], [[0002-neutral-point-is-cut-and-fill-balance]]

**Confirmed 2026-11-03**: the facing edge moves, the opposite edge stays. The controls are in
[[0012-controls]].
