---
tags: [decision, icons, design]
created: 2026-11-03
updated: 2026-11-03
status: active
supersedes:
---
# 0017: Item icons are renders of the models

**Context.** The first item icons were flat orange glyphs in the style of the owner's logos. In the
game the owner found them "too techy for the Valheim theme" (2026-11-03).

**Decision.** `models/build.py --icons` renders each model like Valheim's own icons: on the diagonal
from bottom left to top right with the head up, turned so its working face shows, lit warm from the
top left with a cool rim, on a transparent background with a soft dark-brown rim added in numpy.
512 px rendered, 128 and 64 px written. The logo (RL in Kode Mono) stays the owner's style.

**Alternatives.** Hand-drawn SVGs in a painted style: a second drawing of every item to keep in step.

**Consequences.** Icon and model can't drift apart: change the model, run the script, and both
follow. The hand-drawn item SVGs were removed.

Related: [[0011-model-designs]], [[0009-blender-models-loaded-at-runtime]]
