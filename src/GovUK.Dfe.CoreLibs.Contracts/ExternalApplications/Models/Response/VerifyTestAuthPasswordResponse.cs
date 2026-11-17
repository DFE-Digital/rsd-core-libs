using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

/// <summary>
/// Outcome of verifying a Test Authentication one-time password or authenticator app code.
/// </summary>
/// <param name="IsValid"><c>true</c> when the code matched and has now been consumed.</param>
/// <param name="IsLockedOut">
/// <c>true</c> when sign-in was refused, or will be refused on the next attempt, because the email is locked out.
/// </param>
/// <param name="IsPermanentLockout">
/// <c>true</c> when an administrator locked the email out. It does not expire; only an administrator can unlock it.
/// </param>
/// <param name="RetryAfterSeconds">
/// For a temporary lockout after too many wrong codes, roughly how many seconds until the user can try again.
/// <c>null</c> otherwise.
/// </param>
public sealed record VerifyTestAuthPasswordResponse(
    [property: JsonPropertyName("isValid")] bool IsValid,
    [property: JsonPropertyName("isLockedOut")] bool IsLockedOut = false,
    [property: JsonPropertyName("isPermanentLockout")] bool IsPermanentLockout = false,
    [property: JsonPropertyName("retryAfterSeconds")] int? RetryAfterSeconds = null);
