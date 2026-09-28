using Azure.Core;
using Azure.Identity;

namespace GovUK.Dfe.CoreLibs.AiAgents;

/// <summary>
/// Everything <c>AddAiAgents</c> needs, bound from the <c>"AiAgents"</c> configuration section.
/// </summary>
public sealed class AiAgentsOptions
{
    public const string SectionName = "AiAgents";

    /// <summary>Tags all telemetry and scopes the ephemeral-agent sweep. Defaults to the entry assembly's name.</summary>
    public string? ApplicationName { get; set; }

    public FoundrySettings Foundry { get; set; } = new();

    /// <summary>
    /// The Microsoft Entra ID service principal used for the Foundry project, Azure AI Search and MCP servers.
    /// </summary>
    public ServicePrincipalSettings Authentication { get; set; } = new();

    /// <summary>MCP servers, keyed by a name you choose. Each is called with the service principal.</summary>
    public Dictionary<string, McpServerSettings> McpServers { get; set; } = [];

    public TimeSpan? RunTimeout { get; set; }

    /// <summary>The most agent runs this instance makes at once, across every caller.</summary>
    public int? MaxConcurrency { get; set; }

    /// <summary>A limit on agent runs shared by every instance. Off unless <c>MaxConcurrentRuns</c> is set.</summary>
    public GlobalConcurrencySettings GlobalConcurrency { get; set; } = new();

    public int MaxToolOutputCharacters { get; set; } = 20_000;

    /// <summary>How long a run waits for a free slot (per-instance or global) before failing with a <see cref="TimeoutException"/>.</summary>
    public TimeSpan MaxWaitForRunSlot { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>The most characters of evidence sent with one run; the rest is cut off with a note.</summary>
    public int MaxEvidenceCharacters { get; set; } = 100_000;

    public bool DeleteConversationsAfterRun { get; set; } = true;

    public bool RequireTokenUsageTelemetry { get; set; } = true;

    public bool EnableDriftDetection { get; set; }

    public string? ResponseFormatKey { get; set; }

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

    /// <summary>How often each instance deletes this app's orphaned ephemeral agents. 0 turns the sweep off.</summary>
    public TimeSpan EphemeralAgentSweepInterval { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Agents provisioned by another pipeline, with the version this app runs, e.g. <c>{ "ofsted-agent": "4" }</c>.
    /// This app only runs them. Use <c>"latest"</c> to follow the newest version (not recommended in production).
    /// </summary>
    public Dictionary<string, string> ExternallyManagedAgents { get; set; } = [];

    /// <summary>The version this environment runs, for agents this app builds itself.</summary>
    public Dictionary<string, string> VersionPins { get; set; } = [];

    /// <summary>Versions pruning must keep, per agent, e.g. those other environments are pinned to.</summary>
    public Dictionary<string, List<string>> ProtectedVersions { get; set; } = [];

    /// <summary>Code only: a credential to use instead of the service principal. Set with <c>UseCredential</c>.</summary>
    public TokenCredential? Credential { get; set; }

    internal TokenCredential CreateCredential()
        => Credential ?? new ClientSecretCredential(Authentication.TenantId, Authentication.ClientId, Authentication.ClientSecret,
            new ClientSecretCredentialOptions { AuthorityHost = Authentication.AuthorityHost ?? AzureAuthorityHosts.AzurePublicCloud });

    /// <summary>Lists every missing setting, so startup fails once with the whole picture.</summary>
    internal IReadOnlyList<string> MissingSettings()
    {
        var missing = new List<string>();
        void Require(string? value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                missing.Add($"{SectionName}:{name}");
            }
        }

        Require(Foundry.Endpoint, "Foundry:Endpoint");
        Require(Foundry.DefaultModel, "Foundry:DefaultModel");

        if (KeepLatestVersions is < 2)
        {
            missing.Add($"{SectionName}:KeepLatestVersions (must be at least 2)");
        }

        if (MaxConcurrency is < 1)
        {
            missing.Add($"{SectionName}:MaxConcurrency (must be at least 1)");
        }

        if (AgentCacheDuration < TimeSpan.Zero)
        {
            missing.Add($"{SectionName}:AgentCacheDuration (0 or more)");
        }

        if (EphemeralAgentSweepInterval < TimeSpan.Zero)
        {
            missing.Add($"{SectionName}:EphemeralAgentSweepInterval (0 or more)");
        }

        if (MaxWaitForRunSlot <= TimeSpan.Zero)
        {
            missing.Add($"{SectionName}:MaxWaitForRunSlot (must be positive)");
        }

        if (MaxEvidenceCharacters < 1)
        {
            missing.Add($"{SectionName}:MaxEvidenceCharacters (must be at least 1)");
        }

        missing.AddRange(GlobalConcurrency.Problems().Select(problem => $"{SectionName}:GlobalConcurrency:{problem}"));

        if (Credential is null)
        {
            Require(Authentication.TenantId, "Authentication:TenantId");
            Require(Authentication.ClientId, "Authentication:ClientId");
            Require(Authentication.ClientSecret, "Authentication:ClientSecret");
        }

        // The old list form binds as { "0": "agent-name" }; catch it rather than run an agent called "0".
        if (ExternallyManagedAgents.Keys.Any(static key => key.All(char.IsAsciiDigit)))
        {
            missing.Add($"{SectionName}:ExternallyManagedAgents (use {{ \"agent-name\": \"version\" }}, not a list)");
        }

        // One place per agent's version, so there's nothing to keep in step.
        missing.AddRange(ExternallyManagedAgents.Keys.Where(VersionPins.ContainsKey)
            .Select(agent => $"{SectionName}:VersionPins:{agent} (already versioned under ExternallyManagedAgents; remove one)"));

        foreach (var (key, server) in McpServers)
        {
            Require(server.ServerUri, $"McpServers:{key}:ServerUri");
            Require(server.Scope, $"McpServers:{key}:Scope");
            if (server.AllowedToolNames.Count == 0)
            {
                missing.Add($"{SectionName}:McpServers:{key}:AllowedToolNames");
            }
        }

        return missing;
    }

    /// <summary>Pins from <c>VersionPins</c> plus the versions under <c>ExternallyManagedAgents</c>.</summary>
    internal Factories.AgentVersionPinningOptions ToVersionPinning() => new()
    {
        VersionPins = VersionPins
            .Concat(ExternallyManagedAgents.Where(static agent => !FollowsLatest(agent.Value)))
            .ToDictionary(static pin => pin.Key, static pin => pin.Value),
        ProtectedVersions = ProtectedVersions.ToDictionary(static entry => entry.Key, static entry => (IReadOnlyList<string>)entry.Value),
    };

    private static bool FollowsLatest(string? version)
        => string.IsNullOrWhiteSpace(version) || version.Equals("latest", StringComparison.OrdinalIgnoreCase);

    internal AgentExecutionOptions ToExecutionOptions() => new()
    {
        ApplicationName = ApplicationName,
        RunTimeout = RunTimeout,
        MaxConcurrency = MaxConcurrency,
        MaxToolOutputCharacters = MaxToolOutputCharacters,
        MaxEvidenceCharacters = MaxEvidenceCharacters,
        MaxWaitForRunSlot = MaxWaitForRunSlot,
        DeleteConversationsAfterRun = DeleteConversationsAfterRun,
        RequireTokenUsageTelemetry = RequireTokenUsageTelemetry,
        ValidateAgentToolsAtStartup = ValidateAgentToolsAtStartup,
        EnableDriftDetection = EnableDriftDetection,
        ResponseFormatKey = ResponseFormatKey,
        MaxRetries = MaxRetries,
    };

    public sealed class FoundrySettings
    {
        /// <summary>The Foundry project endpoint, e.g. https://&lt;resource&gt;.services.ai.azure.com/api/projects/&lt;project&gt;.</summary>
        public string? Endpoint { get; set; }

        /// <summary>The model agents use unless their spec says otherwise, e.g. "my-connection/gpt-4o".</summary>
        public string? DefaultModel { get; set; }
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

        /// <summary>An existing blob container used only for run slots. The service principal needs Storage Blob Data Contributor on it.</summary>
        public string? BlobContainerUri { get; set; }

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

    public sealed class McpServerSettings
    {
        public string? ServerUri { get; set; }

        /// <summary>The scope requested for the service principal's token, e.g. "api://school-performance/.default".</summary>
        public string? Scope { get; set; }

        /// <summary>Required: every tool this app may use from the server.</summary>
        public List<string> AllowedToolNames { get; set; } = [];

        public TimeSpan ToolListCacheDuration { get; set; } = TimeSpan.FromMinutes(5);
    }
}
