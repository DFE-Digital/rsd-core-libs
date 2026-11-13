namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// An administrator creates a one-time code that lets the user with <paramref name="Email"/> set up a Test
/// Authentication authenticator app in the current tenant without first receiving an emailed password.
/// </summary>
/// <param name="Email">Email address the code is for. The user does not need to exist yet.</param>
public sealed record CreateTestAuthAuthenticatorInviteRequest(string Email);
