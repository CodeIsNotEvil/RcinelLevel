---
tags: [project, valheim, bepinex]
created: 2026-11-03
updated: 2026-11-03
status: active
---
# RcinelLevel

A Valheim mod (BepInEx plugin) that makes levelling terrain easier, to be published on Thunderstore
after the owner has tried it in the game. GitHub: `CodeIsNotEvil/RcinelLevel` (public, created
2026-11-03, description "Tools to make levling terrain in valheim easier." — typo "levling" to fix
on GitHub). No code yet.

## What the owner asked for (2026-11-03, in their words, lightly cleaned up)

> RcinelLevel: a probe tool that lets the player probe the ground for the neutral point in a specific
> area and can visualise it in a Valheim way. An improved hoe that can be set to a preset height and
> can level like the default hoe, only 90 degree walls and only straight walls. The probe tool should
> be an off-hand tool.

## How it reads so far (to confirm with the owner)

**The probe** (off hand)
- Off hand in Valheim means the left hand, where shields and torches go. Hammer, hoe and cultivator
  sit in the right hand and can be held together with a torch, so a probe made as a left-hand item
  (like the torch) could be held while the hoe is out. To check in the game's `Humanoid` equip code
  which item types can share the hands with a tool.
- "Neutral point" is the **cut-and-fill balance** ([[0002-neutral-point-is-cut-and-fill-balance]]).
  The candidates that were weighed:
  1. the ground's **original height**, where the terrain is unmodified (delta 0). Levelling there
     adds no terrain change, and it is the middle of the ±8 m the game lets you dig or raise
     (to check in `TerrainComp`);
  2. the **cut-and-fill balance**: the height where digging and raising cancel out over the area;
  3. the **average** (or median) of the current ground in the area.
- "In a Valheim way": the game's own look, such as the dotted ring of the hoe and of a workbench's
  range (`CircleProjector`), the hoe's placement ghost, rune-style markers or the hover text. No HUD
  overlay that looks like it came from another game.

**The improved hoe**
- Same as the game's "Level ground", but to a preset height instead of the height the player stands
  at, with walls at the edge that are vertical (90°) and straight, instead of the game's sloped and
  round edge. Presumably a square brush that snaps to a grid and to the cardinal directions.
- The preset presumably comes from the probe (probe, then level to its result), and can be changed
  by hand (a hotkey or the scroll wheel, like the hammer's rotation).
- A separate item; the game's hoe stays as it is ([[0003-separate-tools]]).

## Settled so far
- Recipes: both at the Workbench, the hoe as an upgrade of the game's hoe ([[0007-recipes]]).
- The player sets the area; the hoe levels it in world-aligned tiles ([[0006-probe-area-set-by-player]]).
- Server and every client need the mod; anyone who can craft the tools may use them
  ([[0005-everyone-needs-the-mod]], why in [[terrain-and-multiplayer]]).
- Namespaces `Rcinel.Level.*` ([[0008-rcinel-level-namespace]]).
- Thunderstore: released as the owner's account CodeIsNotEvil ([[thunderstore-publishing]]).
- Icons: the RL logo, a probe and a level hoe, as SVGs in `assets/` (2026-11-03).
- Models: Blender scripts, loaded at runtime ([[0009-blender-models-loaded-at-runtime]]).

## What the game allows (from its code, see [[terrain-and-multiplayer]])
- Walls can't be truly vertical: terrain vertices are 1 m apart, so the sharpest edge is the whole
  step across one 1 m cell. "90° walls" means as steep as the grid allows.
- Straight walls run north–south or east–west only: level ops ignore rotation.

- Frame: circle, square or rectangle, resized with the scroll wheel; the rectangle only toward
  where the player looks. Height from the probe or by hand ([[0010-area-shapes-and-scroll]]).
- Looks: the probe is a long rod you push into the ground to feel for hard layers (a soil probe);
  the level hoe's design was left to Claude ([[0011-model-designs]]).

- Controls: the probe cycles the shape, modifier + scroll resizes, height by modifier + scroll,
  eyedropper or reset to the probe result; keys are config entries ([[0012-controls]]).

- First version of the plugin built on 2026-11-03 (`src/RcinelLevel/`), copied into the LaunchHeim
  `test` instance; the two tools share the hands through a patch ([[0013-off-hand-through-a-patch]]).

- First test (2026-11-03): crafting, both tools in the hands, the frame and the readout worked; shape
  cycling didn't (keyboard layout), the frame was too small, levelling the whole frame was too much,
  and overlapping tiles left jagged edges. Fixed the same day ([[0014-hoe-levels-a-brush-inside-the-frame]]).

- Third round (2026-11-03): shovel-like hoe head, icons rendered from the models, the hoe only
  raises and costs stone, the pickaxe can be locked to the frame ([[0015-hoe-only-fills-and-costs-stone]],
  [[0016-pickaxe-depth-lock]], [[0017-icons-rendered-from-the-models]]).

## Open
- Check the stone rate taken from the game's Raise ground against the game.
- Resizing the hoe's brush in the game, not only in the config file.
- How the tools sit in the hand: the owner sends a screenshot after the first test; the placement is
  in the config file and applies while the game runs.
- Everything in the first version is untried in the game: crafting, holding both tools, the frame's
  look, the readout, levelling, multiplayer.
- The probe measures up to (2·64+1)² vertices twice a second at the largest server limit; fine at the
  default 16, may need a per-zone heightmap cache for big frames.
- A `CHANGELOG.md` and the Thunderstore package build come with the first release.
- Does it go on the owner's app site too?

## Plan (proposal, nothing decided)
1. Read the game's terrain code first (`ilspycmd`, see [[valheim-modding]]): `TerrainOp` and its
   `Settings`, `TerrainComp`, `Heightmap`, and how the hoe's pieces (`mud_road_v2`, `raise_v2`, …)
   level. That settles what "original height" and "90° walls" cost.
2. Build against the publicized game assemblies with Jotunn ([[0004-jotunn]]): `ItemManager` and
   `PieceManager` for the tools and their pieces, config sync for servers. Cost: CI can't build
   without the game's files (see [[valheim-modding]]).
3. A probe that reads heights and shows the result; then the hoe that levels to it.
4. Try it in the owner's LaunchHeim `test` instance, alone and on a dedicated server with a second
   client, then package for Thunderstore ([[thunderstore-publishing]]).

Related: [[index]], [[valheim-modding]], [[thunderstore-publishing]],
[[0001-one-repository-per-application]], [[0002-neutral-point-is-cut-and-fill-balance]],
[[0003-separate-tools]], [[0004-jotunn]]
