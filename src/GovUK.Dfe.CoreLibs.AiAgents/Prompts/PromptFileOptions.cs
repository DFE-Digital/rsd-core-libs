namespace GovUK.Dfe.CoreLibs.AiAgents.Prompts;

/// <summary>
/// Represents the options for prompt files, including system and user prompts.
/// </summary>
public sealed class PromptFileOptions
{
    public Dictionary<string, string> SystemPrompts { get; set; } = [];
    public Dictionary<string, string> UserPrompts { get; set; } = [];
}
