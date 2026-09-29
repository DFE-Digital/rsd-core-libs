namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>A function tool call the model made.</summary>
/// <param name="Arguments">As JSON.</param>
public sealed record ToolCallRequest(string CallId, string FunctionName, string Arguments);
