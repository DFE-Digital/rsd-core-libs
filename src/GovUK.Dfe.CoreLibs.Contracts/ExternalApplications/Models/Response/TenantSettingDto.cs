namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    /// <summary>
    /// Shared tokens for tenant-setting secret redaction. Secret leaves in
    /// <see cref="TenantSettingDto.SettingsJson"/> are replaced with
    /// <see cref="Sentinel"/> before the row leaves the API.
    /// </summary>
    public static class TenantSettingSecretRedaction
    {
        /// <summary>
        /// Placeholder written in place of a withheld secret leaf. Saving a row that still
        /// contains this value preserves the stored secret.
        /// </summary>
        public const string Sentinel = "__REDACTED__";

        /// <summary>
        /// Audit <c>Action</c> recorded when a SuperAdmin reveals a secret via the API.
        /// </summary>
        public const string RevealAuditAction = "SecretRevealed";
    }

    /// <summary>
    /// Describes a single secret-bearing value that was withheld from a
    /// <see cref="TenantSettingDto.SettingsJson"/> payload. Carries enough information to
    /// identify and compare the value (for example against a Key Vault entry) without
    /// disclosing it.
    /// </summary>
    /// <param name="Path">
    /// Colon-delimited path to the redacted leaf inside the settings JSON, for example
    /// <c>Providers[0]:KeyHash</c>.
    /// </param>
    /// <param name="ValueLength">Character length of the withheld value.</param>
    /// <param name="Fingerprint">
    /// Truncated SHA-256 of the withheld value, formatted as <c>sha256:xxxxxxxx</c>.
    /// </param>
    public sealed record TenantSettingRedactedValueDto(
        string Path,
        int ValueLength,
        string Fingerprint);

    /// <summary>
    /// A single TenantConfig settings row (category JSON blob).
    /// </summary>
    /// <remarks>
    /// Secret-bearing values inside <paramref name="SettingsJson"/> are always replaced with a
    /// redaction sentinel before the row leaves the API, in every environment. Plaintext is only
    /// available from the SuperAdmin-only reveal endpoint. Saving a row that still contains the
    /// sentinel preserves the stored value, so an editor never needs the plaintext to make an
    /// unrelated change in the same category.
    /// </remarks>
    public sealed record TenantSettingDto(
        Guid SettingId,
        string Category,
        string Target,
        string SettingsJson,
        bool IsSecret,
        DateTime UpdatedAtUtc,
        IReadOnlyCollection<TenantSettingRedactedValueDto>? RedactedValues = null);
}
