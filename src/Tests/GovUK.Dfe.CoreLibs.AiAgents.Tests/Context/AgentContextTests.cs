using GovUK.Dfe.CoreLibs.AiAgents.Context;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Context;

public sealed class AgentContextTests
{
    [Fact]
    public void GetVariable_ReturnsNull_ForAnUnknownKey()
    {
        var sut = new AgentContext();

        Assert.Null(sut.GetVariable("missing"));
    }

    [Fact]
    public void SetVariable_ThenGetVariable_RoundTrips()
    {
        var sut = new AgentContext();

        sut.SetVariable("key", "value-1");
        sut.SetVariable("key", "value-2");

        Assert.Equal("value-2", sut.GetVariable("key"));
    }

    [Fact]
    public void AddHistory_KeepsAllEntries_UpToTheConfiguredMaximum()
    {
        var sut = new AgentContext(maxHistoryEntries: 3);

        sut.AddHistory(new AgentContextEntry("agent-1", "in-1", "out-1"));
        sut.AddHistory(new AgentContextEntry("agent-2", "in-2", "out-2"));
        sut.AddHistory(new AgentContextEntry("agent-3", "in-3", "out-3"));

        Assert.Equal(3, sut.History.Count);
        Assert.Equal("agent-1", sut.History[0].AgentName);
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
    public void BuildContextPrompt_ReturnsEmptyString_WhenNoHistory()
    {
        var sut = new AgentContext();

        Assert.Equal(string.Empty, sut.BuildContextPrompt());
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
