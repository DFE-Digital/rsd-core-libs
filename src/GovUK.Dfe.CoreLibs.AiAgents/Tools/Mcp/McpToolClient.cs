using GovUK.Dfe.CoreLibs.AiAgents.Constants;
using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp.Interfaces;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OpenAI.Responses;
using System.Text;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;

public sealed class McpToolClient : IMcpToolClient
{
    internal const string HttpClientName = "GovUK.Dfe.CoreLibs.AiAgents.McpToolClient";

    private readonly McpServerConnectionOptions _options;
    private readonly ITokenService _tokenService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _httpClientName;
    private readonly ILogger<McpToolClient> _logger;
    private readonly Lazy<Task<McpClient>> _client;
    private readonly SemaphoreSlim _toolNamesLock = new(1, 1);
    private HashSet<string>? _cachedToolNames;
    private DateTimeOffset _toolNamesCachedAt;

    public McpToolClient(McpServerConnectionOptions options, ITokenService tokenService, IHttpClientFactory httpClientFactory,
        ILogger<McpToolClient> logger, string? httpClientName = null)
    {
        _options = options;
        _tokenService = tokenService;
        _httpClientFactory = httpClientFactory;
        _httpClientName = httpClientName ?? HttpClientName;
        _logger = logger;
        _client = new Lazy<Task<McpClient>>(ConnectAsync);
    }

    /// <summary>
    /// Discovers tools on the MCP server, restricted to the connection's own configured
    /// <see cref="McpServerConnectionOptions.AllowedToolNames"/>. Equivalent to
    /// <c>GetToolsAsync(options.AllowedToolNames, cancellationToken)</c> - use the overload directly
    /// (e.g. via <see cref="McpAllowedToolsProvider"/>) to give different agents different subsets of
    /// the same server's tools without needing a separate connection per subset.
    /// </summary>
    public Task<IReadOnlyList<ResponseTool>> GetToolsAsync(CancellationToken cancellationToken = default)
        => GetToolsAsync(_options.AllowedToolNames, cancellationToken);

    /// <summary>
    /// Discovers tools on the MCP server, restricted to <paramref name="allowedToolNames"/> (or every
    /// tool the server reports, when null/empty) - independent of this connection's own configured
    /// <see cref="McpServerConnectionOptions.AllowedToolNames"/>, so one shared connection can serve
    /// different agents different tool subsets.
    /// </summary>
    /// <param name="allowedToolNames">The tool names to restrict this call to, or null/empty for all tools the server reports.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IReadOnlyList<ResponseTool>> GetToolsAsync(IReadOnlyList<string>? allowedToolNames,
        CancellationToken cancellationToken = default)
    {
        var client = await _client.Value.ConfigureAwait(false);
        var availableNames = await GetAvailableToolNamesAsync(client, cancellationToken).ConfigureAwait(false);

        McpToolFilter? allowedTools = null;
        if (allowedToolNames is { Count: > 0 } names)
        {
            var missing = names.Where(name => !availableNames.Contains(name)).ToList();
            if (missing.Count > 0)
            {
                throw new InvalidOperationException(
                    string.Format(ErrorMessages.McpToolsNotFoundOnServer, _options.ServerLabel, string.Join(", ", missing)));
            }

            allowedTools = new McpToolFilter();
            foreach (var name in names)
            {
                allowedTools.ToolNames.Add(name);
            }
        }

        var approvalPolicy = new McpToolCallApprovalPolicy(
            new GlobalMcpToolCallApprovalPolicy(_options.RequireApproval ? "always" : "never"));

        var authorizationToken = await _tokenService.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);

        var tool = ResponseTool.CreateMcpTool(
            serverLabel: _options.ServerLabel,
            serverUri: _options.ServerUri,
            serverDescription: null,
            authorizationToken: authorizationToken,
            allowedTools: allowedTools,
            toolCallApprovalPolicy: approvalPolicy);

        return [tool];
    }

    public async Task<string> GetPromptAsync(string name, string promptType,
        CancellationToken cancellationToken = default)
    {
        var client = await _client.Value.ConfigureAwait(false);
        var result = await client.GetPromptAsync(name, new Dictionary<string, object?>
            {
                { "promptType", promptType }
            }, cancellationToken: cancellationToken).ConfigureAwait(false);

        var text = new StringBuilder();
        foreach (var message in result.Messages)
        {
            if (message.Content is TextContentBlock textContent)
            {
                text.AppendLine(textContent.Text);
            }
        }

        return text.ToString();
    }

    public async ValueTask DisposeAsync()
    {
        _toolNamesLock.Dispose();

        if (!_client.IsValueCreated)
        {
            return;
        }

        var client = await _client.Value.ConfigureAwait(false);
        await client.DisposeAsync().ConfigureAwait(false);
    }

    private async Task<HashSet<string>> GetAvailableToolNamesAsync(McpClient client, CancellationToken cancellationToken)
    {
        if (_cachedToolNames is not null && DateTimeOffset.UtcNow - _toolNamesCachedAt < _options.ToolListCacheDuration)
        {
            return _cachedToolNames;
        }

        await _toolNamesLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cachedToolNames is not null && DateTimeOffset.UtcNow - _toolNamesCachedAt < _options.ToolListCacheDuration)
            {
                return _cachedToolNames;
            }

            var serverTools = await client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            _cachedToolNames = [.. serverTools.Select(t => t.Name)];
            _toolNamesCachedAt = DateTimeOffset.UtcNow;
            return _cachedToolNames;
        }
        finally
        {
            _toolNamesLock.Release();
        }
    }

    private async Task<McpClient> ConnectAsync()
    {
        var httpClient = _httpClientFactory.CreateClient(_httpClientName);

        var transportOptions = new HttpClientTransportOptions
        {
            Endpoint = _options.ServerUri,
            Name = _options.ServerLabel,
            TransportMode = HttpTransportMode.StreamableHttp,
        };

        var transport = new HttpClientTransport(transportOptions, httpClient);

        try
        {
            var client = await McpClient.CreateAsync(transport, new McpClientOptions { ProtocolVersion = _options.ProtocolVersion })
                .ConfigureAwait(false);
            
            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to MCP server '{ServerLabel}' at {ServerUri}", _options.ServerLabel, _options.ServerUri);
            throw;
        }
    }
}
