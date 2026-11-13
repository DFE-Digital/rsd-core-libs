namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// Uses an administrator-issued setup code to start authenticator app enrolment for <paramref name="Email"/>.
/// The code is consumed; the response is a <see cref="Response.BeginTestAuthAuthenticatorSetupResponse"/>, and the
/// enrolment is completed with a code from the app.
/// </summary>
/// <param name="Email">Email address the setup code was created for.</param>
/// <param name="InviteCode">The setup code. Case, spaces and hyphens are ignored.</param>
public sealed record RedeemTestAuthAuthenticatorInviteRequest(string Email, string InviteCode);
