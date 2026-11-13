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

## Contracts 2.x

Contracts 2.0.0 onwards targets **net10.0 only**. A consumer that still multi-targets net8.0 (historically `GovUK.Dfe.FlexForms.Api.Client`) must drop net8.0 before moving to 2.x.

Test Authentication authenticator app DTOs (`VerifyTestAuthAuthenticatorCodeRequest`, `ConfirmTestAuthAuthenticatorSetupRequest`, `BeginTestAuthAuthenticatorSetupResponse`, `TestAuthAuthenticatorStatusResponse`) were added in 2.0.1. Login verification reuses `VerifyTestAuthPasswordResponse`. Administrator-issued setup codes (`CreateTestAuthAuthenticatorInviteRequest`, `CreateTestAuthAuthenticatorInviteResponse`, `RedeemTestAuthAuthenticatorInviteRequest`) were added in 2.0.2; redeeming reuses `BeginTestAuthAuthenticatorSetupResponse`, and confirming reuses `VerifyTestAuthAuthenticatorCodeRequest` / `VerifyTestAuthPasswordResponse`. Consumers that must bump to the release containing them: flexforms-api (Domain + Api.Client, then republish the client), then flexforms-web.
