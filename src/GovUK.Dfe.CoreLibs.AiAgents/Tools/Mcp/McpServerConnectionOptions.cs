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
    /// The list of allowed tool names for the agent. If null, all tools are allowed.
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
    /// Indicates whether approval is required for the agent to use the MCP server. If true, the agent must be approved before it can access the server.
    /// </summary>
    public bool RequireApproval { get; init; }

    /// <summary>
    /// The authentication configuration for the MCP server, including Azure AD tenant ID, client ID, client secret, and scope.
    /// </summary>
    public required McpServerAuthenticationConfig Authentication { get; init; }

    /// <summary>
    /// Throws if any required field is missing or empty - called at startup so a misconfigured
    /// server fails fast with a clear message instead of surfacing as a confusing error on first use.
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
        if (string.IsNullOrWhiteSpace(Authentication?.TenantId))
        {
            missing.Add($"{nameof(Authentication)}.{nameof(Authentication.TenantId)}");
        }
        if (string.IsNullOrWhiteSpace(Authentication?.ClientId))
        {
            missing.Add($"{nameof(Authentication)}.{nameof(Authentication.ClientId)}");
        }
        if (string.IsNullOrWhiteSpace(Authentication?.ClientSecret))
        {
            missing.Add($"{nameof(Authentication)}.{nameof(Authentication.ClientSecret)}");
        }
        if (string.IsNullOrWhiteSpace(Authentication?.Scope))
        {
            missing.Add($"{nameof(Authentication)}.{nameof(Authentication.Scope)}");
        }

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
/// Represents the authentication configuration for an MCP server, including Azure AD tenant ID, client ID, client secret, and scope.
/// </summary>
public sealed record McpServerAuthenticationConfig
{
    /// <summary>
    /// The Azure AD tenant ID used for authentication.
    /// </summary>
    public required string TenantId { get; init; }

    /// <summary>
    /// The Azure AD client ID used for authentication.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// The Azure AD client secret.
    /// </summary>
    public required string ClientSecret { get; init; }

    /// <summary>
    /// The scope for the authentication request, e.g., "api://your-api-id/.default".
    /// </summary>
    public required string Scope { get; init; }

    /// <summary>Redacts <see cref="ClientSecret"/> so this never leaks via logging or exception messages.</summary>
    public override string ToString() => $"{{ TenantId = {TenantId}, ClientId = {ClientId}, ClientSecret = [REDACTED], Scope = {Scope} }}";
}