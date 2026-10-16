using GovUK.Dfe.CoreLibs.AiAgents.Extensions;
using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using GovUK.Dfe.CoreLibs.AiAgents.Tests.Integration.Fakes;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using System.Text.Json.Nodes;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests;

public sealed class StructuredOutputTests
{
    public sealed record OfstedFindings(string Rating, string? InspectionDate, IReadOnlyList<string> Strengths);

    [Fact]
    public void For_BuildsAStrictSchema_WithEveryPropertyRequired_AndNoExtraProperties()
    {
        var schema = JsonNode.Parse(AgentOutputSchema.For<OfstedFindings>("ofsted_findings").JsonSchema)!;

        Assert.False(schema["additionalProperties"]!.GetValue<bool>());
        Assert.Equal(["rating", "inspectionDate", "strengths"], schema["required"]!.AsArray().Select(node => node!.GetValue<string>()));
    }

    [Fact]
    public void ReadOutputAs_ReadsAStructuredAnswer()
    {
        var result = new AgentResult("ofsted-agent", """{"rating":"Good","inspectionDate":"2024-03-12","strengths":["Leadership"]}""", 10);

        var findings = result.ReadOutputAs<OfstedFindings>();

        Assert.Equal("Good", findings.Rating);
        Assert.Equal(["Leadership"], findings.Strengths);
    }

    [Fact]
    public void ReadOutputAs_ExplainsTheProblem_WhenTheAgentFailed()
    {
        var fallback = new AgentResult("ofsted-agent", "This section could not be generated due to an error retrieving or analysing evidence.", 0);

        var ex = Assert.Throws<InvalidOperationException>(() => fallback.ReadOutputAs<OfstedFindings>());

        Assert.Contains("ofsted-agent", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheSchema_IsStoredOnTheAgent_AndOnlyAChangeToItCreatesANewVersion()
    {
        var foundry = new InMemoryFoundry();
        var factory = new FoundryAgentFactory(foundry.Admin, new FoundryAgentFactoryOptions("gpt-4o"));
        var spec = new AgentSpec { Name = "ofsted-agent", Instructions = "x", OutputSchema = AgentOutputSchema.For<OfstedFindings>("ofsted_findings") };

        await factory.GetOrCreateAsync(spec);
        await factory.GetOrCreateAsync(spec);
        Assert.Single(foundry.Versions("ofsted-agent"));
        Assert.NotNull(foundry.Definition("ofsted-agent", "1").TextOptions);

        await factory.GetOrCreateAsync(spec with { OutputSchema = AgentOutputSchema.For<AgentResult>("other") });
        await factory.GetOrCreateAsync(spec with { OutputSchema = null });
        Assert.Equal(3, foundry.Versions("ofsted-agent").Count);
    }
}
