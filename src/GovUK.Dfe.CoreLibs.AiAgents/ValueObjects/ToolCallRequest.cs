namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>
/// Represents a function tool call requested by an agent run.
/// </summary>
/// <param name="CallId">The call identifier used to correlate the result.</param>
/// <param name="FunctionName">The function to invoke.</param>
/// <param name="Arguments">The function arguments as JSON.</param>
public sealed record ToolCallRequest(string CallId, string FunctionName, string Arguments);
