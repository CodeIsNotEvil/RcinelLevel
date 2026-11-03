# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

RcinelLevel: a Valheim mod (BepInEx plugin) that makes levelling terrain easier. Two tools:

- **A probe**, held in the off hand, that reads the ground in an area, finds its neutral height and
  shows it in the world in a way that looks like Valheim's own UI.
- **An improved hoe** that levels to a preset height like the game's hoe does, but only makes
  straight walls at 90 degrees.

The full feature description, the open design questions and the plan are in the second brain's
[[rcinellevel]] note (`.claude/second-brain/projects/rcinellevel.md`). Read it before working on a
feature.

The owner (CodeIsNotEvil) means to publish it on Thunderstore once they have checked it in the game
themselves. Treat the config, the item names and the Thunderstore page as something strangers will
depend on. The rules below come from the owner's other repositories, CodeIsNotEvil/MyMonoRepo
(LaunchHeim) and CodeIsNotEvil/RcinelTranslate.

There is no plugin code yet. Update this file's layout and commands once the project exists.

- `assets/`: the logo and item icons. The SVGs are the source; `assets/README.md` says how the PNGs
  are rendered. `scripts/text_logo.py` makes the logo.
- `package/`: the files of the Thunderstore package that aren't built (`icon.png` so far).
- `models/build.py`: builds the 3D models in Blender (5.2, headless) and exports them to
  `assets/models/` (OBJ meshes, one PNG atlas, previews). The script is the source; `models/README.md`
  has the command and the conventions the plugin's loader relies on. After changing a model, render
  the previews and look at them before committing.

## One repository per application

The owner keeps each application in its own repository (since 2026-11-03), so a project can be made
private or shown on its own before it is released. Nothing in here may depend on files in another
repository at build time. Knowledge from the other repositories is copied into the second brain, with
a note where it came from.

## Git workflow for Claude

The owner reviews every change. Claude never commits to `main` directly:

1. Start each request on a new branch cut from an up-to-date `main`, with a short kebab-case name
   (`probe-height-markers`) and no `claude/` prefix, unless the session prescribes a branch.
2. Make small, focused commits.
3. Push the branch and open a pull request against `main` that says what changed and why.
4. Don't merge it. Push review follow-ups to the same branch.

**Everything in this repository appears as the owner's work.** Commit with the owner's identity
(`git config user.name CodeIsNotEvil`, `user.email codeisnotevil@gmail.com`) and without
`Co-Authored-By` lines, session links or other AI trailers. Leave "Generated with" footers out of
commit messages, PR descriptions and docs. Claude must not show up as a contributor.

Don't create or push release tags, and never publish to Thunderstore, unless the owner asks. The
owner tries every release in the game before it goes out.

## Style

Formatting comes from `.editorconfig`: two spaces, LF, UTF-8 without BOM. C# uses file-scoped
namespaces and primary constructors.

The plugin runs on Unity's Mono inside Valheim, so it targets `net472`; `LangVersion latest` is fine
because the newer C# features compile down, but no API newer than .NET Framework 4.7.2 is.

Namespaces are `Rcinel.Level.*` (decision 0008), the assembly `RcinelLevel`, the BepInEx GUID
`io.github.codeisnotevil.rcinellevel`. "Rcinel" alone is the prefix of all the owner's apps, so
nothing here may claim the bare name (no class, command, GUID or file called just `Rcinel`).

Build file-system paths with the platform's path API (`Path.Combine`), never by joining strings with
`/` or `\`. The owner plays on Linux, but most players are on Windows.

Comments explain *why* a decision was made, and there are many of them. Keep that density.

## Valheim specifics

- Valheim's assemblies (`assembly_valheim.dll` and friends) belong to Iron Gate and must never be
  committed or shipped, publicized or not. `.gitignore` keeps them out.
- Iron Gate asks mods that change gameplay to set `Game.isModded`. Doing so switches off Steam
  achievements for the session; say so on the Thunderstore page.
- Everything that changes terrain must work in multiplayer. Terrain edits are applied by whoever
  owns that patch of terrain, which looks the op up by prefab hash, so the server and every client
  need the mod and Jotunn enforces it (decision 0005, [[terrain-and-multiplayer]]).
- How to read the game's own code, where the game and the test instance live on the owner's machine,
  and the snags already found are in the second brain ([[valheim-modding]]).

## Second brain

`.claude/second-brain/` holds the why: decisions (ADR style), gotchas and a dated journal, in the
same layout as the ones in MyMonoRepo and RcinelTranslate. Start at its `index.md` before non-trivial
work. When a session settles a design question or finds a gotcha, update the relevant note and append
to `journal/YYYY-MM-DD.md`. Its `README.md` has the conventions.
