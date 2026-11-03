---
tags: [resource, valheim, terrain, multiplayer]
created: 2026-11-03
updated: 2026-11-03
status: active
---
# How Valheim's terrain edits work, and who must have the mod

Read from the decompiled game (`assembly_valheim.dll`, Unity 6000.0.75f1, 2026-11-03) with
`ilspycmd`, as [[valheim-modding]] describes. Nothing here has been tried in a multiplayer game yet.

## Terrain edits
- The hoe and the pickaxe place a `TerrainOp` object. In `Awake` it finds the heightmaps under it,
  calls `TerrainComp.ApplyOperation` on each, and destroys itself.
- `ApplyOperation` **never edits locally**. It packs the position, an optional rotation and the
  op's settings, and sends `RPC_ApplyOperation` to the **owner** of that heightmap's `TerrainComp`
  (the `_TerrainCompiler` network object). Only the owner changes and saves the terrain.
- **The settings travel as the prefab's name hash only** (`TerrainOp.Settings.Serialize`). The owner
  looks them up in its own `ObjectDB` (`TryGetTerrainOp`); if it doesn't know the prefab it logs
  "Failed to deserialize TerrainOp settings for prefab hash …" and drops the edit.
  - So the radius, the square shape and the other settings come from the receiver's copy of the
    prefab and can't be chosen per use. Only the **position** (and so the target height: level ops
    level to `position.y + m_levelOffset`) is free.
  - A player-chosen area ([[0006-probe-area-set-by-player]]) therefore means tiling it with
    fixed-size square level ops at the same height, or registering ops in a few sizes.
- `LevelTerrain` with `m_square` sets every vertex in a square of `ceil(radius / m_scale)` vertices
  around the centre to the target height, with no falloff. `Heightmap.m_scale` is 1, so **vertices
  are 1 m apart**: the steepest wall the terrain can show is the whole height step across one 1 m
  cell (3 m up over 1 m across is about 72°). A truly vertical wall isn't possible in Valheim's
  terrain; the hoe can make the edge as sharp as the grid allows.
- **Square ops are aligned to the world grid.** The rotation in the package is only used by paint
  ops; levelling ignores it. Straight walls run north–south or east–west.
- Every vertex's change is clamped to **±8 m** from the generated ground (`m_levelDelta`), which can
  make the cut-and-fill height unreachable in parts of an area
  ([[0002-neutral-point-is-cut-and-fill-balance]]).

## Who owns what
- Every 2 s `ZDOMan.ReleaseNearbyZDOS` gives each persistent network object near a peer to that peer,
  unless its owner is still near it. The owner of a patch of terrain is therefore usually a player
  standing near it, and not necessarily the one with the hoe.
- The dedicated server takes part as a peer with its own reference position, which no player ever
  sets on a server, so it stays at `(0, 0, 0)`. Near the world's centre (the starting area) the
  server itself can own the terrain and apply the edits.

## What a player without the mod sees
- **Terrain edits fail** wherever that player owns the terrain (see above): the edit is dropped and
  only their log says why.
- **Items in chests are lost**: `Inventory.Load` skips items whose prefab is unknown ("Failed to find
  item prefab"), so the chest looks as if they were never there, and once that player changes the
  chest, it is saved without them.
- Items on the ground or in another player's hands don't appear for them ("Missing prefab hash").

## Conclusion
Every player needs the mod, and so does the server: the server owns the terrain near the world's
centre, and only a server with the mod can enforce that every client has it
([[0005-everyone-needs-the-mod]]).

Related: [[rcinellevel]], [[valheim-modding]]
