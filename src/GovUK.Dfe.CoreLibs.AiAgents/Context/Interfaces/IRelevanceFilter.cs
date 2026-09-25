using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Context.Interfaces;

/// <summary>
/// Filters search results by relevance.
/// </summary>
public interface IRelevanceFilter
{
    /// <summary>
    /// Filters the specified search results by relevance.
    /// </summary>
    /// <param name="results">The search results.</param>
    /// <returns>The relevant search results.</returns>
    IReadOnlyList<SearchResultItem> Filter(IReadOnlyList<SearchResultItem> results);
}
