using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Factories;

public sealed class AgentVersionPinningOptionsTests
{
    private static readonly AgentVersionPinningOptions Options = new()
    {
        VersionPins = new Dictionary<string, string> { ["ofsted-agent"] = "7" },
        ProtectedVersions = new Dictionary<string, IReadOnlyList<string>> { ["ofsted-agent"] = ["3", "5"] },
    };

    [Theory]
    [InlineData("ofsted-agent", "7", true)]    // this environment's pin
    [InlineData("ofsted-agent", "3", true)]    // pinned by another environment
    [InlineData("ofsted-agent", "5", true)]
    [InlineData("ofsted-agent", "6", false)]
    [InlineData("trust-agent", "3", false)]    // protection is per agent
    public void IsProtected_CoversThisEnvironmentsPin_AndEveryVersionOtherEnvironmentsUse(string agentName, string version, bool expected)
        => Assert.Equal(expected, Options.IsProtected(agentName, version));
}
