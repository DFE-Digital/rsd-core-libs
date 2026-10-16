namespace GovUK.Dfe.CoreLibs.AiAgents.Prompts.Interfaces;

/// <summary>
/// Defines a provider for retrieving system and user prompts based on prompt types.
/// </summary>
public interface IPromptProvider
{
    /// <summary>
    /// Gets the system prompt (instructions) for the given agent type.
    /// </summary>
    /// <param name="promptType">The prompt type key.</param>
    string GetSystemPrompt(string promptType);

    /// <summary>
    /// Gets the user prompt template for the given type.
    /// </summary>
    /// <param name="promptType">The prompt type key.</param>
    string GetUserPrompt(string promptType);
}
