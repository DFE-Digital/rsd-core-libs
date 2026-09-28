using Azure.AI.Projects.Agents;
using NSubstitute;
using OpenAI.Responses;
using System.ClientModel;
using System.ClientModel.Primitives;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Integration.Fakes;

/// <summary>
/// An in-memory implementation of Foundry for testing, with a fake <see cref="AgentAdministrationClient"/> that can be used to simulate Foundry behavior without making real network calls.
/// </summary>
internal sealed class InMemoryFoundry
{
    private readonly object _lock = new();
    private readonly Dictionary<string, List<ProjectsAgentVersion>> _agents = new(StringComparer.Ordinal);
    private readonly List<string> _createdNames = [];
    private readonly List<string> _deletedNames = [];
    private readonly Dictionary<string, int> _lastVersion = new(StringComparer.Ordinal);
    private DateTimeOffset _clock = DateTimeOffset.UtcNow.AddDays(-1);

    public AgentAdministrationClient Admin { get; } = Substitute.For<AgentAdministrationClient>();

    /// <summary>When set, agent deletes fail after being recorded - to exercise orphan handling.</summary>
    public bool FailDeletes { get; set; }

    public InMemoryFoundry()
    {
        Admin.GetAgentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => GetAgent(ci.ArgAt<string>(0)));
        Admin.GetAgentVersionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => GetVersion(ci.ArgAt<string>(0), ci.ArgAt<string>(1)));
        Admin.CreateAgentVersionAsync(Arg.Any<string>(), Arg.Any<ProjectsAgentVersionCreationOptions>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => CreateVersion(ci.ArgAt<string>(0), ci.ArgAt<ProjectsAgentVersionCreationOptions>(1).Definition));
        Admin.DeleteAgentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => DeleteAgent(ci.ArgAt<string>(0)));
        Admin.GetAgentsAsync(Arg.Any<ProjectsAgentKind?>(), Arg.Any<int?>(), Arg.Any<AgentListOrder?>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => new InMemoryCollection<ProjectsAgentRecord>(ListAgents()));
        Admin.GetAgentVersionsAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<AgentListOrder?>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(ci => new InMemoryCollection<ProjectsAgentVersion>([.. Versions(ci.ArgAt<string>(0)).Reverse()]));
        Admin.DeleteAgentVersionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => DeleteVersion(ci.ArgAt<string>(0), ci.ArgAt<string>(1)));
    }

    private Task<ClientResult> DeleteVersion(string name, string version)
    {
        lock (_lock)
        {
            // Like Foundry: deleting a version that doesn't exist is a 404.
            if (!_agents.TryGetValue(name, out var versions) || versions.RemoveAll(v => v.Version == version) == 0)
            {
                return Task.FromException<ClientResult>(NotFound());
            }
        }

        return Task.FromResult(ClientResult.FromResponse(FakeResponse()));
    }

    private List<ProjectsAgentRecord> ListAgents()
    {
        lock (_lock)
        {
            return [.. _agents.Keys.Select(name => GetAgent(name).Result.Value)];
        }
    }

    /// <summary>A single-page <see cref="AsyncCollectionResult{T}"/> over an in-memory list.</summary>
    private sealed class InMemoryCollection<T>(IReadOnlyList<T> items) : AsyncCollectionResult<T>
    {
        public override async IAsyncEnumerable<ClientResult> GetRawPagesAsync()
        {
            yield return null!;
            await Task.CompletedTask;
        }

        protected override async IAsyncEnumerable<T> GetValuesFromPageAsync(ClientResult page)
        {
            foreach (var item in items)
            {
                yield return item;
            }

            await Task.CompletedTask;
        }

        public override ContinuationToken GetContinuationToken(ClientResult page) => null!;
    }

    public IReadOnlyList<string> CreatedNames { get { lock (_lock) { return [.. _createdNames]; } } }
    public IReadOnlyList<string> DeletedNames { get { lock (_lock) { return [.. _deletedNames]; } } }
    public IReadOnlyCollection<string> AgentNames { get { lock (_lock) { return [.. _agents.Keys]; } } }

    public IReadOnlyList<ProjectsAgentVersion> Versions(string name)
    {
        lock (_lock)
        {
            return _agents.TryGetValue(name, out var versions) ? [.. versions] : [];
        }
    }

    public DeclarativeAgentDefinition Definition(string name, string version)
        => (DeclarativeAgentDefinition)Versions(name).Single(v => v.Version == version).Definition;

    /// <summary>Adds a version as if another pipeline had provisioned it.</summary>
    public ProjectsAgentVersion Seed(string name, string model, string instructions, params ResponseTool[] tools)
    {
        var definition = new DeclarativeAgentDefinition(model) { Instructions = instructions };
        foreach (var tool in tools)
        {
            definition.Tools.Add(tool);
        }

        return Store(name, definition);
    }

    private Task<ClientResult<ProjectsAgentRecord>> GetAgent(string name)
    {
        lock (_lock)
        {
            if (!_agents.TryGetValue(name, out var versions) || versions.Count == 0)
            {
                return Task.FromException<ClientResult<ProjectsAgentRecord>>(NotFound());
            }

            var latestJson = ModelReaderWriter.Write(versions[^1]).ToString();
            var recordJson = "{\"id\":\"rec-" + name + "\",\"name\":\"" + name + "\",\"versions\":{\"latest\":" + latestJson + "}}";
            var record = ModelReaderWriter.Read<ProjectsAgentRecord>(BinaryData.FromString(recordJson))!;
            return Task.FromResult(ClientResult.FromValue(record, FakeResponse()));
        }
    }

    private Task<ClientResult<ProjectsAgentVersion>> GetVersion(string name, string version)
    {
        lock (_lock)
        {
            var match = _agents.TryGetValue(name, out var versions) ? versions.SingleOrDefault(v => v.Version == version) : null;
            return match is null
                ? Task.FromException<ClientResult<ProjectsAgentVersion>>(NotFound())
                : Task.FromResult(ClientResult.FromValue(match, FakeResponse()));
        }
    }

    private Task<ClientResult<ProjectsAgentVersion>> CreateVersion(string name, ProjectsAgentDefinition definition)
    {
        lock (_lock)
        {
            _createdNames.Add(name);
        }

        return Task.FromResult(ClientResult.FromValue(Store(name, definition), FakeResponse()));
    }

    private ProjectsAgentVersion Store(string name, ProjectsAgentDefinition definition)
    {
        lock (_lock)
        {
            if (!_agents.TryGetValue(name, out var versions))
            {
                versions = [];
                _agents[name] = versions;
            }

            // Like Foundry: version numbers only go up, even after old versions are deleted.
            var next = _lastVersion.GetValueOrDefault(name) + 1;
            _lastVersion[name] = next;
            var number = next.ToString(System.Globalization.CultureInfo.InvariantCulture);
            _clock = _clock.AddSeconds(1);
            var version = ProjectsAgentsModelFactory.ProjectsAgentVersion(id: $"{name}-v{number}", name: name, version: number,
                definition: definition, createdAt: _clock);
            versions.Add(version);
            return version;
        }
    }

    private Task<ClientResult> DeleteAgent(string name)
    {
        lock (_lock)
        {
            _deletedNames.Add(name);
            if (FailDeletes)
            {
                return Task.FromException<ClientResult>(new InvalidOperationException("Foundry delete failed."));
            }

            // Like Foundry: deleting an agent that doesn't exist is a 404.
            if (!_agents.Remove(name))
            {
                return Task.FromException<ClientResult>(NotFound());
            }
        }

        return Task.FromResult(ClientResult.FromResponse(FakeResponse()));
    }

    private static PipelineResponse FakeResponse() => Substitute.For<PipelineResponse>();

    private static ClientResultException NotFound()
    {
        var response = FakeResponse();
        response.Status.Returns(404);
        return new ClientResultException(response, null!);
    }
}
