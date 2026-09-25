using Azure.AI.Projects;
using Azure.Core;
using Azure.Identity;
using Azure.Search.Documents;
using GovUK.Dfe.CoreLibs.AiAgents.Context;
using GovUK.Dfe.CoreLibs.AiAgents.Context.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Orchestration;
using GovUK.Dfe.CoreLibs.AiAgents.Orchestration.Inerfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Prompts;
using GovUK.Dfe.CoreLibs.AiAgents.Prompts.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;
using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ClientModel.Primitives;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Agents;

namespace GovUK.Dfe.CoreLibs.AiAgents;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Foundry Agent Service using an existing <see cref="AIProjectClient"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="optionsFactory">Creates the agent factory options.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddFoundryAgents(this IServiceCollection services,
        Func<IServiceProvider, FoundryAgentFactoryOptions> optionsFactory)
    {
        services.AddSingleton(optionsFactory);
        services.AddSingleton(sp => sp.GetRequiredService<AIProjectClient>().AgentAdministrationClient);
        services.AddSingleton(sp => sp.GetRequiredService<AIProjectClient>().ProjectOpenAIClient);
        services.AddSingleton<IAgentFactory, FoundryAgentFactory>();
        services.AddSingleton<IFoundryConversationClient, FoundryConversationClient>();
        services.AddSingleton<IAgentRunner, FoundryAgentRunner>();
        services.AddSingleton<IAgentOrchestrator, AgentOrchestrator>();
        services.AddSingleton<IAgentRuntime, AgentRuntime>();
        services.AddSingleton<ISpecialistAgentRunner, SpecialistAgentRunner>();

        return services;
    }

    /// <summary>
    /// Registers the Foundry Agent Service and configures its AI project client.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="endpoint">Creates the Foundry Agent Service endpoint.</param>
    /// <param name="credential">Creates the authentication credential.</param>
    /// <param name="optionsFactory">Creates the agent factory options.</param>
    /// <param name="maxRetries">The maximum number of client retries.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddFoundryAgents(this IServiceCollection services,
        Func<IServiceProvider, Uri> endpoint,
        Func<IServiceProvider, TokenCredential> credential,
        Func<IServiceProvider, FoundryAgentFactoryOptions> optionsFactory,
        int maxRetries = 3)
    {
        services.AddSingleton(sp =>
        {
            var options = new AIProjectClientOptions
            {
                RetryPolicy = new ClientRetryPolicy(maxRetries),
            };

            return new AIProjectClient(endpoint(sp), credential(sp), options);
        });

        return services.AddFoundryAgents(optionsFactory);
    }

    /// <summary>
    /// Registers <see cref="AzureSearchContextRetriever"/> and its dependencies with the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="options">The Azure Search configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddAzureSearchContextRetriever(this IServiceCollection services,IConfiguration configuration)
    {
        SetConfiguration<AzureSearchContextRetrieverOptions>(services, configuration, "AzureSearch");
        services.AddSingleton<IRelevanceFilter>(sp =>
        {
            var options = sp.GetRequiredService<AzureSearchContextRetrieverOptions>(); 
            return new RelativeScoreRelevanceFilter(options.MinimumRelevanceFilter);
        });

        services.AddSingleton<IContextRetriever>(sp =>
        {
            var config = sp.GetRequiredService<AzureSearchContextRetrieverOptions>();
            var credential = new ClientSecretCredential(config.TenantId, config.ClientId, config.ClientSecret);

            var clientOptions = new SearchClientOptions
            {
                Retry =
                {
                    MaxRetries = config.MaxRetryAttemps,
                    Mode = RetryMode.Exponential
                }
            };
            var clients = config.Indexes.ToDictionary(indexName => indexName, indexName => new SearchClient(
                new Uri(config.Endpoint), indexName, credential, clientOptions));

            return new AzureSearchContextRetriever(clients, sp.GetRequiredService<IRelevanceFilter>(),
                sp.GetRequiredService<ILogger<AzureSearchContextRetriever>>());
        });

        return services;
    }

    /// <summary>
    /// Binds <see cref="AgentVersionPinningOptions"/> from configuration - the per-environment
    /// version pins described in the target architecture's promotion flow (local/dev floats to
    /// latest; staging/production pin a specific version).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAgentVersionPinning(this IServiceCollection services, IConfiguration configuration)
    {
        SetConfiguration<AgentVersionPinningOptions>(services, configuration, "Agents");
        return services;
    }

    /// <summary>
    /// Registers a hosted service that checks for drift between the pinned agent version and the latest available version, 
    /// logging a warning if a drift is detected. This is useful for environments where you want to ensure that agents are running the expected version and to be alerted if they are not.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddPinnedAgentVersionDriftDetection(this IServiceCollection services)
    {
        services.AddHostedService<PinnedAgentVersionDriftValidator>();
        return services;
    }

    /// <summary>
    /// Registers services for discovering and using tools and prompts from a remote MCP server, keyed by
    /// <paramref name="serverKey"/> so this can be called more than once to wire up several MCP servers -
    /// each gets its own connection, token flow, and cached tool-name list, resolved via
    /// <c>GetRequiredKeyedService&lt;IMcpToolClient&gt;(serverKey)</c> (or bind it straight to an agent's
    /// tools with <see cref="AgentToolBinding"/>). A misconfigured server (missing/empty required field)
    /// fails fast at startup via the registered <see cref="McpToolStartupValidator"/>, not on first use.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serverKey">A key identifying this server, unique across every <c>AddMcpClientServices</c> call - e.g. its <c>ServerLabel</c>.</param>
    /// <param name="optionsFactory">Creates this server's connection options.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddMcpClientServices(this IServiceCollection services, string serverKey,
        Func<IServiceProvider, McpServerConnectionOptions> optionsFactory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverKey);

        var tokenHttpClientName = $"{TokenService.HttpClientName}:{serverKey}";
        var mcpHttpClientName = $"{McpToolClient.HttpClientName}:{serverKey}";

        services.AddKeyedSingleton(serverKey, (sp, _) => optionsFactory(sp));
        services.AddHttpClient(tokenHttpClientName);

        services.AddKeyedSingleton<ITokenService>(serverKey, (sp, _) => new TokenService(
            sp.GetRequiredKeyedService<McpServerConnectionOptions>(serverKey),
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(tokenHttpClientName),
            sp.GetService<ILogger<TokenService>>()));

        services.AddHttpClient(mcpHttpClientName)
            .AddHttpMessageHandler(sp => new McpAuthenticationHandler(sp.GetRequiredKeyedService<ITokenService>(serverKey)));

        services.AddKeyedSingleton<IMcpToolClient>(serverKey, (sp, _) => new McpToolClient(
            sp.GetRequiredKeyedService<McpServerConnectionOptions>(serverKey),
            sp.GetRequiredKeyedService<ITokenService>(serverKey),
            sp.GetRequiredService<IHttpClientFactory>(),
            sp.GetRequiredService<ILogger<McpToolClient>>(),
            mcpHttpClientName));

        // Plain AddSingleton<IHostedService>, not AddHostedService<McpToolStartupValidator>(...) - the
        // latter uses TryAddEnumerable, which dedups by implementation type and would silently drop
        // every server after the first when this method is called more than once.
        services.AddSingleton<IHostedService>(sp => new McpToolStartupValidator(serverKey,
            sp.GetRequiredKeyedService<IMcpToolClient>(serverKey), sp.GetRequiredKeyedService<McpServerConnectionOptions>(serverKey),
            sp.GetRequiredService<ILogger<McpToolStartupValidator>>()));

        return services;
    }
    /// <summary>
    /// Registers file-backed prompt retrieval: binds <see cref="PromptFileOptions"/> from
    /// configuration, and registers <see cref="IPromptProvider"/> (as <see cref="FilePromptProvider"/>)
    /// and <see cref="IPromptTemplateBuilder"/> so a consumer only needs to supply agent definitions
    /// and prompt file paths in configuration - everything else (loading, caching-per-call, appending
    /// a shared response format) is handled by the library. A prompt type with no configured file path
    /// throws immediately, naming the missing type; a configured path that can't be read propagates
    /// as whatever the underlying I/O failure is - neither case is silently substituted with different
    /// prompt content, since that would change agent behaviour without any visible error.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="responseFormatKey">
    /// The system-prompt key holding a shared response format to append to every system prompt, or
    /// <see langword="null"/> to never append one.
    /// </param>
    /// <param name="responseFormatExemptPromptTypes">System prompt types that should not have the response format appended.</param>
    /// <param name="configurationName">The configuration section to bind <see cref="PromptFileOptions"/> from.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddFilePrompts(this IServiceCollection services, IConfiguration configuration,
        string? responseFormatKey = null, IReadOnlySet<string>? responseFormatExemptPromptTypes = null,
        string configurationName = "PromptFiles")
    {
        SetConfiguration<PromptFileOptions>(services, configuration, configurationName);
        services.AddSingleton<IPromptFileReader, FileSystemPromptFileReader>();

        services.AddSingleton<IPromptTemplateStore>(sp => new FilePromptTemplateStore(
            sp.GetRequiredService<PromptFileOptions>().UserPrompts, sp.GetRequiredService<IPromptFileReader>().Read));
        services.AddSingleton<IPromptTemplateBuilder, PromptTemplateBuilder>();

        services.AddSingleton<IPromptProvider>(sp =>
        {
            var options = sp.GetRequiredService<PromptFileOptions>();
            var fileReader = sp.GetRequiredService<IPromptFileReader>();
            var systemPrompts = new FilePromptTemplateStore(options.SystemPrompts, fileReader.Read);

            return new FilePromptProvider(systemPrompts, sp.GetRequiredService<IPromptTemplateStore>(),
                responseFormatKey, responseFormatExemptPromptTypes);
        });

        return services;
    }

    /// <summary>
    /// Registers Foundry Agent Service, file-backed prompt retrieval, and optional version pinning and drift detection in one call. See <see cref="AddFoundryAgents(IServiceCollection, Func{IServiceProvider, Uri}, Func{IServiceProvider, TokenCredential}, Func{IServiceProvider, FoundryAgentFactoryOptions}, int)"/>, <see cref="AddFilePrompts(IServiceCollection, IConfiguration, string?, IReadOnlySet{string}?, string)"/>, <see cref="AddAgentVersionPinning(IServiceCollection, IConfiguration)"/> and <see cref="AddPinnedAgentVersionDriftDetection(IServiceCollection)"/> for details.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="endpoint">A function that provides the endpoint URI.</param>
    /// <param name="credential">A function that provides the token credential.</param>
    /// <param name="optionsFactory">A function that provides the foundry agent factory options.</param>
    /// <param name="agentExecutionOptions">The optional/secondary settings - response format, version pinning, drift detection, retries. Defaults apply when omitted.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAgentExecution(this IServiceCollection services, IConfiguration configuration,
        Func<IServiceProvider, Uri> endpoint,
        Func<IServiceProvider, TokenCredential> credential,
        Func<IServiceProvider, FoundryAgentFactoryOptions> optionsFactory,
        AgentExecutionOptions? agentExecutionOptions = null)
    {
        agentExecutionOptions ??= new AgentExecutionOptions();

        services.AddFoundryAgents(endpoint, credential, optionsFactory, agentExecutionOptions.MaxRetries);
        services.AddFilePrompts(configuration, agentExecutionOptions.ResponseFormatKey, agentExecutionOptions.ResponseFormatExemptPromptTypes);

        if (agentExecutionOptions.EnableVersionPinning)
        {
            services.AddAgentVersionPinning(configuration);
        }

        if (agentExecutionOptions.EnableDriftDetection)
        {
            services.AddPinnedAgentVersionDriftDetection();
        }

        return services;
    }

    /// <summary>
    /// One-call setup for optional context/tool sources: Azure AI Search RAG context
    /// (<see cref="AddAzureSearchContextRetriever"/>) and any number of MCP servers
    /// (<see cref="AddMcpClientServices"/>). Both are opt-in - pass neither argument and this is a no-op.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration, for whichever of Azure Search / MCP is enabled.</param>
    /// <param name="enableAzureSearch">Whether to register <see cref="AddAzureSearchContextRetriever"/>, binding its <c>"AzureSearch"</c> config section.</param>
    /// <param name="mcpServers">Zero or more MCP servers to register, one <see cref="AddMcpClientServices"/> call each.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAgentContextAndTools(this IServiceCollection services, IConfiguration configuration,
        bool enableAzureSearch = false, IReadOnlyList<McpServerRegistration>? mcpServers = null)
    {
        if (enableAzureSearch)
        {
            services.AddAzureSearchContextRetriever(configuration);
        }

        foreach (var server in mcpServers ?? [])
        {
            services.AddMcpClientServices(server.ServerKey, server.OptionsFactory);
        }

        return services;
    }

    private static void SetConfiguration<T>(IServiceCollection services, IConfiguration configuration, string configurationName) where T : class
    {
        services.AddOptions<T>().Bind(configuration.GetSection(configurationName)).ValidateOnStart();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<T>>().Value);
    }
}
