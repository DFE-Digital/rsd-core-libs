namespace GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;

/// <summary>
/// A registration of an MCP server with a unique key and a factory function to create connection options.
/// </summary>
/// <param name="ServerKey">A key identifying this server, unique across every entry - e.g. its <c>ServerLabel</c>.</param>
/// <param name="OptionsFactory">Creates this server's connection options.</param>
public sealed record McpServerRegistration(string ServerKey, Func<IServiceProvider, McpServerConnectionOptions> OptionsFactory);
