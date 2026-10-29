 using GovUK.Dfe.CoreLibs.AiAgents.Prompts;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Prompts;

public sealed class FileSystemPromptFileReaderTests : IDisposable
{
    private readonly string _fullPath;
    private readonly string _relativePath;

    public FileSystemPromptFileReaderTests()
    {
        _relativePath = $"{Guid.NewGuid():N}.md";
        _fullPath = Path.Combine(AppContext.BaseDirectory, _relativePath);
    }

    public void Dispose()
    {
        if (File.Exists(_fullPath))
        {
            File.Delete(_fullPath);
        }
    }

    private static FileSystemPromptFileReader CreateSut() => new(NullLogger<FileSystemPromptFileReader>.Instance);

    [Fact]
    public void Read_ReturnsFileContent_RelativeToTheAppBaseDirectory()
    {
        File.WriteAllText(_fullPath, "Real prompt content.");
        var sut = CreateSut();

        var result = sut.Read(_relativePath);

        Assert.Equal("Real prompt content.", result);
    }

    [Fact]
    public void Read_Throws_WhenTheFileDoesNotExist()
        => Assert.Throws<FileNotFoundException>(() => CreateSut().Read(_relativePath));

    [Fact]
    public void Read_Throws_WhenTheFileIsEmpty()
    {
        File.WriteAllText(_fullPath, "   ");
        var sut = CreateSut();

        Assert.Throws<InvalidOperationException>(() => sut.Read(_relativePath));
    }
}
