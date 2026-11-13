namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// Verifies a code from the authenticator app the user enrolled for Test Authentication in the current tenant.
/// The outcome is returned as <see cref="Response.VerifyTestAuthPasswordResponse"/>.
/// </summary>
/// <param name="Email">Email address the authenticator app was enrolled for.</param>
/// <param name="Code">The 6 digit code currently shown in the authenticator app.</param>
public sealed record VerifyTestAuthAuthenticatorCodeRequest(string Email, string Code);
