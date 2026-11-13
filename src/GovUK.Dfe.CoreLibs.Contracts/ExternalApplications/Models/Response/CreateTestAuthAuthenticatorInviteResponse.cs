using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

/// <summary>
/// A one-time authenticator app setup code. The plaintext code is only ever returned here; the API stores a hash.
/// </summary>
/// <param name="Code">The code to give to the user, for example <c>ABCDE-FGHJK</c>.</param>
/// <param name="ExpiresAtUtc">When the code stops working if it has not been used.</param>
public sealed record CreateTestAuthAuthenticatorInviteResponse(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("expiresAtUtc")] DateTime ExpiresAtUtc);
