using Azure.AI.Projects.Agents;
using GovUK.Dfe.CoreLibs.AiAgents.Constants;
using GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Concurrent;
using System.Net;

namespace GovUK.Dfe.CoreLibs.AiAgents.Factories;

public sealed class FoundryAgentFactory(AgentAdministrationClient administrationClient, FoundryAgentFactoryOptions options,
    ILogger<FoundryAgentFactory>? logger = null, AgentVersionPinningOptions? versionPinning = null, TimeProvider? timeProvider = null)
    : IAgentFactory
{
    private readonly AgentReferenceCache _cache = new(options.AgentCacheDuration, timeProvider ?? TimeProvider.System);

    /// <summary>
    /// How many recent versions to search for one matching a spec when the latest doesn't. Covers
    /// several apps or deployments sharing an agent name, and duplicates created by instances racing.
    /// </summary>
    private const int RecentVersionsToSearch = 50;

    private readonly ILogger<FoundryAgentFactory> _logger = logger ?? NullLogger<FoundryAgentFactory>.Instance;
    private readonly AgentVersionPinningOptions _versionPinning = versionPinning ?? new AgentVersionPinningOptions();

    // Per process only: stops this instance creating duplicate versions. Instances racing each other
    // can still each create one identical version; FindMatchingVersionAsync then reuses them rather
    // than creating more, so the duplicates are harmless.
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _creationGates = new();

    public async Task<AgentReference> GetOrCreateAsync(AgentSpec spec, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentException.ThrowIfNullOrWhiteSpace(spec.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(spec.Instructions);

        // Ephemeral agents are used once, so there's nothing to reuse.
        var cacheKey = Agents.AgentRuntime.IsEphemeralName(spec.Name) ? null : SpecSignature(spec);
        if (cacheKey is not null && _cache.TryGet(spec.Name, cacheKey) is { } cached)
        {
            return cached;
        }

        var gate = _creationGates.GetOrAdd(spec.Name, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var agent = await GetOrCreateOnFoundryAsync(spec, cancellationToken).ConfigureAwait(false);
            if (cacheKey is not null)
            {
                _cache.Set(spec.Name, cacheKey, agent);
            }

            return agent;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to get or create Foundry agent for {AgentName}", spec.Name);
            throw new InvalidOperationException(string.Format(ErrorMessages.AgentGetOrCreateFailed, spec.Name), ex);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<AgentReference> ResolveAsync(string name, string? version = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var cacheKey = "version:" + (version ?? "latest");
        if (_cache.TryGet(name, cacheKey) is { } cached)
        {
            return cached;
        }

        try
        {
            AgentReference resolved;
            if (version is not null)
            {
                var pinned = (await administrationClient.GetAgentVersionAsync(name, version, cancellationToken).ConfigureAwait(false)).Value;
                resolved = new AgentReference(pinned.Id, name, pinned.Version);
            }
            else
            {
                var latest = (await administrationClient.GetAgentAsync(name, cancellationToken).ConfigureAwait(false)).Value.GetLatestVersion();
                resolved = new AgentReference(latest.Id, name, latest.Version);
            }

            _cache.Set(name, cacheKey, resolved);
            return resolved;
        }
        catch (ClientResultException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            var versionSuffix = version is null ? string.Empty : string.Format(ErrorMessages.AgentVersionNotFound, version);
            throw new InvalidOperationException(string.Format(ErrorMessages.AgentNotFound, name, versionSuffix));
        }
    }

    public Task<AgentReference> ResolveLatestAsync(string name, CancellationToken cancellationToken = default)
        => ResolveAsync(name, version: null, cancellationToken);

    public async Task DeleteAgentAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        try
        {
            await administrationClient.DeleteAgentAsync(name, cancellationToken).ConfigureAwait(false);
        }
        catch (ClientResultException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            // Already gone - e.g. another instance's clean-up got there first. Deleting is the goal, so this succeeds.
            _logger.LogDebug(ex, "Foundry agent {AgentName} was already deleted", name);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to delete Foundry agent {AgentName}", name);
            throw new InvalidOperationException(string.Format(ErrorMessages.AgentDeleteFailed, name), ex);
        }
        finally
        {
            // Removed, not disposed: a GetOrCreateAsync for the same name may still be waiting on or
            // holding it, and disposing would make its Release throw.
            _creationGates.TryRemove(name, out _);
            _cache.Remove(name);
        }
    }

    public async Task<bool> MatchesDeployedVersionAsync(AgentSpec spec, string version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentException.ThrowIfNullOrWhiteSpace(spec.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        try
        {
            var deployed = (await administrationClient.GetAgentVersionAsync(spec.Name, version, cancellationToken).ConfigureAwait(false)).Value;
            return SpecMatchesDefinition(spec, spec.Model ?? options.DefaultModel, deployed.Definition);
        }
        catch (ClientResultException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    /// <summary>
    /// Deletes all but the newest <paramref name="keepLatestVersions"/> versions - e.g. 3 keeps 5, 4 and 3 and
    /// deletes 1 and 2. Does nothing for an agent this environment has pinned (it only uses that agent), and
    /// never deletes a protected version.
    /// </summary>
    public async Task PruneVersionsAsync(string name, int keepLatestVersions, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(keepLatestVersions, 1);

        if (_versionPinning.GetPinnedVersion(name) is { } pinned)
        {
            // This environment only uses the agent, at a version it chose; its versions are managed elsewhere.
            _logger.LogInformation("Not pruning {AgentName}: this environment is pinned to version {Version}", name, pinned);
            return;
        }

        try
        {
            var versions = new List<ProjectsAgentVersion>();
            await foreach (var version in administrationClient.GetAgentVersionsAsync(name, limit: null, order: null,
                after: null, before: null, cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                versions.Add(version);
            }

            var toDelete = versions.OrderByDescending(v => v.CreatedAt).Skip(keepLatestVersions).Select(v => v.Version);
            foreach (var version in toDelete)
            {
                if (_versionPinning.IsProtected(name, version))
                {
                    _logger.LogInformation("Keeping version {Version} of {AgentName} because it's protected", version, name);
                    continue;
                }

                await DeleteVersionAsync(name, version, cancellationToken).ConfigureAwait(false);
                _cache.Remove(name);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to prune versions for Foundry agent {AgentName}", name);
            throw new InvalidOperationException(string.Format(ErrorMessages.AgentPruneVersionsFailed, name), ex);
        }
    }

    public async Task<IReadOnlyList<string>> GetFunctionToolNamesAsync(string name, string? version = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        try
        {
            var deployed = version is null
                ? (await administrationClient.GetAgentAsync(name, cancellationToken).ConfigureAwait(false)).Value.GetLatestVersion()
                : (await administrationClient.GetAgentVersionAsync(name, version, cancellationToken).ConfigureAwait(false)).Value;

            return deployed.Definition is DeclarativeAgentDefinition definition
                ? [.. definition.Tools.OfType<OpenAI.Responses.FunctionTool>().Select(tool => tool.FunctionName)]
                : [];
        }
        catch (ClientResultException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            var versionSuffix = version is null ? string.Empty : string.Format(ErrorMessages.AgentVersionNotFound, version);
            throw new InvalidOperationException(string.Format(ErrorMessages.AgentNotFound, name, versionSuffix), ex);
        }
    }

    private async Task DeleteVersionAsync(string name, string version, CancellationToken cancellationToken)
    {
        try
        {
            await administrationClient.DeleteAgentVersionAsync(name, version, cancellationToken).ConfigureAwait(false);
        }
        catch (ClientResultException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            // Another instance pruned it first.
            _logger.LogDebug(ex, "Version {Version} of {AgentName} was already deleted", version, name);
        }
    }

    /// <summary>
    /// After this app creates a new version, keeps only the newest <see cref="FoundryAgentFactoryOptions.KeepLatestVersions"/>.
    /// Best effort: a failure is logged and never undoes or fails the creation.
    /// </summary>
    private async Task PruneAfterCreateAsync(string name, CancellationToken cancellationToken)
    {
        if (options.KeepLatestVersions is not int keep || Agents.AgentRuntime.IsEphemeralName(name))
        {
            return;
        }

        try
        {
            await PruneVersionsAsync(name, keep, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Created a new version of {AgentName} but couldn't prune old versions; will retry after the next one", name);
        }
    }

    public async Task<IReadOnlyList<string>> DeleteStaleAgentsAsync(Func<string, bool> isCandidate, TimeSpan minimumAge,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(isCandidate);

        var cutoff = DateTimeOffset.UtcNow - minimumAge;
        var stale = new List<string>();
        await foreach (var record in administrationClient.GetAgentsAsync(kind: null, limit: null, order: null, after: null, before: null,
            cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            if (isCandidate(record.Name) && record.GetLatestVersion()?.CreatedAt < cutoff)
            {
                stale.Add(record.Name);
            }
        }

        var deleted = new List<string>(stale.Count);
        foreach (var name in stale)
        {
            try
            {
                await DeleteAgentAsync(name, cancellationToken).ConfigureAwait(false);
                deleted.Add(name);
            }
            catch (InvalidOperationException ex)
            {
                // Already logged by DeleteAgentAsync; keep going so one failure doesn't block the rest.
                _logger.LogDebug(ex, "Skipping stale agent {AgentName} after a failed delete", name);
            }
        }

        if (deleted.Count > 0)
        {
            _logger.LogInformation("Deleted {Count} stale Foundry agent(s): {AgentNames}", deleted.Count, string.Join(", ", deleted));
        }

        return deleted;
    }

    private async Task<AgentReference> GetOrCreateOnFoundryAsync(AgentSpec spec, CancellationToken cancellationToken)
    {
        var existing = await FindMatchingVersionAsync(spec, cancellationToken);
        if (existing is not null)
        {
            return new AgentReference(existing.Id, spec.Name, existing.Version);
        }

        var created = await CreateAgentVersionAsync(spec, cancellationToken).ConfigureAwait(false);
        await PruneAfterCreateAsync(spec.Name, cancellationToken).ConfigureAwait(false);
        return created;
    }

    /// <summary>
    /// Finds an existing version of the agent named <c>spec.Name</c> that matches the spec: the latest
    /// version if it matches (one call), otherwise the most recent matching version among the last
    /// <see cref="RecentVersionsToSearch"/>. Searching beyond the latest stops two apps or deployments
    /// that share a name from creating a new version on every call as each overtakes the other.
    /// </summary>
    private async Task<ProjectsAgentVersion?> FindMatchingVersionAsync(AgentSpec spec, CancellationToken cancellationToken)
    {
        var model = spec.Model ?? options.DefaultModel;

        ProjectsAgentRecord record;
        try
        {
            record = (await administrationClient.GetAgentAsync(spec.Name, cancellationToken).ConfigureAwait(false)).Value;
        }
        catch (ClientResultException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            return null;
        }

        var latest = record.GetLatestVersion();
        if (SpecMatchesDefinition(spec, model, latest.Definition))
        {
            return latest;
        }

        var searched = 0;
        await foreach (var version in administrationClient.GetAgentVersionsAsync(spec.Name, limit: RecentVersionsToSearch,
            order: AgentListOrder.Descending, after: null, before: null, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            if (SpecMatchesDefinition(spec, model, version.Definition))
            {
                _logger.LogInformation("Reusing version {Version} of {AgentName}, which matches this spec but isn't the latest",
                    version.Version, spec.Name);
                return version;
            }

            if (++searched >= RecentVersionsToSearch)
            {
                break;
            }
        }

        return null;
    }

    /// <summary>
    /// Compares a spec's content (model, instructions, tools) against a deployed version's definition -
    /// shared by <see cref="FindMatchingVersionAsync"/> (against the latest version) and
    /// <see cref="MatchesDeployedVersionAsync"/> (against a specific pinned version).
    /// </summary>
    private static bool SpecMatchesDefinition(AgentSpec spec, string model, ProjectsAgentDefinition? deployedDefinition)
        => deployedDefinition is DeclarativeAgentDefinition definition
            && definition.Model == model
            && definition.Instructions == spec.Instructions
            && ToolsMatch(definition.Tools, spec.Tools)
            && OutputFormatMatches(definition.TextOptions, spec.OutputSchema);

    /// <summary>
    /// Compares the structured-output format. Plain text and no format are treated as the same (Foundry
    /// may report either for a version created without one), and schemas are compared as JSON values so
    /// whitespace or property order never causes a new version.
    /// </summary>
    private static bool OutputFormatMatches(OpenAI.Responses.ResponseTextOptions? deployed, AgentOutputSchema? expected)
    {
        var deployedFormat = deployed?.TextFormat;
        var deployedIsPlainText = deployedFormat is null || deployedFormat.Kind == OpenAI.Responses.ResponseTextFormatKind.Text;

        if (expected is null)
        {
            return deployedIsPlainText;
        }

        return !deployedIsPlainText && System.Text.Json.Nodes.JsonNode.DeepEquals(
            System.Text.Json.Nodes.JsonNode.Parse(ModelReaderWriter.Write(deployedFormat!).ToString()),
            System.Text.Json.Nodes.JsonNode.Parse(ModelReaderWriter.Write(ToTextOptions(expected).TextFormat).ToString()));
    }

    internal static OpenAI.Responses.ResponseTextOptions ToTextOptions(AgentOutputSchema schema) => new()
    {
        TextFormat = OpenAI.Responses.ResponseTextFormat.CreateJsonSchemaFormat(schema.Name, BinaryData.FromString(schema.JsonSchema),
            schema.Description, jsonSchemaIsStrict: true),
    };

    // Compared by serialized signature, in order - this is what closes the gap where changing only
    // a spec's Tools used to silently reuse a version created for a different tool set.
    private static bool ToolsMatch(IEnumerable<OpenAI.Responses.ResponseTool> deployedTools, IEnumerable<OpenAI.Responses.ResponseTool> specTools)
        => deployedTools.Select(ToolSignature).SequenceEqual(specTools.Select(ToolSignature));

    /// <summary>
    /// Creates a new version of the agent named <c>spec.Name</c> on Foundry based on the given spec.
    /// </summary>
    /// <param name="spec">The agent specification to use for creation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created agent reference.</returns>
    private async Task<AgentReference> CreateAgentVersionAsync(AgentSpec spec, CancellationToken cancellationToken)
    {
        try
        {
            var definition = new DeclarativeAgentDefinition(model: spec.Model ?? options.DefaultModel)
            {
                Instructions = spec.Instructions,
                TextOptions = spec.OutputSchema is null ? null : ToTextOptions(spec.OutputSchema),
            };
            foreach (var tool in spec.Tools)
            {
                definition.Tools.Add(tool);
            }

            var createOptions = new ProjectsAgentVersionCreationOptions(definition) { Description = spec.Description };
            var version = (await administrationClient.CreateAgentVersionAsync(spec.Name, createOptions, null, cancellationToken)
                .ConfigureAwait(false)).Value;

            return new AgentReference(version.Id, spec.Name, version.Version);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to create Foundry agent for {AgentName}", spec.Name);
            throw new InvalidOperationException(string.Format(ErrorMessages.AgentCreateFailed, spec.Name), ex);
        }
    }

    private static string ToolSignature(OpenAI.Responses.ResponseTool tool) => ModelReaderWriter.Write(tool).ToString();

    /// <summary>A hash of everything that decides the version: model, instructions, tools and output schema.</summary>
    private string SpecSignature(AgentSpec spec)
    {
        var content = string.Join('\u001f', [spec.Model ?? options.DefaultModel, spec.Instructions,
            .. spec.Tools.Select(ToolSignature), spec.OutputSchema?.Name ?? string.Empty, spec.OutputSchema?.JsonSchema ?? string.Empty]);
        return "spec:" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(content)));
    }
}
