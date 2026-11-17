# GovUK.Dfe.CoreLibs.Contracts

Shared request/response DTOs and enums for FlexForms (`ExternalApplications` namespaces).

## Notable shapes

- `AddContributorRequest.Permissions` (`ContributorInvitePermissions`) — invite-time Application / ApplicationFiles grants.
- `UserAuthorizationDto.InviteScopedApplicationKeys` (2.0.4+) — application resource keys that must be re-emitted as `ContributorInvite:{key}:Scope` permission claims when clients rebuild claims from GetMyPermissions, so the invite's checkboxes stay the effective file access for that application.
