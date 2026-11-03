---
tags: [decision, release, thunderstore]
created: 2026-11-03
updated: 2026-11-03
status: active
supersedes:
---
# 0023: Releases are packaged locally and uploaded by the owner

**Context.** The owner wants 0.1.0 on Thunderstore (2026-11-03). The plugin compiles against Valheim's
assemblies, which can't be in the repository, so CI can't build it; the owner signed in to Thunderstore
as CodeIsNotEvil with GitHub.

**Decision.** `scripts/package.py` builds and packages on the owner's machine, checking that the
version, the plugin's constant and the changelog agree. The owner uploads the zip on thunderstore.io;
no Thunderstore token is stored anywhere. MIT license, like the owner's other apps. The tag `vX.Y.Z` and
a GitHub release with the same zip follow when the owner says.

**Alternatives.** A workflow uploading with a service account token: needs the build in CI first, which
needs the game's assemblies.

**Consequences.** Releasing is a few manual steps (README "Building", [[thunderstore-publishing]]).
If CI can ever build (a reference-assembly package for Valheim), the upload can move into a workflow.

Related: [[thunderstore-publishing]], [[0001-one-repository-per-application]]
