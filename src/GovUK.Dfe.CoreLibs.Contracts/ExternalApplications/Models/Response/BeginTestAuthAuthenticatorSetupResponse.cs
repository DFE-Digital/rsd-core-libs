using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

/// <summary>
/// A pending Test Authentication authenticator app enrolment for the signed-in user. Nothing is saved until the
/// user confirms a code; the pending key expires after a few minutes.
/// </summary>
/// <param name="SharedKey">Base32 key for users who type it into the app instead of scanning the QR code.</param>
/// <param name="AuthenticatorUri"><c>otpauth://totp/...</c> URI to render as a QR code.</param>
public sealed record BeginTestAuthAuthenticatorSetupResponse(
    [property: JsonPropertyName("sharedKey")] string SharedKey,
    [property: JsonPropertyName("authenticatorUri")] string AuthenticatorUri);
