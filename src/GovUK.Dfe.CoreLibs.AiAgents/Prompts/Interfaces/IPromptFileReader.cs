namespace GovUK.Dfe.CoreLibs.AiAgents.Prompts.Interfaces;

/// <summary>
/// Defines a reader for prompt files, allowing retrieval of prompt content from specified paths.
/// </summary>
public interface IPromptFileReader
{
    /// <summary>
    /// Reads the content of a prompt file from the specified path.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    string Read(string path);
}
