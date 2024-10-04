namespace DfE.CoreLibs.Http.Middlewares.CorrelationId;

/// <summary>
/// The keys used by the correlation id middleware.
/// </summary>
internal static class Keys
{
    /// <summary>
    /// The header key use to detect incoming correlation ids, and to send them in responses.
    /// Use this key if you are making subsequent requests so that correlation flows between services
    /// </summary>
    public const string HeaderKey = "x-correlationId";
}
