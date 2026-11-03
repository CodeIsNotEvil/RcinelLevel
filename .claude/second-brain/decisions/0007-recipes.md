---
tags: [decision, items, recipes]
created: 2026-11-03
updated: 2026-11-03
status: active
supersedes:
---
# 0007: Recipes: both tools at the Workbench in the Meadows

**Context.** The tools are new items ([[0003-separate-tools]]) usable by anyone who can craft them
([[0005-everyone-needs-the-mod]]), so the recipe is the only gate. The owner chose from three
options each (Meadows, Black Forest, bronze age).

**Decision.** (owner, 2026-11-03)
- **Probe**: Wood 2, Flint 2, Resin 1 at the Workbench.
- **Level hoe**: an upgrade of the game's hoe: Hoe 1, Flint 4, Leather scraps 2 at the Workbench.
  The hoe is used up.

**Alternatives.** Probe with Copper and Greydwarf eyes (Black Forest) or Fine wood and Bronze at the
Forge; level hoe as a standalone Workbench item (Wood 5, Stone 4, Flint 4) or at the Forge (Fine
wood 4, Bronze 4, like the cultivator).

**Consequences.** Both are available from the first days of a world, which suits a building aid.
The prefab names (`Wood`, `Flint`, `Resin`, `Hoe`, `LeatherScraps`, `piece_workbench`) are to check
against the game's `ObjectDB` when the recipes are written. Amounts and the station are worth making
settings, synced from the server.

Related: [[rcinellevel]], [[0003-separate-tools]]
