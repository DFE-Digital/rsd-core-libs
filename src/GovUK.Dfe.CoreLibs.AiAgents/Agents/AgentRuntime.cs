using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Orchestration.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents;

public sealed class AgentRuntime(IAgentFactory factory, IAgentRunner runner, IAgentOrchestrator orchestrator,
    AgentVersionPinningOptions? versionPinning = null, ILogger<AgentRuntime>? logger = null, AgentRunOptions? runOptions = null)
    : IAgentRuntime
{
    private const int EphemeralSuffixLength = 32;
    private const int ApplicationHashLength = 8;

    /// <summary>How long deleting an ephemeral agent may take once its run has finished (or been cancelled).</summary>
    private static readonly TimeSpan EphemeralCleanupTimeout = TimeSpan.FromSeconds(30);

    /// <summary>The longest a run is assumed to take when no <see cref="AgentRunOptions.RunTimeout"/> is set.</summary>
    private static readonly TimeSpan AssumedMaximumRunTime = TimeSpan.FromHours(1);

    private readonly AgentVersionPinningOptions _versionPinning = versionPinning ?? new AgentVersionPinningOptions();
    private readonly ILogger<AgentRuntime> _logger = logger ?? NullLogger<AgentRuntime>.Instance;
    private readonly AgentRunOptions _runOptions = runOptions ?? new AgentRunOptions();

    /// <summary>
    /// Identifies this application's ephemeral agents in a shared Foundry project, so its orphan sweep
    /// never deletes another application's agents mid-run.
    /// </summary>
    private string ApplicationHash => ApplicationHashOf(_runOptions.ApplicationName);

    /// <summary>
    /// The smallest <c>minimumAge</c> the orphan sweep accepts: longer than any run can take plus its
    /// clean-up, so an agent that's still in use (on any instance) is never treated as orphaned.
    /// </summary>
    internal TimeSpan MinimumOrphanAge => (_runOptions.RunTimeout ?? AssumedMaximumRunTime) + EphemeralCleanupTimeout + TimeSpan.FromMinutes(5);

    public IAgentOrchestrator Orchestrator => orchestrator;

    public Task<AgentReference> ResolveAsync(string agentName, CancellationToken cancellationToken)
        => factory.ResolveAsync(agentName, _versionPinning.GetPinnedVersion(agentName), cancellationToken);

    public Task<AgentReference> ResolveAsync(AgentReference created, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(created);

        var pinnedVersion = _versionPinning.GetPinnedVersion(created.Name);
        return pinnedVersion is null
            ? Task.FromResult(created)
            : factory.ResolveAsync(created.Name, pinnedVersion, cancellationToken);
    }

    public async Task<AgentReference> GetOrCreateAsync(string agentName, Func<CancellationToken, Task<AgentSpec>> buildSpec,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);
        ArgumentNullException.ThrowIfNull(buildSpec);

        if (_versionPinning.GetPinnedVersion(agentName) is { } pinnedVersion)
        {
            return await factory.ResolveAsync(agentName, pinnedVersion, cancellationToken).ConfigureAwait(false);
        }

        var spec = await buildSpec(cancellationToken).ConfigureAwait(false);
        return await factory.GetOrCreateAsync(spec, cancellationToken).ConfigureAwait(false);
    }

    public Task<AgentResult> RunEphemeralAsync(AgentSpec spec, string prompt, CancellationToken cancellationToken = default)
        => RunEphemeralAsync(spec, prompt, resolveToolCalls: null, evidence: null, cancellationToken: cancellationToken);

    public async Task<AgentResult> RunEphemeralAsync(AgentSpec spec, string prompt,
        ToolCallResolver? resolveToolCalls,
        string? evidence = null, Func<AgentResult, string?>? validateOutput = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var reportedName = spec.Name;
        // <name>-<8 hex: this application><24 hex: random>: unique per run, and recognisable as this
        // application's so the orphan sweep can tell its own agents apart in a shared project.
        var randomPart = Guid.NewGuid().ToString("N")[..(EphemeralSuffixLength - ApplicationHashLength)];
        var ephemeralSpec = spec with { Name = $"{spec.Name}-{ApplicationHash}{randomPart}" };

        try
        {
            var result = await runner.RunAsync(ephemeralSpec, prompt, resolveToolCalls: resolveToolCalls, cancellationToken: cancellationToken,
                    additionalContext: evidence, validateOutput: validateOutput)
                .ConfigureAwait(false);
            return result with { AgentName = reportedName, AgentVersion = null };
        }
        finally
        {
            // Its own budget rather than the caller's token, so a cancelled run still cleans up.
            using var cleanupSource = new CancellationTokenSource(EphemeralCleanupTimeout);
            try
            {
                await factory.DeleteAgentAsync(ephemeralSpec.Name, cleanupSource.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete ephemeral agent {AgentName}; it may be orphaned in Foundry.", ephemeralSpec.Name);
            }
        }
    }

    public Task<IReadOnlyList<string>> DeleteOrphanedEphemeralAgentsAsync(CancellationToken cancellationToken = default)
        => DeleteOrphanedEphemeralAgentsAsync(MinimumOrphanAge, cancellationToken);

    public Task<IReadOnlyList<string>> DeleteOrphanedEphemeralAgentsAsync(TimeSpan minimumAge, CancellationToken cancellationToken = default)
    {
        // Every instance may run this sweep at once: deletes of an agent another instance already removed succeed.
        ArgumentOutOfRangeException.ThrowIfLessThan(minimumAge, MinimumOrphanAge);

        var ownPrefix = ApplicationHash;
        return factory.DeleteStaleAgentsAsync(
            name => IsEphemeralName(name) && name[^EphemeralSuffixLength..].StartsWith(ownPrefix, StringComparison.Ordinal),
            minimumAge, cancellationToken);
    }

    internal static string ApplicationHashOf(string applicationName)
        => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(applicationName)))
            [..ApplicationHashLength].ToLowerInvariant();

    /// <summary>Matches the <c>&lt;name&gt;-&lt;Guid:N&gt;</c> names <see cref="RunEphemeralAsync"/> creates.</summary>
    internal static bool IsEphemeralName(string agentName)
        => agentName.Length > EphemeralSuffixLength + 1
           && agentName[^(EphemeralSuffixLength + 1)] == '-'
           && Guid.TryParseExact(agentName[^EphemeralSuffixLength..], "N", out _);
}
