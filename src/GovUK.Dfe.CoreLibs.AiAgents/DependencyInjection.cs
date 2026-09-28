using Azure.AI.Projects;
using Azure.Core;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Storage.Blobs;
using GovUK.Dfe.CoreLibs.AiAgents.Agents;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Concurrency;
using GovUK.Dfe.CoreLibs.AiAgents.Context;
using GovUK.Dfe.CoreLibs.AiAgents.Context.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Orchestration;
using GovUK.Dfe.CoreLibs.AiAgents.Orchestration.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Prompts;
using GovUK.Dfe.CoreLibs.AiAgents.Prompts.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Tools;
using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;
using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ClientModel.Primitives;

namespace GovUK.Dfe.CoreLibs.AiAgents;

public static class DependencyInjection
{
    /// <summary>
    /// Registers everything from the <c>"AiAgents"</c> configuration section: the Foundry project, prompt files,
    /// version pins, Azure AI Search (when <c>AiAgents:Search</c> is set), MCP servers, and each agent's MCP tools.
    /// One Microsoft Entra ID service principal (<c>AiAgents:Authentication</c>) signs in to all of them.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The app's configuration root.</param>
    /// <param name="configure">Adds the app's agents and any non-MCP tools, e.g. <c>agents => agents.AddAgents(...)</c>.</param>
    /// <exception cref="InvalidOperationException">A setting is missing or invalid; the message lists them all.</exception>
    public static IServiceCollection AddAiAgents(this IServiceCollection services, IConfiguration configuration,
        Action<AiAgentsBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var builder = new AiAgentsBuilder(services);
        configure?.Invoke(builder);

        var section = configuration.GetSection(AiAgentsOptions.SectionName);
        var options = section.Get<AiAgentsOptions>() ?? new AiAgentsOptions();
        builder.ApplyTo(options);

        var missing = options.MissingSettings();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(string.Format(Constants.ErrorMessages.AiAgentsSettingsMissing, string.Join(", ", missing)));
        }

        var credential = options.CreateCredential();
        var execution = options.ToExecutionOptions();

        services.AddSingleton(execution.ToRunOptions());
        services.AddFoundryAgents(_ => new Uri(options.Foundry.Endpoint!), _ => credential,
            _ => new FoundryAgentFactoryOptions(options.Foundry.DefaultModel!)
            {
                KeepLatestVersions = options.KeepLatestVersions,
                AgentCacheDuration = options.AgentCacheDuration,
            }, options.MaxRetries);

        if (options.GlobalConcurrency.MaxConcurrentRuns is int maxConcurrentRuns)
        {
            var slotContainer = new Uri(options.GlobalConcurrency.BlobContainerUri!);
            services.AddSingleton<IRunSlotStore>(sp => new BlobRunSlotStore(new BlobContainerClient(slotContainer, credential),
                maxConcurrentRuns, sp.GetService<ILogger<BlobRunSlotStore>>()));
        }
        services.AddFilePrompts(section, execution.ResponseFormatKey, execution.ResponseFormatExemptPromptTypes);
        services.AddSingleton(options.ToVersionPinning());

        if (options.EnableDriftDetection)
        {
            services.AddPinnedAgentVersionDriftDetection();
        }

        if (section.GetSection("Search").Exists())
        {
            services.AddAzureSearchContextRetriever(section, _ => credential, sectionName: "Search");
        }

        foreach (var (key, server) in options.McpServers)
        {
            services.AddMcpClientServices(key, _ => new McpServerConnectionOptions
            {
                ServerLabel = key,
                ServerUri = new Uri(server.ServerUri!),
                AllowedToolNames = server.AllowedToolNames,
                ToolListCacheDuration = server.ToolListCacheDuration,
                Authentication = new McpServerAuthenticationConfig { Credential = credential, Scope = server.Scope! },
            });
        }

        RegisterAgents(services, builder.Definitions, options);
        return services;
    }

    /// <summary>
    /// Registers the builder's definitions, binds each agent's allowed MCP tools to the server that allows
    /// them, and marks the configured agents as externally managed.
    /// </summary>
    private static void RegisterAgents(IServiceCollection services, IReadOnlyList<AgentDefinition> definitions, AiAgentsOptions options)
    {
        if (definitions.Count > 0)
        {
            services.AddSingleton<IAgentDefinitionProvider>(new StaticAgentDefinitionProvider(definitions));
        }

        foreach (var definition in definitions)
        {
            foreach (var (key, server) in options.McpServers)
            {
                var tools = server.AllowedToolNames.Where(name => definition.AllowedTools.Contains(McpToolClient.ToFunctionName(name))).ToList();
                if (tools.Count > 0)
                {
                    services.AddSingleton(sp => new AgentToolBinding(definition.Name,
                        new McpAllowedToolsProvider(sp.GetRequiredKeyedService<IMcpToolClient>(key), tools)));
                }
            }
        }

        foreach (var agentName in options.ExternallyManagedAgents.Keys)
        {
            services.AddSingleton<IManagedAgentProvider>(sp => new ExternallyManagedAgentProvider(agentName,
                sp.GetRequiredService<IAgentFactory>(), sp.GetRequiredService<IAgentRuntime>()));
        }
    }

    private sealed class StaticAgentDefinitionProvider(IReadOnlyCollection<AgentDefinition> definitions) : IAgentDefinitionProvider
    {
        public IReadOnlyCollection<AgentDefinition> GetAgentsDefinitions() => definitions;
    }

    // ===== Building blocks for AddAiAgents; internal so apps have one way to register. =====

    internal static IServiceCollection AddFoundryAgents(this IServiceCollection services,
        Func<IServiceProvider, FoundryAgentFactoryOptions> optionsFactory)
    {
        services.AddSingleton(optionsFactory);
        services.TryAddSingleton(new AgentRunOptions());

        // Mandatory however the library is registered; turned off only through RequireTokenUsageTelemetry.
        services.AddHostedService<Diagnostics.TokenUsageTelemetryValidator>();
        services.AddHostedService<AgentToolCompatibilityValidator>();
        services.AddSingleton(sp => sp.GetRequiredService<AIProjectClient>().AgentAdministrationClient);
        services.AddSingleton(sp => sp.GetRequiredService<AIProjectClient>().ProjectOpenAIClient);
        services.AddSingleton<IAgentFactory, FoundryAgentFactory>();
        services.AddSingleton<IFoundryConversationClient, FoundryConversationClient>();
        services.AddSingleton<IAgentRunLimiter>(sp => new AgentRunLimiter(sp.GetRequiredService<AgentRunOptions>(), sp.GetService<IRunSlotStore>()));
        services.AddSingleton<IAgentRunner, FoundryAgentRunner>();
        services.AddSingleton<IAgentOrchestrator, AgentOrchestrator>();
        services.AddSingleton<IAgentRuntime, AgentRuntime>();
        services.AddSingleton(sp => new AgentSpecBuilder(sp.GetRequiredService<IPromptProvider>(),
            sp.GetServices<AgentToolBinding>(), sp.GetServices<IManagedAgentProvider>()));
        services.AddSingleton<IAgentService, AgentService>();

        return services;
    }

    internal static IServiceCollection AddFoundryAgents(this IServiceCollection services,
        Func<IServiceProvider, Uri> endpoint,
        Func<IServiceProvider, TokenCredential> credential,
        Func<IServiceProvider, FoundryAgentFactoryOptions> optionsFactory,
        int maxRetries = 3)
    {
        services.AddSingleton(sp => new AIProjectClient(endpoint(sp), credential(sp),
            new AIProjectClientOptions { RetryPolicy = new ClientRetryPolicy(maxRetries) }));

        return services.AddFoundryAgents(optionsFactory);
    }

    /// <param name="credential">Azure Search's credential. Without one, the section's client-secret fields are used.</param>
    internal static IServiceCollection AddAzureSearchContextRetriever(this IServiceCollection services, IConfiguration configuration,
        Func<IServiceProvider, TokenCredential>? credential = null, string sectionName = "AzureSearch")
    {
        SetConfiguration<AzureSearchContextRetrieverOptions>(services, configuration, sectionName);
        services.AddOptions<AzureSearchContextRetrieverOptions>()
            .Validate(options => options.IndexesAreValid, Constants.ErrorMessages.AzureSearchIndexesInvalid);
        services.AddSingleton<IRelevanceFilter>(sp =>
            new RelativeScoreRelevanceFilter(sp.GetRequiredService<AzureSearchContextRetrieverOptions>().MinimumRelevanceFilter));

        services.AddSingleton<IContextRetriever>(sp =>
        {
            var config = sp.GetRequiredService<AzureSearchContextRetrieverOptions>();
            var searchCredential = credential?.Invoke(sp)
                ?? (config.HasClientSecretCredential
                    ? new ClientSecretCredential(config.TenantId, config.ClientId, config.ClientSecret)
                    : throw new InvalidOperationException(Constants.ErrorMessages.AzureSearchCredentialMissing));

            var clientOptions = new SearchClientOptions { Retry = { MaxRetries = config.MaxRetryAttempts, Mode = RetryMode.Exponential } };
            var clients = config.Indexes.ToDictionary(index => index.Name, index => new SearchClient(
                new Uri(config.Endpoint), index.Name, searchCredential, clientOptions));

            return new AzureSearchContextRetriever(clients, sp.GetRequiredService<IRelevanceFilter>(),
                sp.GetRequiredService<ILogger<AzureSearchContextRetriever>>(),
                config.Indexes.ToDictionary(index => index.Name, index => index.ContentFields));
        });

        return services;
    }

    /// <summary>Binds per-environment version pins from the <c>"Agents"</c> section.</summary>
    internal static IServiceCollection AddAgentVersionPinning(this IServiceCollection services, IConfiguration configuration)
    {
        SetConfiguration<AgentVersionPinningOptions>(services, configuration, "Agents");
        return services;
    }

    internal static IServiceCollection AddPinnedAgentVersionDriftDetection(this IServiceCollection services)
    {
        services.AddHostedService<PinnedAgentVersionDriftValidator>();
        return services;
    }

    /// <summary>
    /// Registers one MCP server, keyed by <paramref name="serverKey"/>: its connection, token flow and
    /// startup check. Call once per server.
    /// </summary>
    internal static IServiceCollection AddMcpClientServices(this IServiceCollection services, string serverKey,
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
            sp.GetRequiredService<IHttpClientFactory>(),
            sp.GetRequiredService<ILogger<McpToolClient>>(),
            mcpHttpClientName));

        // Not AddHostedService: it dedups by type and would drop every server after the first.
        services.AddSingleton<IHostedService>(sp => new McpToolStartupValidator(serverKey,
            sp.GetRequiredKeyedService<IMcpToolClient>(serverKey), sp.GetRequiredKeyedService<McpServerConnectionOptions>(serverKey),
            sp.GetRequiredService<ILogger<McpToolStartupValidator>>()));

        return services;
    }

    /// <summary>
    /// Registers file-backed prompts from <paramref name="configurationName"/>. A missing or unreadable prompt
    /// file throws rather than being replaced with other text.
    /// </summary>
    internal static IServiceCollection AddFilePrompts(this IServiceCollection services, IConfiguration configuration,
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
            var systemPrompts = new FilePromptTemplateStore(options.SystemPrompts, sp.GetRequiredService<IPromptFileReader>().Read);

            return new FilePromptProvider(systemPrompts, sp.GetRequiredService<IPromptTemplateStore>(),
                responseFormatKey, responseFormatExemptPromptTypes);
        });

        return services;
    }

    /// <summary>Foundry, prompt files, version pins and drift detection, with credentials supplied in code.</summary>
    internal static IServiceCollection AddAgentExecution(this IServiceCollection services, IConfiguration configuration,
        Func<IServiceProvider, Uri> endpoint,
        Func<IServiceProvider, TokenCredential> credential,
        Func<IServiceProvider, FoundryAgentFactoryOptions> optionsFactory,
        AgentExecutionOptions? agentExecutionOptions = null)
    {
        agentExecutionOptions ??= new AgentExecutionOptions();

        services.AddSingleton(agentExecutionOptions.ToRunOptions());
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

    /// <summary>Azure AI Search and any number of MCP servers. Pass neither and this does nothing.</summary>
    internal static IServiceCollection AddAgentContextAndTools(this IServiceCollection services, IConfiguration configuration,
        bool enableAzureSearch = false, IReadOnlyList<McpServerRegistration>? mcpServers = null,
        Func<IServiceProvider, TokenCredential>? searchCredential = null)
    {
        if (enableAzureSearch)
        {
            services.AddAzureSearchContextRetriever(configuration, searchCredential);
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
