using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Extensions;

/// <summary>Helpers for agent names and results.</summary>
public static class AgentResultExtensions
{
    /// <summary>Turns "trust-agent" into "Trust": drops "-agent" and capitalises each word.</summary>
    public static string ToDisplayName(this string agentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);

        var trimmed = agentName.EndsWith("-agent", StringComparison.Ordinal)
            ? agentName[..^"-agent".Length]
            : agentName;

        return string.Join(' ', trimmed.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
    }

    /// <summary>
    /// Reads a structured answer (from an agent with an <see cref="AgentDefinition.OutputSchema"/>) as
    /// <typeparamref name="T"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The agent failed, or its answer isn't valid JSON for <typeparamref name="T"/>.</exception>
    public static T ReadOutputAs<T>(this AgentResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(result.Output ?? string.Empty, AgentOutputSchema.SerializerOptions)
                ?? throw new InvalidOperationException(string.Format(Constants.ErrorMessages.StructuredOutputUnreadable, result.AgentName, typeof(T).Name));
        }
        catch (System.Text.Json.JsonException ex)
        {
            // Usually a failed agent, whose output is the fallback message rather than JSON.
            throw new InvalidOperationException(string.Format(Constants.ErrorMessages.StructuredOutputUnreadable, result.AgentName, typeof(T).Name), ex);
        }
    }

    /// <summary>
    /// Adds up the tokens used across <paramref name="results"/> - e.g. every specialist result plus the
    /// synthesis result of a briefing - in total and per agent. A failed agent counts as zero.
    /// </summary>
    public static TokenUsageSummary ToTokenUsageSummary(this IEnumerable<AgentResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var byAgent = new Dictionary<string, TokenUsage>(StringComparer.Ordinal);
        var total = TokenUsage.None;
        foreach (var result in results)
        {
            byAgent[result.AgentName] = byAgent.GetValueOrDefault(result.AgentName, TokenUsage.None) + result.Usage;
            total += result.Usage;
        }

        return new TokenUsageSummary(total, byAgent);
    }
}