namespace GovUK.Dfe.CoreLibs.Http.Interfaces;

/// <summary>
/// Provides access to the current correlation id. You should register this as a scoped / per web request
/// dependency in your IoC/DI container.
/// </summary>
public interface ICorrelationContext
{
    /// <summary>
    /// Returns the current correlation id if it has been set
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// Used by the middleware to store the current correlation id. Do not call this method yourself.
    /// </summary>
    /// <param name="correlationId"></param>
    public void SetContext(Guid correlationId);
}
