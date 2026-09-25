using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Context;

/// <summary>
/// Maintains agent variables and interaction history.
/// </summary>
/// <param name="maxHistoryEntries">The maximum number of history entries to retain (Default: 200).</param>
public sealed class AgentContext(int maxHistoryEntries = 200)
{
    private readonly Dictionary<string, string> _variables = [];
    private readonly List<AgentContextEntry> _history = [];
    private readonly object _lock = new();

    public void SetVariable(string name, string value)
    {
        lock (_lock)
        {
            _variables[name] = value;
        }
    }

    public string? GetVariable(string name)
    {
        lock (_lock)
        {
            return _variables.GetValueOrDefault(name);
        }
    }

    public void AddHistory(AgentContextEntry entry)
    {
        lock (_lock)
        {
            _history.Add(entry);
            if (_history.Count > maxHistoryEntries)
            {
                _history.RemoveAt(0);
            }
        }
    }

    public IReadOnlyList<AgentContextEntry> History
    {
        get
        {
            lock (_lock)
            {
                return [.. _history];
            }
        }
    }

    /// <summary>A compact summary of recent history, suitable for prepending to the next agent's prompt.</summary>
    public string BuildContextPrompt()
    {
        lock (_lock)
        {
            return _history.Count == 0
                ? string.Empty
                : string.Join(Environment.NewLine + Environment.NewLine, _history.Select(entry =>
                $"[{entry.AgentName}] {entry.Output}"));
        }
    }
}
