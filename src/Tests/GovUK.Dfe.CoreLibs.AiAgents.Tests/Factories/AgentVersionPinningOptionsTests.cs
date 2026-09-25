using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Factories;

public sealed class AgentVersionPinningOptionsTests
{
    [Fact]
    public void GetPinnedVersion_ReturnsThePinnedVersion_WhenTheAgentIsPinned()
    {
        var options = new AgentVersionPinningOptions
        {
            VersionPins = new Dictionary<string, string> { ["establishment-agent"] = "3" },
        };

        Assert.Equal("3", options.GetPinnedVersion("establishment-agent"));
    }

    [Fact]
    public void GetPinnedVersion_ReturnsNull_WhenTheAgentIsNotPinned()
    {
        var options = new AgentVersionPinningOptions
        {
            VersionPins = new Dictionary<string, string> { ["establishment-agent"] = "3" },
        };

        Assert.Null(options.GetPinnedVersion("ofsted-agent"));
    }

    [Fact]
    public void GetPinnedVersion_ReturnsNull_WhenNoVersionsArePinned()
    {
        var options = new AgentVersionPinningOptions();

        Assert.Null(options.GetPinnedVersion("establishment-agent"));
    }
}
