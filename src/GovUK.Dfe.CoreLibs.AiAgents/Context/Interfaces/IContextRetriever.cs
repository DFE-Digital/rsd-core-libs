using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Context.Interfaces;

/// <summary>
/// Retrieves context for a given scope and query.
/// </summary>
public interface IContextRetriever
{
    /// <summary>
    /// Retrieves context for the specified scope and query.
    /// </summary>
    /// <param name="scope">The context scope.</param>
    /// <param name="query">The query.</param>
    /// <param name="size">The maximum number of results.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The retrieved context and whether matching evidence was found.</returns>
    Task<ContextResult> GetContextAsync(string scope, string query, int size = 10, CancellationToken cancellationToken = default);
}
