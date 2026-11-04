# Feature: Security package

`AddApplicationAuthorization`, `AddCustomOpenIdConnect`, `AddEntraSso`, `AddUserTokenService` / factory, `AddExternalIdentityValidation`, `AddTokenRefresh*`, Cypress auth, `ICurrentUser`.

FlexForms Web uses OIDC + Entra overlays + token refresh. API uses JWT factory + external identity validation for exchange.

Read the package `readme.md` before changing policy config shape (`Authorization:Policies`).
