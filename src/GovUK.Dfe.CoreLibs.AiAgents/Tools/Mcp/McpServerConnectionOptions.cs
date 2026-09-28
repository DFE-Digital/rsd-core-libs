using Azure.Core;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;

/// <summary>One MCP server: where it is, which of its tools this app may use, and how to sign in.</summary>
public sealed record McpServerConnectionOptions
{
    public required string ServerLabel { get; init; }

    public required Uri ServerUri { get; init; }

    /// <summary>
    /// Every tool this app may use from the server. A tool added to the server later stays unavailable
    /// until it's listed here.
    /// </summary>
    public IReadOnlyList<string>? AllowedToolNames { get; init; }

    public TimeSpan ToolListCacheDuration { get; init; } = TimeSpan.FromMinutes(5);

    public string ProtocolVersion { get; init; } = "2025-11-25";

    /// <summary>Signs this app in to the server. Only the app calls it; Foundry never does.</summary>
    public required TokenCredential Credential { get; init; }

    /// <summary>The token scope, e.g. "api://school-performance/.default".</summary>
    public required string Scope { get; init; }

    /// <summary>Throws, naming every missing setting, so a misconfigured server fails at startup.</summary>
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

        if (Credential is null)
        {
            missing.Add(nameof(Credential));
        }

        if (string.IsNullOrWhiteSpace(Scope))
        {
            missing.Add(nameof(Scope));
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                string.Format(Constants.ErrorMessages.McpOptionsInvalid, serverKey, string.Join(", ", missing)));
        }
    }
}
