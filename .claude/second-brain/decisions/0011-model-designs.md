---
tags: [decision, models, design]
created: 2026-11-03
updated: 2026-11-03
status: active
supersedes:
---
# 0011: A soil-probe rod and a square-scoop hoe

**Context.** The owner described the probe as "a long stick you would stick in the ground to check if
it is harder a few metres down" and left the hoe to Claude. Both are made by `models/build.py`
([[0009-blender-models-loaded-at-runtime]]) from the materials their recipes use ([[0007-recipes]]):
wood, flint, resin, and leather for the hoe.

**Decision.** (2026-11-03)
- **Probe**: a soil probe, as surveyors and gardeners use one. A 1.6 m fire-hardened wooden rod with
  a T-handle lashed on with leather and sealed with resin, resin depth bands every 30 cm, and a
  knapped flint point bound with leather. Held just under the handle.
- **Level hoe** (Claude's design): a square scoop on a 1.25 m handle with a leather grip. A flint
  floor with a dead-straight front edge, two flint side walls and a wooden straightedge as the back
  wall, all at right angles, so the tool looks like the 90° cuts it makes and shows where the
  hoe's flint and leather went.
- Low-poly (about 200 triangles each) with one coarse 256 px texture, like the game's own tools.

**Alternatives.** For the hoe, Claude first tried a screed board: a wide straightedge beam on the
handle with the blade under it and flint end plates hanging down. In the renders it read as a bench,
not a tool, so the head became the open scoop.

**Consequences.** How the tools sit in the hand (the attach rotation and offset) can only be tuned in
the game. The owner hasn't seen the models in the game yet; the previews in
`assets/models/previews/` are what they were judged by.

Related: [[0009-blender-models-loaded-at-runtime]], [[0007-recipes]], [[rcinellevel]]
