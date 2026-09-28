using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

/// <summary>
/// Run agents, either by resolving them from a specification or by running an already-resolved agent.
/// </summary>
public interface IAgentRunner
{
    /// <summary>
    /// Gets or creates the agent for <paramref name="spec"/>, then runs it.
    /// </summary>
    /// <param name="spec">The specification of the agent to run.</param>
    /// <param name="prompt">The prompt to provide to the agent.</param>
    /// <param name="conversationId">The optional conversation ID to use.</param>
    /// <param name="resolveToolCalls">The optional callback to resolve tool calls.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="additionalContext">Untrusted evidence, sent fenced as data; null for none.</param>
    /// <returns>The result of the agent run.</returns>
    Task<AgentResult> RunAsync(AgentSpec spec, string prompt, string? conversationId = null,
        Func<IReadOnlyList<ToolCallRequest>, CancellationToken, Task<IEnumerable<ToolCallOutput>>>? resolveToolCalls = null,
        string? additionalContext = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs an already-resolved agent.
    /// </summary>
    /// <param name="agent">The agent to run.</param>
    /// <param name="prompt">The prompt to run.</param>
    /// <param name="conversationId">The conversation to continue; null starts a new conversation.</param>
    /// <param name="additionalContext">Untrusted evidence, sent fenced as data; null for none.</param>
    /// <param name="resolveToolCalls">Optional callback for resolving paused tool calls.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The agent execution result.</returns>
    Task<AgentResult> RunAsync(AgentReference agent, string prompt, string? conversationId = null,
        string? additionalContext = null,
        Func<IReadOnlyList<ToolCallRequest>, CancellationToken, Task<IEnumerable<ToolCallOutput>>>? resolveToolCalls = null,
        CancellationToken cancellationToken = default);
}

