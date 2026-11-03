---
tags: [decision, probe, frame, visuals]
created: 2026-11-03
updated: 2026-11-03
status: active
supersedes:
---
# 0018: The probe shows unreachable ground in red

**Context.** In a large frame on uneven ground, parts can't reach the target: the game moves no
vertex more than 8 m from the generated ground (`TerrainComp`), so a deep hollow can't be filled
and a high hump can't be dug down. The readout only gave a count ("1992 points beyond the limit");
the owner asked for a reddish warning on the ground while holding the probe (2026-11-03).

**Decision.** (2026-11-03)
- While the probe is held, a translucent red layer covers every unreachable vertex's square metre,
  one mesh that follows the terrain (each corner at the mean of the four vertices around it). It is
  rebuilt whenever the probe measures, twice a second at most.
- The readout splits the count: square metres that can't be raised that far, and ones that can't be
  dug that deep.
- Unlit, transparent built-in shader (`Sprites/Default`, falling back to `UI/Default` and
  `Hidden/Internal-Colored`), so no asset bundle is needed.

**Alternatives.** Red ring segments at each spot: thousands of objects on a big frame. Colouring the
terrain itself (paint): changes the world for everyone and stays after the probe is put away.

**Consequences.** A 129 m frame can have thousands of red squares; one mesh with 32-bit indices
holds them. With the hoe or the pickaxe the overlay is hidden, as asked. Untested in the game when written.

Related: [[0002-neutral-point-is-cut-and-fill-balance]], [[terrain-and-multiplayer]]
