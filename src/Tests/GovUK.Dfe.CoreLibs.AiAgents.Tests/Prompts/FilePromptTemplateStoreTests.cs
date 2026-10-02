using GovUK.Dfe.CoreLibs.AiAgents.Prompts;
using GovUK.Dfe.CoreLibs.AiAgents.Tests.Constants;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Prompts;

public sealed class FilePromptTemplateStoreTests
{
    [Fact]
    public void GetTemplate_Throws_WhenKeyNotConfigured()
    {
        var sut = new FilePromptTemplateStore(
            paths: new Dictionary<string, string>(),
            readFile: _ => throw new InvalidOperationException(TestErrorMessages.ShouldNotBeCalled));

        var ex = Assert.Throws<InvalidOperationException>(() => sut.GetTemplate("greeting"));
        Assert.Contains("greeting", ex.Message, StringComparison.Ordinal);
    }
}
