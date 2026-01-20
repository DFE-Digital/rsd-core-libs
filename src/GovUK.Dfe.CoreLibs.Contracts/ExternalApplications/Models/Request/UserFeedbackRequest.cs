using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

[JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization,
    IgnoreUnrecognizedTypeDiscriminators = false)]
[JsonDerivedType(typeof(BugReport), nameof(BugReport))]
[JsonDerivedType(typeof(SupportRequest), nameof(SupportRequest))]
[JsonDerivedType(typeof(FeedbackOrSuggestion), nameof(FeedbackOrSuggestion))]
public abstract record UserFeedbackRequest(
    string Message,
    string? ReferenceNumber,
    Guid TemplateId
)
{
    [JsonIgnore]
    public abstract UserFeedbackType Type { get; }
}

public record BugReport(string Message, string? ReferenceNumber, string? EmailAddress, Guid TemplateId)
    : UserFeedbackRequest(Message, ReferenceNumber, TemplateId)
{
    [JsonIgnore]
    public override UserFeedbackType Type => UserFeedbackType.BugReport;
}

public record SupportRequest(string Message, string ReferenceNumber, string EmailAddress, Guid TemplateId)
    : UserFeedbackRequest(Message, ReferenceNumber, TemplateId)
{
    [JsonIgnore]
    public override UserFeedbackType Type => UserFeedbackType.SupportRequest;
}

public record FeedbackOrSuggestion(string Message, string? ReferenceNumber, SatisfactionScore SatisfactionScore, Guid TemplateId)
    : UserFeedbackRequest(Message, ReferenceNumber, TemplateId)
{
    [JsonIgnore]
    public override UserFeedbackType Type => UserFeedbackType.FeedbackOrSuggestion;
}

public enum UserFeedbackType
{
    BugReport,
    SupportRequest,
    FeedbackOrSuggestion
}

public enum SatisfactionScore
{
    VerySatisfied,
    Satisfied,
    NeitherSatisfiedOrDissatisfied,
    Dissatisfied,
    VeryDissatisfied
}
