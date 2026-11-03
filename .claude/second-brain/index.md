---
tags: [moc]
created: 2026-11-03
updated: 2026-11-03
status: active
---
# Index

## Projects
- [[rcinellevel]]: the probe and the levelling hoe; what the owner asked for, open questions, plan

## Resources
- [[valheim-modding]]: the game and the test instance on the owner's machine, reading the game's
  code, building a BepInEx plugin, gotchas (copied from LaunchHeim)
- [[thunderstore-publishing]]: the package, the owner's release habits, the first-upload checklist
- [[terrain-and-multiplayer]]: how terrain edits travel, why walls can't be vertical, who needs the mod

## Decisions
- [[0001-one-repository-per-application]]: every new app gets its own repository; knowledge is
  copied in, naming its source
- [[0002-neutral-point-is-cut-and-fill-balance]]: the probe finds the height where cut equals fill
- [[0003-separate-tools]]: the hoe and the probe are new items; the game's hoe is unchanged
- [[0004-jotunn]]: items, pieces and config sync through Jotunn, compiled against the game
- [[0005-everyone-needs-the-mod]]: server and every client need it; anyone who can craft may use it
- [[0006-probe-area-set-by-player]]: the player sets the area; the hoe levels it in tiles
- [[0007-recipes]]: probe and level hoe at the Workbench, the hoe as an upgrade of the game's hoe
- [[0008-rcinel-level-namespace]]: namespaces `Rcinel.Level.*`
- [[0009-blender-models-loaded-at-runtime]]: Blender scripts make the models, the plugin loads them
- [[0010-area-shapes-and-scroll]]: circle, square or rectangle, scroll to resize; height also by hand
- [[0011-model-designs]]: a soil-probe rod and a screed-board hoe with square end walls
- [[0012-controls]]: the probe picks the shape; modifier + scroll resizes and sets the height
- [[0013-off-hand-through-a-patch]]: the probe (a torch clone as a left-hand shield) and the level hoe share the hands
- [[0014-hoe-levels-a-brush-inside-the-frame]]: the hoe levels a brush where you aim, inside the frame, to the target fixed at pinning
- [[0015-hoe-only-fills-and-costs-stone]]: the level hoe only raises, paid in stone at Raise ground's rate
- [[0016-pickaxe-depth-lock]]: a lock keeps the pickaxe inside the frame and above the target
- [[0017-icons-rendered-from-the-models]]: item icons are Valheim-style renders of the models
- [[0018-red-overlay-for-unreachable-ground]]: the probe shows ground beyond the ±8 m limit in red
- [[0019-level-hoe-upgrades]]: the level hoe upgrades three times, for durability
