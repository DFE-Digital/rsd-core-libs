using GovUK.Dfe.CoreLibs.AiAgents.Constants;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tools;

/// <summary>
/// Builds the <c>resolveToolCalls</c> callback that <see cref="Agents.Interfaces.IAgentRunner"/> uses to
/// run an agent's function tool calls in this app. <c>IAgentService</c> does this for you from
/// <see cref="AgentToolBinding"/>s; call it yourself when using <c>IAgentRunner</c> directly.
/// </summary>
public static class AgentToolExecution
{
    /// <summary>Creates a callback that runs each call on the first of <paramref name="providers"/> that owns it.</summary>
    /// <param name="providers">The agent's tool providers. Only those implementing <see cref="IAgentToolExecutor"/> run calls.</param>
    /// <param name="allowedTools">
    /// The tool names this agent may call. A call to any other tool is refused, even if a provider could
    /// run it. <see langword="null"/> allows every tool the providers run.
    /// </param>
    /// <returns>The callback, or <see langword="null"/> when none of the providers runs tools in this app.</returns>
    public static ToolCallResolver? CreateResolver(
        IEnumerable<IAgentToolProvider> providers, IReadOnlyCollection<string>? allowedTools = null)
    {
        ArgumentNullException.ThrowIfNull(providers);

        var executors = providers.OfType<IAgentToolExecutor>().ToList();
        if (executors.Count == 0)
        {
            return null;
        }

        var allowed = allowedTools?.ToHashSet(StringComparer.Ordinal);
        return async (calls, cancellationToken) =>
            await Task.WhenAll(calls.Select(call => ExecuteAsync(executors, allowed, call, cancellationToken))).ConfigureAwait(false);
    }

    private static async Task<ToolCallOutput> ExecuteAsync(IReadOnlyList<IAgentToolExecutor> executors, HashSet<string>? allowed,
        ToolCallRequest call, CancellationToken cancellationToken)
    {
        if (allowed is not null && !allowed.Contains(call.FunctionName))
        {
            // The model asked for a tool this agent wasn't given - e.g. prompted by injected text.
            throw new InvalidOperationException(string.Format(ErrorMessages.ToolNotAllowedForAgent, call.FunctionName));
        }

        foreach (var executor in executors)
        {
            var output = await executor.TryExecuteAsync(call, cancellationToken).ConfigureAwait(false);
            if (output is not null)
            {
                return new ToolCallOutput(call.CallId, output);
            }
        }

        throw new InvalidOperationException(string.Format(ErrorMessages.NoToolExecutor, call.FunctionName));
    }
}
