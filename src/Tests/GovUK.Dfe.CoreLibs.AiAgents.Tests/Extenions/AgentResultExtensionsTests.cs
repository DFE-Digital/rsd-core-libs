using GovUK.Dfe.CoreLibs.AiAgents.Extensions;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Extenions;

public sealed class AgentResultExtensionsTests
{
    [Theory]
    [InlineData("trust-agent", "Trust")]
    [InlineData("rise-concerns-agent", "Rise Concerns")]
    [InlineData("web-search-agent", "Web Search")]
    [InlineData("no-suffix", "No Suffix")]
    public void ToDisplayName_FormatsKebabCaseAgentNames(string agentName, string expected)
        => Assert.Equal(expected, agentName.ToDisplayName());
}
