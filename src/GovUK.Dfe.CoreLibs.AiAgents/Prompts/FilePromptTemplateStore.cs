using GovUK.Dfe.CoreLibs.AiAgents.Constants;
using GovUK.Dfe.CoreLibs.AiAgents.Prompts.Interfaces;

namespace GovUK.Dfe.CoreLibs.AiAgents.Prompts;

public sealed class FilePromptTemplateStore(IReadOnlyDictionary<string, string> paths, Func<string, string> readFile) : IPromptTemplateStore
{
    public string GetTemplate(string key)
        => paths.TryGetValue(key, out var path)
            ? readFile(path)
            : throw new InvalidOperationException(string.Format(ErrorMessages.NoPromptFileConfigured, key));
}
