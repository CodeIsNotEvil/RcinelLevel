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

## Open questions for the owner
- How the player sets the area: mark two corners with the probe, type width and depth, or scroll to
  resize a frame?
- How the hoe gets its height: always the probe's last result, or also set by hand (and how)?
- What the models look like (a dowsing rod or a stake for the probe?), and whether the level hoe
  should look like the game's hoe with a change or like something new.
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
