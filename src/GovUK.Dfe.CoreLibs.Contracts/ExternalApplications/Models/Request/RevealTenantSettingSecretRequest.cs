namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// Break-glass request for the plaintext of a single redacted tenant setting value.
/// Interactive SuperAdmin only, rate limited, and recorded in the tenant setting audit log.
/// </summary>
/// <param name="Category">Setting category, for example <c>ConnectionStrings</c>.</param>
/// <param name="Target">Setting target: <c>Shared</c>, <c>Api</c> or <c>Web</c>.</param>
/// <param name="Path">
/// Colon-delimited path to the redacted leaf, as reported in
/// <c>TenantSettingDto.RedactedValues</c>, for example <c>Providers[0]:KeyHash</c>.
/// </param>
/// <param name="Reason">
/// Operational justification recorded against the audit entry. Required.
/// </param>
public sealed record RevealTenantSettingSecretRequest(
    string Category,
    string Target,
    string Path,
    string Reason);
