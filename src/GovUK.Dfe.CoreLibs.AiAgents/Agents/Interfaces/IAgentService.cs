using GovUK.Dfe.CoreLibs.AiAgents.Context;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

/// <summary>
/// Runs your agents - the one service most apps need. Give it <see cref="AgentDefinition"/>s and it
/// creates or resolves each agent (respecting version pins), attaches its allowed tools, runs any tool
/// calls, fences evidence, deletes the conversation afterwards and records token usage.
/// </summary>
public interface IAgentService
{
    /// <summary>
    /// Runs one agent.
    /// </summary>
    /// <param name="definition">The agent to run.</param>
    /// <param name="prompt">Your instruction for this run.</param>
    /// <param name="evidence">
    /// Untrusted material for the agent to work from (search results, documents, another agent's output).
    /// Sent fenced as data the model is told not to take instructions from.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="InvalidOperationException">The run failed; the original error is the inner exception.</exception>
    /// <exception cref="TimeoutException">The run took longer than <c>RunTimeout</c>.</exception>
    Task<AgentResult> RunAsync(AgentDefinition definition, string prompt, string? evidence = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs agents side by side. A failed agent returns a fallback result instead of throwing, unless
    /// <paramref name="shouldSuppress"/> says otherwise - then the others are cancelled and the failure is thrown.
    /// </summary>
    /// <param name="definitions">The agents to run.</param>
    /// <param name="resolvePrompt">Your instruction for each agent.</param>
    /// <param name="context">Holds history for sequential runs; unused here, but kept so both calls look alike.</param>
    /// <param name="shouldSuppress">Which failures to turn into fallback results. Default: all but cancellation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="resolveEvidence">Untrusted material for each agent, sent fenced; <see langword="null"/> for none.</param>
    /// <returns>One result per definition, in the same order.</returns>
    Task<IReadOnlyList<AgentResult>> RunParallelAsync(IReadOnlyCollection<AgentDefinition> definitions,
        Func<AgentDefinition, CancellationToken, Task<string>> resolvePrompt, AgentContext context,
        Func<Exception, bool>? shouldSuppress = null, CancellationToken cancellationToken = default,
        Func<AgentDefinition, CancellationToken, Task<string?>>? resolveEvidence = null);

    /// <summary>
    /// Runs agents one after another. Each agent receives the previous agent's output (the first receives
    /// <paramref name="initialInput"/>) as fenced evidence, so text passed along the chain can't act as
    /// instructions to the next agent.
    /// </summary>
    /// <param name="definitions">The agents to run, in order.</param>
    /// <param name="resolvePrompt">Your instruction for each agent.</param>
    /// <param name="initialInput">The material the first agent works from.</param>
    /// <param name="context">Records each successful step's prompt and output.</param>
    /// <param name="shouldSuppress">Which failures to turn into fallback results. Default: all but cancellation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>One result per definition, in the same order.</returns>
    Task<IReadOnlyList<AgentResult>> RunSequentialAsync(IReadOnlyList<AgentDefinition> definitions,
        Func<AgentDefinition, CancellationToken, Task<string>> resolvePrompt, string initialInput,
        AgentContext context, Func<Exception, bool>? shouldSuppress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates managed agents without running them - for a central job that owns the agents,
    /// while apps only use them (pinned, or through <c>ExternallyManagedAgentProvider</c>). Each spec is
    /// built exactly as a run would build it: same prompt files, tool bindings, allowed tools and schema.
    /// A matching version is reused; otherwise a new one is created (and old ones pruned when
    /// <c>KeepLatestVersions</c> is set). Version pins are ignored: this job is the one that creates.
    /// Ephemeral and externally managed definitions are skipped.
    /// </summary>
    /// <param name="definitions">The agents to provision.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Each provisioned agent and the version it's now at - the versions to pin in the apps.</returns>
    Task<IReadOnlyList<AgentReference>> ProvisionAsync(IReadOnlyCollection<AgentDefinition> definitions,
        CancellationToken cancellationToken = default);
}
