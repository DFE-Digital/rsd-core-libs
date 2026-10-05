using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;
using ModelContextProtocol.Protocol;
using OpenAI.Responses;
using System.ClientModel.Primitives;
using System.Text.Json;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Tools.Mcp;

/// <summary>
/// MCP tools reach Foundry only as function definitions, which are persisted in every agent version
/// that uses them. They must never carry a credential or a server address, and must be identical
/// from one call to the next.
/// </summary>
public sealed class McpToolDefinitionTests
{
    private static Tool ServerTool(string name, string description = "Gets data.") => new()
    {
        Name = name,
        Description = description,
        InputSchema = JsonDocument.Parse("""{"type":"object","properties":{"urn":{"type":"string"}},"required":["urn"]}""").RootElement,
    };

    private static string Serialize(ResponseTool tool) => ModelReaderWriter.Write(tool).ToString();

    [Fact]
    public void BuildFunctionTools_DescribesEachServerToolAsAFunction_WithItsSchemaAndDescription()
    {
        var tool = Assert.IsType<FunctionTool>(Assert.Single(McpToolClient.BuildFunctionTools("performance",
            [ServerTool("get_performance_data", "Gets KS2 results for a school.")])));

        Assert.Equal("get_performance_data", tool.FunctionName);
        Assert.Equal("Gets KS2 results for a school.", tool.FunctionDescription);
        Assert.Contains("\"urn\"", tool.FunctionParameters.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void BuildFunctionTools_CarriesNoCredentialOrServerAddress()
    {
        var json = Serialize(Assert.Single(McpToolClient.BuildFunctionTools("performance", [ServerTool("get_performance_data")])));

        Assert.DoesNotContain("authorization", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("server_url", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"mcp\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildFunctionTools_IsIdenticalWhateverOrderTheServerListsTools_SoItNeverCausesANewAgentVersion()
    {
        var first = McpToolClient.BuildFunctionTools("performance", [ServerTool("b_tool"), ServerTool("a_tool")]).Select(Serialize);
        var second = McpToolClient.BuildFunctionTools("performance", [ServerTool("a_tool"), ServerTool("b_tool")]).Select(Serialize);

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData("get_performance_data", "get_performance_data")]
    [InlineData("schools.get-data", "schools_get-data")]
    [InlineData("ofsted rating/latest", "ofsted_rating_latest")]
    public void ToFunctionName_ReplacesCharactersFunctionNamesDontAllow(string toolName, string expected)
        => Assert.Equal(expected, McpToolClient.ToFunctionName(toolName));

    [Fact]
    public void ToFunctionName_TruncatesToSixtyFourCharacters()
        => Assert.Equal(64, McpToolClient.ToFunctionName(new string('a', 100)).Length);

    [Fact]
    public void BuildFunctionTools_Throws_WhenTwoToolsBecomeTheSameFunctionName()
    {
        var ex = Assert.Throws<McpToolConfigurationException>(() =>
            McpToolClient.BuildFunctionTools("performance", [ServerTool("get.data"), ServerTool("get_data")]));

        Assert.Contains("get_data", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadOutput_JoinsTextBlocks()
    {
        var result = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "Rated Good." }, new TextContentBlock { Text = "Inspected March 2024." }],
        };

        Assert.Equal($"Rated Good.{Environment.NewLine}Inspected March 2024.", McpToolClient.ReadOutput(result));
    }
}
