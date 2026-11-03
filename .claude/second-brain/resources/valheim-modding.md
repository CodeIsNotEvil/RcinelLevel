---
tags: [resource, valheim, bepinex, gotcha]
created: 2026-11-03
updated: 2026-11-03
status: active
---
# Valheim modding: setup, sources and gotchas

What the owner's earlier Valheim work found out, copied here so this repository stands on its own.
Most of it comes from LaunchHeim in CodeIsNotEvil/MyMonoRepo (its `projects/launchheim.md` and
decisions 0016 and 0017, its BepInEx plugin `Applications/LaunchHeim/src/GamePlugin`). Each point
says when it was found.

## On the owner's machine (checked 2026-11-03)
- Valheim is in the second Steam library: `/mnt/games/SteamLibrary/steamapps/common/Valheim`
  (`~/.local/share/Steam` is the first). The game's code is in `valheim_Data/Managed/`
  (`assembly_valheim.dll`, `assembly_utils.dll`, `assembly_guiutils.dll`).
- Unity **6000.0.75f1** (`strings UnityPlayer.so | grep -m1 '^6000'`). Valheim 1.0.17 ran on the
  same version on 2026-10-14.
- .NET SDK 10.0.114 in `/usr/share/dotnet`. No Mono needed: the .NET Framework reference assemblies
  come from NuGet. On Arch/CachyOS a missing `aspnet-targeting-pack` fails ASP.NET builds with
  `NETSDK1226`; a plugin doesn't need it.
- LaunchHeim (the owner's launcher) has an instance `test` at
  `~/.local/share/LaunchHeim/instances/test`: BepInExPack_Valheim 5.4.2360, PlantEverything 1.21.4,
  MultiUserChest 0.6.2 and LaunchHeim's own plugin. That is where RcinelLevel gets tried.
  - Each instance has its own BepInEx, started through Doorstop, so the Steam install stays vanilla
    and nothing in the game folder needs repairing after a test.
  - Thunderstore mods go to `BepInEx/plugins/<Team>-<Name>/` and are listed in `instance.json`.
    LaunchHeim copies its own plugin into `BepInEx/plugins/LaunchHeim/` before every launch. A dev
    build of RcinelLevel should get a folder of its own that isn't the Thunderstore name, so a later
    install from Thunderstore doesn't mix with it.
  - Seen 2026-11-03: MultiUserChest's manifest depends on `ValheimModding-Jotunn-2.30.0`, but Jotunn
    was neither in `plugins/` nor in `instance.json`. Later that day the owner installed Jotunn 2.30.2
    through LaunchHeim and removed MultiUserChest.
  - The dev build goes to `BepInEx/plugins/RcinelLevel-dev/` (the csproj's `CopyToTestInstance`).
    LaunchHeim only deletes files it recorded for a mod, and its own `LaunchHeim/` folder.
- Started directly (as LaunchHeim does), Valheim shows a black window if Steam isn't logged in.
  LaunchHeim starts Steam and waits for the login first (2026-09-30).

## Reading the game's code
The game's behaviour is best read in the game itself (2026-10-04): install `ilspycmd` into a scratch
folder (`dotnet tool install ilspycmd --tool-path <scratch>/tools`) and decompile
`assembly_valheim.dll` and `assembly_utils.dll`. Useful classes found so far: `FejdStartup` (menus,
command line), `Player`, `Humanoid` (equipping), `Achievements`, `PlayerProfile`. For this mod start
with `TerrainOp`, `TerrainComp`, `Heightmap`, `TerrainModifier`, `ItemDrop`, `PieceTable` and
`CircleProjector`.

## Building a plugin (from LaunchHeim's GamePlugin, 2026-10-14)
- Target `net472` (Unity's Mono), `LangVersion latest`, `ImplicitUsings disable`.
- `BepInEx.Core` 5.4.21 and `UnityEngine.Modules` 6000.0.75 from **nuget.bepinex.dev**, as
  compile-time references only (`PrivateAssets="all" ExcludeAssets="runtime"`): the game and
  BepInExPack bring the real ones. `Microsoft.NETFramework.ReferenceAssemblies` 1.0.3 makes it build
  on Linux and in CI.
- `nuget.config` clears the sources and maps only `BepInEx.*` and `UnityEngine.Modules` to the BepInEx
  feed (package source mapping), so that feed can never stand in for a nuget.org package.
- LaunchHeim's plugin reaches Valheim's classes by **reflection** so CI can build it without the game.
  That suits a plugin that calls a handful of methods; RcinelLevel adds items and terrain changes and
  needs the game's types at compile time (see [[rcinellevel]], "Plan"). Then CI needs the game's
  assemblies, which can't be committed (Iron Gate's) — still to be solved: build in CI only what can
  be built without them, or keep release builds local.
- Fail soft after a game update: when a member isn't found (or a Harmony patch target is gone), write
  one line to the BepInEx log and leave the game's behaviour alone, never throw into the game loop.
- Plugin GUIDs are reverse-DNS: LaunchHeim uses `io.github.codeisnotevil.launchheim`, so
  `io.github.codeisnotevil.rcinellevel` fits.

## Gotchas
- **Unity's `== null`** is overloaded: a destroyed object (the player after going back to the menu)
  compares equal to null while the C# reference isn't null. Compare with `== null` on
  `UnityEngine.Object`, not `is null` or `ReferenceEquals` (2026-10-14).
- **`Game.isModded` switches off Steam achievements** (`Achievements.IsCheatedAtAll`); the game never
  sets it itself, so BepInEx alone doesn't. Iron Gate asks gameplay mods to set it. Players who want
  achievements anyway can use the dev command `yesiuseddevcommandsbutiwantmyachievementsanyway 1`,
  or LaunchHeim's "Keep Steam achievements with mods" setting, which sets the same key (2026-10-14).
- **GitHub's Windows runners check out with `core.autocrlf=true`**; `.gitattributes` forces LF so
  files and test fixtures stay the same on every runner (2026-10-04).
- **.NET's `Encoding.UTF8` writes a BOM.** Files other tools parse (Thunderstore's `manifest.json`)
  must be written without one (2026-09-26).
- **Unity 6's ImageConversionModule needs netstandard 2.1**, which a net472 plugin can't reference
  (CS1705). Load PNGs with Jotunn's `AssetUtils.LoadImage(texture, bytes)`, which calls
  `ImageConversion.LoadImage` by reflection (2026-11-03).
- **JotunnLib's NuGet build props write into the game folder**: with `ExecutePrebuild` they publicize
  the game's assemblies into `valheim_Data/Managed/publicized_assemblies` and reference them from
  there. RcinelLevel references JotunnLib with `ExcludeAssets="build;runtime"` and publicizes into
  `obj/` with BepInEx.AssemblyPublicizer.MSBuild instead (2026-11-03).
- **Valheim's default keys** (`ZInput`, assembly_utils): E use, R hide, Space jump, Left Ctrl crouch,
  Left Shift run and AltPlace, C walk, Q autorun, X sit, F guardian power, V auto-pickup, Tab
  inventory, M map, G radial menu, T emotes, F5 console, 1–8 hotbar. Read keys with
  `ZInput.GetKey(KeyCode, logWarning: false)`; Valheim runs on Unity's new Input System (2026-11-03).
- **Keys are physical positions.** `ZInput.GetKey(KeyCode)` reads `Keyboard.current[Key]`, and the
  Input System's `Key` is a position on a US keyboard: `KeyCode.Y` is the key labelled Z on QWERTZ.
  For letters, look the key up with `Keyboard.FindKeyOnCurrentKeyboardLayout("y")` (needs a reference
  to the game's `Unity.InputSystem.dll`) (2026-11-03).
- **BepInEx keeps saved config values over new defaults**: changing a default in code doesn't change
  an existing config file; say so, or change the file, when a default moves (2026-11-03).
- **The scroll wheel in build mode** turns the ghost only when the selected piece has `m_canRotate`;
  otherwise the camera zooms (`GameCamera`). RcinelLevel hides the wheel from the game while a frame
  modifier is held (`ScrollPatch`) (2026-11-03).
- **Reading prefabs without Unity**: UnityPy (in a scratch venv) reads the game's asset bundles in
  `valheim_Data/StreamingAssets/SoftRef/Bundles/`. The tools and pieces are in bundle `c4210710`;
  hierarchies and transforms read fine, MonoBehaviour fields don't (no type trees), so a component's
  settings are logged at run time instead (2026-11-03).
- **MSBuild item metadata doesn't expand inside a property function**: `%(Folder)` inside
  `$([System.IO.Path]::Combine(...))` in an item transform created a folder literally named
  `%(Folder)`. One `Copy` per destination folder instead (2026-11-03).
- **Check transitive licenses**, not only direct ones: LaunchHeim found an LGPL library pulled in by
  an MIT one (2026-09-30). Matters if RcinelLevel ever ships a library inside its package.

Related: [[rcinellevel]], [[thunderstore-publishing]], [[index]]
