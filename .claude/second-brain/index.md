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
