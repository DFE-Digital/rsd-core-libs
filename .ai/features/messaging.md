# Feature: Messaging

`TopicNames`: `file-scanner-requests`, `file-scanner-results`, `transfer-application-submitted`, `flexforms-prism`.

Events: scan pair, Prism pair, transfers submit. `DeterministicGuid`, `MessageNotForThisInstanceException` (local dev isolation).

`AddDfEMassTransit` → `IEventPublisher`. Optional entity auto-create hosted service.

Changing a topic name or event contract is a coordinated release: API, Web, scanner, Prism.

## Contract details (**Verified** 2026-11-17)

| Record | Fields | Producer → consumer |
|--------|--------|---------------------|
| `ScanRequestedEvent` | `FileId?`, `FileName`, `FileHash?`, `Reference?`, `Path?`, `IsAzureFileShare?`, `FileUri` (SAS), `ServiceName`, `Metadata?` | FlexForms API → scanner |
| `ScanResultEvent` | `ServiceName`, `FileUri`, `FileName`, then optional `FileId`, `Reference`, `Path`, `IsAzureFileShare`, `CorrelationId`, `Status` (`ScanStatus`, default Completed), `Outcome` (`VirusScanOutcome`), `MalwareName`, `ScannedAt`, `ScannerVersion`, `Message`, `TimeoutSeconds`, `VendorJobId`, `Metadata` | scanner → FlexForms API + Web |
| `ApplicationProjectionRequestedEvent` | `ContractVersion` (`CurrentContractVersion = 1`), `TenantId`, `ApplicationId`, `Reason` (`ProjectionReason` Saved/Submitted/Deleted/Resync), `SourceRevision`, `ResponseId?`, `SubmissionId?`, `TemplateId?`, `TemplateVersionId?`, `OperationId?`, `OccurredAt` | FlexForms API (outbox), Prism control plane → Prism |
| `TemplateVersionPublishedEvent` | `ContractVersion` (1), `TenantId`, `TemplateId`, `TemplateVersionId`, `VersionNumber`, `CreatedAt` | FlexForms API → Prism |
| `TransferApplicationSubmittedEvent` (+ `TransferringAcademy`) | Transfers submit payload | FlexForms API `EventTriggers` → external |

`ApplicationProjectionIdentifiers` (UUIDv5 via `DeterministicGuid`, fixed namespace GUID): `SessionId` `{tenant}:{application}`, `TemplateSessionId` `{tenant}:template:{template}`, `MessageId` (Resync requires an operation id), `TemplateVersionMessageId`, `SubmissionId(applicationId, submittedRevision)`. Changing the namespace or the name formats changes every id, which defeats Service Bus duplicate detection and Prism's deterministic submission ids. Never change them.

Prism and the scanner read the MassTransit envelope by hand and match `urn:message:{namespace}:{type}`, so moving a record to another namespace is breaking. Enum integer values are persisted; append new members, never renumber.
