namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

/// <summary>
/// Outcome of verifying a Test Authentication one-time password.
/// </summary>
/// <param name="IsValid"><c>true</c> when the password matched and has now been consumed.</param>
public sealed record VerifyTestAuthPasswordResponse(bool IsValid);
