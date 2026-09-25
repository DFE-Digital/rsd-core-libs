using GovUK.Dfe.CoreLibs.AiAgents.Prompts;
using GovUK.Dfe.CoreLibs.AiAgents.Tests.Constants;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Prompts;

public sealed class FilePromptTemplateStoreTests
{
    [Fact]
    public void GetTemplate_ReturnsFileContent_WhenConfiguredAndReadable()
    {
        var sut = new FilePromptTemplateStore(
            paths: new Dictionary<string, string> { ["greeting"] = "greeting.md" },
            readFile: _ => "Hello from file.");

        var result = sut.GetTemplate("greeting");

        Assert.Equal("Hello from file.", result);
    }

    [Fact]
    public void GetTemplate_Throws_WhenKeyNotConfigured()
    {
        var sut = new FilePromptTemplateStore(
            paths: new Dictionary<string, string>(),
            readFile: _ => throw new InvalidOperationException(TestErrorMessages.ShouldNotBeCalled));

        var ex = Assert.Throws<InvalidOperationException>(() => sut.GetTemplate("greeting"));
        Assert.Contains("greeting", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetTemplate_Propagates_WhenFileReadThrows()
    {
        var readException = new FileNotFoundException();
        var sut = new FilePromptTemplateStore(
            paths: new Dictionary<string, string> { ["greeting"] = "greeting.md" },
            readFile: _ => throw readException);

        var thrown = Assert.Throws<FileNotFoundException>(() => sut.GetTemplate("greeting"));
        Assert.Same(readException, thrown);
    }
}
