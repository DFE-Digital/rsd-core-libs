using GovUK.Dfe.CoreLibs.AiAgents.Constants;
using GovUK.Dfe.CoreLibs.AiAgents.Prompts.Interfaces;
using Microsoft.Extensions.Logging;

namespace GovUK.Dfe.CoreLibs.AiAgents.Prompts;

public sealed class FileSystemPromptFileReader(ILogger<FileSystemPromptFileReader> logger) : IPromptFileReader
{
    public string Read(string path)
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, path);

        if (!File.Exists(fullPath))
        {
            logger.LogError("Prompt file not found: {FullPath}", fullPath);
            throw new FileNotFoundException(ErrorMessages.PromptFileNotFound, fullPath);
        }

        var content = File.ReadAllText(fullPath);

        if (string.IsNullOrWhiteSpace(content))
        {
            logger.LogError("Prompt file is empty: {FullPath}", fullPath);
            throw new InvalidOperationException(string.Format(ErrorMessages.PromptFileEmpty, fullPath));
        }

        return content;
    }
}
