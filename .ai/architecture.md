# Architecture (CoreLibs)

Not a deployed service. DI extension methods per package (`AddHybridCaching`, `AddDfEMassTransit`, `AddCustomOpenIdConnect`, `AddFileStorage`, …). Caching/AsyncProcessing extensions live in namespace `Microsoft.Extensions.DependencyInjection`.

**Contracts** is reference-only types: `ExternalApplications.*` (FlexForms) and `Academies.*`.

**Messaging.Contracts** owns `TopicNames` and events (`ScanRequestedEvent`, Prism events). Changing topic strings is a production incident.

Target framework: **net10.0** (all packages and test projects). Consumers must be on .NET 10 to take package versions published after the upgrade.

## Package map (**Verified** 2026-11-17, all `net10.0`)

| Package (`src/GovUK.Dfe.CoreLibs.*`) | Public entry points | FlexForms consumers |
|---------|---------------------|---------------------|
| Contracts | `ExternalApplications.Enums` (`ApplicationStatus`, `AccessType`, `ResourceType`, `FileValidationStatus`, `FileValidationMode`, `NotificationType`, `NotificationPriority`, `DomainErrorCode`, reporting export enums), `ExternalApplications.Models.Request/Response` (REST DTOs incl. `PrismDtos`), `Academies.*` | API (Domain, Api.Client), Web (via Api.Client), Prism |
| Messaging.Contracts | `Entities.Topics.TopicNames`, `Messages.Events.*`, `Messages.Enums.*`, `Messages.Identifiers.ApplicationProjectionIdentifiers` + `DeterministicGuid`, `Exceptions.MessageNotForThisInstanceException` | API, Web, Prism, scanner |
| Messaging.MassTransit | `AddDfEMassTransit` → `IEventPublisher` | API, Web, scanner |
| Security | `AddApplicationAuthorization` (`Authorization:Policies` config), `AddCustomJwtAuthentication`, `AddCustomOpenIdConnect`, `AddEntraSso`, `AddEntraSsoTokenValidation`, `AddExternalIdentityValidation`, `AddUserTokenService[Factory]`, `AddApiOboTokenService`, `AddTokenRefresh[WithOidc]`, `AddCustomClaimProvider`, `AddCustomRequestCheckerProvider`, Cypress auth/antiforgery helpers | API, Web, ClamAV API |
| Caching | `AddServiceCaching` (memory), `AddRedisCaching`, `AddHybridCaching` (two independent caches); `ICacheService<T>`, `DistributedCacheAdapter`, `SessionDistributedCacheAdapter` | API, Web, scanner |
| Http | `AddCorrelationId` / `UseCorrelationId` (`x-correlationId`), `AddCustomExceptionHandler(s)` / `UseGlobalExceptionHandler`, `AddNoScriptDetection` | API, Web |
| AsyncProcessing | `AddBackgroundService`, `AddBackgroundServiceWithParallelism` (in-process channel) | API, ClamAV API |
| Email | `AddEmailServices`, `AddEmailServicesWithGovUkNotify`, `AddEmailServicesWithCustomProvider` | API |
| FileStorage | `AddFileStorage` (Azure File Share / local) | API |
| Notifications | `AddNotificationServicesWithRedis/Session/InMemory/CustomProviders` | API |
| Utilities | `AddRateLimiting<TKey>` (per-process store; FlexForms API swaps in a Redis limiter) | API |
| Testing | AutoFixture customisations, `CustomWebApplicationFactory`, WireMock helpers (`AddGetWithJsonResponse` …), `ExpectedSecurity` validators, `CleanArchitectureGuard`, Razor Page fixtures | API tests, Web tests |
| ApplicationSettings, SharePoint | `AddApplicationSettings*`, `AddSharePointServices` | none in FlexForms |
| TestPackage | pipeline canary | none |

Every package has its own `build-deploy-*.yml` workflow calling `build-test-template.yml` + `pack-template.yml`.

Platform context, including which version each consumer pins: [`ecosystem.md`](ecosystem.md).
