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

    public FoundryAgentFactoryTests()
    {
        // By default an agent has no other versions to search; tests that need some set them up.
        _admin.GetAgentVersionsAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<AgentListOrder?>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => new FakeAgentVersionsResult([]));
    }

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
    public async Task DeleteAgentAsync_Succeeds_WhenTheAgentWasAlreadyDeleted_ByAnotherInstance()
    {
        var notFound = NotFoundException();
        _admin.DeleteAgentAsync("my-agent", Arg.Any<CancellationToken>()).Returns(Task.FromException<ClientResult>(notFound));

        Assert.Null(await Record.ExceptionAsync(() => CreateSut().DeleteAgentAsync("my-agent", cancellationToken)));
    }

    [Fact]
    public async Task DeleteAgentAsync_WhileAGetOrCreateHoldsTheSameNamesLock_DoesNotBreakIt()
    {
        SetUpNoExistingAgent("my-agent");
        var releaseCreate = new TaskCompletionSource<ClientResult<ProjectsAgentVersion>>();
        _admin.CreateAgentVersionAsync("my-agent", Arg.Any<ProjectsAgentVersionCreationOptions>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => releaseCreate.Task);
        _admin.DeleteAgentAsync("my-agent", Arg.Any<CancellationToken>()).Returns(Task.FromResult(ClientResult.FromResponse(FakeResponse())));
        var sut = CreateSut();

        var creating = sut.GetOrCreateAsync(new AgentSpec { Name = "my-agent", Instructions = "Do the thing." }, cancellationToken);
        await sut.DeleteAgentAsync("my-agent", cancellationToken);
        releaseCreate.SetResult(ClientResult.FromValue(
            ProjectsAgentsModelFactory.ProjectsAgentVersion(id: "id-1", name: "my-agent", version: "1"), FakeResponse()));

        Assert.Equal("1", (await creating).Version);
    }

    [Fact]
    public async Task PruneVersionsAsync_RefusesToKeepFewerThanOne()
        => await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => CreateSut().PruneVersionsAsync("my-agent", 0, cancellationToken));

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
