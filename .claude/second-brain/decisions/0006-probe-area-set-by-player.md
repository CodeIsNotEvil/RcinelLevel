---
tags: [decision, probe, hoe]
created: 2026-11-03
updated: 2026-11-03
status: active
supersedes:
---
# 0006: The player sets the area; the hoe levels it in tiles

**Context.** The owner: the probe's area is set by the player, because the hoe's brush is far too
small. A level op's size can't be chosen per use, only its position ([[terrain-and-multiplayer]]).

**Decision.** (2026-11-03) The player chooses the area (how exactly, such as marking two corners
with the probe or setting width and depth, is still open). The probe computes the cut-and-fill
height over that area, and the hoe levels the same area by placing square, world-aligned level ops
side by side at that height.

**Alternatives.** *One big op per size*: a prefab per size the player might want, and the area could
still only be a square.

**Consequences.** Areas are rectangles on the world's 1 m grid, aligned north–south and east–west
(level ops can't rotate). The number of ops per use grows with the area, so the largest area is a
setting the server can enforce.

Related: [[0002-neutral-point-is-cut-and-fill-balance]], [[0005-everyone-needs-the-mod]]
