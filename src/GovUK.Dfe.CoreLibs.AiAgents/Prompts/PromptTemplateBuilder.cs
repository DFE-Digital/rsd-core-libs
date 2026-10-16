using GovUK.Dfe.CoreLibs.AiAgents.Prompts.Interfaces;

namespace GovUK.Dfe.CoreLibs.AiAgents.Prompts;

public sealed class PromptTemplateBuilder(IPromptTemplateStore templateStore) : IPromptTemplateBuilder
{
    public string Build(string key, IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var prompt = templateStore.GetTemplate(key);

        foreach (var (name, value) in values)
        {
            prompt = prompt.Replace($"{{{{{name}}}}}", value ?? string.Empty, StringComparison.Ordinal);
        }

        return prompt;
    }
}
