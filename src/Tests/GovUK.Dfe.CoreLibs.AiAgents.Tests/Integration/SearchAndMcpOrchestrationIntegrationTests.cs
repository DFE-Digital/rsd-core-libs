using Azure;
using Azure.AI.Projects.Agents;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using GovUK.Dfe.CoreLibs.AiAgents.Context;
using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using GovUK.Dfe.CoreLibs.AiAgents.Orchestration;
using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp.Interfaces;
using NSubstitute;
using OpenAI.Responses;
using System.ClientModel;
using System.ClientModel.Primitives;
using Xunit;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Agents;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Integration;

public sealed class SearchAndMcpOrchestrationIntegrationTests
{
    private const string ConversationId = "conversation-1";

    private readonly CancellationToken cancellationToken = default;
    private readonly AgentAdministrationClient _admin = Substitute.For<AgentAdministrationClient>();
    private readonly IFoundryConversationClient _conversationClient = Substitute.For<IFoundryConversationClient>();
    private readonly IMcpToolClient _mcpToolClient = Substitute.For<IMcpToolClient>();
    private readonly FoundryAgentFactoryOptions _options = new("gpt-4o");

    private FoundryAgentFactory? _factory;

    private FoundryAgentRunner CreateRunner()
    {
        _conversationClient.CreateConversationAsync(Arg.Any<CancellationToken>()).Returns(ConversationId);
        _factory = new FoundryAgentFactory(_admin, _options);
        return new FoundryAgentRunner(_factory, _conversationClient);
    }

    private void SetUpNewAgent(string name)
    {
        var notFound = NotFoundException();
        _admin.GetAgentAsync(name, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ClientResult<ProjectsAgentRecord>>(notFound));

        var version = ProjectsAgentsModelFactory.ProjectsAgentVersion(id: $"{name}-v1", name: name, version: "1");
        _admin.CreateAgentVersionAsync(name, Arg.Any<ProjectsAgentVersionCreationOptions>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ClientResult.FromValue(version, FakeResponse())));
    }

    private static PipelineResponse FakeResponse() => Substitute.For<PipelineResponse>();

    private static ClientResultException NotFoundException()
    {
        var response = FakeResponse();
        response.Status.Returns(404);
        return new ClientResultException(response, null!);
    }

    private static ResponseResult ResponseWithOutputItems(string id, IEnumerable<ResponseItem> items, int? totalTokens = null, string status = "completed")
    {
        var itemsJson = string.Join(',', items.Select(item => ModelReaderWriter.Write(item).ToString()));
        var usageJson = totalTokens is null
            ? ""
            : $",\"usage\":{{\"input_tokens\":10,\"output_tokens\":{totalTokens - 10},\"total_tokens\":{totalTokens}}}";
        var json = $"{{\"id\":\"{id}\",\"object\":\"response\",\"created_at\":0,\"status\":\"{status}\",\"model\":\"gpt-4o\",\"output\":[{itemsJson}]{usageJson}}}";
        return ModelReaderWriter.Read<ResponseResult>(BinaryData.FromString(json))!;
    }

    private static ResponseResult CompletedResponse(string id, string text, int totalTokens = 30)
        => ResponseWithOutputItems(id, [ResponseItem.CreateAssistantMessageItem(text, (IEnumerable<ResponseMessageAnnotation>?)null)], totalTokens);

    private static ResponseResult ResponseWithFunctionCall(string id, string callId, string functionName)
        => ResponseWithOutputItems(id,
            [ResponseItem.CreateFunctionCallItem(callId: callId, functionName: functionName, functionArguments: BinaryData.FromString("{}"))]);

    private static string SerializeInput(IReadOnlyList<ResponseItem> items)
        => string.Join(' ', items.Select(item => ModelReaderWriter.Write(item).ToString()));

    [Fact]
    public async Task RunParallelAsync_ResolvesPromptFromRealAzureSearchEvidence_BeforeRunningTheAgent()
    {
        var searchClient = Substitute.For<SearchClient>();
        var document = new SearchDocument { ["content"] = "The school was rated Outstanding by Ofsted." };
        var results = SearchModelFactory.SearchResults(
            values: [SearchModelFactory.SearchResult(document, 1.0, highlights: null)],
            totalCount: 1, facets: null, coverage: null, rawResponse: null!);
        searchClient.SearchAsync<SearchDocument>(Arg.Any<string>(), Arg.Any<SearchOptions>(), Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(results, null!));
        var contextRetriever = new AzureSearchContextRetriever(
            new Dictionary<string, SearchClient> { ["ofsted"] = searchClient }, new RelativeScoreRelevanceFilter());

        SetUpNewAgent("ofsted");
        var capturedPrompt = new List<ResponseItem>();
        _conversationClient.CreateResponseAsync("ofsted", ConversationId,
                Arg.Do<IReadOnlyList<ResponseItem>>(items => capturedPrompt.AddRange(items)), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CompletedResponse("resp-1", "The inspection rating is Outstanding."));

        var runner = CreateRunner();
        var orchestrator = new AgentOrchestrator(runner);
        var context = new AgentContext();
        var spec = new AgentSpec { Name = "ofsted", Instructions = "You summarise Ofsted findings." };
        var agent = await _factory!.GetOrCreateAsync(spec, cancellationToken);

        var steps = new List<AgentOrchestrationStep>
        {
            new(agent.Name, _ => Task.FromResult(agent), ct => contextRetriever.GetContextAsync("ofsted", "inspection rating", cancellationToken: ct)
                .ContinueWith(t => t.Result.Text, ct)),
        };

        var result = await orchestrator.RunParallelAsync(steps, context, cancellationToken: cancellationToken);

        Assert.True(result.Results.Single().Succeeded);
        Assert.Contains("--- ofsted Evidence 1 ---", SerializeInput(capturedPrompt), StringComparison.Ordinal);
        Assert.Contains("The school was rated Outstanding by Ofsted.", SerializeInput(capturedPrompt), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_CreatesAgentWithMcpDiscoveredTools_AndResolvesTheResultingFunctionCall()
    {
        var mcpTool = ResponseTool.CreateMcpTool(
            serverLabel: "my-tools", serverUri: new Uri("https://mcp.example.com"), serverDescription: null,
            allowedTools: null, toolCallApprovalPolicy: null);
        _mcpToolClient.GetToolsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<ResponseTool>>([mcpTool]));

        SetUpNewAgent("tool-user");
        var pendingResponse = ResponseWithFunctionCall("resp-1", "call-1", "lookup");
        var resolvedResponse = CompletedResponse("resp-2", "Resolved via MCP-discovered tool.");
        _conversationClient.CreateResponseAsync("tool-user", ConversationId, Arg.Any<IReadOnlyList<ResponseItem>>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(pendingResponse, resolvedResponse);

        var spec = new AgentSpec
        {
            Name = "tool-user",
            Instructions = "You use tools discovered from an MCP server.",
            Tools = await _mcpToolClient.GetToolsAsync(cancellationToken),
        };

        var runner = CreateRunner();
        var resolved = false;

        var result = await runner.RunAsync(spec, "Look this up.", cancellationToken: cancellationToken, resolveToolCalls: (calls, ct) =>
        {
            resolved = true;
            Assert.Equal("lookup", Assert.Single(calls).FunctionName);
            return Task.FromResult<IEnumerable<ToolCallOutput>>([new ToolCallOutput("call-1", "42")]);
        });

        Assert.True(resolved);
        Assert.Equal("Resolved via MCP-discovered tool.", result.Output);

        await _admin.Received(1).CreateAgentVersionAsync("tool-user",
            Arg.Is<ProjectsAgentVersionCreationOptions>(o =>
                ((DeclarativeAgentDefinition)o.Definition).Tools.OfType<McpTool>().Any(t => t.ServerLabel == "my-tools")),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
