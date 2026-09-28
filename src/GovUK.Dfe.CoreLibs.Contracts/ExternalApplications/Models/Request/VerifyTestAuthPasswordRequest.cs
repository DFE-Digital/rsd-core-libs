namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// Verifies a Test Authentication one-time password previously emailed to the user.
/// A successful verification consumes the password so it cannot be reused.
/// </summary>
/// <param name="Email">Email address the password was sent to.</param>
/// <param name="Password">The 6 digit one-time password the user entered.</param>
public sealed record VerifyTestAuthPasswordRequest(string Email, string Password);
