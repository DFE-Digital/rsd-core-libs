using GovUK.Dfe.CoreLibs.AiAgents.Agents;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Prompts.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Tools;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OpenAI.Responses;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Agents;

public sealed class PinnedAgentVersionDriftValidatorTests
{
    private static readonly AgentDefinition Pinned = new("pinned-agent", "PinnedPromptType");
    private static readonly AgentDefinition Unpinned = new("unpinned-agent", "UnpinnedPromptType");
    private static readonly AgentDefinition Ephemeral = new("ephemeral-agent", "EphemeralPromptType", IsManagedAgent: false);

    private readonly CancellationToken cancellationToken = default;
    private readonly IAgentDefinitionProvider _definitionProvider = Substitute.For<IAgentDefinitionProvider>();
    private readonly IAgentFactory _agentFactory = Substitute.For<IAgentFactory>();
    private readonly IPromptProvider _promptProvider = Substitute.For<IPromptProvider>();

    private readonly AgentVersionPinningOptions _versionPinning = new()
    {
        VersionPins = new Dictionary<string, string> { [Pinned.Name] = "3", [Ephemeral.Name] = "1" },
    };

    private PinnedAgentVersionDriftValidator CreateSut(IEnumerable<IManagedAgentProvider>? managedAgentProviders = null,
        IEnumerable<AgentToolBinding>? toolBindings = null)
        => new(_definitionProvider, _versionPinning, _agentFactory, _promptProvider, managedAgentProviders, toolBindings);

    /// <summary>Configures both checks to report no drift, so a test can focus on just one of them.</summary>
    private void SetUpNoDrift(string agentName, string pinnedVersion)
    {
        _agentFactory.MatchesDeployedVersionAsync(Arg.Any<AgentSpec>(), pinnedVersion, Arg.Any<CancellationToken>()).Returns(true);
        _agentFactory.ResolveLatestAsync(agentName, Arg.Any<CancellationToken>())
            .Returns(new AgentReference($"{agentName}-id", agentName, pinnedVersion));
    }

    [Fact]
    public async Task StartAsync_ChecksEachPinnedManagedDefinition_AgainstItsPinnedVersion_UsingItsCurrentSpec()
    {
        _definitionProvider.GetAgentsDefinitions().Returns([Pinned]);
        _promptProvider.GetSystemPrompt(Pinned.SystemPromptType).Returns("current instructions");
        SetUpNoDrift(Pinned.Name, "3");

        var sut = CreateSut();
        await sut.StartAsync(cancellationToken);

        await _agentFactory.Received(1).MatchesDeployedVersionAsync(
            Arg.Is<AgentSpec>(spec => spec.Name == Pinned.Name && spec.Instructions == "current instructions"),
            "3", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_SkipsEphemeralDefinitions_EvenIfTheyHaveAVersionPinEntry()
    {
        _definitionProvider.GetAgentsDefinitions().Returns([Ephemeral]);

        var sut = CreateSut();
        await sut.StartAsync(cancellationToken);

        await _agentFactory.DidNotReceiveWithAnyArgs().MatchesDeployedVersionAsync(default!, default!, cancellationToken);
        await _agentFactory.DidNotReceiveWithAnyArgs().ResolveLatestAsync(default!, cancellationToken);
    }

    [Fact]
    public async Task StartAsync_SkipsUnpinnedDefinitions()
    {
        _definitionProvider.GetAgentsDefinitions().Returns([Unpinned]);

        var sut = CreateSut();
        await sut.StartAsync(cancellationToken);

        await _agentFactory.DidNotReceiveWithAnyArgs().MatchesDeployedVersionAsync(default!, default!, cancellationToken);
        await _agentFactory.DidNotReceiveWithAnyArgs().ResolveLatestAsync(default!, cancellationToken);
    }

    [Fact]
    public async Task StartAsync_SkipsDefinitionsWithACustomManagedAgentProvider()
    {
        _definitionProvider.GetAgentsDefinitions().Returns([Pinned]);
        var customProvider = Substitute.For<IManagedAgentProvider>();
        customProvider.AgentName.Returns(Pinned.Name);

        var sut = CreateSut(managedAgentProviders: [customProvider]);
        await sut.StartAsync(cancellationToken);

        await _agentFactory.DidNotReceiveWithAnyArgs().MatchesDeployedVersionAsync(default!, default!, cancellationToken);
        await _agentFactory.DidNotReceiveWithAnyArgs().ResolveLatestAsync(default!, cancellationToken);
    }

    [Fact]
    public async Task StartAsync_DoesNotThrow_WhenTheDeployedVersionHasDrifted()
    {
        _definitionProvider.GetAgentsDefinitions().Returns([Pinned]);
        _agentFactory.MatchesDeployedVersionAsync(Arg.Any<AgentSpec>(), "3", Arg.Any<CancellationToken>()).Returns(false);
        _agentFactory.ResolveLatestAsync(Pinned.Name, Arg.Any<CancellationToken>())
            .Returns(new AgentReference($"{Pinned.Name}-id", Pinned.Name, "3"));

        var sut = CreateSut();
        var exception = await Record.ExceptionAsync(() => sut.StartAsync(cancellationToken));

        Assert.Null(exception);
    }

    [Fact]
    public async Task StartAsync_DoesNotThrow_WhenTheDriftCheckItselfFails()
    {
        _definitionProvider.GetAgentsDefinitions().Returns([Pinned]);
        _agentFactory.MatchesDeployedVersionAsync(Arg.Any<AgentSpec>(), "3", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Foundry unavailable"));
        _agentFactory.ResolveLatestAsync(Pinned.Name, Arg.Any<CancellationToken>())
            .Returns(new AgentReference($"{Pinned.Name}-id", Pinned.Name, "3"));

        var sut = CreateSut();
        var exception = await Record.ExceptionAsync(() => sut.StartAsync(cancellationToken));

        Assert.Null(exception);
    }

    [Fact]
    public async Task StartAsync_IncludesToolsFromRegisteredBindings_InTheComparedSpec()
    {
        _definitionProvider.GetAgentsDefinitions().Returns([Pinned]);
        var tool = ResponseTool.CreateWebSearchTool();
        var toolProvider = Substitute.For<IAgentToolProvider>();
        toolProvider.GetToolsAsync(Arg.Any<CancellationToken>()).Returns([tool]);
        SetUpNoDrift(Pinned.Name, "3");

        var sut = CreateSut(toolBindings: [new AgentToolBinding(Pinned.Name, toolProvider)]);
        await sut.StartAsync(cancellationToken);

        await _agentFactory.Received(1).MatchesDeployedVersionAsync(
            Arg.Is<AgentSpec>(spec => spec.Tools.Count == 1 && spec.Tools[0] == tool), "3", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_ChecksTheLatestVersion_ForEachPinnedManagedDefinition()
    {
        _definitionProvider.GetAgentsDefinitions().Returns([Pinned]);
        SetUpNoDrift(Pinned.Name, "3");

        var sut = CreateSut();
        await sut.StartAsync(cancellationToken);

        await _agentFactory.Received(1).ResolveLatestAsync(Pinned.Name, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_DoesNotThrow_WhenThePinIsBehindTheLatestVersion_EvenIfItsOwnContentStillMatches()
    {
        _definitionProvider.GetAgentsDefinitions().Returns([Pinned]);
        _agentFactory.MatchesDeployedVersionAsync(Arg.Any<AgentSpec>(), "3", Arg.Any<CancellationToken>()).Returns(true);
        _agentFactory.ResolveLatestAsync(Pinned.Name, Arg.Any<CancellationToken>())
            .Returns(new AgentReference($"{Pinned.Name}-id", Pinned.Name, "5"));

        var sut = CreateSut();
        var exception = await Record.ExceptionAsync(() => sut.StartAsync(cancellationToken));

        Assert.Null(exception);
    }

    [Fact]
    public async Task StartAsync_DoesNotThrow_WhenResolvingTheLatestVersionFails()
    {
        _definitionProvider.GetAgentsDefinitions().Returns([Pinned]);
        _agentFactory.MatchesDeployedVersionAsync(Arg.Any<AgentSpec>(), "3", Arg.Any<CancellationToken>()).Returns(true);
        _agentFactory.ResolveLatestAsync(Pinned.Name, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Foundry unavailable"));

        var sut = CreateSut();
        var exception = await Record.ExceptionAsync(() => sut.StartAsync(cancellationToken));

        Assert.Null(exception);
    }

    [Fact]
    public async Task StopAsync_CompletesWithoutError()
    {
        var sut = CreateSut();

        var exception = await Record.ExceptionAsync(() => sut.StopAsync(cancellationToken));

        Assert.Null(exception);
    }
}
