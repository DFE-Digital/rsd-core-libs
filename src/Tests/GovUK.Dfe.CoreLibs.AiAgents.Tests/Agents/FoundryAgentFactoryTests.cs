using Azure.AI.Projects.Agents;
using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using NSubstitute;
using OpenAI.Responses;
using System.ClientModel;
using System.ClientModel.Primitives;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Agents;

public sealed class FoundryAgentFactoryTests
{
    private readonly CancellationToken cancellationToken = default;
    private readonly AgentAdministrationClient _admin = Substitute.For<AgentAdministrationClient>();
    private readonly FoundryAgentFactoryOptions _options = new("gpt-5.1");

    private FoundryAgentFactory CreateSut() => new(_admin, _options);

    private void SetUpNoExistingAgent(string name)
    {
        var notFound = NotFoundException();
        _admin.GetAgentAsync(name, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ClientResult<ProjectsAgentRecord>>(notFound));
    }

    private void SetUpExistingAgent(string name, string versionId, string model, string instructions, IEnumerable<ResponseTool>? tools = null)
    {
        var record = AgentRecordWithLatestVersion(versionId, name, model, instructions, tools);
        _admin.GetAgentAsync(name, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ClientResult.FromValue(record, FakeResponse())));
    }

    private void SetUpCreatedAgentVersion(string name, string versionId)
    {
        var version = ProjectsAgentsModelFactory.ProjectsAgentVersion(id: versionId, name: name, version: "1");
        _admin.CreateAgentVersionAsync(name, Arg.Any<ProjectsAgentVersionCreationOptions>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ClientResult.FromValue(version, FakeResponse())));
    }

    private void SetUpDeployedVersion(string name, string version, string versionId, string model, string instructions,
        IEnumerable<ResponseTool>? tools = null)
    {
        var definition = new DeclarativeAgentDefinition(model) { Instructions = instructions };
        foreach (var tool in tools ?? [])
        {
            definition.Tools.Add(tool);
        }

        var agentVersion = ProjectsAgentsModelFactory.ProjectsAgentVersion(id: versionId, name: name, version: version, definition: definition);
        _admin.GetAgentVersionAsync(name, version, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ClientResult.FromValue(agentVersion, FakeResponse())));
    }

    private static PipelineResponse FakeResponse() => Substitute.For<PipelineResponse>();

    private static ClientResultException NotFoundException()
    {
        var response = FakeResponse();
        response.Status.Returns(404);
        return new ClientResultException(response, null!);
    }
     
    private static ProjectsAgentRecord AgentRecordWithLatestVersion(string versionId, string name, string model, string instructions,
        IEnumerable<ResponseTool>? tools = null)
    {
        var definition = new DeclarativeAgentDefinition(model) { Instructions = instructions };
        foreach (var tool in tools ?? [])
        {
            definition.Tools.Add(tool);
        }

        var version = ProjectsAgentsModelFactory.ProjectsAgentVersion(id: versionId, name: name, version: "1", definition: definition);
        var versionJson = ModelReaderWriter.Write(version).ToString();
        var recordJson = "{\"id\":\"rec-1\",\"name\":\"" + name + "\",\"versions\":{\"latest\":" + versionJson + "}}";
        return ModelReaderWriter.Read<ProjectsAgentRecord>(BinaryData.FromString(recordJson))!;
    }
    /// <summary>
    /// A fake implementation of AsyncCollectionResult<ProjectsAgentVersion> that returns a predefined list of items.
    /// </summary>
    /// <param name="items"></param>
    private sealed class FakeAgentVersionsResult(IReadOnlyList<ProjectsAgentVersion> items) : AsyncCollectionResult<ProjectsAgentVersion>
    {
        public override async IAsyncEnumerable<ClientResult> GetRawPagesAsync()
        {
            yield return null!;
            await Task.CompletedTask;
        }

        protected override async IAsyncEnumerable<ProjectsAgentVersion> GetValuesFromPageAsync(ClientResult page)
        {
            foreach (var item in items)
            {
                yield return item;
            }
            await Task.CompletedTask;
        }

        public override ContinuationToken GetContinuationToken(ClientResult page) => null!;
    }

    [Fact]
    public async Task GetOrCreateAsync_SecondCallForIdenticalSpec_ReusesFoundryVersion_WithoutCreatingAgain()
    {
        SetUpNoExistingAgent("my-agent");
        SetUpCreatedAgentVersion("my-agent", "id-1");
        var sut = CreateSut();
        var spec = new AgentSpec { Name = "my-agent", Instructions = "Do the thing." };

        var first = await sut.GetOrCreateAsync(spec, cancellationToken);

        // There's no local cache any more - the second call is a fresh Foundry round trip. It
        // reuses the version created by the first call because Foundry itself now reports an
        // agent matching the spec (FindMatchingVersionAsync), not because anything is cached.
        SetUpExistingAgent("my-agent", "id-1", _options.DefaultModel, "Do the thing.");
        var second = await sut.GetOrCreateAsync(spec, cancellationToken);

        Assert.Equal(new AgentReference("id-1", "my-agent", "1"), first);
        Assert.Equal(first, second);
        await _admin.Received(1).CreateAgentVersionAsync("my-agent", Arg.Any<ProjectsAgentVersionCreationOptions>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrCreateAsync_ReusesExistingFoundryAgent_WithoutCreating()
    {
        SetUpExistingAgent("my-agent", "existing-id", _options.DefaultModel, "Do the thing.");

        var sut = CreateSut();
        var spec = new AgentSpec { Name = "my-agent", Instructions = "Do the thing." };

        var result = await sut.GetOrCreateAsync(spec, cancellationToken);

        Assert.Equal(new AgentReference("existing-id", "my-agent", "1"), result);
        await _admin.DidNotReceiveWithAnyArgs().CreateAgentVersionAsync(
            default!, default!, default, cancellationToken);
    }

    [Fact]
    public async Task GetOrCreateAsync_CreatesNewVersion_WhenInstructionsDiffer()
    {
        SetUpExistingAgent("my-agent", "existing-id", _options.DefaultModel, "Old instructions.");
        SetUpCreatedAgentVersion("my-agent", "new-id");

        var sut = CreateSut();
        var spec = new AgentSpec { Name = "my-agent", Instructions = "New instructions." };

        var result = await sut.GetOrCreateAsync(spec, cancellationToken);

        Assert.Equal(new AgentReference("new-id", "my-agent", "1"), result);
    }

    [Fact]
    public async Task GetOrCreateAsync_DoesNotCacheFailure_RetriesOnNextCall()
    {
        SetUpNoExistingAgent("my-agent");
        var version = ProjectsAgentsModelFactory.ProjectsAgentVersion(id: "id-1", name: "my-agent", version: "1");
        _admin.CreateAgentVersionAsync("my-agent", Arg.Any<ProjectsAgentVersionCreationOptions>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException<ClientResult<ProjectsAgentVersion>>(new InvalidOperationException("transient failure")),
                Task.FromResult(ClientResult.FromValue(version, FakeResponse())));

        var sut = CreateSut();
        var spec = new AgentSpec { Name = "my-agent", Instructions = "Do the thing." };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetOrCreateAsync(spec, cancellationToken));

        var result = await sut.GetOrCreateAsync(spec, cancellationToken);

        Assert.Equal(new AgentReference("id-1", "my-agent", "1"), result);
    }

    [Fact]
    public async Task GetOrCreateAsync_ConcurrentCallsForSameSpec_OnlyCreatesOnce()
    {
        // No local cache any more, so this now relies on two things together: the per-name
        // creation guard serialising the check-then-create step (so no two calls create
        // simultaneously), and Foundry itself reporting the newly created version to whichever
        // call runs next (simulated here via createdRecord) - together they mean only the first
        // caller ever creates; every later caller's check finds a match and reuses it.
        ProjectsAgentRecord? createdRecord = null;
        _admin.GetAgentAsync("my-agent", Arg.Any<CancellationToken>())
            .Returns(_ => createdRecord is null
                ? Task.FromException<ClientResult<ProjectsAgentRecord>>(NotFoundException())
                : Task.FromResult(ClientResult.FromValue(createdRecord, FakeResponse())));

        var gate = new TaskCompletionSource<ClientResult<ProjectsAgentVersion>>();
        var callCount = 0;

        _admin.CreateAgentVersionAsync("my-agent", Arg.Any<ProjectsAgentVersionCreationOptions>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                Interlocked.Increment(ref callCount);
                var result = await gate.Task;
                createdRecord = AgentRecordWithLatestVersion(result.Value.Id, "my-agent", _options.DefaultModel, "Do the thing.");
                return result;
            });

        var sut = CreateSut();
        var spec = new AgentSpec { Name = "my-agent", Instructions = "Do the thing." };

        var calls = Enumerable.Range(0, 10).Select(_ => sut.GetOrCreateAsync(spec, cancellationToken)).ToList();

        await Task.Delay(50, cancellationToken);

        var version = ProjectsAgentsModelFactory.ProjectsAgentVersion(id: "id-1", name: "my-agent", version: "1");
        gate.SetResult(ClientResult.FromValue(version, FakeResponse()));

        var results = await Task.WhenAll(calls);

        Assert.All(results, r => Assert.Equal(new AgentReference("id-1", "my-agent", "1"), r));
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task GetOrCreateAsync_CreatesNewVersion_WhenOnlyToolsDiffer()
    {
        var existingTool = ResponseTool.CreateFunctionTool("old_tool", BinaryData.FromString("{}"), strictModeEnabled: null);
        SetUpExistingAgent("my-agent", "existing-id", _options.DefaultModel, "Do the thing.", [existingTool]);
        SetUpCreatedAgentVersion("my-agent", "new-id");

        var sut = CreateSut();
        var newTool = ResponseTool.CreateFunctionTool("new_tool", BinaryData.FromString("{}"), strictModeEnabled: null);
        var spec = new AgentSpec { Name = "my-agent", Instructions = "Do the thing.", Tools = [newTool] };

        var result = await sut.GetOrCreateAsync(spec, cancellationToken);

        Assert.Equal(new AgentReference("new-id", "my-agent", "1"), result);
        await _admin.Received(1).CreateAgentVersionAsync("my-agent", Arg.Any<ProjectsAgentVersionCreationOptions>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrCreateAsync_ReusesExistingVersion_WhenToolsMatch()
    {
        var tool = ResponseTool.CreateFunctionTool("same_tool", BinaryData.FromString("{}"), strictModeEnabled: null);
        SetUpExistingAgent("my-agent", "existing-id", _options.DefaultModel, "Do the thing.", [tool]);

        var sut = CreateSut();
        var spec = new AgentSpec
        {
            Name = "my-agent",
            Instructions = "Do the thing.",
            Tools = [ResponseTool.CreateFunctionTool("same_tool", BinaryData.FromString("{}"), strictModeEnabled: null)],
        };

        var result = await sut.GetOrCreateAsync(spec, cancellationToken);

        Assert.Equal(new AgentReference("existing-id", "my-agent", "1"), result);
        await _admin.DidNotReceiveWithAnyArgs().CreateAgentVersionAsync(default!, default!, default, cancellationToken);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsLatestVersion_WhenNoVersionSpecified()
    {
        SetUpExistingAgent("my-agent", "existing-id", _options.DefaultModel, "Do the thing.");
        var sut = CreateSut();

        var result = await sut.ResolveAsync("my-agent", cancellationToken: cancellationToken);

        Assert.Equal(new AgentReference("existing-id", "my-agent", "1"), result);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsPinnedVersion_WhenVersionSpecified()
    {
        var version = ProjectsAgentsModelFactory.ProjectsAgentVersion(id: "pinned-id", name: "my-agent", version: "2");
        _admin.GetAgentVersionAsync("my-agent", "2", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ClientResult.FromValue(version, FakeResponse())));

        var sut = CreateSut();

        var result = await sut.ResolveAsync("my-agent", version: "2", cancellationToken: cancellationToken);

        Assert.Equal(new AgentReference("pinned-id", "my-agent", "2"), result);
    }

    [Fact]
    public async Task ResolveAsync_Throws_WhenAgentDoesNotExist()
    {
        SetUpNoExistingAgent("my-agent");
        var sut = CreateSut();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ResolveAsync("my-agent", cancellationToken: cancellationToken));
        Assert.Contains("my-agent", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResolveAsync_Throws_WhenPinnedVersionDoesNotExist()
    {
        var notFound = NotFoundException();
        _admin.GetAgentVersionAsync("my-agent", "9", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ClientResult<ProjectsAgentVersion>>(notFound));

        var sut = CreateSut();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ResolveAsync("my-agent", version: "9", cancellationToken: cancellationToken));
        Assert.Contains("my-agent", ex.Message, StringComparison.Ordinal);
        Assert.Contains("9", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResolveLatestAsync_ReturnsLatestVersion_IgnoringAnyPin()
    {
        SetUpExistingAgent("my-agent", "existing-id", _options.DefaultModel, "Do the thing.");
        var sut = CreateSut();

        var result = await sut.ResolveLatestAsync("my-agent", cancellationToken);

        Assert.Equal(new AgentReference("existing-id", "my-agent", "1"), result);
    }

    [Fact]
    public async Task ResolveAsync_NeverCreatesAgent()
    {
        SetUpNoExistingAgent("my-agent");
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ResolveAsync("my-agent", cancellationToken: cancellationToken));

        await _admin.DidNotReceiveWithAnyArgs().CreateAgentVersionAsync(default!, default!, default, cancellationToken);
    }

    [Fact]
    public async Task DeleteAgentAsync_CallsFoundryDelete()
    {
        _admin.DeleteAgentAsync("my-agent", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ClientResult.FromResponse(FakeResponse())));

        var sut = CreateSut();

        await sut.DeleteAgentAsync("my-agent", cancellationToken);

        await _admin.Received(1).DeleteAgentAsync("my-agent", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrCreateAsync_AfterDelete_RecreatesAgent_WhenNoLongerInFoundry()
    {
        SetUpNoExistingAgent("my-agent");
        SetUpCreatedAgentVersion("my-agent", "id-1");
        _admin.DeleteAgentAsync("my-agent", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ClientResult.FromResponse(FakeResponse())));

        var sut = CreateSut();
        var spec = new AgentSpec { Name = "my-agent", Instructions = "Do the thing." };

        var first = await sut.GetOrCreateAsync(spec, cancellationToken);
        await sut.DeleteAgentAsync("my-agent", cancellationToken);

        SetUpNoExistingAgent("my-agent");
        SetUpCreatedAgentVersion("my-agent", "id-2");
        var second = await sut.GetOrCreateAsync(spec, cancellationToken);

        Assert.Equal("id-1", first.Id);
        Assert.Equal("id-2", second.Id);
        await _admin.Received(2).CreateAgentVersionAsync("my-agent", Arg.Any<ProjectsAgentVersionCreationOptions>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PruneVersionsAsync_KeepsNewestVersions_AndDeletesTheRest()
    {
        var now = DateTimeOffset.UtcNow;
        var v1 = ProjectsAgentsModelFactory.ProjectsAgentVersion(id: "id-1", name: "my-agent", version: "1", createdAt: now.AddDays(-3));
        var v2 = ProjectsAgentsModelFactory.ProjectsAgentVersion(id: "id-2", name: "my-agent", version: "2", createdAt: now.AddDays(-2));
        var v3 = ProjectsAgentsModelFactory.ProjectsAgentVersion(id: "id-3", name: "my-agent", version: "3", createdAt: now.AddDays(-1));
        _admin.GetAgentVersionsAsync("my-agent", null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new FakeAgentVersionsResult([v1, v2, v3]));
        _admin.DeleteAgentVersionAsync("my-agent", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ClientResult.FromResponse(FakeResponse())));

        var sut = CreateSut();

        await sut.PruneVersionsAsync("my-agent", keepLatestVersions: 1, cancellationToken);

        await _admin.Received(1).DeleteAgentVersionAsync("my-agent", "1", Arg.Any<CancellationToken>());
        await _admin.Received(1).DeleteAgentVersionAsync("my-agent", "2", Arg.Any<CancellationToken>());
        await _admin.DidNotReceive().DeleteAgentVersionAsync("my-agent", "3", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PruneVersionsAsync_DeletesNothing_WhenVersionCountWithinLimit()
    {
        var v1 = ProjectsAgentsModelFactory.ProjectsAgentVersion(id: "id-1", name: "my-agent", version: "1");
        _admin.GetAgentVersionsAsync("my-agent", null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new FakeAgentVersionsResult([v1]));

        var sut = CreateSut();

        await sut.PruneVersionsAsync("my-agent", keepLatestVersions: 5, cancellationToken);

        await _admin.DidNotReceiveWithAnyArgs().DeleteAgentVersionAsync(default!, default!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MatchesDeployedVersionAsync_ReturnsTrue_WhenTheDeployedVersionMatchesTheSpec()
    {
        SetUpDeployedVersion("my-agent", "3", "id-3", _options.DefaultModel, "Do the thing.");
        var sut = CreateSut();
        var spec = new AgentSpec { Name = "my-agent", Instructions = "Do the thing." };

        var matches = await sut.MatchesDeployedVersionAsync(spec, "3", cancellationToken);

        Assert.True(matches);
    }

    [Fact]
    public async Task MatchesDeployedVersionAsync_ReturnsFalse_WhenInstructionsHaveDrifted()
    {
        SetUpDeployedVersion("my-agent", "3", "id-3", _options.DefaultModel, "Old instructions, pinned a while ago.");
        var sut = CreateSut();
        var spec = new AgentSpec { Name = "my-agent", Instructions = "New instructions from an updated prompt file." };

        var matches = await sut.MatchesDeployedVersionAsync(spec, "3", cancellationToken);

        Assert.False(matches);
    }

    [Fact]
    public async Task MatchesDeployedVersionAsync_ReturnsFalse_WhenToolsHaveDrifted()
    {
        var deployedTool = ResponseTool.CreateFunctionTool("old_tool", BinaryData.FromString("{}"), strictModeEnabled: null);
        SetUpDeployedVersion("my-agent", "3", "id-3", _options.DefaultModel, "Do the thing.", [deployedTool]);
        var sut = CreateSut();
        var newTool = ResponseTool.CreateFunctionTool("new_tool", BinaryData.FromString("{}"), strictModeEnabled: null);
        var spec = new AgentSpec { Name = "my-agent", Instructions = "Do the thing.", Tools = [newTool] };

        var matches = await sut.MatchesDeployedVersionAsync(spec, "3", cancellationToken);

        Assert.False(matches);
    }

    [Fact]
    public async Task MatchesDeployedVersionAsync_ReturnsFalse_WhenThePinnedVersionNoLongerExists()
    {
        var notFound = NotFoundException();
        _admin.GetAgentVersionAsync("my-agent", "9", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ClientResult<ProjectsAgentVersion>>(notFound));
        var sut = CreateSut();
        var spec = new AgentSpec { Name = "my-agent", Instructions = "Do the thing." };

        var matches = await sut.MatchesDeployedVersionAsync(spec, "9", cancellationToken);

        Assert.False(matches);
    }
}
