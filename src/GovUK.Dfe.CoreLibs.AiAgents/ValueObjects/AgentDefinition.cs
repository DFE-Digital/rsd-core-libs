namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>One agent your app runs.</summary>
/// <param name="Name">Its stable Foundry name, e.g. "ofsted-agent". Unique to your app.</param>
/// <param name="SystemPromptType">Its key under <c>PromptFiles:SystemPrompts</c>.</param>
/// <param name="IsManagedAgent">True (default): kept and reused. False: created per run, then deleted.</param>
public sealed record AgentDefinition(string Name, string SystemPromptType, bool IsManagedAgent = true)
{
    /// <summary>Tools it may use. None if empty.</summary>
    public IReadOnlyList<string> AllowedTools { get; init; } = [];

    /// <summary>Optional JSON schema for its answer; read it back with <c>ReadOutputAs&lt;T&gt;()</c>.</summary>
    public AgentOutputSchema? OutputSchema { get; init; }

    /// <summary>
    /// Optional: returns why an answer is invalid, or null if it's fine. An invalid answer is sent back once with the
    /// reason; if it's still invalid, the run fails.
    /// </summary>
    public Func<AgentResult, string?>? Validate { get; init; }

    /// <summary>
    /// Requires the answer to cite numbered evidence (e.g. search results) as <c>[Evidence n]</c>, and only evidence
    /// that exists. Checked like <see cref="Validate"/>. Has no effect when the evidence isn't numbered.
    /// </summary>
    public bool RequireCitations { get; init; }
}
