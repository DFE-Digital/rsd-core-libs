namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// Completes Test Authentication authenticator app enrolment for the signed-in user by proving the app was set up
/// with the key returned by the setup step.
/// </summary>
/// <param name="Code">The 6 digit code currently shown in the authenticator app.</param>
public sealed record ConfirmTestAuthAuthenticatorSetupRequest(string Code);
