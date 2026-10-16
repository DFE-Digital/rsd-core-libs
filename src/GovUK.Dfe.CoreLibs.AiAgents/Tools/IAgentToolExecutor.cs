using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tools;

/// <summary>
/// A tool provider whose tools run in this app, not Foundry, so their credentials stay here. Foundry only sees definitions.
/// </summary>
public interface IAgentToolExecutor
{
    /// <summary>Runs <paramref name="call"/> if it's one of this provider's tools.</summary>
    /// <param name="call">The function call the model made.</param>
    /// <returns>The tool's output, or <see langword="null"/> when the call isn't one of this provider's tools.</returns>
    Task<string?> TryExecuteAsync(ToolCallRequest call, CancellationToken cancellationToken = default);
}
