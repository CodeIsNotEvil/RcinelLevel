# RcinelLevel

Level ground for building the way you plan it: probe an area, see what it takes, and level it with
straight walls, a stroke at a time, without the hoe doing the pickaxe's work for free.

## The tools

**Ground probe** (off hand) · Workbench · Wood 2, Flint 2, Resin 1

A long rod you push into the ground. Hold it in your off hand and it frames an area in front of you:
a circle, a square or a rectangle, from a few metres up to 129 m across. The probe finds the height
where levelling the frame digs away exactly as much as it fills, and shows

- the frame on the ground, drawn like a workbench's range,
- the target height as a blue line,
- in red, every spot the game's terrain limit (±8 m from the generated ground) keeps from reaching it,
- how much there is to dig and to fill.

**Level hoe** (main hand, together with the probe) · Workbench · Hoe, Flint 4, Leather scraps 2

A square-mouth shovel made from your old hoe. Each stroke raises the ground under a 5 × 5 m brush, as
far as the frame reaches, up to the target height. The frame's edge becomes a wall as steep as
Valheim's terrain allows, and the levelled ground is cleared like the hoe's Level ground. It only
fills: digging is the pickaxe's job. Raising ground costs stone, a quarter of what Raise ground
costs for the same amount, and the first 0.25 m on every spot is free, so evening out almost flat
ground costs nothing. Upgradeable three times for more durability.

**Pickaxe depth lock**

Lock the pinned frame and dig with any pickaxe: inside the frame it stops at the target height, and
it never digs past the frame's edge, so the edge stays a straight wall.

**Moat**

Give the frame a moat for a base that's only reached over a drawbridge: 4 m wide for the Rustic
Drawbridge or 8 m for the Drawbridge, and 3 m below the target, so enemies can't walk out of it. The
locked pickaxe digs it.

## Controls

The controls show in the key hint bar at the bottom while you hold the tools.

| Key | With | Does |
|---|---|---|
| Mouse-1 | level hoe | Fill under the brush |
| Y | probe | Next shape: circle, square, rectangle |
| B | probe | Pin the frame where you aim, or let it follow your aim |
| L | probe | Lock or unlock the pickaxe to the frame |
| K | probe | Moat: none, Rustic Drawbridge, Drawbridge |
| Left Alt + wheel | probe or hoe | Resize the frame; a rectangle grows on the side you face |
| H + wheel | probe or hoe | Raise or lower the target height |
| J | probe or hoe | Take the height of the ground or building you aim at |
| U | probe or hoe | Back to the probe's height |

Letters follow your keyboard layout. Every key can be changed in
`BepInEx/config/io.github.codeisnotevil.rcinellevel.cfg`. The frame is kept with your character for
each world.

## Multiplayer

**Install it on the server and on every client.** In Valheim, terrain is changed by whoever owns that
part of the world, and without the mod they can't apply the tools' edits. The mod checks that
everyone has the same version.

Anyone who can craft the tools may use them. The server's settings for the largest frame, the stone
price, the free height and the moat's depth are synced to every player.

## Good to know

- Valheim's terrain has a point every metre, so walls are as steep as one metre allows, not vertical,
  and straight edges run north–south or east–west.
- Like every gameplay mod, RcinelLevel marks the game as modded, which turns off Steam achievements.
  The game's own dev command `yesiuseddevcommandsbutiwantmyachievementsanyway 1` (after `devcommands`)
  allows them again for that character.
- Dev commands (after `devcommands`, only for the current session): `rcinellevel_nostone` lets the
  hoe fill without stone, `rcinellevel_dig` lets it dig too.

## Requirements

[BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) and
[Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/).

Source, issues and changes: [github.com/CodeIsNotEvil/RcinelLevel](https://github.com/CodeIsNotEvil/RcinelLevel). MIT license.
