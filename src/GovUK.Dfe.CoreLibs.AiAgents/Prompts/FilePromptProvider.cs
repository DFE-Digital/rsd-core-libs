using GovUK.Dfe.CoreLibs.AiAgents.Prompts.Interfaces;

namespace GovUK.Dfe.CoreLibs.AiAgents.Prompts;

public sealed class FilePromptProvider(IPromptTemplateStore systemPrompts, IPromptTemplateStore userPrompts,
    string? responseFormatKey = null, IReadOnlySet<string>? responseFormatExemptPromptTypes = null) : IPromptProvider
{
    public string GetSystemPrompt(string promptType)
    {
        var prompt = systemPrompts.GetTemplate(promptType);

        if (responseFormatKey is null || responseFormatExemptPromptTypes?.Contains(promptType) == true)
        {
            return prompt;
        }

        var responseFormat = systemPrompts.GetTemplate(responseFormatKey);
        return $"{prompt}{Environment.NewLine}{Environment.NewLine}{responseFormat}";
    }

    public string GetUserPrompt(string promptType) => userPrompts.GetTemplate(promptType);
}
