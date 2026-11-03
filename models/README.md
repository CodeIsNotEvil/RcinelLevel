# Models

`build.py` builds RcinelLevel's two models in Blender, exports them to `assets/models/` and renders
previews. It is the source: to change a model, change the script and run it again. Never edit the
exported files by hand.

```sh
# from the repository root; Blender 5.2 (sudo pacman -S blender)
blender -b --factory-startup --python models/build.py                # export only, a second or two
blender -b --factory-startup --python models/build.py -- --previews  # also render previews (Cycles, CPU)
blender -b --factory-startup --python models/build.py -- --icons     # also render the item icons
```

| Output | What it is |
|---|---|
| `assets/models/probe.obj` | The probe: a soil probe, 1.6 m, T-handle, resin depth bands every 30 cm, flint point. |
| `assets/models/level-hoe.obj` | The level hoe: a square-mouth shovel, its flint blade straight on the haft with raised side walls and a wooden straightedge across its back, about 1.5 m. |
| `assets/models/atlas.png` | The one 256 px texture both are mapped onto: wood, flint, leather, resin. |
| `assets/models/previews/*.png` | Renders to look at: each tool next to a 1.8 m figure (a Valheim player's height), plus close-ups. |
| `assets/items/*-{128,64}.png` | The item icons (`--icons`): the model on the diagonal, head top right, lit warm, with a soft dark rim, like Valheim's own icons. |

Conventions the plugin's loader relies on (also in the script's docstring):

- Metres. The OBJ is Y-up with -Z forward; Unity is left-handed, so the loader mirrors X.
- The origin is where the hand holds the tool; its length runs along +Y in the OBJ.
- One mesh, one material, triangles with normals and UVs into the atlas. The plugin puts the atlas
  on a material copied from a game item, so light and shading match Valheim.
- The texture is drawn with numpy from fixed seeds, so every run gives the same picture.

The previews use nearest-neighbour texture filtering to show the texels as coarse as they are; the
game will filter them.
