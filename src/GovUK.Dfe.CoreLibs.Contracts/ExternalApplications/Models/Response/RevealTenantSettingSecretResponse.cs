namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

/// <summary>
/// Plaintext of a single tenant setting value, returned only to interactive SuperAdmins.
/// Every successful response has a corresponding <c>SecretRevealed</c> audit entry.
/// </summary>
public sealed record RevealTenantSettingSecretResponse(
    string Category,
    string Target,
    string Path,
    string Value,
    int ValueLength,
    string Fingerprint,
    DateTime RevealedAtUtc,
    string RevealedBy);
