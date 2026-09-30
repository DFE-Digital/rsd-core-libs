namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>One agent your app runs.</summary>
/// <param name="Name">Its stable Foundry name, e.g. "ofsted-agent". Unique to your app.</param>
/// <param name="SystemPromptType">Its key under <c>PromptFiles:SystemPrompts</c>.</param>
/// <param name="IsManagedAgent">True (default): kept and reused. False: created per run, then deleted.</param>
public sealed record AgentDefinition(string Name, string SystemPromptType, bool IsManagedAgent = true)
{
    /// <summary>The only tools it may call. Empty (default): no tools.</summary>
    public IReadOnlyList<string> AllowedTools { get; init; } = [];

    /// <summary>Optional JSON schema for its answer; read it back with <c>ReadOutputAs&lt;T&gt;()</c>.</summary>
    public AgentOutputSchema? OutputSchema { get; init; }

    /// <summary>
    /// Optional: returns why an answer is invalid, or null if it's fine. An invalid answer is sent back once with the
    /// reason; if it's still invalid, the run fails.
    /// </summary>
    public Func<AgentResult, string?>? Validate { get; init; }

    /// <summary>
    /// On by default: given numbered evidence (e.g. search results), the answer must cite it as <c>[Evidence n]</c>, and
    /// only evidence that exists. No effect on unnumbered evidence. Turn off for an answer with nowhere to cite, e.g. a
    /// schema with no text fields.
    /// </summary>
    public bool RequireCitations { get; init; } = true;
}
