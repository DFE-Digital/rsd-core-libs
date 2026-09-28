namespace GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;

/// <summary>An MCP server's tools don't match this app's configuration, e.g. an allowed tool the server doesn't have.</summary>
internal sealed class McpToolConfigurationException(string message) : InvalidOperationException(message);
