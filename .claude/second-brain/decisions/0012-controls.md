---
tags: [decision, controls, probe, hoe]
created: 2026-11-03
updated: 2026-11-03
status: active
supersedes:
---
# 0012: Controls: the probe picks the shape, modifier + scroll resizes and sets the height

**Context.** [[0010-area-shapes-and-scroll]] left open how the shape is picked, how resizing keeps
clear of the scroll wheel's own uses (rotating build pieces, zooming), and how the height is set by
hand. The owner chose from options.

**Decision.** (owner, 2026-11-03)
- **Shape**: a hotkey while the probe is held cycles circle → square → rectangle. The probe owns the
  frame; the hoe levels whatever frame the probe last showed (so the hoe's menu needs one "Level"
  entry, not one per shape).
- **Resize**: modifier + scroll; plain scroll keeps the game's behaviour.
- **Rectangle**: the edge on the side the player faces (snapped to N/E/S/W) moves out on scroll up
  and in on scroll down; the opposite edge stays. Confirms the reading in 0010.
- **Height by hand**, all three:
  - a second modifier + scroll moves the target height in steps (default 0.25 m), shown on the frame;
  - a key takes the height of what the player aims at, ground or building piece (eyedropper);
  - a key resets the target to the probe's cut-and-fill result.

All keys and the step are BepInEx config entries. Provisional defaults, to check against the game's
own bindings in the first test:

| Action | Default |
|---|---|
| Cycle shape (probe held) | `T` |
| Resize | `Left Alt` + scroll |
| Height step | `Left Shift` + scroll |
| Take height from aim | `G` |
| Reset height to probe result | `H` |

`Left Ctrl` is left out on purpose: it is crouch in Valheim, so holding it would make the player
sneak while resizing.

**Alternatives.** Shape from the hoe's build menu (one entry per shape) or a global cycle key; plain
scroll for resizing (rotation is useless for level ops) or keys only; typing a height in a command;
the rectangle growing evenly on both sides.

**Consequences.** The frame is the probe's state, so it must stay put while the player switches
between the probe and the hoe, and be visible when only the hoe is held. Which keys conflict with
the game (and with common mods) is checked in the first test; the defaults change then, the actions
don't. Key hints belong on the frame or in the hover text, the way the game shows its own.

Related: [[0010-area-shapes-and-scroll]], [[0006-probe-area-set-by-player]], [[rcinellevel]]
