using GovUK.Dfe.CoreLibs.Security.Configurations;

namespace GovUK.Dfe.CoreLibs.Security.Interfaces;

/// <summary>
/// Optional extension for <see cref="IExternalIdentityValidator"/> implementations that
/// support swapping the multi-provider OIDC list at runtime (e.g. after a tenant config refresh).
/// Existing single-provider and static multi-provider consumers are unaffected.
/// </summary>
public interface IMultiProviderExternalIdentityReloader
{
    /// <summary>
    /// Replaces the active multi-provider list with <paramref name="providers"/>.
    /// Each entry must include a <see cref="OpenIdConnectOptions.DiscoveryEndpoint"/>.
    /// Thread-safe; in-flight validations continue against the previous snapshot.
    /// </summary>
    /// <param name="providers">New provider configurations (must not be empty).</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the validator was not started in multi-provider mode.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providers"/> is empty or any entry lacks DiscoveryEndpoint.
    /// </exception>
    void ReloadProviders(IReadOnlyList<OpenIdConnectOptions> providers);

    /// <summary>
    /// Number of providers currently registered for multi-provider validation.
    /// </summary>
    int ProviderCount { get; }
}
