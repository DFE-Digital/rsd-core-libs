using GovUK.Dfe.CoreLibs.AiAgents.Agents;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Agents;

public sealed class EphemeralAgentSweepServiceTests
{
    private readonly IAgentRuntime _runtime = Substitute.For<IAgentRuntime>();

    private EphemeralAgentSweepService Service(TimeSpan interval) => new(_runtime, interval, NullLogger<EphemeralAgentSweepService>.Instance);

    [Fact]
    public async Task Sweeps_WithTheSafeMinimumAge()
    {
        await Service(TimeSpan.FromMinutes(30)).SweepAsync(CancellationToken.None);

        await _runtime.Received(1).DeleteOrphanedEphemeralAgentsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AFailedSweep_IsLogged_AndTheNextOneStillRuns()
    {
        _runtime.DeleteOrphanedEphemeralAgentsAsync(Arg.Any<CancellationToken>()).Throws(new InvalidOperationException("Foundry unavailable."));
        using var service = Service(TimeSpan.FromMilliseconds(50));

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(400);
        await service.StopAsync(CancellationToken.None);

        Assert.True(_runtime.ReceivedCalls().Count() >= 2);
    }
}
