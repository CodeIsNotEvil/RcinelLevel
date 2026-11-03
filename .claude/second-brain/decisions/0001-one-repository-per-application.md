---
tags: [decision, conventions]
created: 2026-11-03
updated: 2026-11-03
status: active
supersedes:
---
# 0001: One repository per application

**Context.** The owner's first apps (GroceryTracker, LaunchHeim) live in CodeIsNotEvil/MyMonoRepo,
next to configs and experiments, with per-app tag prefixes, per-app changelogs and one shared second
brain. RcinelTranslate was the first app in a repository of its own. On 2026-11-03 the owner decided
that every new application gets its own repository.

**Decision.** RcinelLevel lives in CodeIsNotEvil/RcinelLevel, with its own CLAUDE.md, second brain,
CI and releases. What it needs from the other repositories (conventions, Valheim knowledge from
LaunchHeim) is copied in, naming the source, not linked to at build time.

**Alternatives.** *Another folder in MyMonoRepo*: the owner's reasons against it are that a single
project can't be hidden on GitHub (made private) without hiding everything, and that an unreleased
app can't be shown on its own.

**Consequences.**
- Tags are plain `vX.Y.Z`; no app prefix is needed.
- Knowledge copied from MyMonoRepo can go stale here when it is corrected there, so copied notes say
  where they came from ([[README]], conventions).
- MyMonoRepo's app site (`site/`) doesn't know about this repository; whether RcinelLevel gets a
  page there is open ([[rcinellevel]]).

Related: [[rcinellevel]]
