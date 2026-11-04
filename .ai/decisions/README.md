# Decisions (CoreLibs)

No ADR folder. Evident decisions:

- Independent package versioning (GitVersion per tag prefix).
- FlexForms contracts kept under `ExternalApplications` after product rename (breaking-change avoidance).
- Session adapter split after data-loss (fail-loud vs cache-miss).

Rationale for independent versioning vs a single CoreLibs version is **not recorded** — do not invent it.
