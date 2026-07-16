namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// Request body for creating a new template in the current tenant.
/// </summary>
/// <param name="Name">Display name for the template.</param>
/// <param name="InitialVersionNumber">Optional first version number (requires <paramref name="JsonSchema"/>).</param>
/// <param name="JsonSchema">Optional Base64-encoded JSON schema for the initial version.</param>
public record CreateTemplateRequest(
    string Name,
    string? InitialVersionNumber = null,
    string? JsonSchema = null);
