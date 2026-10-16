using Azure.AI.Projects;
using Azure.Core;
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
    /// Registers everything from the <c>"AiAgents"</c> section: Foundry, prompts, version pins, Azure AI Search (when set),
    /// MCP servers and each agent's tools. Each service uses its own credential, or the default.
    /// </summary>
    /// <param name="configure">Adds the app's agents and any non-MCP tools, e.g. <c>agents => agents.AddAgents(...)</c>.</param>
    /// <exception cref="InvalidOperationException">A setting is missing or invalid; the message lists them all.</exception>
    public static IServiceCollection AddAiAgents(this IServiceCollection services, IConfiguration configuration,
        Action<AiAgentsBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddLogging();   // every library service logs; a no-op if the app already added logging
        var builder = new AiAgentsBuilder(services);
        configure?.Invoke(builder);

        var section = configuration.GetSection(AiAgentsOptions.SectionName);
        var options = section.Get<AiAgentsOptions>() ?? new AiAgentsOptions();
        options.ExternallyManagedAgents.ReadAgents(section.GetSection(nameof(AiAgentsOptions.ExternallyManagedAgents)));
        builder.ApplyTo(options);

        // A Search section with only Endpoint and Indexes binds no SearchSettings property; it's still in use.
        if (section.GetSection("Search").Exists())
        {
            options.Search ??= new AiAgentsOptions.SearchSettings();
        }

        var missing = options.MissingSettings();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(string.Format(Constants.ErrorMessages.AiAgentsSettingsMissing, string.Join(", ", missing)));
        }

        var foundryCredential = options.CredentialFor(nameof(AiAgentsService.Foundry), options.Foundry.Authentication);

        services.AddSingleton(options.ToRunOptions());
        services.AddSingleton(options.ToVersionPinning());
        services.AddFoundryAgents(_ => new Uri(options.Foundry.Endpoint!), _ => foundryCredential,
            _ => new FoundryAgentFactoryOptions(options.Foundry.DefaultModel!)
            {
                KeepLatestVersions = options.KeepLatestVersions,
                AgentCacheDuration = options.AgentCacheDuration,
            }, options.MaxRetries);
        services.AddFilePrompts(section, options.ResponseFormatKey, options.ResponseFormatExemptPromptTypes);

        if (options.EnableDriftDetection)
        {
            services.AddHostedService<PinnedAgentVersionDriftValidator>();
        }

        if (options.Search is { } search)
        {
            services.AddAzureSearchContextRetriever(section.GetSection("Search"),
                options.CredentialFor(nameof(AiAgentsService.Search), search.Authentication));
        }

        foreach (var (key, server) in options.McpServers)
        {
            services.AddMcpClientServices(key, new McpServerConnectionOptions
            {
                ServerLabel = key,
                ServerUri = new Uri(server.ServerUri!),
                AllowedToolNames = server.AllowedToolNames,
                ToolListCacheDuration = server.ToolListCacheDuration,
                Credential = options.CredentialFor(AiAgentsOptions.McpCredentialKey(key), server.Authentication),
                Scope = server.Scope!,
            });
        }

        if (options.GlobalConcurrency.MaxConcurrentRuns is int maxConcurrentRuns)
        {
            var slotContainer = new BlobContainerClient(new Uri(options.GlobalConcurrency.BlobContainerUri!),
                options.CredentialFor(nameof(AiAgentsService.RunSlots), options.GlobalConcurrency.Authentication));
            services.AddSingleton(sp => new BlobRunSlotStore(slotContainer, maxConcurrentRuns, sp.GetService<ILogger<BlobRunSlotStore>>()));
            services.AddSingleton<IRunSlotStore>(sp => sp.GetRequiredService<BlobRunSlotStore>());
            services.AddHostedService<RunSlotStartupValidator>();
        }

        RegisterAgents(services, builder.Definitions, options, foundryCredential);
        return services;
    }

    /// <summary>
    /// Registers the builder's definitions, binds each agent's allowed MCP tools to the server that allows them,
    /// and marks the configured agents as externally managed.
    /// </summary>
    private static void RegisterAgents(IServiceCollection services, IReadOnlyList<AgentDefinition> definitions, AiAgentsOptions options,
        TokenCredential foundryCredential)
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

        RegisterExternallyManagedAgents(services, options, foundryCredential);
    }

    /// <summary>
    /// Agents another pipeline provisions. In this app's project they're resolved at their pinned version; in another
    /// project (<c>Endpoint</c> set) they're resolved and run there, with its own credential or this app's Foundry one.
    /// </summary>
    private static void RegisterExternallyManagedAgents(IServiceCollection services, AiAgentsOptions options, TokenCredential foundryCredential)
    {
        var external = options.ExternallyManagedAgents;
        if (!external.InOtherProject)
        {
            foreach (var agentName in external.Agents.Keys)
            {
                services.AddSingleton<IManagedAgentProvider>(sp => new ExternallyManagedAgentProvider(agentName,
                    sp.GetRequiredService<IAgentFactory>(), sp.GetRequiredService<IAgentRuntime>()));
            }

            return;
        }

        var credential = options.ExternallyManagedCredentialFor(foundryCredential);
        services.AddSingleton(sp =>
        {
            var client = new AIProjectClient(new Uri(external.Endpoint!), credential,
                new AIProjectClientOptions { RetryPolicy = new ClientRetryPolicy(options.MaxRetries) });
            return new ExternalFoundryProject(credential, client.AgentAdministrationClient, new FoundryConversationClient(client.ProjectOpenAIClient),
                new FoundryAgentFactoryOptions(options.Foundry.DefaultModel!) { AgentCacheDuration = options.AgentCacheDuration },
                sp.GetRequiredService<AgentRunOptions>(), sp.GetRequiredService<IAgentRunLimiter>(), sp.GetRequiredService<ILoggerFactory>());
        });

        foreach (var (agentName, version) in external.Agents)
        {
            services.AddSingleton<IManagedAgentProvider>(sp => new ExternalProjectAgentProvider(agentName,
                AiAgentsOptions.FollowsLatest(version) ? null : version, sp.GetRequiredService<ExternalFoundryProject>));
        }
    }

    private sealed class StaticAgentDefinitionProvider(IReadOnlyCollection<AgentDefinition> definitions) : IAgentDefinitionProvider
    {
        public IReadOnlyCollection<AgentDefinition> GetAgentsDefinitions() => definitions;
    }

    // ===== Building blocks for AddAiAgents; internal so apps have one way to register. =====

    /// <summary>The Foundry services, over a registered <see cref="AIProjectClient"/>.</summary>
    internal static IServiceCollection AddFoundryAgents(this IServiceCollection services,
        Func<IServiceProvider, FoundryAgentFactoryOptions> optionsFactory)
    {
        services.AddSingleton(optionsFactory);
        services.TryAddSingleton(new AgentRunOptions());

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
        services.AddSingleton<Quality.IAgentTestRunner>(sp => new Quality.AgentTestRunner(sp.GetRequiredService<IAgentService>(),
            sp.GetService<Quality.IAgentRunEvaluator>()));

        return services;
    }

    internal static IServiceCollection AddFoundryAgents(this IServiceCollection services, Func<IServiceProvider, Uri> endpoint,
        Func<IServiceProvider, TokenCredential> credential, Func<IServiceProvider, FoundryAgentFactoryOptions> optionsFactory,
        int maxRetries = 3)
    {
        services.AddSingleton(sp => new AIProjectClient(endpoint(sp), credential(sp),
            new AIProjectClientOptions { RetryPolicy = new ClientRetryPolicy(maxRetries) }));

        return services.AddFoundryAgents(optionsFactory);
    }

    internal static IServiceCollection AddAzureSearchContextRetriever(this IServiceCollection services, IConfiguration section,
        TokenCredential credential)
    {
        services.AddOptions<AzureSearchContextRetrieverOptions>().Bind(section)
            .Validate(options => options.IndexesAreValid, Constants.ErrorMessages.AzureSearchIndexesInvalid).ValidateOnStart();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AzureSearchContextRetrieverOptions>>().Value);
        services.AddSingleton<IRelevanceFilter>(sp =>
            new RelativeScoreRelevanceFilter(sp.GetRequiredService<AzureSearchContextRetrieverOptions>().MinimumRelevanceFilter));

        services.AddSingleton<IContextRetriever>(sp =>
        {
            var config = sp.GetRequiredService<AzureSearchContextRetrieverOptions>();
            var clientOptions = new SearchClientOptions { Retry = { MaxRetries = config.MaxRetryAttempts, Mode = RetryMode.Exponential } };
            var clients = config.Indexes.ToDictionary(index => index.Name,
                index => new SearchClient(new Uri(config.Endpoint), index.Name, credential, clientOptions));

            return new AzureSearchContextRetriever(clients, sp.GetRequiredService<IRelevanceFilter>(),
                sp.GetRequiredService<ILogger<AzureSearchContextRetriever>>(),
                config.Indexes.ToDictionary(index => index.Name, index => index.ContentFields));
        });

        return services;
    }

    /// <summary>One MCP server, keyed by <paramref name="serverKey"/>: its connection, sign-in and startup check.</summary>
    internal static IServiceCollection AddMcpClientServices(this IServiceCollection services, string serverKey,
        McpServerConnectionOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverKey);

        var httpClientName = $"{McpToolClient.HttpClientName}:{serverKey}";
        services.AddKeyedSingleton(serverKey, options);
        services.AddKeyedSingleton<ITokenService>(serverKey, (sp, _) =>
            new TokenService(options.Credential, options.Scope, sp.GetService<ILogger<TokenService>>()));
        services.AddHttpClient(httpClientName)
            .AddHttpMessageHandler(sp => new McpAuthenticationHandler(sp.GetRequiredKeyedService<ITokenService>(serverKey)));
        services.AddKeyedSingleton<IMcpToolClient>(serverKey, (sp, _) => new McpToolClient(options,
            sp.GetRequiredService<IHttpClientFactory>(), sp.GetRequiredService<ILogger<McpToolClient>>(), httpClientName));

        // Not AddHostedService: it dedups by type, which would drop every server after the first.
        services.AddSingleton<IHostedService>(sp => new McpToolStartupValidator(serverKey,
            sp.GetRequiredKeyedService<IMcpToolClient>(serverKey), options, sp.GetRequiredService<ILogger<McpToolStartupValidator>>()));

        return services;
    }

    /// <summary>Prompt files from <paramref name="configuration"/>'s <c>PromptFiles</c> section.</summary>
    internal static IServiceCollection AddFilePrompts(this IServiceCollection services, IConfiguration configuration,
        string? responseFormatKey = null, IReadOnlySet<string>? responseFormatExemptPromptTypes = null)
    {
        services.AddOptions<PromptFileOptions>().Bind(configuration.GetSection("PromptFiles")).ValidateOnStart();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<PromptFileOptions>>().Value);
        services.AddSingleton<IPromptFileReader, FileSystemPromptFileReader>();
        services.AddSingleton<IPromptTemplateStore>(sp => new FilePromptTemplateStore(
            sp.GetRequiredService<PromptFileOptions>().UserPrompts, sp.GetRequiredService<IPromptFileReader>().Read));
        services.AddSingleton<IPromptTemplateBuilder, PromptTemplateBuilder>();
        services.AddSingleton<IPromptProvider>(sp =>
        {
            var systemPrompts = new FilePromptTemplateStore(sp.GetRequiredService<PromptFileOptions>().SystemPrompts,
                sp.GetRequiredService<IPromptFileReader>().Read);
            return new FilePromptProvider(systemPrompts, sp.GetRequiredService<IPromptTemplateStore>(), responseFormatKey,
                responseFormatExemptPromptTypes);
        });

        return services;
    }
}
