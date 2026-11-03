---
tags: [decision, probe, hoe, items]
created: 2026-11-03
updated: 2026-11-03
status: active
supersedes:
---
# 0013: The probe and the level hoe share the hands through a patch

**Context.** The probe is an off-hand tool held while the level hoe is out. Valheim doesn't allow
that: `Humanoid.EquipItem` empties both hands for a tool (`ItemType.Tool`, the hoe), and for a shield
it empties the right hand unless that holds a one-handed weapon or a torch. A torch goes to the
left hand only next to a one-handed weapon (decompiled 1.0.17, 2026-11-03).

**Decision.** (2026-11-03)
- The probe is a clone of the game's **torch** (the game's rod held in either hand, with an attach
  point that suits a stick) without its fire, made an `ItemType.Shield` so it always goes to the left
  hand, with `m_animationState = LeftTorch` so it is held like a torch in the left hand
  (`Player.SetupAnimationState` uses the left item's own state for anything but a torch). No block
  power, no durability.
- The level hoe is a clone of the game's **hoe** with its own piece table and one piece.
- `TwoToolsPatch` (prefix and postfix on `EquipItem`) takes the other of our two tools out of its
  hand before the game's code runs and puts it back afterwards, then runs `SetupEquipment` again.
  Only for these two items; every other item equips as the game decides.

**Alternatives.** Making the probe a torch (`ItemType.Torch`): next to a tool the game puts it in the
right hand, so it needs a bigger patch. Rewriting `EquipItem` for our items: copies the game's checks
and breaks with updates.

**Consequences.** Without the hoe, the right mouse button with the probe blocks like a shield (with no
block power), since the game sees a shield. Untested in the game when written.

Related: [[0012-controls]], [[0011-model-designs]], [[terrain-and-multiplayer]]
