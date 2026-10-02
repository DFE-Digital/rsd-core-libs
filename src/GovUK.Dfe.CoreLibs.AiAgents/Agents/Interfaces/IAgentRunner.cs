using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

/// <summary>Advanced: runs one agent, from a spec or an already-resolved reference.</summary>
public interface IAgentRunner
{
    /// <summary>Gets or creates the agent for <paramref name="spec"/>, then runs it.</summary>
    /// <param name="conversationId">A conversation to continue; null starts (and afterwards deletes) a new one.</param>
    /// <param name="additionalContext">Untrusted evidence, sent fenced as data.</param>
    /// <param name="validateOutput">Returns why an answer is invalid, or null. An invalid answer is retried once.</param>
    Task<AgentResult> RunAsync(AgentSpec spec, string prompt, string? conversationId = null,
        ToolCallResolver? resolveToolCalls = null,
        string? additionalContext = null, Func<AgentResult, string?>? validateOutput = null,
        CancellationToken cancellationToken = default);

    /// <summary>Runs an already-resolved agent.</summary>
    /// <param name="conversationId">A conversation to continue; null starts (and afterwards deletes) a new one.</param>
    /// <param name="additionalContext">Untrusted evidence, sent fenced as data.</param>
    /// <param name="validateOutput">Returns why an answer is invalid, or null. An invalid answer is retried once.</param>
    Task<AgentResult> RunAsync(AgentReference agent, string prompt, string? conversationId = null,
        string? additionalContext = null,
        ToolCallResolver? resolveToolCalls = null,
        Func<AgentResult, string?>? validateOutput = null,
        CancellationToken cancellationToken = default);
}
