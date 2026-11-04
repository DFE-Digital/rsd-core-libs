# DfE.CoreLibs — Agent entry point

Shared **.NET 8** NuGet libraries for DfE Digital (heavily used by FlexForms). Each package is **independently GitVersioned** and published to nuget.org.

There is **no root README**. Per-package `readme.md` files (some empty) live under `src/GovUK.Dfe.CoreLibs.*/`.

## Constraints

1. **Do not mix ProjectReference and PackageReference** to the same CoreLibs assembly in a consumer. Local builds are `1.0.0.0`; published packages bind to real assembly versions → `FileLoadException`.
2. Packages version **independently**. Bumping Contracts without publishing dependents (Notifications, Messaging) will break consumers.
3. `ExternalApplications` namespaces are the **FlexForms contract**. Renaming them is a breaking change across API, Web, Prism, and this repo.
4. Session cache vs general cache: `DistributedCacheAdapter` swallows read errors (null = miss). `SessionDistributedCacheAdapter` **rethrows** so ASP.NET does not write an empty session. Do not “unify” them.
5. `AddDfEMassTransit`: if you pass `configureAzureServiceBus`, automatic `ConfigureEndpoints` is skipped — you must wire endpoints.
6. FileStorage production csproj historically includes test packages (NSubstitute/xunit) — don’t copy that pattern.
7. ApplicationSettings and SharePoint are **not** used by current FlexForms csprojs.
8. Trusted publishing: nuget.org policy matches reusable workflow `pack-template.yml` (`job_workflow_ref`). Callers need `id-token: write`. `NUGET_USER` is nuget.org **username**, not email.

## Knowledge map

| File | Use |
|------|-----|
| [`.ai/README.md`](.ai/README.md) | Package index |
| [`.ai/architecture.md`](.ai/architecture.md) | How packages relate |
| [`.ai/dependencies.md`](.ai/dependencies.md) | FlexForms consumption matrix |
| [`.ai/development-workflows.md`](.ai/development-workflows.md) | Pack, test, GitVersion |
| [`.ai/known-pitfalls.md`](.ai/known-pitfalls.md) | Footguns |
| [`.ai/history-lessons.md`](.ai/history-lessons.md) | Lessons from past incidents |
| [`.ai/features/`](.ai/features/) | Security, caching, messaging, testing |

Http details: `src/GovUK.Dfe.CoreLibs.Http/ExceptionHandler.md`. Security: package readme. Messaging packages have **no** readme — use `.ai/features/messaging.md`.

## Knowledge maintenance

When changing a public DTO/enum/event used by FlexForms, update this `.ai/` **and** call out required consumer bumps. Never document a FlexForms-only workaround as a CoreLibs invariant without checking other consumers. Trust code over docs, and fix the docs when they disagree.

### Definition of done (mandatory)

A task is **not complete** until you have checked whether it changed anything the knowledge base describes. Before finishing:

1. Check the change against the triggers below.
2. Update every affected doc in the same change.
3. In your final summary or PR description, list the docs you updated, **or** state “No knowledge impact” with a one-line reason.

| If you changed… | Update |
|-----------------|--------|
| A public type, DI extension or config shape in any package | That package’s `readme.md`, and the matching `.ai/features/` file if one exists |
| `Contracts` (`ExternalApplications`) or `Messaging.Contracts` | `.ai/features/messaging.md` or `.ai/dependencies.md`, plus a note of which consumer repos must bump |
| Caching/session error behaviour | `.ai/features/caching.md`, `.ai/known-pitfalls.md` |
| Security/auth behaviour | `.ai/features/security.md`, Security `readme.md` |
| Packing, versioning, CI | `.ai/development-workflows.md` |
| A fix for an incident or footgun | `.ai/known-pitfalls.md` |
| Something that makes an existing statement wrong | Correct or delete it — never leave it stale |

This repository is **public**. Never write secrets or descriptions of unfixed security weaknesses into docs.
