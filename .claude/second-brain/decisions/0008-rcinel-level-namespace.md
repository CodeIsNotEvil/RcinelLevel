---
tags: [decision, dotnet, conventions]
created: 2026-11-03
updated: 2026-11-03
status: active
supersedes:
---
# 0008: Namespaces `Rcinel.Level.*`

**Context.** MyMonoRepo puts all .NET code under `CINE.*` (its decision 0004); RcinelTranslate moved
to `Rcinel.Translate.*`. "Rcinel" alone is the prefix of all the owner's apps and may not be claimed
by one of them.

**Decision.** (owner, 2026-11-03) `Rcinel.Level` as the root namespace (`Rcinel.Level.Probe`, …),
set once in `Directory.Build.props`. The assembly is `RcinelLevel`, the BepInEx GUID
`io.github.codeisnotevil.rcinellevel`. (The owner first wrote `Rcinel.Levl`; asked, it was a typo.)

**Alternatives.** `CINE.Level.*` like MyMonoRepo; `Rcinel.Levl.*`.

**Consequences.** Like in RcinelTranslate, no type may be called `Rcinel` or `Level` on its own at
the top: a class `Rcinel` would make `Rcinel.Level...` bind to the class inside our own code
(RcinelTranslate's gotcha CS0118).

Related: [[rcinellevel]]
