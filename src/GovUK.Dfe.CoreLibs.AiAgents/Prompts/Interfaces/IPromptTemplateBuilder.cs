namespace GovUK.Dfe.CoreLibs.AiAgents.Prompts.Interfaces;

/// <summary>
/// Defines a builder for creating prompt templates based on a key and a set of values.
/// </summary>
public interface IPromptTemplateBuilder
{
    /// <summary>
    /// Builds a prompt template by replacing placeholders in the template associated with the given key using the provided values.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="values"></param>
    /// <returns></returns>
    string Build(string key, IReadOnlyDictionary<string, string> values);
}
