---
tags: [decision, hoe, probe, frame]
created: 2026-11-03
updated: 2026-11-03
status: active
supersedes:
---
# 0014: The hoe levels a brush where the player aims, inside the frame, to a target fixed at pinning

**Context.** In the first test the hoe levelled the whole frame at once. The owner wants it to level
only the part they look at, and found the largest frame (16, 33 m across) far too small.

**Decision.** (owner, 2026-11-03; details by Claude)
- The level hoe levels a square **brush** around where its build ghost sits, cut off at the frame's
  edge, to the frame's target height. Walls only form where the frame ends, so painting stroke by
  stroke still gives the straight edges of [[0006-probe-area-set-by-player]]. Brush size is a setting
  (`Level hoe brush`, default 2: 5 × 5 m); the frame shows the next stroke in orange.
- **Pinning fixes the target.** Every stroke changes the ground and with it the frame's mean, so a
  live balance would drift while the player works. The probe's balance is taken when the frame is
  pinned (`B`, or automatically on the first stroke with an unpinned frame); `U` takes it again.
- **Larger frames**: default limit 64 (129 m across), up to 128 (257 m), admin-only. The probe reads
  each loaded heightmap's arrays instead of looking up every vertex, and says how much of the frame
  is loaded when part of it is too far away.

**Alternatives.** Levelling the whole frame per click (the first version): too much at once and no
control. A live target: drifts. A round brush: its edge would leave a staircase inside the frame.

**Consequences.** Supersedes the "the hoe levels the frame" part of 0006; the frame is now the plan,
the brush the work. The brush can't be resized in the game yet, only in the config file.

Related: [[0006-probe-area-set-by-player]], [[0012-controls]], [[terrain-and-multiplayer]]

**Changed 2026-11-03** by [[0015-hoe-only-fills-and-costs-stone]]: the brush only raises, and costs stone.
