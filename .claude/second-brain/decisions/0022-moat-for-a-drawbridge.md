---
tags: [decision, frame, moat, pickaxe]
created: 2026-11-03
updated: 2026-11-03
status: active
supersedes:
---
# 0022: A moat around the frame, as wide as a drawbridge can cross

**Context.** The owner wants a moat mode for bases reached only over a drawbridge, sized for one of the
game's two drawbridges, at least 3 m deep so enemies can't run over it, along straight cardinal edges;
for a circle at least the bridge's width, a little more where the edge zigzags (2026-11-03).

**The bridges** (read from the game's prefabs with UnityPy, 2026-11-03): `piece_drawbridge` ("Drawbridge",
the owner's "Timberwood") lowers a deck reaching about 9.6 m from its gate, 4.6 m wide;
`piece_drawbridge_log` ("Rustic Drawbridge") about 5.9 m, 2.3 m wide. Measured from the deck collider
(`door/collider`) along the door's X.

**Decision.**
- `K` with the probe cycles the frame's moat: none, Rustic, Drawbridge. Kept with the frame
  ([[0020-hints-in-the-key-bar-and-frame-kept-per-world]], save format version 2).
- The moat is a ring outside the frame. Its width counts the moat's vertices between the frame's last
  vertex and the far bank's first; the gap at the top is a metre more (the frame's edge cell slopes
  into it), and the deck must rest half a metre on the far bank: width = reach − 1.5 m, rounded down,
  so 4 m (Rustic) and 8 m (Drawbridge). Worked out from the prefabs at start-up, the log says what.
- Square and rectangle: the frame's bounds grown by the width, straight cardinal edges with square
  corners. Circle: a ring measured straight out from the circle, so the width is the bridge's at the
  four cardinal points and more along the axes on the zigzag parts.
- The floor lies `Moat depth` below the target (server setting, at least 3 m, default 3).
- Digging: the locked pickaxe digs the moat down to its floor and the frame to the target, nothing
  outside ([[0016-pickaxe-depth-lock]]); the dev dig hoe digs it too ([[0021-dev-commands]]). The hoe
  never fills the moat.
- Shown: the moat's outer edge in blue on the ground, the volume still to dig and its floor in the
  readout, moat ground the -8 m limit keeps above the floor in red ([[0018-red-overlay-for-unreachable-ground]]).

**Alternatives.** A free width setting: the owner wanted it tied to the two bridges. Measuring the gap
diagonally on circles: a bridge is placed along a cardinal direction, where the ring's width is exact.

**Consequences.** The widths are only as good as reading the deck from its collider, and whether half a
metre is enough for the deck to rest; to check in the game with both bridges. Untested when written.

Related: [[0016-pickaxe-depth-lock]], [[0014-hoe-levels-a-brush-inside-the-frame]]
