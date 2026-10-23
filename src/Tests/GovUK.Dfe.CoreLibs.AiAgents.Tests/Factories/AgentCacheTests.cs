using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using GovUK.Dfe.CoreLibs.AiAgents.Tests.Integration.Fakes;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using NSubstitute;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Factories;

public sealed class AgentCacheTests
{
    private readonly CancellationToken cancellationToken = default;
    private readonly InMemoryFoundry _foundry = new();
    private readonly ManualClock _clock = new();
    private static readonly AgentSpec Spec = new() { Name = "ofsted-agent", Instructions = "You analyse Ofsted reports." };

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }

    private FoundryAgentFactory Factory(TimeSpan? cacheDuration = null)
        => new(_foundry.Admin, new FoundryAgentFactoryOptions("gpt-4o") { AgentCacheDuration = cacheDuration ?? TimeSpan.FromSeconds(30) },
            timeProvider: _clock);

    private void FoundryLookups(int expected)
        => _ = _foundry.Admin.Received(expected).GetAgentAsync("ofsted-agent", Arg.Any<CancellationToken>());

    [Fact]
    public async Task RepeatedRuns_ReuseTheResolvedVersion_WithoutAskingFoundry_UntilTheCacheExpires()
    {
        var factory = Factory();

        await factory.GetOrCreateAsync(Spec, cancellationToken);
        await factory.GetOrCreateAsync(Spec, cancellationToken);
        await factory.GetOrCreateAsync(Spec, cancellationToken);
        FoundryLookups(1);

        _clock.Advance(TimeSpan.FromSeconds(31));
        await factory.GetOrCreateAsync(Spec, cancellationToken);
        FoundryLookups(2);
    }

    [Fact]
    public async Task ADifferentSpec_IsNeverServedAnotherSpecsVersion()
    {
        var factory = Factory();

        var first = await factory.GetOrCreateAsync(Spec, cancellationToken);
        var changed = await factory.GetOrCreateAsync(Spec with { Instructions = "Cite the inspection date." }, cancellationToken);

        Assert.Equal(("1", "2"), (first.Version, changed.Version));
    }

    [Fact]
    public async Task ZeroDuration_AsksFoundryOnEveryRun()
    {
        var factory = Factory(TimeSpan.Zero);

        await factory.GetOrCreateAsync(Spec, cancellationToken);
        await factory.GetOrCreateAsync(Spec, cancellationToken);

        FoundryLookups(2);
    }

    [Fact]
    public async Task PinnedLookups_AreCachedToo()
    {
        _foundry.Seed("ofsted-agent", "gpt-4o", "Provisioned centrally.");
        var factory = Factory();

        await factory.ResolveAsync("ofsted-agent", "1", cancellationToken);
        await factory.ResolveAsync("ofsted-agent", "1", cancellationToken);

        await _foundry.Admin.Received(1).GetAgentVersionAsync("ofsted-agent", "1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeletingTheAgent_ClearsItsCache_SoTheNextRunRecreatesIt()
    {
        var factory = Factory();
        await factory.GetOrCreateAsync(Spec, cancellationToken);

        await factory.DeleteAgentAsync("ofsted-agent", cancellationToken);
        var recreated = await factory.GetOrCreateAsync(Spec, cancellationToken);

        Assert.Equal("2", recreated.Version);
        Assert.Equal(["ofsted-agent", "ofsted-agent"], _foundry.CreatedNames);
    }
}
