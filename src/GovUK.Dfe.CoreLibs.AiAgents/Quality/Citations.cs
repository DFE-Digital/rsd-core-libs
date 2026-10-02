using GovUK.Dfe.CoreLibs.AiAgents.Constants;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GovUK.Dfe.CoreLibs.AiAgents.Quality;

/// <summary>Checks answers cite numbered evidence (<c>--- index Evidence n ---</c>) as <c>[Evidence n]</c>.</summary>
internal static partial class Citations
{
    /// <summary>The prompt and validator for a run: the citation check when required and possible, then the definition's own.</summary>
    public static (string Prompt, Func<AgentResult, string?>? Validate) ForRun(AgentDefinition definition, string prompt, string? evidence)
    {
        var evidenceCount = definition.RequireCitations ? CountEvidence(evidence) : 0;
        if (evidenceCount == 0)
        {
            return (prompt, definition.Validate);
        }

        var ownCheck = definition.Validate;
        return (prompt + PromptText.CiteEvidence, result => Check(result.Output, evidenceCount) ?? ownCheck?.Invoke(result));
    }

    /// <summary>The highest evidence number, or 0 when the evidence isn't numbered.</summary>
    public static int CountEvidence(string? evidence)
        => string.IsNullOrEmpty(evidence) ? 0 : Numbers(EvidenceLabel(), evidence).DefaultIfEmpty(0).Max();

    /// <summary>Null when the answer cites at least one piece of evidence and only evidence that exists; otherwise why not.</summary>
    public static string? Check(string? answer, int evidenceCount)
    {
        var cited = Numbers(CitationPattern(), answer ?? string.Empty).ToHashSet();
        if (cited.Count == 0)
        {
            return "Cite the evidence each point relies on as [Evidence n].";
        }

        var unknown = cited.Where(number => number < 1 || number > evidenceCount).Order().ToList();
        return unknown.Count == 0
            ? null
            : $"{string.Join(", ", unknown.Select(number => $"[Evidence {number}]"))} doesn't exist; cite only [Evidence 1] to [Evidence {evidenceCount}].";
    }

    private static IEnumerable<int> Numbers(Regex pattern, string text)
    {
        foreach (Match match in pattern.Matches(text))
        {
            if (int.TryParse(match.Groups[1].ValueSpan, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            {
                yield return number;
            }
        }
    }

    [GeneratedRegex(@"Evidence (\d{1,6}) ---", RegexOptions.CultureInvariant)]
    private static partial Regex EvidenceLabel();

    [GeneratedRegex(@"\[Evidence (\d{1,6})\]", RegexOptions.CultureInvariant)]
    private static partial Regex CitationPattern();
}
