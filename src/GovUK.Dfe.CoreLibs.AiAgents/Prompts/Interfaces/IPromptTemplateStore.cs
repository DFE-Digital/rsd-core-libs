namespace GovUK.Dfe.CoreLibs.AiAgents.Prompts.Interfaces;

/// <summary>
/// Defines a store for prompt templates, allowing retrieval of prompt templates based on specified keys.
/// </summary>
public interface IPromptTemplateStore
{
    /// <summary>
    /// Gets the prompt template associated with the specified key.
    /// </summary>
    /// <param name="key">The key for the prompt template.</param>
    /// <returns>The prompt template.</returns>
    string GetTemplate(string key);
}
