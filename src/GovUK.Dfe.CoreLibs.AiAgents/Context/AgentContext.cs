using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Context;

/// <summary>
/// The outputs of an orchestration's agents so far, newest last. Keeps at most <paramref name="maxHistoryEntries"/>
/// entries, and at most <paramref name="maxContextPromptCharacters"/> in <see cref="BuildContextPrompt"/>.
/// </summary>
public sealed class AgentContext(int maxHistoryEntries = 200, int maxContextPromptCharacters = 32_000)
{
    private readonly List<AgentContextEntry> _history = [];
    private readonly object _lock = new();

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

    /// <summary>
    /// A compact summary of recent history, suitable for prepending to the next agent's prompt. Kept
    /// within the character budget by dropping the oldest entries first; if even the newest entry is
    /// too long, only its most recent text is kept.
    /// </summary>
    public string BuildContextPrompt()
    {
        var separator = Environment.NewLine + Environment.NewLine;
        var blocks = new List<string>();
        var length = 0;

        lock (_lock)
        {
            for (var i = _history.Count - 1; i >= 0; i--)
            {
                var block = $"[{_history[i].AgentName}] {_history[i].Output}";
                var added = block.Length + (blocks.Count == 0 ? 0 : separator.Length);

                if (length + added > maxContextPromptCharacters)
                {
                    if (blocks.Count == 0 && maxContextPromptCharacters > 0)
                    {
                        blocks.Add(block[^maxContextPromptCharacters..]);
                    }

                    break;
                }

                blocks.Add(block);
                length += added;
            }
        }

        blocks.Reverse();
        return string.Join(separator, blocks);
    }
}
