# FlexForms ecosystem map

**Shared file.** The same `ecosystem.md` is kept in the `.ai/` folder of all six repositories (flexforms-api, flexforms-web, flexforms-prism, rsd-file-scanner-function, rsd-clamav-api, rsd-core-libs). If you change it, change it in every repo, or note the drift in the repo you could not update.

Read it first for any task that touches more than one repository, any message or contract, or when you are not sure which repository owns a behaviour. Then read the owning repo's `AGENTS.md` and `.ai/code-map.md`.

**Verified** against source on 2026-11-17 (`main` heads after the contributor-invite permissions merge). Version numbers drift. Check the csproj / `Directory.Packages.props` before relying on them.

## 1. What the platform is

FlexForms is a multi-tenant, template-driven GOV.UK application platform for DfE. A tenant (Transfers, Visits, LSRP, …) is a service with its own hostnames, identity provider, templates, users, email templates and settings, served by one shared deployment. Applicants sign in, start an application from a JSON form template, work through a GOV.UK task list, upload files, invite contributors and submit. Admins manage templates, users, roles and tenant settings. Analysts report from Prism.

The historical product name is **External Applications** ("EA"). It survives in namespaces (`GovUK.Dfe.CoreLibs.Contracts.ExternalApplications`), the EA database / `ExternalApplicationsContext`, the `ExternalApplicationsApiClient` config section, the `extapi` / `extweb` subscription prefixes and `serviceName = "extapi-{tenant}"`. Do not rename these.

## 2. Repositories

| Repo | Kind | Runtime | Owns | Talks to |
|------|------|---------|------|----------|
| **flexforms-api** | ASP.NET Core API (Clean Architecture, MediatR CQRS) + NSwag client package `GovUK.Dfe.FlexForms.Api.Client` | .NET 10, Azure Container Apps | System of record: TenantConfig DB, EA DB(s), auth/token exchange, permissions, files, email, SignalR hub, outbox, outbound events | SQL Server ×2, Redis, Service Bus, Azure File Share, Azure SignalR, GOV.UK Notify, Prism control API, Data Protection blob + Key Vault |
| **flexforms-web** | Razor Pages form engine + admin (Clean Architecture, use-case services) | .NET 10, Azure Container Apps | UI, cookie session (Redis), OIDC/Entra/Test sign-in, form navigation and response JSON shape, Template Studio, malware UX | API (REST via Api.Client), Redis, Service Bus (scan results), external autocomplete APIs |
| **flexforms-prism** | Azure Functions (isolated) analytics projector + control plane | .NET 10, Functions v4 | Schema `prism` (long answer facts, catalogue, export policy, views for the data team) | Service Bus topic `flexforms-prism`, API `/v1/internal/prism` (read), its own SQL DB |
| **rsd-file-scanner-function** | Azure Function (isolated), Service Bus trigger | .NET 10, Functions v4 | Scan orchestration, scan-result cache by file hash | Service Bus, Redis, ClamAV API |
| **rsd-clamav-api** | Minimal API + `clamd` in one container, plus `GovUK.Dfe.ClamAV.Api.Client` package | .NET 10, Ubuntu 24.04 image, ACA | Virus scanning of a URL or upload, async job store | Redis (job store, optional), the file URL it is given |
| **rsd-core-libs** | 15 independently versioned NuGet packages (`GovUK.Dfe.CoreLibs.*`) | net10.0 | Shared contracts (DTOs, enums, events, topic names), security, caching, messaging, email, file storage, testing | none at runtime |

Not in this workspace (do not invent their code): tenant file-validation Azure Functions, downstream consumers of `transfer-application-submitted`, scanner Function App infrastructure, any data-lake jobs.

## 3. Runtime topology

```
Browser ──HTTPS(cookie)──► flexforms-web ──HTTPS(user JWT + X-Tenant-ID)──► flexforms-api ──► TenantConfig SQL (schema tenantconfig)
   │                          │   ▲                                           │  │  │  ├──► EA SQL per tenant (schema ea; shared or isolated)
   │                          │   │ ScanResultEvent (extweb-scan-result)      │  │  │  ├──► Azure File Share (files)
   │                          │   │                                           │  │  │  ├──► GOV.UK Notify (email)
   │                          ▼   │                                           │  │  │  └──► Redis (cache, OTP, rate limits, notifications, pub/sub)
   │                        Redis (session, tickets, two-level cache)         │  │  │
   └──WebSocket /hubs/notifications (hubauth cookie) ─────────────────────────►  │  │ (Azure SignalR in deployed envs)
                                                                                 │  │
             Service Bus ◄── ScanRequestedEvent ────────────────────────────────┘  │
   file-scanner-requests ──► rsd-file-scanner-function ──► rsd-clamav-api        │
   file-scanner-results  ◄── ScanResultEvent ──┘   (POST /scan/async/url, poll)  │
        │  ├──► API  ScanResultConsumer (extapi-scan-result): metadata, delete infected, notify
        │  └──► Web  ScanResultConsumer (extweb-scan-result): blacklist, strip from session, notify
             flexforms-prism topic ◄── ApplicationProjectionRequestedEvent / TemplateVersionPublishedEvent (EF outbox, pre-commit)
                    └──► flexforms-prism ProjectionFunction (sub prism-projector, sessions) ──GET /v1/internal/prism──► API
                                    └──► Prism SQL (schema prism) ──views──► Power BI / data team
             tenant topics ◄── EventTriggers (Typed / Schema events, e.g. transfer-application-submitted) ──► external consumers
   Tenant validation function ──POST /v1/integrations/files/{id}/validation-result──► API
   API ──HTTPS (managed identity)──► Prism control API (export policy proxy)
```

## 4. Identity and authorisation matrix

### Entra app roles (client-credentials `roles` claim, never `scp`)

| Role | On which app registration | Held by | Grants |
|------|---------------------------|---------|--------|
| `Platform.Host.Read` | Platform API app (`Platform:AzureAd`) | Web managed identity / app | `GET /v1/host-config` (policy `PlatformHost`) |
| `Platform.TenantConfig.Read` | Platform API app | Web | `GET /v1/tenant-config/resolve`, `/tenant-config/tenants/{id}`, `POST /v1/admin/tenants/seed` (policy `PlatformTenantConfig`) |
| `Prism.Read` | Platform API app | Prism Function managed identity | `/v1/internal/prism/*` (policy `PlatformPrismRead`). Putting it on a tenant app gives 403 |
| `Prism.Admin` / `Prism.Delegate` | Prism app registration | API (proxy) / operators | Prism control endpoints; Delegate + `X-Prism-Acting-User` records the acting user |
| `API.Read` | ClamAV API app | Scanner Function | ClamAV `/scan*`, `/version` (policy `CanRead`) |

### API authentication schemes (flexforms-api `Api/Security/AuthorizationExtensions.cs`)

`CompositeScheme` dispatches to: `TenantBearer` (exchanged user JWT signed with the tenant's `Authorization:TokenSettings`, or a tenant Entra service token), `ApiKey` (`X-Api-Key`, SHA-256 hex stored in TenantConfig `AuthProviders`), `Mtls` (thumbprint), `PlatformBearer` (platform Entra app), `HubCookie` (all `/hubs/*`). The JWT `tenant_id` must equal the resolved tenant.

Named policies: `ServiceCallers` (service principal), `TenantAdminUser` (interactive Admin/SuperAdmin with email; machines refused), `PlatformHost`, `PlatformTenantConfig`, `PlatformPrismRead`, `Cookies.CanReadNotifications`, and the permission policies `CanReadTemplate`, `CanWriteTemplate`, `CanListTemplates`, `CanCreateTemplate`, `CanReadUser`, `CanManageUsers`, `CanReadApplication`, `CanUpdateApplication`, `CanDeleteApplication`, `CanReadAnyApplication`, `CanCreateAnyApplication`, `CanReadApplicationFiles`, `CanWriteApplicationFiles`, `CanDeleteApplicationFiles`, `CanReadNotifications`, `CanWriteNotifications`, `CanDeleteNotifications`, `CanRecordFileValidation`. Endpoint → policy is pinned by `src/Tests/GovUK.Dfe.FlexForms.Api.Tests/Security/ExpectedSecurity.json`.

### User identity end to end

1. Web picks a scheme per request (`TenantAuthSchemeSelector`): internal service headers → Test Authentication (non-production only) → Entra SSO (`EntraSso:Enabled`) → DfE Sign-In OIDC. Cookie is the authenticate scheme; tickets in Redis.
2. Web's Api.Client `TokenExchangeHandler` calls `POST /v1/tokens/exchange` with its Entra service token plus the user's IdP token. The API validates the IdP token (CoreLibs `ExternalIdentityValidator`), finds/creates the `User` and `TenantMembership`, and returns a tenant-scoped JWT carrying role and permission claims.
3. Every later API call carries that JWT, `X-Tenant-ID`, `x-correlationId` and optionally `X-Template-Id` / `X-Application-Reference`.

### Roles and permission claims

- Platform SuperAdmin: `Users.RoleId == RoleConstants.AdminRoleId` (fixed GUID, never change it). Tenant Admin, User and custom roles are tenant-scoped `Roles` rows joined by `TenantMembership`.
- Permission claim format `{ResourceType}:{ResourceKey}:{AccessType}`, e.g. `Template:Any:Manage`, `Application:{guid}:Write`, `Notifications:{tenantId}:{email}:Read`. Merged from `RolePermissions` + per-user `Permissions` (`PermissionClaimMerger`).
- Contributor invites: the invite form's checkboxes (`AddContributorRequest.Permissions`) become the contributor's effective `Application` / `ApplicationFiles` grants on that application. The merge adds a marker claim `ContributorInvite:{applicationId}:Scope`, and `PermissionClaimEvaluator` then ignores tenant-wide `Any` grants for that application. Any client that rebuilds claims from `GET /v1/me/permissions` must re-emit the markers from `UserAuthorizationDto.InviteScopedApplicationKeys` (API `PermissionClaimProvider`, Web `PermissionsClaimProvider`).
- `ResourceType` (int values persisted): Application 0, User 1, Template 2, TaskGroup 3, Task 4, Page 5, Field 6, ApplicationFiles 7, File 8, Notifications 9, FileValidation 10. `AccessType`: Read 0, Write 1, Delete 2, Manage 3.

## 5. Shared contracts

### Service Bus (`GovUK.Dfe.CoreLibs.Messaging.Contracts`)

| Topic (`TopicNames`) | Message | Publisher | Subscriptions / consumers |
|---|---|---|---|
| `file-scanner-requests` (`ScanRequests`) | `ScanRequestedEvent(FileId, FileName, FileHash, Reference, Path, IsAzureFileShare, FileUri, ServiceName, Metadata)` | API `FileUploadedDomainEventHandler` | `file-scanner-function-app` → rsd-file-scanner-function |
| `file-scanner-results` (`ScanResult`) | `ScanResultEvent(ServiceName, FileUri, FileName, FileId, Reference, Path, IsAzureFileShare, CorrelationId, Status, Outcome, MalwareName, ScannedAt, ScannerVersion, Message, TimeoutSeconds, VendorJobId, Metadata)` | scanner | `extapi-scan-result` (API), `extweb-scan-result` (Web). Prefix from `MassTransit:SubscriptionPrefix` |
| `flexforms-prism` (`FlexFormsPrism`) | `ApplicationProjectionRequestedEvent(ContractVersion, TenantId, ApplicationId, Reason, SourceRevision, ResponseId, SubmissionId, TemplateId, TemplateVersionId, OperationId, OccurredAt)`, `TemplateVersionPublishedEvent(ContractVersion, TenantId, TemplateId, TemplateVersionId, VersionNumber, CreatedAt)` | API via EF transactional outbox (always); Prism control plane re-enqueues `Resync` | `prism-projector` (session-enabled) → Prism `ProjectionFunction` |
| `transfer-application-submitted` (`TransferApplicationSubmitted`) | `TransferApplicationSubmittedEvent` | API `EventTriggerDispatcher` when a tenant's `EventTriggers` binds it | outside this workspace |
| tenant-defined topics | Schema events from TenantConfig `SchemaEvents` | API `EventTriggerDispatcher` | outside this workspace |

Enums: `VirusScanOutcome` Clean 0 / Infected 1 / Error 2; `ScanStatus` Accepted / Completed / Failed / TimedOut; `ProjectionReason` Saved / Submitted / Deleted / Resync.

Identifiers (`ApplicationProjectionIdentifiers`, UUIDv5 under a fixed namespace): session `{tenantId}:{applicationId}` (template events `{tenantId}:template:{templateId}`); message id from `prism:{tenant}:{app}:{revision}:{reason}` (+ `:{operationId}` for Resync, which requires one); `SubmissionId(applicationId, submittedRevision)`. The topic must have duplicate detection and the subscription sessions, both at creation.

Wire format is the MassTransit JSON envelope (`message` property, `messageType` URNs `urn:message:{namespace}:{type}`). Prism and the scanner read it by hand, so **moving an event to another namespace or renaming it is a breaking change**. Routing headers on scan messages: `TenantId`, `TenantName`, `TemplateId`, `UserId` (plus matching `Metadata` keys); consumers filter in code, and the scanner re-stamps them on cache hits.

### REST contracts (`GovUK.Dfe.CoreLibs.Contracts.ExternalApplications`)

Request/response DTOs used by the API, `GovUK.Dfe.FlexForms.Api.Client`, Web and Prism (`PrismDtos.cs`). Persisted enums: `ApplicationStatus` Created −1, InProgress 0, Submitted 1, Deleted 2; `FileValidationStatus` NotRequired 0, Pending 1, Passed 2, Failed 3; `FileValidationMode` Off 0, FailOnInvalid 1, RequirePassed 2. Custom application statuses are labels only.

### Form template JSON (owned by Web's form engine, stored by the API, flattened by Prism)

```
{ templateId, templateName, description, contributorPattern, contributorRoleId, displayCopy, emailTemplates,
  dashboard, applicationMetaData, conditionalLogic[], defaultFieldRequirementPolicy, hideFieldLabelWhenOnlyOneField,
  taskGroups: [ { groupId, groupName, groupOrder, groupStatus,
    tasks: [ { taskId, taskName, caption, taskOrder, taskStatus, visibleInTaskList, mode, summary,
               flows[] (multi-collection: flowId, fieldId, minItems, maxItems, itemTitleBinding, pages[]),
               derivedFlows[] (sourceFieldId, sourceType, pages[]),
      pages: [ { pageId, slug, title, pageOrder, returnToSummaryPage, navigationAfterSave, saveButtonLabel,
        fields: [ { fieldId, type, label, placeholder, tooltip, required, order, visibility, validations[], options[], complexField } ] } ] } ] } ] }
```

Field types rendered by Web partials: text input, character count, textarea, radios, checkboxes, select, date input, complex (autocomplete, upload). Templates also carry `retiredFields` / `semanticKey` for reporting (Prism catalogue). Rules: never recycle a `fieldId`; applications pin `TemplateVersionId`; new templates start not live.

Response body (one immutable `ApplicationResponse` per save, full snapshot): keyed by field id, values shaped `{ question, dataType, value, completed }`, collection values nested JSON, plus form plumbing such as `TaskStatus_*` (Prism skips it). Saves send Base64 (`WafSafeUtf8Base64`), create sends raw JSON, so readers accept both.

### Tenant configuration

TenantConfig DB rows `(TenantId, Category, Target)`, Target `Shared` | `Api` | `Web`. A category lives under Shared **or** app targets, never both (rejected on write by `TenantSettingTargetOverlap`). Secret categories are Data Protection-encrypted (`TenantSettings.v1`). Web reads its view through `GET /v1/tenant-config` (Shared + Web) and host settings through `GET /v1/host-config?target=Web`. Key categories: `ConnectionStrings`, `Authorization`, `AzureAd`, `Authentication`, `DfESignIn`, `EntraSso`, `TestAuthentication`, `TokenRefresh`, `AuthProviders`, `Email`, `EmailTemplates`, `EmailPlaceholderMappings`, `ApplicationTemplates` (HostMappings), `Template`, `FileStorage`, `FormEngine`, `FeatureManagement`, `Layout`, `EventTriggers`, `EventMappings`, `SchemaEvents`, `MassTransit`, `CacheSettings`, `ApplicationTerminology`, `ApplicationReference`, `NotificationBanner`, `ApplicationInsights`.

## 6. End-to-end flows

| Flow | Path (owner of each step in brackets) |
|------|------------------------------------|
| Web startup | app-only Entra token → `GET /v1/host-config?target=Web` merged over appsettings (Web `PlatformHostConfigurationBootstrapper`). `PlatformBootstrap:Enabled` must be true |
| Per request | hostname → `GET /v1/tenant-config/resolve` (cached `TenantHostnameCache`) → `GET /v1/tenant-config` (cached `TenantConfigurationCache`, `PlatformBootstrap:TenantConfigurationCacheMinutes`) → per-request OIDC/Entra overlay → session → token exchange (API) |
| Invite contributor | Web `Applications/Contributors-Invite` (checkbox defaults from template `contributorRoleId`, file options only when the template has uploads) → `POST /v1/Applications/{id}/contributors` with `Permissions` → `AddContributorCommandHandler` + `ContributorInviteAccess.ResolveAccess` (re-invite replaces that application's grants; the owner cannot be invited) → contributor emails |
| Create application | Web `Templates/Index` → `POST /v1/Applications` (`CreateApplicationCommandHandler`: live-template check, reference generation) → outbox `ApplicationProjectionRequestedEvent(Saved)` for the initial response → `ApplicationCreatedEvent` |
| Save page | Web `SaveFormPageService` merges posted fields into the session copy (reloaded from API; `StaleFormSessionException` guard) → `POST /v1/Applications/{id}/responses` full body → `AppendResponseVersionAsync` increments `SourceRevision` → outbox `ApplicationProjectionRequestedEvent(Saved)` before commit |
| Submit | Web `SubmitFormApplicationService` → `POST .../submit` → file-validation gate → `Application.Submit` → outbox `Submitted` → post-commit `ApplicationSubmittedEventHandler`: Notify email + `EventTriggers:ApplicationSubmitted` |
| Upload | Web → `POST .../files` (`UploadFileCommandHandler`, File Share) → `FileUploadedDomainEventHandler`: SAS URI, `ScanRequestedEvent`, `EventTriggers:FileUploaded` → scanner (cache by hash, 5s delay, ClamAV async URL + poll) → `ScanResultEvent` → API deletes infected / records outcome and notifies (Redis + SignalR); Web blacklists infected file and strips it from session |
| Template publish | Web Template Manager / Studio → `POST /v1/Templates` or `.../versions` → outbox `TemplateVersionPublishedEvent` → Prism catalogues fields before any application uses them |
| Owner change / delete | `PUT .../owner` → outbox `Saved` (latest response); `DELETE` → outbox `Deleted` → Prism tombstone |
| Tenant file validation | tenant function → `POST /v1/integrations/files/{id}/validation-result` (`CanRecordFileValidation`, service principal) → `FileValidationRecordedEvent` → SignalR; submit gate per `FileValidationMode` |
| Notifications | API `NotificationSignalRService` → hub groups (tenant + user); browser gets ticket via Web `/internal/hub-ticket` → API `POST /auth/hub-ticket` → `GET /auth/hub-cookie` |
| Reporting | outbox event → Prism re-reads `/v1/internal/prism` (`X-Tenant-ID`) → revision CAS → flatten under export policy → facts + views. Admin export-policy UI: Web → API `ReportingExportController` → Prism control API |
| Tenant config change | Web admin page → API `UpsertTenantSetting` → `ReplicatedTenantConfigurationProvider` refresh + Redis pub/sub to API replicas → Web `ITenantConfigurationInvalidator` pub/sub to Web replicas |

## 7. Redis usage (one Redis may be shared; prefixes keep apps apart)

| Owner | Keys / channels |
|------|-----------------|
| API | `FlexForms:Cache:t:{tenantId}:*` (tenant-prefixed, foreign prefix throws), `FlexForms:notifications:*`, `FlexForms:RateLimit:*`, OTP / authenticator / hub-ticket entries via `IAtomicCacheStore`, channel `FlexForms:Api:TenantConfigurationChanged` |
| Web | ASP.NET session + auth tickets, `FlexForms:Cache:{FormTemplate_*}` / `CustomApplicationStatuses_*` (+ `:version`), `FlexForms:InfectedFile*`, channels `FlexForms:TenantConfigurationInvalidated`, `FlexForms:LocalCacheKeyRemoved` |
| Scanner | `file-scan-result:{FileHash}` |
| ClamAV | `{ScanJobs:RedisKeyPrefix}{jobId}` + sorted-set index (only when `ConnectionStrings:Redis` set) |

## 8. Package and version matrix (2026-11-17)

| Consumer | CoreLibs and client pins |
|----------|--------------------------|
| flexforms-api | Contracts 2.0.6, Security 2.0.2, Caching / Http / AsyncProcessing / Utilities / Testing 2.0.0, Email / FileStorage / Notifications / Messaging.Contracts / Messaging.MassTransit 1.0.0. Publishes Api.Client (net10.0) |
| flexforms-web | Api.Client 0.0.60, Contracts 2.0.6 (pinned directly), Security 2.0.2, Caching 2.0.0, Http 2.0.0, Messaging.Contracts 1.0.0, Messaging.MassTransit 1.0.0, Testing 2.0.0 |
| flexforms-prism | Api.Client 0.0.50, Contracts 1.0.112, Messaging.Contracts 0.1.6 (lags API; see Prism `.ai/dependencies.md`) |
| rsd-file-scanner-function | ClamAV.Api.Client 1.0.0, Caching 2.0.0, Messaging.Contracts 1.0.0, Messaging.MassTransit 1.0.0 |
| rsd-clamav-api | AsyncProcessing 2.0.0, Security 2.0.0. Publishes ClamAV.Api.Client |

Rules: CoreLibs via NuGet only (local project refs build as `1.0.0.0` → `FileLoadException`); Contracts 2.x is net10.0 only; Application Insights stays on SDK 2.x everywhere (Serilog sink / Functions worker); Swashbuckle 10 needs `Microsoft.OpenApi` 2.x in the API.

## 9. Cross-repo change playbooks

| Change | Order of work |
|--------|---------------|
| New/changed REST DTO | CoreLibs Contracts (publish) → API Domain/Application + controller + `ExpectedSecurity.json` → regenerate NSwag, publish Api.Client → bump Web (and Prism if it reads it) |
| New API endpoint used by Web | API handler + policy + `ExpectedSecurity.json` + `.ai/security.md` → Api.Client release → Web Application port + Infrastructure adapter + use-case service → PageModel |
| New Service Bus event/topic | CoreLibs Messaging.Contracts (`TopicNames`, record) → publisher (API) → every consumer (Web, scanner, Prism) → create topic/subscription in infrastructure (sessions/duplicate detection at creation) |
| Change `ScanRequestedEvent`/`ScanResultEvent` | CoreLibs → scanner → API consumer + Web consumer, deployed together; keep routing headers |
| Change Prism events or `/v1/internal/prism` DTOs | CoreLibs → API publisher/handlers → Prism projector + `docs/prism-contract-v1.md`; bump `PrismVersions` if output changes |
| Template JSON shape | Web Domain models + renderer + Template Studio JS + designer manual → Prism `TemplateCatalogueBuilder` / `ResponseParser` + golden fixtures + `ProjectorVersion` |
| Response JSON shape | Web `TransformToResponseJson` path → Prism `ResponseParser` / `ResponseFlattener` + golden fixtures |
| New TenantConfig category | API (reader/validator, `SuperAdminOnlyTenantSettingCategories` / secret categories, cookbook, `NewTenantSettingsRequirements` if mandatory) → Web admin (`BuildDraft` if it must be copied) → docs `multi-tenancy-config.md` |
| ClamAV endpoint shape | rsd-clamav-api + client package → scanner `VirusScannerService` |
| Release order for coupled changes | CoreLibs → API (Web's deploy can wait via `required_api_release`) → Web / Prism / scanner |

## 10. Cross-cutting invariants (break none)

1. Tenant on every API request; JWT `tenant_id` equals resolved tenant; Redis keys tenant-prefixed.
2. API is the only writer of EA and TenantConfig. Prism never writes EA; scanner and ClamAV never touch SQL.
3. Prism events go through the outbox, are published before commit, and carry ids only; Prism always re-reads and compares `SourceRevision`.
4. Virus scanning is asynchronous and cannot be configured off by tenants (`EventTriggerDispatcher` refuses `ScanRequestedEvent`).
5. Both scan-result consumers (API and Web) stay.
6. Web session fails loud on Redis errors; API cache may degrade to SQL.
7. Test Authentication is hard-blocked in Production (API and Web gates).
8. Never log SAS URLs, tokens or secrets. These repos are public: record rules, not unfixed weaknesses.
9. Every production code change updates the repo's `.ai/` (CI "Knowledge base check").

## 11. Glossary

EA: the FlexForms OLTP database (`ea` schema). TenantConfig: tenant catalogue database (`tenantconfig` schema). HostMappings: `ApplicationTemplates` setting mapping hostnames to template ids. SourceRevision: per-application monotonically increasing revision, also the optimistic concurrency token. Generation: Prism's immutable flattened snapshot. Export policy: Prism's Allowed/Denied classification per field. Outbox: MassTransit EF Core transactional outbox in the EA database. SuperAdmin: platform admin (fixed role GUID). Contributor: a user invited onto an application. Derived flow: repeated pages generated from another field's values. Collection flow: user-added repeating items.
