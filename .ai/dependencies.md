# Dependencies

Internal: Security → Caching; Notifications → Contracts (older pin possible). Tests → Testing project.

FlexForms consumption (typical; pins drift):

| Package | api | web | prism |
|---------|-----|-----|-------|
| Contracts | yes | yes | yes |
| Security | yes | yes | no |
| Caching | yes | yes | no |
| Http | yes | yes | no |
| Email, FileStorage, Notifications, AsyncProcessing, Utilities | api | — | — |
| Messaging.Contracts | yes | yes | yes |
| Messaging.MassTransit | yes | yes | no |
| Testing | tests | tests | — |

ClamAV API uses AsyncProcessing + Security. File-scanner uses Messaging.* + Caching + ClamAV.Api.Client.
