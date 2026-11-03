---
tags: [resource, thunderstore, release]
created: 2026-11-03
updated: 2026-11-03
status: active
---
# Publishing on Thunderstore

The owner publishes RcinelLevel on Thunderstore (Valheim community) only after trying the build in
the game themselves. Nothing has been uploaded yet; the points marked *to check* come from general
knowledge, not from a real upload, and should be checked against Thunderstore's own docs (the
package format page and the upload page's validator) when the first package is built.

## The package
- A zip with `manifest.json`, `icon.png` (256×256), `README.md` (the package page) and, optionally,
  `CHANGELOG.md` at its root, plus the plugin DLL. Mod managers (r2modman, LaunchHeim) unpack it into
  `BepInEx/plugins/<Team>-<Name>/`.
- `manifest.json`: `name` (letters, digits and `_` only, so `RcinelLevel`), `version_number`
  (`X.Y.Z`), `website_url` (the GitHub repository), `description` (short, max 250 characters, *to
  check*) and `dependencies` as `Team-Name-X.Y.Z` strings: `denikson-BepInExPack_Valheim-…` and
  `ValheimModding-Jotunn-…` ([[0004-jotunn]]). Written without a BOM ([[valheim-modding]], gotchas).
- A version can't be uploaded twice or replaced (*to check*), so a mistake costs a new version.
- The package lives under a Thunderstore **team**, whose name is the `<Team>` part of the package id.
  The owner signed in to Thunderstore with GitHub as CodeIsNotEvil and releases under that name
  (2026-11-03), so the package is `CodeIsNotEvil-RcinelLevel`. A team of that name has to exist
  (Thunderstore's team settings) before the first upload; *to check* whether it already does.

## How the owner's other projects release (MyMonoRepo decision 0008, RcinelTranslate)
- One version, in one place (for .NET: `<Version>` in `Directory.Build.props`), and the manifest
  takes it from there instead of keeping its own copy.
- `CHANGELOG.md` in a narrow format: `## X.Y.Z - YYYY-MM-DD`, then `### Added` / `### Changed` /
  `### Fixed` and bullets. CI rejects a tag without a dated section; "Unreleased" fails the check.
  The same file becomes the package's changelog tab here.
- A release is a `vX.Y.Z` tag matching the version; a workflow builds and publishes it. Only the owner
  creates tags. RcinelTranslate moved every registry it could to **trusted publishing** (OIDC) so no
  long-lived token sits in the repository secrets. Thunderstore's upload works with a team service
  account token (the Thunderstore CLI, `tcli`, *to check*); keep it a GitHub secret.
- Build-in-CI is harder here than in the other repositories: the plugin compiles against Valheim's
  assemblies, which can't be put in the repository ([[valheim-modding]]). Until that is solved,
  release builds come from the owner's machine and the workflow only packages and uploads.

## Before the first upload (checklist)
- The owner tried the exact zip in the LaunchHeim `test` instance, alone and with a second player on
  a server.
- The Thunderstore README says the mod sets `Game.isModded` (achievements) and whether the server
  and every player need it.
- Icon: `package/icon.png`, 256×256, rendered from `assets/logo/rcinellevel.svg` (done 2026-11-03).
- MIT license file in the repository, matching the owner's other apps (to confirm).

Related: [[rcinellevel]], [[valheim-modding]], [[index]]
