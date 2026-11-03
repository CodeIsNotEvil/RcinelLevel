# Changelog

What changed in each RcinelLevel release, newest first. The Thunderstore package's changelog tab is
this file, so write for the players: what they notice, not how it was done.

Format: a `## <version> - <yyyy-mm-dd>` heading per release (the version must match `<Version>` in
`Directory.Build.props` and `RcinelLevelPlugin.Version`), then `### Added`, `### Changed` or
`### Fixed` with one bullet per change.

## 0.1.0 - 2026-11-03

### Added
- The **ground probe**, an off-hand tool (Wood 2, Flint 2, Resin 1 at the Workbench). It frames an
  area, a circle, a square or a rectangle up to 129 m across, and finds the height where levelling it
  digs away as much as it fills. It shows the frame with the workbench's ring, the target height in
  blue, and in red every spot the game's ±8 m terrain limit keeps from reaching the target.
- The **level hoe**, an upgrade of the hoe (Hoe, Flint 4, Leather scraps 2 at the Workbench), held
  together with the probe. Each stroke raises the ground under a 5 × 5 m brush, inside the frame, up
  to the target, with straight walls at the frame's edge and the grass cleared. It costs stone for
  the ground it raises, a quarter of what Raise ground costs, and the first 0.25 m per spot is free.
  It can be upgraded three times for more durability.
- A **depth lock** for the pickaxe: inside a pinned frame it digs no deeper than the target and never
  past the frame's edge.
- A **moat** around the frame, as wide as the Rustic Drawbridge (4 m) or the Drawbridge (8 m) can
  cross and at least 3 m deep, dug with the locked pickaxe.
- The target height can be set by hand, taken from any ground or building, or reset to the probe's.
- The frame is kept with the character for each world.
- Controls in the game's key hint bar; every key can be changed in the config file.
- Server settings for the largest frame, the stone price, the free height and the moat's depth.
- Dev commands `rcinellevel_nostone` and `rcinellevel_dig` (with `devcommands`, for the session).
