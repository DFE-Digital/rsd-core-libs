using Azure.Core;
using Azure.Identity;

namespace GovUK.Dfe.CoreLibs.AiAgents;

/// <summary>Everything <c>AddAiAgents</c> needs, bound from the <c>"AiAgents"</c> configuration section.</summary>
public sealed class AiAgentsOptions
{
    public const string SectionName = "AiAgents";

    /// <summary>Tags all telemetry and scopes the ephemeral-agent sweep. Defaults to the entry assembly's name.</summary>
    public string? ApplicationName { get; set; }

    public FoundrySettings Foundry { get; set; } = new();

    /// <summary>The default Microsoft Entra ID service principal, for every service without its own <c>Authentication</c> block.</summary>
    public ServicePrincipalSettings Authentication { get; set; } = new();

    /// <summary>Azure AI Search. Only its <c>Authentication</c> is read here; null when there's no <c>Search</c> section.</summary>
    public SearchSettings? Search { get; set; }

    /// <summary>MCP servers, keyed by a name you choose.</summary>
    public Dictionary<string, McpServerSettings> McpServers { get; set; } = [];

    public TimeSpan? RunTimeout { get; set; }

    /// <summary>The most agent runs this instance makes at once, across every caller.</summary>
    public int? MaxConcurrency { get; set; }

    /// <summary>A limit on agent runs shared by every instance. Off unless <c>MaxConcurrentRuns</c> is set.</summary>
    public GlobalConcurrencySettings GlobalConcurrency { get; set; } = new();

    public int MaxToolOutputCharacters { get; set; } = 20_000;

    /// <summary>Fences tool output as data the model mustn't take instructions from.</summary>
    public bool FenceToolOutput { get; set; } = true;

    /// <summary>How long a run waits for a free slot (per-instance or global) before failing with a <see cref="TimeoutException"/>.</summary>
    public TimeSpan MaxWaitForRunSlot { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>The most characters of evidence sent with one run; the rest is cut off with a note.</summary>
    public int MaxEvidenceCharacters { get; set; } = 100_000;

    public bool DeleteConversationsAfterRun { get; set; } = true;

    public bool RequireTokenUsageTelemetry { get; set; } = true;

    public bool EnableDriftDetection { get; set; }

    /// <summary>A system prompt (by key) appended to every agent's instructions, e.g. a shared answer format.</summary>
    public string? ResponseFormatKey { get; set; }

    /// <summary>Prompt types that don't get <see cref="ResponseFormatKey"/> appended.</summary>
    public HashSet<string> ResponseFormatExemptPromptTypes { get; set; } = [];

    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// When set, each new version this app creates prunes older ones, keeping this many of the newest (e.g. 3 keeps
    /// 5, 4 and 3). At least 2, so instances still on the previous version during a rolling deploy keep it.
    /// Pinned and <c>ProtectedVersions</c> are always kept. Unset (default): never prunes.
    /// </summary>
    public int? KeepLatestVersions { get; set; }

    public bool ValidateAgentToolsAtStartup { get; set; } = true;

    /// <summary>How long a resolved agent version is reused before asking Foundry again. 0 turns caching off.</summary>
    public TimeSpan AgentCacheDuration { get; set; } = TimeSpan.FromSeconds(30);
    /// <summary>Agents provisioned by another pipeline, which this app only runs; optionally in another Foundry project.</summary>
    public ExternallyManagedAgentsSettings ExternallyManagedAgents { get; set; } = new();

    /// <summary>The version this environment runs, for agents this app builds itself.</summary>
    public Dictionary<string, string> VersionPins { get; set; } = [];

    /// <summary>Versions pruning must keep, per agent, e.g. those other environments are pinned to.</summary>
    public Dictionary<string, List<string>> ProtectedVersions { get; set; } = [];

    /// <summary>Code only: the default credential, instead of <see cref="Authentication"/>. Set with <c>UseCredential</c>.</summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>Code only: per-service credentials, set with <c>UseCredentialFor</c> and <c>UseMcpCredential</c>.</summary>
    internal Dictionary<string, TokenCredential> CredentialOverrides { get; } = new(StringComparer.Ordinal);

    internal static string McpCredentialKey(string serverName) => $"Mcp:{serverName}";

    internal const string ExternallyManagedCredentialKey = nameof(ExternallyManagedAgents);

    private TokenCredential? _defaultCredential;

    /// <summary>A service's credential: its code override, else its own <c>Authentication</c> block, else the default.</summary>
    internal TokenCredential CredentialFor(string serviceKey, ServicePrincipalSettings? own)
        => CredentialOverrides.GetValueOrDefault(serviceKey)
           ?? (own is null ? _defaultCredential ??= Credential ?? Create(Authentication) : Create(own));

    /// <summary>The external project's credential: its code override, else its own block, else this app's Foundry credential.</summary>
    internal TokenCredential ExternallyManagedCredentialFor(TokenCredential foundryCredential)
        => CredentialOverrides.GetValueOrDefault(ExternallyManagedCredentialKey)
           ?? (ExternallyManagedAgents.Authentication is { } own ? Create(own) : foundryCredential);

    private static ClientSecretCredential Create(ServicePrincipalSettings principal)
        => new(principal.TenantId, principal.ClientId, principal.ClientSecret,
            new ClientSecretCredentialOptions { AuthorityHost = principal.AuthorityHost ?? AzureAuthorityHosts.AzurePublicCloud });

    /// <summary>Lists every missing or invalid setting, so startup fails once with the whole picture.</summary>
    internal IReadOnlyList<string> MissingSettings()
        => [.. FoundryProblems(), .. LimitProblems(), .. CredentialProblems(), .. VersionProblems(), .. McpServerProblems(),
            .. ExternallyManagedProblems()];

    private List<string> ExternallyManagedProblems()
    {
        const string Path = $"{SectionName}:ExternallyManagedAgents";
        var external = ExternallyManagedAgents;
        var problems = new List<string>();

        // The list form binds as { "0": "agent-name" }; catch it rather than run an agent called "0".
        if (external.Agents.Keys.Any(static key => key.All(char.IsAsciiDigit)))
        {
            problems.Add($"{Path} (use {{ \"agent-name\": \"version\" }}, not a list)");
        }

        if (external.Endpoint is not null && !Uri.TryCreate(external.Endpoint, UriKind.Absolute, out _))
        {
            problems.Add($"{Path}:Endpoint");
        }

        // A credential only means something for another project; this app's own project uses its Foundry credential.
        var hasOwnCredential = external.Authentication is not null || CredentialOverrides.ContainsKey(ExternallyManagedCredentialKey);
        if (external.Endpoint is null && hasOwnCredential)
        {
            problems.Add($"{Path}:Endpoint (a credential for externally managed agents needs the other project's Endpoint)");
        }
        else if (external.Authentication is { } own && !CredentialOverrides.ContainsKey(ExternallyManagedCredentialKey))
        {
            problems.AddRange(PrincipalProblems(own, "ExternallyManagedAgents:Authentication"));
        }

        return problems;
    }

    private static IEnumerable<string> Missing(string? value, string name)
        => string.IsNullOrWhiteSpace(value) ? [$"{SectionName}:{name}"] : [];

    private IEnumerable<string> FoundryProblems()
        => [.. Missing(Foundry.Endpoint, "Foundry:Endpoint"), .. Missing(Foundry.DefaultModel, "Foundry:DefaultModel")];

    private IEnumerable<string> LimitProblems()
    {
        (bool Invalid, string Problem)[] checks =
        [
            (KeepLatestVersions is < 2, "KeepLatestVersions (must be at least 2)"),
            (MaxConcurrency is < 1, "MaxConcurrency (must be at least 1)"),
            (AgentCacheDuration < TimeSpan.Zero, "AgentCacheDuration (0 or more)"),
            (MaxWaitForRunSlot <= TimeSpan.Zero, "MaxWaitForRunSlot (must be positive)"),
            (MaxEvidenceCharacters < 1, "MaxEvidenceCharacters (must be at least 1)"),
        ];

        return checks.Where(static check => check.Invalid).Select(static check => $"{SectionName}:{check.Problem}")
            .Concat(GlobalConcurrency.Problems().Select(static problem => $"{SectionName}:GlobalConcurrency:{problem}"));
    }

    /// <summary>Each service needs a code credential, its own complete block, or the default; the default only if used.</summary>
    private IEnumerable<string> CredentialProblems()
    {
        List<(string ServiceKey, ServicePrincipalSettings? Own, string Path)> services =
            [(nameof(AiAgentsService.Foundry), Foundry.Authentication, "Foundry:Authentication")];
        if (Search is not null)
        {
            services.Add((nameof(AiAgentsService.Search), Search.Authentication, "Search:Authentication"));
        }

        if (GlobalConcurrency.IsEnabled)
        {
            services.Add((nameof(AiAgentsService.RunSlots), GlobalConcurrency.Authentication, "GlobalConcurrency:Authentication"));
        }

        services.AddRange(McpServers.Select(server => (McpCredentialKey(server.Key), server.Value.Authentication, $"McpServers:{server.Key}:Authentication")));

        var needingOwn = services.Where(service => !CredentialOverrides.ContainsKey(service.ServiceKey)).ToList();
        var problems = needingOwn.Where(static service => service.Own is not null)
            .SelectMany(static service => PrincipalProblems(service.Own!, service.Path));

        if (Credential is null && needingOwn.Exists(static service => service.Own is null))
        {
            problems = problems.Concat(PrincipalProblems(Authentication, "Authentication"));
        }

        return problems.Concat(CredentialOverrides.Keys
            .Where(key => key.StartsWith("Mcp:", StringComparison.Ordinal) && !McpServers.ContainsKey(key[4..]))
            .Select(static key => $"{SectionName}:McpServers:{key[4..]} (UseMcpCredential names a server that isn't configured)"));
    }

    private static IEnumerable<string> PrincipalProblems(ServicePrincipalSettings principal, string path)
        => [.. Missing(principal.TenantId, $"{path}:TenantId"), .. Missing(principal.ClientId, $"{path}:ClientId"),
            .. Missing(principal.ClientSecret, $"{path}:ClientSecret")];

    // One place per agent's version, so there's nothing to keep in step.
    private IEnumerable<string> VersionProblems()
        => ExternallyManagedAgents.Agents.Keys.Where(VersionPins.ContainsKey)
            .Select(static agent => $"{SectionName}:VersionPins:{agent} (already versioned under ExternallyManagedAgents; remove one)");

    private IEnumerable<string> McpServerProblems()
        => McpServers.SelectMany(static server => (IEnumerable<string>)
        [
            .. Missing(server.Value.ServerUri, $"McpServers:{server.Key}:ServerUri"),
            .. Missing(server.Value.Scope, $"McpServers:{server.Key}:Scope"),
            .. server.Value.AllowedToolNames.Count == 0 ? [$"{SectionName}:McpServers:{server.Key}:AllowedToolNames"] : Array.Empty<string>(),
        ]);

    /// <summary>
    /// Pins from <c>VersionPins</c>, plus <c>ExternallyManagedAgents</c> versions when those agents are in this app's project.
    /// Agents in another project are resolved there, at their own version.
    /// </summary>
    internal Factories.AgentVersionPinningOptions ToVersionPinning() => new()
    {
        VersionPins = VersionPins
            .Concat(ExternallyManagedAgents.InOtherProject ? [] : ExternallyManagedAgents.Agents.Where(static agent => !FollowsLatest(agent.Value)))
            .ToDictionary(static pin => pin.Key, static pin => pin.Value),
        ProtectedVersions = ProtectedVersions.ToDictionary(static entry => entry.Key, static entry => (IReadOnlyList<string>)entry.Value),
    };

    internal static bool FollowsLatest(string? version)
        => string.IsNullOrWhiteSpace(version) || version.Equals("latest", StringComparison.OrdinalIgnoreCase);

    internal AgentRunOptions ToRunOptions() => new()
    {
        ApplicationName = string.IsNullOrWhiteSpace(ApplicationName) ? Diagnostics.AgentTelemetry.DefaultApplicationName : ApplicationName,
        RunTimeout = RunTimeout,
        MaxConcurrency = MaxConcurrency,
        MaxToolOutputCharacters = MaxToolOutputCharacters,
        FenceToolOutput = FenceToolOutput,
        MaxEvidenceCharacters = MaxEvidenceCharacters,
        MaxWaitForRunSlot = MaxWaitForRunSlot,
        DeleteConversationsAfterRun = DeleteConversationsAfterRun,
        RequireTokenUsageTelemetry = RequireTokenUsageTelemetry,
        ValidateAgentToolsAtStartup = ValidateAgentToolsAtStartup,
    };

    public sealed class FoundrySettings
    {
        /// <summary>The Foundry project endpoint, e.g. https://&lt;resource&gt;.services.ai.azure.com/api/projects/&lt;project&gt;.</summary>
        public string? Endpoint { get; set; }

        /// <summary>The model agents use unless their spec says otherwise, e.g. "my-connection/gpt-4o".</summary>
        public string? DefaultModel { get; set; }

        /// <summary>Optional: a service principal for Foundry only. Unset: the default.</summary>
        public ServicePrincipalSettings? Authentication { get; set; }
    }

    public sealed class SearchSettings
    {
        /// <summary>Optional: a service principal for Azure AI Search only. Unset: the default.</summary>
        public ServicePrincipalSettings? Authentication { get; set; }
    }

    public sealed class ServicePrincipalSettings
    {
        public string? TenantId { get; set; }

        public string? ClientId { get; set; }

        /// <summary>Keep this out of appsettings: supply it from Key Vault or an environment variable.</summary>
        public string? ClientSecret { get; set; }

        /// <summary>Defaults to the Azure public cloud.</summary>
        public Uri? AuthorityHost { get; set; }

        /// <summary>Redacts <see cref="ClientSecret"/> so it never appears in logs.</summary>
        public override string ToString() => $"{{ TenantId = {TenantId}, ClientId = {ClientId}, ClientSecret = [REDACTED] }}";
    }

    /// <summary>
    /// Caps agent runs across every instance (and every app pointed at the same container), so scaling out
    /// can't exceed the Foundry quota. Each run holds a blob lease; a crashed instance's lease expires in a minute.
    /// </summary>
    public sealed class GlobalConcurrencySettings
    {
        /// <summary>The most agent runs at once across all instances. Unset (default): no global limit.</summary>
        public int? MaxConcurrentRuns { get; set; }

        /// <summary>An existing blob container used only for run slots. Its identity needs Storage Blob Data Contributor on it.</summary>
        public string? BlobContainerUri { get; set; }

        /// <summary>Optional: a service principal for the run-slot container only. Unset: the default.</summary>
        public ServicePrincipalSettings? Authentication { get; set; }

        internal bool IsEnabled => MaxConcurrentRuns is not null;

        internal IEnumerable<string> Problems()
        {
            if (!IsEnabled)
            {
                return string.IsNullOrWhiteSpace(BlobContainerUri) ? [] : ["MaxConcurrentRuns"];
            }

            var problems = new List<string>();
            if (MaxConcurrentRuns < 1)
            {
                problems.Add("MaxConcurrentRuns (must be at least 1)");
            }

            if (!Uri.TryCreate(BlobContainerUri, UriKind.Absolute, out _))
            {
                problems.Add("BlobContainerUri");
            }

            return problems;
        }
    }

    /// <summary>
    /// <c>{ "Endpoint": ..., "Authentication": { ... }, "ofsted-agent": "4" }</c>: every key except <c>Endpoint</c> and
    /// <c>Authentication</c> is an agent, with the version to run (or <c>"latest"</c>).
    /// </summary>
    public sealed class ExternallyManagedAgentsSettings
    {
        private static readonly string[] Reserved = [nameof(Endpoint), nameof(Authentication)];

        /// <summary>Optional: the agents' Foundry project. Unset: this app's own project.</summary>
        public string? Endpoint { get; set; }

        /// <summary>Optional: a service principal for <see cref="Endpoint"/>'s project. Unset: this app's Foundry credential.</summary>
        public ServicePrincipalSettings? Authentication { get; set; }

        /// <summary>The agents, with the version to run.</summary>
        public IReadOnlyDictionary<string, string> Agents { get; internal set; } = new Dictionary<string, string>();

        internal bool InOtherProject => Endpoint is not null;

        /// <summary>Reads the agents: the keys binding can't map, as they're names chosen by the app.</summary>
        internal void ReadAgents(Microsoft.Extensions.Configuration.IConfigurationSection section)
            => Agents = section.GetChildren()
                .Where(static child => !Reserved.Contains(child.Key, StringComparer.OrdinalIgnoreCase) && child.Value is not null)
                .ToDictionary(static child => child.Key, static child => child.Value!);
    }

    public sealed class McpServerSettings
    {
        public string? ServerUri { get; set; }

        /// <summary>The token scope, e.g. "api://school-performance/.default".</summary>
        public string? Scope { get; set; }

        /// <summary>Optional: a service principal for this server only, possibly in another tenant. Unset: the default.</summary>
        public ServicePrincipalSettings? Authentication { get; set; }

        /// <summary>Required: every tool this app may use from the server.</summary>
        public List<string> AllowedToolNames { get; set; } = [];

        public TimeSpan ToolListCacheDuration { get; set; } = TimeSpan.FromMinutes(5);
    }
}
