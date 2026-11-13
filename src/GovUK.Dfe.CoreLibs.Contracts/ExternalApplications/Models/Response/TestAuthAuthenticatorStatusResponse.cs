using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

/// <summary>
/// Whether the signed-in user has an authenticator app enrolled for Test Authentication in the current tenant.
/// </summary>
/// <param name="IsEnrolled"><c>true</c> when an authenticator app is enrolled.</param>
/// <param name="EnrolledOnUtc">When the current enrolment was confirmed, if enrolled.</param>
public sealed record TestAuthAuthenticatorStatusResponse(
    [property: JsonPropertyName("isEnrolled")] bool IsEnrolled,
    [property: JsonPropertyName("enrolledOnUtc")] DateTime? EnrolledOnUtc);
