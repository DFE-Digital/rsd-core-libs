using GovUK.Dfe.CoreLibs.AiAgents.Quality;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Quality;

public sealed class CitationsTests
{
    private const string NumberedEvidence = "--- ofsted_index Evidence 1 ---\nRated Good.\n\n--- ofsted_index Evidence 2 ---\nInspected 2024.";

    [Theory]
    [InlineData("Rated Good [Evidence 1], inspected 2024 [Evidence 2].", null)]
    [InlineData("Rated Good.", "Cite the evidence")]
    [InlineData("Rated Good [Evidence 3].", "[Evidence 3] doesn't exist")]
    public void Check_AcceptsOnlyAnswersCitingEvidenceThatExists(string answer, string? expectedProblem)
    {
        var problem = Citations.Check(answer, Citations.CountEvidence(NumberedEvidence));

        if (expectedProblem is null)
        {
            Assert.Null(problem);
        }
        else
        {
            Assert.Contains(expectedProblem, problem, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ForRun_ByDefault_AsksForCitations_AndChecksThemBeforeTheAgentsOwnValidation()
    {
        var definition = new AgentDefinition("ofsted-agent", "Ofsted") { Validate = _ => "own check" };

        var (prompt, validate) = Citations.ForRun(definition, "Summarise.", NumberedEvidence);

        Assert.EndsWith("as [Evidence n].", prompt, StringComparison.Ordinal);
        Assert.Contains("Cite the evidence", validate!(new AgentResult("ofsted-agent", "No citations.", 0)), StringComparison.Ordinal);
        Assert.Equal("own check", validate(new AgentResult("ofsted-agent", "Good [Evidence 1].", 0)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Another agent's unnumbered output.")]
    public void ForRun_ChangesNothing_WhenTheEvidenceIsntNumbered(string? evidence)
    {
        var (prompt, validate) = Citations.ForRun(new AgentDefinition("ofsted-agent", "Ofsted"), "Summarise.", evidence);

        Assert.Equal("Summarise.", prompt);
        Assert.Null(validate);
    }

    [Fact]
    public void ForRun_ChangesNothing_WhenTheAgentTurnsCitationsOff()
    {
        var definition = new AgentDefinition("ofsted-agent", "Ofsted") { RequireCitations = false };

        var (prompt, validate) = Citations.ForRun(definition, "Summarise.", NumberedEvidence);

        Assert.Equal("Summarise.", prompt);
        Assert.Null(validate);
    }
}
