---
tags: [decision, hoe, items, recipes]
created: 2026-11-03
updated: 2026-11-03
status: active
supersedes:
---
# 0019: The level hoe can be upgraded three times, for durability

**Context.** The owner asked for the shovel to be upgradeable three times to increase its durability.

**Decision.** (owner, 2026-11-03; amounts by Claude) `m_maxQuality = 4` with 50 durability per level,
as the game's tools have. Each upgrade costs Flint 2 and Leather scraps 1 per level reached (the hoe
itself only for crafting), at the Workbench; the game asks for workbench level 2, 3 and 4 for the three
upgrades, from the recipe's station level.

**Alternatives.** Upgrades that also grow the brush: the brush is a setting, and the owner only asked
for durability.

**Consequences.** A level hoe crafted earlier upgrades too: items take their shared data from the
prefab. The amounts are a first guess, to tune with the owner.

Related: [[0007-recipes]], [[0015-hoe-only-fills-and-costs-stone]]
