namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>Runs the tool calls a model made in one round and returns an output for each call.</summary>
public delegate Task<IEnumerable<ToolCallOutput>> ToolCallResolver(IReadOnlyList<ToolCallRequest> calls, CancellationToken cancellationToken);
