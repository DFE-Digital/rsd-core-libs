namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

/// <summary>
/// Platform typed-event catalogue discovered from Messaging.Contracts.
/// </summary>
public sealed class GetEventCatalogueResponse
{
    public IReadOnlyList<EventCatalogueItemDto> Events { get; init; } = [];
}

/// <summary>
/// One platform-curated (typed) messaging event.
/// </summary>
public sealed class EventCatalogueItemDto
{
    public string EventTypeName { get; init; } = string.Empty;

    public string? TopicName { get; init; }

    public string ClrTypeName { get; init; } = string.Empty;

    public string? Description { get; init; }

    /// <summary>Contract version hint (assembly informational version or "1.0").</summary>
    public string Version { get; init; } = "1.0";

    /// <summary>Always <c>Typed</c> for platform catalogue entries.</summary>
    public string Kind { get; init; } = "Typed";

    public IReadOnlyList<EventCataloguePropertyDto> Properties { get; init; } = [];
}

/// <summary>
/// A property (or nested record property) on a catalogue event.
/// </summary>
public sealed class EventCataloguePropertyDto
{
    public string Name { get; init; } = string.Empty;

    public string ClrType { get; init; } = string.Empty;

    public bool IsNullable { get; init; }

    public IReadOnlyList<EventCataloguePropertyDto>? Properties { get; init; }
}
