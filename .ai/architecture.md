# Architecture (CoreLibs)

Not a deployed service. DI extension methods per package (`AddHybridCaching`, `AddDfEMassTransit`, `AddCustomOpenIdConnect`, `AddFileStorage`, …). Caching/AsyncProcessing extensions live in namespace `Microsoft.Extensions.DependencyInjection`.

**Contracts** is reference-only types: `ExternalApplications.*` (FlexForms) and `Academies.*`.

**Messaging.Contracts** owns `TopicNames` and events (`ScanRequestedEvent`, Prism events). Changing topic strings is a production incident.

Target framework: **net10.0** (all packages and test projects). Consumers must be on .NET 10 to take package versions published after the upgrade.
