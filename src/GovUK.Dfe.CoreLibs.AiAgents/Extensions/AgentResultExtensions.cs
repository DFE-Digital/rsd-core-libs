using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Extensions;

/// <summary>
/// Provides extension methods for working with <see cref="AgentResult"/> instances and related data, such as formatting agent names for display and compiling multiple results into a single context block.
/// </summary>
public static class AgentResultExtensions
{
    /// <summary>
    /// Converts a kebab-case agent name (e.g., "trust-agent") into a human-readable display name (e.g., "Trust"). It removes the "-agent" suffix if present and capitalizes each word.
    /// </summary>
    /// <param name="agentName">The agent's stable name, e.g. <see cref="AgentDefinition.Name"/> or <see cref="AgentResult.AgentName"/>.</param>
    /// <returns>The human-readable display name.</returns>
    public static string ToDisplayName(this string agentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);

        var trimmed = agentName.EndsWith("-agent", StringComparison.Ordinal)
            ? agentName[..^"-agent".Length]
            : agentName;

        return string.Join(' ', trimmed.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
    }
}