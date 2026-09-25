namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// Asks the API to generate a one-time password for Test Authentication and email it to the user.
/// Any previously issued password for the same email address is replaced.
/// </summary>
/// <param name="Email">Email address the user entered on the Test Authentication login page.</param>
public sealed record SendTestAuthPasswordRequest(string Email);
