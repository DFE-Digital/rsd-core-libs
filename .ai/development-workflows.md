# Development workflows (CoreLibs)

Per-package GitHub workflows under `.github/workflows/`. `pack-template.yml` + `build-test-template.yml`. GitVersion tag prefix `{PackageName}-`.

Contracts CI often `run_tests: false` (no dedicated test project).

When adding FlexForms DTOs, put them under `Contracts/ExternalApplications/` and publish **before** API NSwag/shared-DTO consumption.

Razor Page test helpers (`RazorPageTestFixture`, `InMemorySession`, `CleanArchitectureGuard`) were added for FlexForms Web without breaking default NSubstitute behaviour (`configureMembers` optional).
