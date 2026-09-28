using GovUK.Dfe.CoreLibs.AiAgents.Context;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

/// <summary>
/// Runs your agents. Creates or resolves each one (respecting pins), attaches its allowed tools, runs tool calls,
/// fences evidence, deletes the conversation and records token usage.
/// </summary>
public interface IAgentService
{
    /// <summary>Runs one agent.</summary>
    /// <param name="prompt">Your instruction.</param>
    /// <param name="evidence">Untrusted material (search results, other agents' output), sent fenced as data.</param>
    /// <exception cref="InvalidOperationException">The run failed; the cause is the inner exception.</exception>
    /// <exception cref="TimeoutException">The run exceeded <c>RunTimeout</c>, or no run slot came free in time.</exception>
    Task<AgentResult> RunAsync(AgentDefinition definition, string prompt, string? evidence = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs agents side by side. A failure returns a fallback result, unless <paramref name="shouldSuppress"/> returns
    /// false: then the others are cancelled and it's thrown.
    /// </summary>
    /// <param name="context">Unused here; matches <see cref="RunSequentialAsync"/>.</param>
    /// <param name="shouldSuppress">Which failures become fallback results. Default: all but cancellation.</param>
    /// <param name="resolveEvidence">Each agent's untrusted material, sent fenced; null for none.</param>
    /// <returns>One result per definition, in order.</returns>
    Task<IReadOnlyList<AgentResult>> RunParallelAsync(IReadOnlyCollection<AgentDefinition> definitions,
        Func<AgentDefinition, CancellationToken, Task<string>> resolvePrompt, AgentContext context,
        Func<Exception, bool>? shouldSuppress = null,
        Func<AgentDefinition, CancellationToken, Task<string?>>? resolveEvidence = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs agents in order. Each gets the previous output (the first gets <paramref name="initialInput"/>) as fenced
    /// evidence, so nothing passed along the chain can act as an instruction.
    /// </summary>
    /// <param name="context">Records each successful step.</param>
    /// <param name="shouldSuppress">Which failures become fallback results. Default: all but cancellation.</param>
    /// <returns>One result per definition, in order.</returns>
    Task<IReadOnlyList<AgentResult>> RunSequentialAsync(IReadOnlyList<AgentDefinition> definitions,
        Func<AgentDefinition, CancellationToken, Task<string>> resolvePrompt, string initialInput,
        AgentContext context, Func<Exception, bool>? shouldSuppress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// For a provisioning job: creates or reuses each managed agent's version without running it, built exactly as a
    /// run would build it. Ignores pins; skips ephemeral and externally managed agents.
    /// </summary>
    /// <returns>Each agent's version, for apps to list under <c>ExternallyManagedAgents</c>.</returns>
    Task<IReadOnlyList<AgentReference>> ProvisionAsync(IReadOnlyCollection<AgentDefinition> definitions,
        CancellationToken cancellationToken = default);
}
