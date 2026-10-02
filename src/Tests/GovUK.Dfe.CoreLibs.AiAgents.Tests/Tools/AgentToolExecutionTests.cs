using GovUK.Dfe.CoreLibs.AiAgents.Tools;
using GovUK.Dfe.CoreLibs.AiAgents.Tools.WebSearch;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using NSubstitute;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Tools;

public sealed class AgentToolExecutionTests
{
    private readonly CancellationToken cancellationToken = default;

    /// <summary>A provider that both describes and runs its tools, as McpToolClient does.</summary>
    public interface IExecutingProvider : IAgentToolProvider, IAgentToolExecutor;

    /// <summary>A provider that owns exactly one function and declines every other call.</summary>
    private static IExecutingProvider Owning(string functionName, string output)
    {
        var provider = Substitute.For<IExecutingProvider>();
        provider.TryExecuteAsync(Arg.Any<ToolCallRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<ToolCallRequest>().FunctionName == functionName ? output : null);
        return provider;
    }

    [Fact]
    public void CreateResolver_ReturnsNull_WhenNoProviderRunsToolsInThisApp()
        => Assert.Null(AgentToolExecution.CreateResolver([new WebSearchToolProvider()]));

    [Fact]
    public async Task CreateResolver_RunsEachCall_OnTheProviderThatOwnsIt()
    {
        var performance = Owning("get_performance_data", "72%");
        var ofsted = Owning("get_ofsted_rating", "Good");

        var resolve = AgentToolExecution.CreateResolver([new WebSearchToolProvider(), performance, ofsted])!;
        var outputs = (await resolve(
            [new ToolCallRequest("call-1", "get_performance_data", "{}"), new ToolCallRequest("call-2", "get_ofsted_rating", "{}")],
            cancellationToken)).ToList();

        Assert.Equal([new ToolCallOutput("call-1", "72%"), new ToolCallOutput("call-2", "Good")], outputs);
    }
}
