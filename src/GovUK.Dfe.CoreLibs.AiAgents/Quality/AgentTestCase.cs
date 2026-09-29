using System.Text.Json;

namespace GovUK.Dfe.CoreLibs.AiAgents.Quality;

/// <summary>A test for an agent: a prompt, optional evidence, and facts the answer must or mustn't contain (case-insensitive).</summary>
public sealed record AgentTestCase(string Name, string Prompt)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public string? Evidence { get; init; }

    public IReadOnlyList<string> MustMention { get; init; } = [];

    public IReadOnlyList<string> MustNotMention { get; init; } = [];

    /// <summary>
    /// Loads every <c>*.json</c> file in <paramref name="directory"/>, named after the file:
    /// <c>{ "prompt": "...", "evidence": "...", "mustMention": [ ... ], "mustNotMention": [ ... ] }</c>.
    /// </summary>
    public static async Task<IReadOnlyList<AgentTestCase>> LoadAsync(string directory, CancellationToken cancellationToken = default)
    {
        var cases = new List<AgentTestCase>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.json").Order(StringComparer.Ordinal))
        {
            await using var stream = File.OpenRead(path);
            var file = await JsonSerializer.DeserializeAsync<CaseFile>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
            var name = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrWhiteSpace(file?.Prompt))
            {
                throw new InvalidOperationException(string.Format(Constants.ErrorMessages.TestCaseWithoutPrompt, path));
            }

            cases.Add(new AgentTestCase(name, file.Prompt)
            {
                Evidence = file.Evidence,
                MustMention = file.MustMention ?? [],
                MustNotMention = file.MustNotMention ?? [],
            });
        }

        return cases;
    }

    private sealed record CaseFile(string? Prompt, string? Evidence, List<string>? MustMention, List<string>? MustNotMention);
}

/// <summary>How one test case went.</summary>
/// <param name="Output">Null when the run failed.</param>
/// <param name="Failures">Missing or forbidden facts, or why the run failed. Empty when it passed.</param>
public sealed record AgentTestResult(string CaseName, string? Output, IReadOnlyList<string> Failures, IReadOnlyDictionary<string, double> Scores)
{
    public bool Passed => Failures.Count == 0;
}

/// <summary>An agent's test results. Save it as JSON to compare the next version against.</summary>
public sealed record AgentEvaluationReport(string AgentName, IReadOnlyList<AgentTestResult> Results)
{
    /// <summary>Every case ran and got its facts right.</summary>
    public bool Passed => Results.All(static result => result.Passed);

    /// <summary>Each metric's average score across the cases.</summary>
    public IReadOnlyDictionary<string, double> AverageScores => Results
        .SelectMany(static result => result.Scores)
        .GroupBy(static score => score.Key)
        .ToDictionary(static group => group.Key, static group => group.Average(static score => score.Value));

    /// <summary>
    /// Metrics averaging below <paramref name="minimum"/>, plus any <paramref name="requiredMetrics"/> with no score at
    /// all, so a failing judge fails the gate instead of passing it.
    /// </summary>
    public IReadOnlyList<string> BelowMinimum(double minimum, params string[] requiredMetrics)
    {
        var averages = AverageScores;
        return [.. averages.Where(score => score.Value < minimum).Select(static score => score.Key)
            .Concat(requiredMetrics.Where(metric => !averages.ContainsKey(metric)))
            .Distinct().Order()];
    }

    /// <summary>
    /// Metrics whose average fell by more than <paramref name="tolerance"/> since <paramref name="baseline"/>, or that
    /// the baseline scored and this run didn't.
    /// </summary>
    public IReadOnlyList<string> RegressionsFrom(AgentEvaluationReport baseline, double tolerance = 0)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        var current = AverageScores;
        return [.. baseline.AverageScores
            .Where(before => !current.TryGetValue(before.Key, out var now) || now < before.Value - tolerance)
            .Select(static before => before.Key).Order()];
    }
}
