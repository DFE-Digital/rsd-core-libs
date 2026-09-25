using GovUK.Dfe.CoreLibs.AiAgents.Prompts;
using GovUK.Dfe.CoreLibs.AiAgents.Prompts.Interfaces;
using NSubstitute;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Prompts;

public sealed class PromptTemplateBuilderTests
{
    private readonly IPromptTemplateStore _templateStore = Substitute.For<IPromptTemplateStore>();

    private PromptTemplateBuilder CreateSut(string template)
    {
        _templateStore.GetTemplate("template").Returns(template);
        return new PromptTemplateBuilder(_templateStore);
    }

    [Fact]
    public void Build_ReplacesKnownTokens()
    {
        var sut = CreateSut("Academy: {{SchoolOrTrustName}}\nEvidence: {{Context}}");

        var result = sut.Build("template", new Dictionary<string, string>
        {
            ["SchoolOrTrustName"] = "Test Academy",
            ["Context"] = "some evidence"
        });

        Assert.Equal("Academy: Test Academy\nEvidence: some evidence", result);
    }

    [Fact]
    public void Build_LeavesUnknownTokensUntouched()
    {
        var sut = CreateSut("Academy: {{SchoolOrTrustName}}, Other: {{NotSupplied}}");

        var result = sut.Build("template", new Dictionary<string, string>
        {
            ["SchoolOrTrustName"] = "Test Academy"
        });

        Assert.Equal("Academy: Test Academy, Other: {{NotSupplied}}", result);
    }

    [Fact]
    public void Build_ReplacesWithEmptyString_WhenValueIsNull()
    {
        var sut = CreateSut("Value: [{{Value}}]");

        var result = sut.Build("template", new Dictionary<string, string>
        {
            ["Value"] = null!
        });

        Assert.Equal("Value: []", result);
    }
}
