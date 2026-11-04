# Architecture (CoreLibs)

Not a deployed service. DI extension methods per package (`AddHybridCaching`, `AddDfEMassTransit`, `AddCustomOpenIdConnect`, `AddFileStorage`, …). Caching/AsyncProcessing extensions live in namespace `Microsoft.Extensions.DependencyInjection`.

**Contracts** is reference-only types: `ExternalApplications.*` (FlexForms) and `Academies.*`.

**Messaging.Contracts** owns `TopicNames` and events (`ScanRequestedEvent`, Prism events). Changing topic strings is a production incident.

Target framework: **net8.0** while FlexForms hosts are **net10.0** — that is supported; don’t “upgrade CoreLibs to net10” as a drive-by unless all consumers agree.
