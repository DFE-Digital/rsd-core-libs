namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>
/// Represents the result of a <see cref="ToolCallRequest"/>.
/// </summary>
/// <param name="CallId">The originating tool call identifier.</param>
/// <param name="Output">The tool result, typically as JSON.</param>
public sealed record ToolCallOutput(string CallId, string Output);
