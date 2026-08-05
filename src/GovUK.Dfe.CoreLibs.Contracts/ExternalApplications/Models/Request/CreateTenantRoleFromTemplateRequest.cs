namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>Creates a custom tenant role from a named preset (Caseworker, Reviewer).</summary>
public sealed record CreateTenantRoleFromTemplateRequest(string TemplateKey);
