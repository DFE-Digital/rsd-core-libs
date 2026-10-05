using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using System.Collections.Concurrent;

namespace GovUK.Dfe.CoreLibs.AiAgents.Factories;

/// <summary>
/// Resolved agent versions, kept for a short time so each run doesn't ask Foundry again. Per instance.
/// A zero duration turns it off.
/// </summary>
internal sealed class AgentReferenceCache(TimeSpan duration, TimeProvider timeProvider)
{
    private readonly ConcurrentDictionary<(string Name, string Key), (AgentReference Agent, DateTimeOffset Expires)> _entries = new();

    public AgentReference? TryGet(string name, string key)
    {
        if (!_entries.TryGetValue((name, key), out var entry))
        {
            return null;
        }

        if (entry.Expires > timeProvider.GetUtcNow())
        {
            return entry.Agent;
        }

        _entries.TryRemove((name, key), out _);
        return null;
    }

    public void Set(string name, string key, AgentReference agent)
    {
        if (duration > TimeSpan.Zero)
        {
            _entries[(name, key)] = (agent, timeProvider.GetUtcNow() + duration);
        }
    }

    /// <summary>Forgets every entry for an agent, e.g. after deleting it or some of its versions.</summary>
    public void Remove(string name)
    {
        foreach (var key in _entries.Keys.Where(key => key.Name == name))
        {
            _entries.TryRemove(key, out _);
        }
    }
}
