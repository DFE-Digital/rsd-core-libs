# GovUK.Dfe.CoreLibs.AiAgents

Run Azure AI Foundry agents from .NET: in parallel or in sequence, with tools from your own MCP servers,
evidence from Azure AI Search, structured answers, and token usage for every run.

## Quick start

### 1. Install

```sh
dotnet add package GovUK.Dfe.CoreLibs.AiAgents
dotnet add package Azure.Monitor.OpenTelemetry.AspNetCore   # token usage must be recorded
```

### 2. Configure

```json
{
  "AiAgents": {
    "ApplicationName": "briefing-tool",
    "Foundry": {
      "Endpoint": "https://<resource>.services.ai.azure.com/api/projects/<project>",
      "DefaultModel": "my-connection/gpt-4o"
    },
    "Authentication": { "TenantId": "<tenant>", "ClientId": "<service principal client id>" },
    "PromptFiles": { "SystemPrompts": { "Ofsted": "Prompts/Ofsted.md", "Synthesis": "Prompts/Synthesis.md" } },
    "Search": {
      "Endpoint": "https://<search>.search.windows.net",
      "Indexes": [ { "Name": "ofsted_index", "ContentFields": [ "title", "content" ] } ]
    },
    "McpServers": {
      "school-performance": {
        "ServerUri": "https://mcp.internal.example/mcp",
        "Scope": "api://school-performance/.default",
        "AllowedToolNames": [ "get_performance_data" ]
      }
    },
    "RunTimeout": "00:02:00",
    "MaxConcurrency": 4
  }
}
```

- One Microsoft Entra ID service principal signs in to everything. Supply `Authentication:ClientSecret`
  from Key Vault, never appsettings.json.
- Roles: **Azure AI User** (Foundry project), **Search Index Data Reader** (search), access to each MCP
  server's API, and **Storage Blob Data Contributor** on the run-slot container if you use `GlobalConcurrency`.
- `Search` and `McpServers` are optional. Set prompt files to *Copy to output directory*.
- Later snippets show only keys inside `"AiAgents"`.

### 3. Register

```csharp
builder.Services.AddAiAgents(builder.Configuration, agents => agents.AddAgents(BriefingAgents.All));

// Required: startup fails unless token usage is recorded.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("briefing-tool"))
    .UseAzureMonitor()   // reads APPLICATIONINSIGHTS_CONNECTION_STRING
    .WithTracing(t => t.AddSource(AgentTelemetry.SourceName))
    .WithMetrics(m => m.AddMeter(AgentTelemetry.SourceName));
```

This is the only registration. `AddAgents` also binds each agent's `AllowedTools` to the MCP server that
allows them. Missing settings fail startup in one error.

### 4. Define agents

```csharp
public sealed record OfstedFindings(string Rating, string? InspectionDate, IReadOnlyList<string> Strengths);

public static class BriefingAgents
{
    public static readonly AgentDefinition Ofsted = new("ofsted-agent", SystemPromptType: "Ofsted")
    {
        AllowedTools = ["get_performance_data"],                                // tools it may use
        OutputSchema = AgentOutputSchema.For<OfstedFindings>("ofsted_findings"), // optional structured answer
    };

    public static readonly AgentDefinition Synthesis = new("synthesis-agent", "Synthesis");

    public static AgentDefinition[] All => [Ofsted, Synthesis];
}
```

### 5. Run

Inject `IAgentService`:

```csharp
using Azure.Search.Documents;   // SearchFilter

public sealed class BriefingService(IAgentService agents, IContextRetriever search)
{
    public async Task<string> CreateAsync(string urn, CancellationToken ct)
    {
        var evidence = await search.GetContextAsync("ofsted_index", $"Ofsted inspection {urn}",
            filter: SearchFilter.Create($"urn eq {urn}"), cancellationToken: ct);

        var ofsted = await agents.RunAsync(BriefingAgents.Ofsted, $"Summarise the latest inspection for URN {urn}.",
            evidence: evidence.Text, cancellationToken: ct);
        var findings = ofsted.ReadOutputAs<OfstedFindings>();

        var briefing = await agents.RunAsync(BriefingAgents.Synthesis, $"Write a one-page briefing. Ofsted rating: {findings.Rating}.",
            evidence: ofsted.Output, cancellationToken: ct);

        return briefing.Output!;
    }
}
```

`evidence` is fenced as data the model mustn't take instructions from. Put search results and other
agents' outputs there; keep your instructions in the prompt. Evidence over `MaxEvidenceCharacters`
(100,000) is cut, keeping the start.

## `IAgentService`

| Method | Use for | On failure |
| --- | --- | --- |
| `RunAsync(definition, prompt, evidence)` | One agent | Throws |
| `RunParallelAsync(definitions, resolvePrompt, context, resolveEvidence: ...)` | Independent specialists | Fallback result; others carry on |
| `RunSequentialAsync(definitions, resolvePrompt, initialInput, context)` | A chain, e.g. draft then review | Fallback result; next agent gets the last good output |
| `ProvisionAsync(definitions)` | A provisioning job (see [Centrally managed agents](#centrally-managed-agents)) | Throws |

- Results come back one per definition, in order. Total them with `results.ToTokenUsageSummary()`.
- `shouldSuppress: ex => false` throws instead of returning fallbacks, and cancels the other agents.

## Agents

| `AgentDefinition` | |
| --- | --- |
| `Name` | Its Foundry name. Unique per app if the project is shared. |
| `SystemPromptType` | Its key under `PromptFiles:SystemPrompts` |
| `IsManagedAgent` | `true` (default): kept and reused. `false`: created per run, then deleted. Use `false` only if its instructions or tools change every run; it costs two extra Foundry calls per run. |
| `AllowedTools` | Tools it may use. None if empty. |
| `OutputSchema` | JSON schema for its answer; read with `result.ReadOutputAs<T>()` |

A managed agent gets a new version only when its prompt, tools or schema change. For a different model,
subclass `ManagedAgentProviderBase` and add it with `agents.AddAgentProvider<T>()`.

## Tools

MCP tools need no code: list them in the server's `AllowedToolNames` and the agent's `AllowedTools`.
Other tools: `agents.AddTools("news-agent", new WebSearchToolProvider())`.

- **MCP tools run in your app.** Foundry sees only each tool's name, description and schema, never the
  server or a credential.
- Deny by default: a tool must be in both allow-lists. Checked on every call, pinned versions included.
- Malformed arguments and tool errors go back to the model. Tool calls are never retried.
- A missing allowed tool fails startup; an unreachable server only warns.
- Output is limited to `MaxToolOutputCharacters` (20,000); a run allows 10 tool rounds.

## Search and prompts

```csharp
var evidence = await search.GetContextAsync("ofsted_index", query, size: 10,
    filter: SearchFilter.Create($"urn eq {urn}"), cancellationToken: ct);   // Create escapes values
```

- Set `ContentFields` per index, or every string field (ids, URLs) is sent.
- Results below half the top score are dropped (`Search:MinimumRelevanceFilter`).
- Always filter to one school: relevance alone brings in similarly named ones.

User prompt templates (`PromptFiles:UserPrompts`) are filled with `IPromptTemplateBuilder`:

```csharp
var prompt = promptBuilder.Build("Synthesis", new Dictionary<string, string> { ["Urn"] = urn });   // {{Urn}}
```

Values aren't fenced: put user-typed or retrieved text in `evidence` instead. Missing prompt files throw.

## Environments and versions

- **Separate Foundry project per environment (simplest):** set no versions. Each environment builds its
  agents from its deployed prompt files. Needs write access in production and the same `DefaultModel`
  version in every project.
- **One shared project:** dev creates versions; staging and production pin the tested one:
  `"VersionPins": { "ofsted-agent": "3" }`. A pinned agent never creates or prunes anything.
  `"EnableDriftDetection": true` warns when a pin is stale.

**Pruning.** `"KeepLatestVersions": 3` makes each new version delete older ones (versions 1–5: keeps 5, 4, 3).
Minimum 2, so a rolling deploy's previous version survives. Pinned and `ProtectedVersions` are never deleted,
e.g. `"ProtectedVersions": { "ofsted-agent": [ "1" ] }` for production. Unset: never prunes.

### Centrally managed agents

1. **Provisioning job:** same setup, then `await agents.ProvisionAsync(BriefingAgents.All)`. Creates or
   reuses each version and returns them.
2. **Each app** lists the agents with the version to run, and needs no prompt files for them:

   ```json
   "ExternallyManagedAgents": { "establishment-agent": "1", "ofsted-agent": "4", "trust-agent": "2" }
   ```

3. Apps keep their MCP servers and `AllowedTools`, because they run the tools. At startup
   (`ValidateAgentToolsAtStartup`) each app checks it can run every tool the version calls.

`"latest"` follows the newest version (dev only). `VersionPins` is for agents the app builds; an agent
can't be in both.

## Telemetry

Startup fails unless the metrics are subscribed (step 3). Tests and local dev only:
`"RequireTokenUsageTelemetry": false`.

| Recorded | What |
| --- | --- |
| `aiagents.tokens` | Input and output tokens per run, including every tool round and failed runs |
| `aiagents.run.duration` | Seconds per run, by outcome |
| `aiagents.orchestration.tokens` | Total tokens per parallel or sequential run, e.g. one briefing |
| `aiagents.run.slot_wait` | Seconds waited for a slot. Rising means the limits are too low. |
| `orchestrate_agents`, `invoke_agent` | Spans for the briefing and each run |

All tagged with `ApplicationName` and agent name. Tokens per app and agent per day:

```kusto
customMetrics
| where name == "aiagents.tokens"
| summarize tokens = sum(valueSum) by cloud_RoleName, agent = tostring(customDimensions["gen_ai.agent.name"]), bin(timestamp, 1d)
```

## Scaling out

- **Concurrency.** Each run holds a slot. `MaxConcurrency` limits runs per instance; `GlobalConcurrency`
  limits them across all instances (blob leases; a dead instance's slots free within a minute). A run
  waits up to `MaxWaitForRunSlot` (2 minutes), then throws `TimeoutException`.

  ```json
  "GlobalConcurrency": { "MaxConcurrentRuns": 20, "BlobContainerUri": "https://<account>.blob.core.windows.net/aiagents-run-slots" }
  ```

  Apps sharing a container share the limit. For Redis, implement `IRunSlotStore` and register it.
- **Caching.** A resolved version is reused for `AgentCacheDuration` (30 seconds; `0` turns it off).
- **Orphans.** Each instance deletes this app's leftover ephemeral agents every `EphemeralAgentSweepInterval`
  (30 minutes), only once they're older than any run can be. Set `RunTimeout` and keep `ApplicationName`
  unique per app.
- Instances share no state; racing deletes and duplicate versions are handled.

## Production checklist

- [ ] `ApplicationName` set, and the same name in `AddService(...)`.
- [ ] Secret from Key Vault (or a managed identity), with the roles above.
- [ ] Telemetry arriving in Application Insights.
- [ ] Shared project: agents pinned in staging and production, and those pins in `ProtectedVersions`.
- [ ] `KeepLatestVersions` wherever versions are created.
- [ ] Both tool allow-lists set; `ContentFields` and a school `filter` on every search.
- [ ] `RunTimeout`, `MaxConcurrency` and `GlobalConcurrency` sized to your Foundry quota.
- [ ] `IsManagedAgent: false` only where the definition changes per run.

## Options

All under `AiAgents`:

| Setting | Default | |
| --- | --- | --- |
| `ApplicationName` | Entry assembly | Telemetry tag and orphan-sweep scope |
| `RunTimeout` | None | Longest one run may take |
| `MaxConcurrency` | None | Runs at once per instance |
| `GlobalConcurrency` | Off | `MaxConcurrentRuns` across instances, `BlobContainerUri` for slots |
| `MaxWaitForRunSlot` | 2 min | Longest a run waits for a slot |
| `AgentCacheDuration` | 30 s | Reuse of a resolved version; `0` = off |
| `EphemeralAgentSweepInterval` | 30 min | Orphan sweep; `0` = off |
| `MaxEvidenceCharacters` | 100000 | Longer evidence is cut |
| `MaxToolOutputCharacters` | 20000 | Longer tool output is cut |
| `DeleteConversationsAfterRun` | `true` | Keeps prompts and evidence out of Foundry |
| `RequireTokenUsageTelemetry` | `true` | Startup fails without token metrics |
| `EnableDriftDetection` | `false` | Warns about stale pins |
| `ValidateAgentToolsAtStartup` | `true` | Fails if a pinned or external agent's tools can't run here |
| `KeepLatestVersions` | None | Versions kept on each new one (min 2); unset = never prune |
| `VersionPins` / `ProtectedVersions` | None | Versions this environment runs / other environments need kept |
| `ExternallyManagedAgents` | None | Agents from a provisioning job, with the version (or `"latest"`) |
| `ResponseFormatKey` | None | System prompt appended to every agent |
| `MaxRetries` | 3 | Foundry client retries (a retried call may be billed twice) |

Managed identity: `agents.UseCredential(new ManagedIdentityCredential())` and drop `Authentication`. It's
used for every service, so give it the same roles. Other settings in code: `agents.Configure(o => ...)`.

## Advanced

| Service | For |
| --- | --- |
| `IAgentRunner` | Running an `AgentSpec` or `AgentReference` yourself |
| `IAgentRuntime.Orchestrator` | Orchestrating resolved agents; tools via `AgentToolExecution.CreateResolver` |
| `IAgentFactory` | Version maintenance, e.g. pruning by hand |
| `agents.Services` | Anything else on the same service collection |

## Testing

- Substitute `IAgentService` or `IContextRetriever`.
- Tests that start the host: `"RequireTokenUsageTelemetry": false`.
- `ProductionScenarioTests` shows each pattern end to end.
- Against real Azure: set `AIAGENTS_LIVE_FOUNDRY_ENDPOINT` and `AIAGENTS_LIVE_SLOT_CONTAINER` (an empty
  container) and run the `Live*` tests before relying on a new environment.

Not yet supported: human approval before a tool runs, streaming, built-in health checks.
