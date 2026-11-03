# Assets

RcinelLevel's logo and item icons, in the look of the owner's other logos (the C logo, LaunchHeim's
LH): flat orange `#DE5833` on a transparent background, Kode Mono Bold for letters, chamfered corners
for shapes.

**The SVGs are the source.** Every PNG is rendered from them, each size straight from the vector
rather than scaled down from a bigger PNG, so small sizes stay sharp. Never edit a PNG by hand.

| Files | What for |
|---|---|
| `logo/rcinellevel.svg` | "RL" in Kode Mono Bold as outlines, made by `scripts/text_logo.py`. |
| `logo/rcinellevel-{1024,512,256,128,64,32}.png` | Where SVG isn't taken. `package/icon.png` is the 256 one (Thunderstore wants 256×256). |
| `items/probe.svg`, `items/level-hoe.svg` | The inventory icons of the two tools, drawn by hand. |
| `items/*-{128,64}.png` | What the plugin loads as the items' sprites. |
| `fonts/KodeMono.woff2`, `fonts/OFL.txt` | Kode Mono (SIL Open Font License 1.1), the variable font the logo is cut from. Copied from MyMonoRepo. |

The level hoe is drawn upright and rotated as a whole group, so handle and blade stay square to each
other when the angle changes.

Regenerate after changing an SVG (from the repository root; needs `rsvg-convert` from librsvg, and
fontTools for the logo):

```sh
scripts/text_logo.py RL assets/logo/rcinellevel.svg
for n in 1024 512 256 128 64 32; do
  rsvg-convert -w $n -h $n assets/logo/rcinellevel.svg -o assets/logo/rcinellevel-$n.png
done
cp assets/logo/rcinellevel-256.png package/icon.png
for f in probe level-hoe; do
  for n in 128 64; do rsvg-convert -w $n -h $n assets/items/$f.svg -o assets/items/$f-$n.png; done
done
```
