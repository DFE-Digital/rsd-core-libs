# Feature: Messaging

`TopicNames`: `file-scanner-requests`, `file-scanner-results`, `transfer-application-submitted`, `flexforms-prism`.

Events: scan pair, Prism pair, transfers submit. `DeterministicGuid`, `MessageNotForThisInstanceException` (local dev isolation).

`AddDfEMassTransit` → `IEventPublisher`. Optional entity auto-create hosted service.

Changing a topic name or event contract is a coordinated release: API, Web, scanner, Prism.
