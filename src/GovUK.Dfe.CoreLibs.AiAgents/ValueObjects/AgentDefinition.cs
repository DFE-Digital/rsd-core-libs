namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>
/// Declares one of your agents: its name, the system prompt it's built from, and what it may use.
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
    /// The tools this app runs (e.g. MCP tools) that this agent may be given and call, by name. Only these
    /// are attached to the agent, and only these are run when the model calls them - even for a pinned
    /// version, whatever its stored definition contains. Empty (the default) gives the agent none.
    /// Built-in Foundry tools such as web search come from their <c>AgentToolBinding</c> and aren't affected.
    /// </summary>
    public IReadOnlyList<string> AllowedTools { get; init; } = [];

    /// <summary>
    /// A JSON schema the agent's answer must follow, e.g. <c>AgentOutputSchema.For&lt;OfstedFindings&gt;("ofsted_findings")</c>.
    /// <see langword="null"/> (the default) for free text.
    /// </summary>
    public AgentOutputSchema? OutputSchema { get; init; }
}
