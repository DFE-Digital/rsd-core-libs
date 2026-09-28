namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>
/// Defines an agent, including its name, system prompt type, and whether it is managed by the system.
/// </summary>
/// <param name="Name">The agent's stable name in Foundry, e.g. "ofsted-agent". Keep it unique to your app.</param>
/// <param name="SystemPromptType">The key of its system prompt under <c>PromptFiles:SystemPrompts</c>.</param>
/// <param name="IsManagedAgent">
/// <see langword="true"/> (default) keeps the agent in Foundry and reuses it; <see langword="false"/>
/// creates it for one run and deletes it afterwards.
/// </param>
public sealed record AgentDefinition(string Name, string SystemPromptType, bool IsManagedAgent = true)
{
    /// <summary>
    /// A list of tool names the agent is allowed to use, e.g. <c>["ofsted-finder", "ofsted-finder-v2"]</c>.
    /// </summary>
    public IReadOnlyList<string> AllowedTools { get; init; } = [];

    /// <summary>
    /// The schema of the agent's output, if any. This is used to validate the output and provide structured data to the caller.
    /// </summary>
    public AgentOutputSchema? OutputSchema { get; init; }
}
