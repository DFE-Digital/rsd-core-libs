using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

/// <summary>
/// Runs agents and returns their results.
/// </summary>
public interface IAgentRunner
{
    /// <summary>
    /// Runs an agent with the specified prompt and optional conversation ID, resolving any tool calls made by the model using the provided callback.
    /// </summary>
    /// <param name="spec">The specification of the agent to run.</param>
    /// <param name="prompt">The prompt to provide to the agent.</param>
    /// <param name="conversationId">The optional conversation ID to use.</param>
    /// <param name="resolveToolCalls">The optional callback to resolve tool calls.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the agent run.</returns>
    Task<AgentResult> RunAsync(AgentSpec spec, string prompt, string? conversationId = null,
        Func<IReadOnlyList<ToolCallRequest>, CancellationToken, Task<IEnumerable<ToolCallOutput>>>? resolveToolCalls = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the specified agent with the given prompt and optional conversation ID, additional context, and tool call resolution callback.
    /// </summary>
    /// <param name="agent">The agent to run.</param>
    /// <param name="prompt">The prompt to run.</param>
    /// <param name="conversationId">The conversation to continue; null starts a new conversation.</param>
    /// <param name="additionalContext">Optional per-call context provided to the agent.</param>
    /// <param name="resolveToolCalls">Optional callback for resolving paused tool calls.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The agent execution result.</returns>
    /// <param name="resolveToolCalls">Optional callback for resolving paused tool calls.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The agent execution result.</returns>
    Task<AgentResult> RunAsync(AgentReference agent, string prompt, string? conversationId = null,
        string? additionalContext = null,
        Func<IReadOnlyList<ToolCallRequest>, CancellationToken, Task<IEnumerable<ToolCallOutput>>>? resolveToolCalls = null,
        CancellationToken cancellationToken = default);
}

