---
tags: [decision, dependencies, jotunn]
created: 2026-11-03
updated: 2026-11-03
status: active
supersedes:
---
# 0004: Built on Jotunn

**Context.** The mod adds two items, their recipes and terrain pieces, and needs settings that a
server can enforce. LaunchHeim's plugin gets by with BepInEx and reflection, but it calls only a
few methods ([[valheim-modding]]).

**Decision.** (owner, 2026-11-03) Use Jotunn (`ValheimModding-Jotunn` on Thunderstore, the
`JotunnLib` NuGet package) for items, pieces, recipes, localization and config sync, and compile
against the game's publicized assemblies from the local install.

**Alternatives.** *BepInEx and Harmony only*: no extra dependency for players, but our own code for
registering prefabs at the right moment, recipes, and server-enforced settings, all of which Jotunn
already does and keeps working across game updates.

**Consequences.**
- The Thunderstore package depends on Jotunn as well as BepInExPack; the test instance needs Jotunn
  installed (it isn't, see [[valheim-modding]]).
- Building needs the game's assemblies, so CI can't build without them (open, see
  [[thunderstore-publishing]]).
- Which Jotunn version to depend on, and how it publicizes the game's assemblies, is checked when the
  project is created.

Related: [[rcinellevel]], [[0003-separate-tools]]
