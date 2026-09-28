using Azure.Core;
using Azure.Identity;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;

/// <summary>
/// Represents the connection options for an MCP server, including endpoint, authentication, and tool access configuration.
/// </summary>
public sealed record McpServerConnectionOptions
{
    /// <summary>
    /// The label for the MCP server, used for display purposes.
    /// </summary>
    public required string ServerLabel { get; init; }

    /// <summary>
    /// The URI of the MCP server endpoint, e.g., "https://mcp.example.com".
    /// </summary>
    public required Uri ServerUri { get; init; }

    /// <summary>
    /// The server's tools this app may use - required. It's the most any agent can be given or call, so a
    /// tool added to the server later (perhaps one that changes data) stays unavailable until it's listed
    /// here. Give agents smaller subsets with <see cref="McpAllowedToolsProvider"/>.
    /// </summary>
    public IReadOnlyList<string>? AllowedToolNames { get; init; }

    /// <summary>
    /// The duration for which the tool list is cached. Defaults to 5 minutes.
    /// </summary>
    public TimeSpan ToolListCacheDuration { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The protocol version to use when communicating with the MCP server. Defaults to "2025-11-25".
    /// </summary>
    public string ProtocolVersion { get; init; } = "2025-11-25";

    /// <summary>
    /// How this app authenticates to the MCP server - for tool discovery, startup validation, prompts
    /// and every tool call during a run. The app makes all calls to the server; Foundry never does, so
    /// these credentials never leave the app.
    /// </summary>
    public required McpServerAuthenticationConfig Authentication { get; init; }

    /// <summary>
    /// Throws if any required field is missing or empty - called at startup so a misconfigured
    /// server fails fast with a clear message instead of on first use.
    /// </summary>
    /// <param name="serverKey">The key this configuration was registered under, for the error message.</param>
    public void Validate(string serverKey)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(ServerLabel))
        {
            missing.Add(nameof(ServerLabel));
        }
        if (ServerUri is null || !ServerUri.IsAbsoluteUri)
        {
            missing.Add(nameof(ServerUri));
        }
        if (AllowedToolNames is null || AllowedToolNames.Count == 0 || AllowedToolNames.Any(string.IsNullOrWhiteSpace))
        {
            missing.Add(nameof(AllowedToolNames));
        }

        missing.AddRange((Authentication?.MissingFields() ?? [nameof(Authentication)])
            .Select(field => field == nameof(Authentication) ? field : $"{nameof(Authentication)}.{field}"));

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                string.Format(Constants.ErrorMessages.McpOptionsInvalid, serverKey, string.Join(", ", missing)));
        }
    }

    /// <summary>Redacts <see cref="Authentication"/> so this never leaks a secret via logging or exception messages.</summary>
    public override string ToString() => $"{{ ServerLabel = {ServerLabel}, ServerUri = {ServerUri}, Authentication = {Authentication} }}";
}

/// <summary>
/// How the app obtains an Entra ID access token for an MCP server: either a <see cref="Credential"/>
/// (managed identity, workload identity, <c>DefaultAzureCredential</c>...) or a client secret.
/// </summary>
public sealed record McpServerAuthenticationConfig
{
    /// <summary>
    /// The credential used to get tokens. When set, <see cref="TenantId"/>, <see cref="ClientId"/> and
    /// <see cref="ClientSecret"/> are ignored. Prefer this in Azure-hosted environments.
    /// </summary>
    public TokenCredential? Credential { get; init; }

    /// <summary>The Entra ID tenant ID, for the client-secret flow.</summary>
    public string? TenantId { get; init; }

    /// <summary>The Entra ID client ID, for the client-secret flow.</summary>
    public string? ClientId { get; init; }

    /// <summary>The Entra ID client secret, for the client-secret flow.</summary>
    public string? ClientSecret { get; init; }

    /// <summary>
    /// The scope for the authentication request, e.g., "api://your-api-id/.default".
    /// </summary>
    public required string Scope { get; init; }

    /// <summary>
    /// The Entra ID authority for the client-secret flow. Defaults to the Azure public cloud.
    /// </summary>
    public Uri AuthorityHost { get; init; } = AzureAuthorityHosts.AzurePublicCloud;

    internal IReadOnlyList<string> MissingFields()
    {
        var missing = new List<string>();
        if (Credential is null)
        {
            if (string.IsNullOrWhiteSpace(TenantId))
            {
                missing.Add(nameof(TenantId));
            }
            if (string.IsNullOrWhiteSpace(ClientId))
            {
                missing.Add(nameof(ClientId));
            }
            if (string.IsNullOrWhiteSpace(ClientSecret))
            {
                missing.Add(nameof(ClientSecret));
            }
        }
        if (string.IsNullOrWhiteSpace(Scope))
        {
            missing.Add(nameof(Scope));
        }

        return missing;
    }

    /// <summary>Redacts <see cref="ClientSecret"/> so this never leaks via logging or exception messages.</summary>
    public override string ToString() => Credential is null
        ? $"{{ TenantId = {TenantId}, ClientId = {ClientId}, ClientSecret = [REDACTED], Scope = {Scope} }}"
        : $"{{ Credential = {Credential.GetType().Name}, Scope = {Scope} }}";
}
