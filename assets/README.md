# Assets

RcinelLevel's logo, in the look of the owner's other logos (the C logo, LaunchHeim's LH): flat orange
`#DE5833` on a transparent background, Kode Mono Bold. The item icons look like Valheim's own instead:
they are renders of the 3D models (decision 0017).

**Nothing here is edited by hand.** The logo's PNGs are rendered from its SVG, each size straight from
the vector; the item icons and the models are made by `models/build.py`.

| Files | What for |
|---|---|
| `logo/rcinellevel.svg` | "RL" in Kode Mono Bold as outlines, made by `scripts/text_logo.py`. |
| `logo/rcinellevel-{1024,512,256,128,64,32}.png` | Where SVG isn't taken. `package/icon.png` is the 256 one (Thunderstore wants 256×256). |
| `items/{probe,level-hoe}-{128,64}.png` | The tools' inventory icons, rendered from the models by `models/build.py --icons`. The plugin loads the 128 ones. |
| `models/` | The models, their texture and previews, made by `models/build.py` (see `models/README.md`). |
| `fonts/KodeMono.woff2`, `fonts/OFL.txt` | Kode Mono (SIL Open Font License 1.1), the variable font the logo is cut from. Copied from MyMonoRepo. |

Regenerate after a change (from the repository root; needs `rsvg-convert` from librsvg and fontTools
for the logo, Blender for the icons):

```sh
scripts/text_logo.py RL assets/logo/rcinellevel.svg
for n in 1024 512 256 128 64 32; do
  rsvg-convert -w $n -h $n assets/logo/rcinellevel.svg -o assets/logo/rcinellevel-$n.png
done
cp assets/logo/rcinellevel-256.png package/icon.png
blender -b --factory-startup --python models/build.py -- --icons   # the item icons
```
