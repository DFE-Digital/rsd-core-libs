using GovUK.Dfe.CoreLibs.AiAgents.Prompts.Interfaces;

namespace GovUK.Dfe.CoreLibs.AiAgents.Prompts;

public sealed class DelegatingPromptTemplateStore(Func<string, string> getTemplate) : IPromptTemplateStore
{
    public string GetTemplate(string key) => getTemplate(key);
}
