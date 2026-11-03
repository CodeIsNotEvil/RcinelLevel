---
tags: [decision, models, tooling]
created: 2026-11-03
updated: 2026-11-03
status: active
supersedes:
---
# 0009: Models are made with Blender scripts and loaded by the plugin at runtime

**Context.** The owner wants a way for Claude to make the tools' 3D models itself, and asked which
tooling that needs. Options offered: the game's own parts at runtime, Blender plus a runtime loader,
Blender plus Unity asset bundles, meshes written in code.

**Decision.** (owner, 2026-11-03) Blender, driven headless by Python scripts in this repository:
the scripts build the models, render preview pictures (which Claude can look at to check its work)
and export a mesh and texture. The plugin loads them at runtime into Unity meshes, with a material
copied from a game item so lighting and shading match Valheim. No Unity Editor.

**Alternatives.**
- *The game's own parts* (clone the hoe, assemble the probe from game meshes): no tools, looks like
  Valheim at once, but the probe can only be what the game's pieces make.
- *Unity asset bundles*: the usual way mods ship models, but needs the Unity 6000.0 Editor (Unity Hub
  from the AUR, an account login, about 10 GB) on top of Blender.
- *Meshes written in C#*: no tools, but very little detail and nothing to preview.

**Consequences.**
- Blender has to be installed on the owner's machine (`sudo pacman -S blender`), and in CI if models
  are ever built there; the exported files are committed, so a plugin build doesn't need Blender.
- The plugin needs a small loader for the export format, and must handle a missing or broken file
  without breaking the game. Chosen with the first models (2026-11-03): **OBJ** (triangles, normals,
  UVs, one object, no `.mtl`) plus one shared **PNG** atlas. OBJ is plain text a hundred-line loader
  reads; glTF would need a JSON parser and buffer accessors for no gain at 200 triangles. The axes
  and the origin convention are in `models/README.md`.
- The scripts are the source: models are changed by editing the script and exporting again, never
  by hand in the exported file. That matches the icons (`assets/README.md`), where the SVG is the
  source.

Related: [[rcinellevel]], [[0003-separate-tools]]
