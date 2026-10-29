using GovUK.Dfe.CoreLibs.AiAgents.Context;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Context;

public sealed class AgentContextTests
{
    [Fact]
    public void BuildContextPrompt_KeepsTheNewestEntries_WithinTheCharacterBudget()
    {
        var sut = new AgentContext(maxContextPromptCharacters: 60);
        sut.AddHistory(new AgentContextEntry("first", "p", new string('a', 40)));
        sut.AddHistory(new AgentContextEntry("second", "p", "short one"));
        sut.AddHistory(new AgentContextEntry("third", "p", "short two"));

        var prompt = sut.BuildContextPrompt();

        Assert.True(prompt.Length <= 60, $"Expected at most 60 characters, got {prompt.Length}.");
        Assert.DoesNotContain("[first]", prompt, StringComparison.Ordinal);
        Assert.True(prompt.IndexOf("[second]", StringComparison.Ordinal) < prompt.IndexOf("[third]", StringComparison.Ordinal),
            "Kept entries should stay in chronological order.");
    }

    [Fact]
    public void BuildContextPrompt_KeepsTheEndOfTheNewestEntry_WhenItAloneExceedsTheBudget()
    {
        var sut = new AgentContext(maxContextPromptCharacters: 10);
        sut.AddHistory(new AgentContextEntry("writer", "p", "0123456789-conclusion"));

        Assert.Equal("conclusion", sut.BuildContextPrompt()[^10..]);
        Assert.Equal(10, sut.BuildContextPrompt().Length);
    }

    [Fact]
    public void AddHistory_EvictsTheOldestEntry_WhenOverTheConfiguredMaximum()
    {
        var sut = new AgentContext(maxHistoryEntries: 2);

        sut.AddHistory(new AgentContextEntry("agent-1", "in-1", "out-1"));
        sut.AddHistory(new AgentContextEntry("agent-2", "in-2", "out-2"));
        sut.AddHistory(new AgentContextEntry("agent-3", "in-3", "out-3"));

        Assert.Equal(2, sut.History.Count);
        Assert.Equal("agent-2", sut.History[0].AgentName);
        Assert.Equal("agent-3", sut.History[1].AgentName);
    }

    [Fact]
    public void BuildContextPrompt_FormatsEachEntry_AsAgentNameThenOutput()
    {
        var sut = new AgentContext();
        sut.AddHistory(new AgentContextEntry("researcher", "in-1", "Fact: sky is blue."));
        sut.AddHistory(new AgentContextEntry("writer", "in-2", "Report: the sky is blue."));

        var prompt = sut.BuildContextPrompt();

        Assert.Equal(
            "[researcher] Fact: sky is blue." + Environment.NewLine + Environment.NewLine + "[writer] Report: the sky is blue.",
            prompt);
    }

    [Fact]
    public async Task AddHistory_IsSafeUnderConcurrentWrites()
    {
        var sut = new AgentContext(maxHistoryEntries: 1000);

        var tasks = Enumerable.Range(0, 200)
            .Select(i => Task.Run(() => sut.AddHistory(new AgentContextEntry($"agent-{i}", "in", "out"))));
        await Task.WhenAll(tasks);

        Assert.Equal(200, sut.History.Count);
    }
}
