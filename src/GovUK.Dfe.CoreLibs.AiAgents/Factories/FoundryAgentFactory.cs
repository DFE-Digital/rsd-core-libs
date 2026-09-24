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
    ILogger<FoundryAgentFactory>? logger = null) : IAgentFactory
{
    private readonly ILogger<FoundryAgentFactory> _logger = logger ?? NullLogger<FoundryAgentFactory>.Instance;
     
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _creationGates = new();

    public async Task<AgentReference> GetOrCreateAsync(AgentSpec spec, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentException.ThrowIfNullOrWhiteSpace(spec.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(spec.Instructions);

        var gate = _creationGates.GetOrAdd(spec.Name, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await GetOrCreateOnFoundryAsync(spec, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get or create Foundry agent for {AgentName}", spec.Name);
            throw;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<AgentReference> ResolveAsync(string name, string? version = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        try
        {
            if (version is not null)
            {
                var pinned = (await administrationClient.GetAgentVersionAsync(name, version, cancellationToken).ConfigureAwait(false)).Value;
                return new AgentReference(pinned.Id, name, pinned.Version);
            }

            var record = (await administrationClient.GetAgentAsync(name, cancellationToken).ConfigureAwait(false)).Value;
            var latest = record.GetLatestVersion();
            return new AgentReference(latest.Id, name, latest.Version);
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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to delete Foundry agent {AgentName}", name);
            throw;
        }
        finally
        {
            if (_creationGates.TryRemove(name, out var gate))
            {
                gate.Dispose();
            }
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

    public async Task PruneVersionsAsync(string name, int keepLatestVersions, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(keepLatestVersions);

        try
        {
            var versions = new List<ProjectsAgentVersion>();
            await foreach (var version in administrationClient.GetAgentVersionsAsync(name, limit: null, order: null,
                after: null, before: null, cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                versions.Add(version);
            }

            var toDelete = versions.OrderByDescending(v => v.CreatedAt).Skip(keepLatestVersions);
            foreach (var version in toDelete)
            {
                await administrationClient.DeleteAgentVersionAsync(name, version.Version, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to prune versions for Foundry agent {AgentName}", name);
            throw;
        }
    }

    private async Task<AgentReference> GetOrCreateOnFoundryAsync(AgentSpec spec, CancellationToken cancellationToken)
    {
        var existing = await FindMatchingVersionAsync(spec, cancellationToken);
        if (existing is not null)
        {
            return new AgentReference(existing.Id, spec.Name, existing.Version);
        }

        return await CreateAgentVersionAsync(spec, cancellationToken);
    }

    /// <summary>
    /// Finds an existing version of the agent named <c>spec.Name</c> on Foundry that matches the given spec.
    /// </summary>
    /// <param name="spec"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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
        return SpecMatchesDefinition(spec, model, latest.Definition) ? latest : null;
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
            && ToolsMatch(definition.Tools, spec.Tools);

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
            throw;
        }
    }

    private static string ToolSignature(OpenAI.Responses.ResponseTool tool) => ModelReaderWriter.Write(tool).ToString();
}
