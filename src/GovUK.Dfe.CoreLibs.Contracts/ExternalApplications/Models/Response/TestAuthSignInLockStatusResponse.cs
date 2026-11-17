using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

/// <summary>
/// Whether a user is locked out of Test Authentication sign-in in the current tenant, as shown to administrators.
/// </summary>
/// <param name="IsManuallyLocked">
/// <c>true</c> when an administrator locked the user out. Blocks every Test Authentication sign-in method and never
/// expires.
/// </param>
/// <param name="LockedAtUtc">When the manual lock was set, if locked.</param>
/// <param name="LockedByEmail">Email of the administrator who set the manual lock, if known.</param>
/// <param name="IsTemporarilyLocked">
/// <c>true</c> while authenticator app sign-in is locked after too many wrong codes. Expires on its own.
/// </param>
/// <param name="RetryAfterSeconds">Roughly how many seconds until the temporary lock expires, if temporarily locked.</param>
public sealed record TestAuthSignInLockStatusResponse(
    [property: JsonPropertyName("isManuallyLocked")] bool IsManuallyLocked,
    [property: JsonPropertyName("lockedAtUtc")] DateTime? LockedAtUtc,
    [property: JsonPropertyName("lockedByEmail")] string? LockedByEmail,
    [property: JsonPropertyName("isTemporarilyLocked")] bool IsTemporarilyLocked,
    [property: JsonPropertyName("retryAfterSeconds")] int? RetryAfterSeconds);
