# History lessons (CoreLibs)

| Lesson | Status |
|--------|--------|
| Session cache must rethrow Redis errors, or FlexForms wipes sessions and can persist empty form JSON | **Verified** — `SessionDistributedCacheAdapter` |
| `ExternalIdentityValidator` tries multiple providers and logs issuer errors for the wrong ones | **Historical** — check current validator |
| Trusted publishing via OIDC with a single policy on `pack-template.yml` | **Historical** — check workflows |
| Razor Page test fixtures were added to Testing for FlexForms Web | **Verified** |
| OTP DTOs live in Contracts where NSwag shares types | **Historical** |
| After the .NET 10 upgrade, test WebApplicationFactory setups needed to remove extra EF Core registrations | **Unverified** |

Do not document FlexForms TenantConfig here — that belongs to flexforms-api.
