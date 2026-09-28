# GovUK.Dfe.CoreLibs.AiAgents

Run Azure AI Foundry agents from .NET: specialists in parallel or in sequence, tools from your own
MCP servers, evidence from Azure AI Search, structured answers, and token usage for every run.

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

- **One Microsoft Entra ID service principal** signs in to the Foundry project, Azure AI Search and
  every MCP server. Supply `AiAgents:Authentication:ClientSecret` from Key Vault or an app setting,
  never from appsettings.json.
- The service principal needs **Azure AI User** on the Foundry project, **Search Index Data Reader**
  on the search service, access to each MCP server's API, and **Storage Blob Data Contributor** on
  the run-slot container if you use a global limit.
- `Search` and `McpServers` are optional. Prompt files must be set to *Copy to output directory*.

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

That's all the registration: `AddAiAgents` wires Foundry, prompts, search and MCP servers from
configuration, and binds each agent's `AllowedTools` to the MCP server that allows them. A missing
setting stops startup with one error listing all of them.

### 4. Define your agents

```csharp
public sealed record OfstedFindings(string Rating, string? InspectionDate, IReadOnlyList<string> Strengths);

public static class BriefingAgents
{
    public static readonly AgentDefinition Ofsted = new("ofsted-agent", SystemPromptType: "Ofsted")
    {
        AllowedTools = ["get_performance_data"],                              // the tools this agent may use
        OutputSchema = AgentOutputSchema.For<OfstedFindings>("ofsted_findings"), // optional structured answer
    };

    public static readonly AgentDefinition Synthesis = new("synthesis-agent", "Synthesis", IsManagedAgent: false);

    public static AgentDefinition[] All => [Ofsted, Synthesis];
}
```

### 5. Run

Inject `IAgentService`. It's the only service most apps need.

```csharp
public sealed class BriefingService(IAgentService agents, IContextRetriever search)
{
    public async Task<string> CreateAsync(string urn, CancellationToken ct)
    {
        var evidence = await search.GetContextAsync("ofsted_index", $"Ofsted inspection {urn}", cancellationToken: ct);

        var ofsted = await agents.RunAsync(BriefingAgents.Ofsted, $"Summarise the latest inspection for URN {urn}.",
            evidence: evidence.Text, cancellationToken: ct);
        OfstedFindings findings = ofsted.ReadOutputAs<OfstedFindings>();

        var briefing = await agents.RunAsync(BriefingAgents.Synthesis, "Write a one-page briefing.",
            evidence: ofsted.Output, cancellationToken: ct);

        return briefing.Output!;
    }
}
```

`evidence` is always sent fenced, as data the model is told not to take instructions from. Put
search results, documents and other agents' outputs there, and keep your own instructions in the prompt.
Evidence over `MaxEvidenceCharacters` (default 100,000, about 25,000 tokens) is cut, keeping the start,
with a note telling the model it was cut.

## `IAgentService`

| Method | Use for | On failure |
| --- | --- | --- |
| `RunAsync(definition, prompt, evidence)` | One agent | Throws |
| `RunParallelAsync(definitions, resolvePrompt, context, resolveEvidence: ...)` | Independent specialists side by side | Returns a fallback result; the others carry on |
| `RunSequentialAsync(definitions, resolvePrompt, initialInput, context)` | A chain, e.g. draft then review | Returns a fallback result; the next agent gets the last good output |

- Parallel and sequential runs return one result per definition, in order.
- In a sequential run, each agent receives the previous output as fenced evidence, never in its prompt.
- Pass `shouldSuppress: ex => false` to throw instead of returning fallbacks. The other agents are
  then cancelled at once.
- Add up a briefing's tokens with `results.ToTokenUsageSummary()`.

## Agents

| `AgentDefinition` | Meaning |
| --- | --- |
| `Name` | Its name in Foundry. Keep it unique to your app if the project is shared. |
| `SystemPromptType` | Its key under `PromptFiles:SystemPrompts` |
| `IsManagedAgent` | `true` (default): kept in Foundry and reused. `false`: created for one run, then deleted. |
| `AllowedTools` | The tools it may use. None if empty. |
| `OutputSchema` | Optional JSON schema for its answer. Read it back with `result.ReadOutputAs<T>()`. |

A managed agent gets a new Foundry version when its prompt file, tools or schema change; otherwise
the existing version is reused. Need a different model or a hand-built spec? Subclass
`ManagedAgentProviderBase` and add it with `agents.AddAgentProvider<T>()`. The definition's
`AllowedTools` and `OutputSchema` still apply. For an agent created by another pipeline, see
[Centrally managed agents](#centrally-managed-agents).

## Tools

MCP tools need no code: list them in the server's `AllowedToolNames` and the agent's `AllowedTools`.
Add other tools in `AddAiAgents`:

```csharp
agents.AddTools("news-agent", new WebSearchToolProvider());
```

**MCP tools run in your app.** Foundry only sees each tool's name, description and input schema.
When the model calls one, your app calls the MCP server as the service principal and returns the
result. Foundry never contacts the server or holds a credential, so the server can stay private.

- Deny by default at two levels: the server's `AllowedToolNames` and the agent's `AllowedTools`.
  Both are checked on every call, including for pinned versions.
- Built-in Foundry tools such as web search aren't affected by `AllowedTools`.
- A dropped MCP connection reconnects on the next call. Tool calls are never retried automatically.
- Malformed arguments and tool errors go back to the model so it can correct itself.
- At startup, an allowed tool the server lacks fails startup. An unreachable server only logs a
  warning, so an outage can't stop instances scaling out.
- Tool output is limited to 20,000 characters (`MaxToolOutputCharacters`); a run allows 10 tool rounds.

## Evidence from Azure AI Search

```csharp
var evidence = await search.GetContextAsync("ofsted_index", query, size: 10,
    filter: SearchFilter.Create($"urn eq {urn}"), cancellationToken: ct);   // Create escapes the value
// evidence.Text: relevance-filtered results · evidence.HasEvidence: false when nothing matched
```

- List each index once under `Search:Indexes`, with the `ContentFields` to send to the model. With
  no `ContentFields`, every string field is sent, including ids and URLs.
- Results scoring below half the top score are dropped (`Search:MinimumRelevanceFilter`, default 0.5).
- Use `filter` to keep evidence to one school. Relevance alone can bring in similarly named schools.

## Prompts

```csharp
var prompt = promptBuilder.Build("Synthesis", new Dictionary<string, string> { ["SchoolName"] = name });  // {{SchoolName}}
```

User prompt templates go under `PromptFiles:UserPrompts`. A missing or empty prompt file throws
rather than being replaced with other text.

## Environments and versions

Dev creates versions freely. Pin staging and production to the version you tested:

```json
{ "AiAgents": { "VersionPins": { "ofsted-agent": "3" } } }
```

- A pinned agent runs exactly that version and never creates or prunes anything, so production needs
  only read access.
- `"EnableDriftDetection": true` logs a warning at startup when a pin is out of date.
- During a rolling deploy, old and new instances each keep reusing their own version. They don't
  create new ones back and forth.

**Pruning.** To stop versions piling up, set `"KeepLatestVersions": 3` wherever versions are created
(dev, or the provisioning job). Each new version then deletes the older ones: with versions 1 to 5,
it keeps 5, 4 and 3 and deletes 1 and 2. The minimum is 2, so instances still on the previous version
during a rolling deploy keep it. Some versions are never deleted:

- this environment's pin;
- anything listed under `ProtectedVersions`, i.e. what other environments still run. For example,
  `"ProtectedVersions": { "ofsted-agent": [ "1" ] }` keeps version 1 alive for production.

Leave `KeepLatestVersions` unset (the default) to never prune. You can also call
`IAgentFactory.PruneVersionsAsync(name, keep)` yourself; it does nothing for a pinned agent.

### Centrally managed agents

To have one pipeline own the agents and let apps only use them:

1. **Provisioning job.** Same setup as an app, then `await agents.ProvisionAsync(BriefingAgents.All)`.
   It creates or reuses each managed agent's version, runs nothing, and returns the versions to pin.
2. **Each app.** List each agent with the version to run. The app needs no prompt files for them.
3. **The app keeps its MCP servers and `AllowedTools`**, because it runs the tools itself. At startup
   (`ValidateAgentToolsAtStartup`, on by default) it checks it can run every tool the deployed version
   calls. If it can't, startup fails with one error listing each gap.

```json
"ExternallyManagedAgents": { "establishment-agent": "1", "ofsted-agent": "4", "trust-agent": "2" }
```

Use `"latest"` to follow the newest version (fine in dev, not in production). `VersionPins` is only
for agents the app builds itself, and an agent can't be listed in both.

## Token usage and telemetry

Startup fails unless the library's metrics are subscribed (step 3). For local development and tests
only, set `"RequireTokenUsageTelemetry": false`.

| Recorded | What |
| --- | --- |
| `aiagents.tokens` | Input and output tokens per agent run |
| `aiagents.run.duration` | Seconds per run, by outcome |
| `aiagents.orchestration.tokens` | Total tokens for one parallel or sequential run, e.g. one briefing |
| `aiagents.run.slot_wait` | Seconds runs waited for a slot. Rising values mean the limits are too low. |
| `orchestrate_agents`, `invoke_agent` | Spans for the briefing and each agent run under it |

- Everything is tagged with your `ApplicationName` and the agent name.
- Tokens cover every tool-call round, and failed runs report what they used.
- The source name is the same in every app. Each app's own OpenTelemetry setup decides where its
  data goes, and `AddService(...)` tells apps apart.

Tokens per app and agent per day:

```kusto
customMetrics
| where name == "aiagents.tokens"
| summarize tokens = sum(valueSum) by cloud_RoleName, agent = tostring(customDimensions["gen_ai.agent.name"]), bin(timestamp, 1d)
```

## Production checklist

- [ ] `ApplicationName` set, and the same name in `AddService(...)`.
- [ ] Service principal secret supplied from Key Vault, with the roles above.
- [ ] Telemetry arrives in Application Insights. The startup check confirms a subscription, not delivery.
- [ ] Every managed agent pinned in staging and production.
- [ ] `KeepLatestVersions` set where versions are created, with production's pins in `ProtectedVersions`.
- [ ] `AllowedToolNames` on every MCP server and `AllowedTools` on every agent that uses tools.
- [ ] `ContentFields` set for every search index.
- [ ] `RunTimeout` and `MaxConcurrency` set, and `GlobalConcurrency` sized to your Foundry quota.
- [ ] A scheduled job runs `IAgentRuntime.DeleteOrphanedEphemeralAgentsAsync(TimeSpan.FromHours(2))`.
      It only deletes this app's agents and is safe on every instance.

### Scaling out

Instances share no in-process state. Deletes tolerate another instance getting there first, and
duplicate versions from instances racing are reused rather than multiplied.

**Concurrency.** Every agent run holds a slot while it runs. Extra runs wait, for up to
`MaxWaitForRunSlot` (default 2 minutes), then fail with a `TimeoutException`.

- `MaxConcurrency`: runs at once on one instance, shared by every caller.
- `GlobalConcurrency`: runs at once across every instance, so scaling out can't exceed your Foundry
  quota. Slots are blob leases in a container you create. If an instance dies, its slots free
  themselves within a minute.

```json
"GlobalConcurrency": { "MaxConcurrentRuns": 20, "BlobContainerUri": "https://<account>.blob.core.windows.net/aiagents-run-slots" }
```

To share one limit between apps, point them at the same container. To use Redis or another store,
implement `IRunSlotStore` and register it with `agents.Services.AddSingleton<IRunSlotStore>(...)`.

**Caching.** Each instance reuses a resolved agent version for `AgentCacheDuration` (default 30
seconds) rather than asking Foundry on every run. Set it to `0` to turn caching off. A version deleted
by another instance can be used for up to that long; keeping `KeepLatestVersions` at 2 or more
makes this practically impossible.

## Options

All under `AiAgents`:

| Setting | Default | |
| --- | --- | --- |
| `ApplicationName` | Entry assembly name | Tags telemetry and scopes the orphan sweep |
| `RunTimeout` | None | Longest a single run may take |
| `MaxConcurrency` | None | Agent runs at once on this instance |
| `GlobalConcurrency` | Off | `MaxConcurrentRuns` across all instances, and the `BlobContainerUri` for their slots |
| `MaxWaitForRunSlot` | 2 minutes | Longest a run waits for a slot |
| `AgentCacheDuration` | 30 seconds | How long a resolved version is reused; `0` turns caching off |
| `MaxEvidenceCharacters` | 100000 | Longer evidence is truncated |
| `MaxToolOutputCharacters` | 20000 | Longer tool output is truncated |
| `DeleteConversationsAfterRun` | `true` | Keeps prompts and evidence out of Foundry |
| `RequireTokenUsageTelemetry` | `true` | Startup fails without token metrics |
| `EnableDriftDetection` | `false` | Warns at startup about stale pins |
| `ValidateAgentToolsAtStartup` | `true` | Startup fails if a pinned or externally managed agent calls a tool this app can't run |
| `KeepLatestVersions` | None (never prune) | Newest versions kept when a new one is created; at least 2 |
| `VersionPins` / `ProtectedVersions` | None | Versions this environment runs for agents the app builds / other environments' versions that pruning must keep |
| `ExternallyManagedAgents` | None | Agents another pipeline provisions, with the version to run (or `"latest"`) |
| `ResponseFormatKey` | None | A system prompt appended to every agent's instructions |
| `MaxRetries` | 3 | Foundry client retries. A retried request may be billed twice. |

Using a managed identity instead of the service principal? `agents.UseCredential(new ManagedIdentityCredential())`.
Change any setting in code with `agents.Configure(o => ...)`.

## Advanced

`IAgentService` covers most apps. Reach for these only when you need them:

| Service | For |
| --- | --- |
| `IAgentRunner` | Running an `AgentSpec` or an already-resolved `AgentReference` yourself |
| `IAgentRuntime.Orchestrator` | Orchestrating already-resolved agents. Give each step its tools with `AgentToolExecution.CreateResolver`. |
| `IAgentFactory` | Version maintenance, e.g. pruning by hand |
| `agents.Services` | Anything else, registered on the same service collection |

## Testing

- Substitute `IAgentService` or `IContextRetriever` in your own tests.
- The library's own `ProductionScenarioTests` show the patterns end to end: scaling out, rolling
  deploys, pruning, central provisioning, a full briefing, and several apps sharing one project.
- In tests that start the host, set `"RequireTokenUsageTelemetry": false`.
- To check against a real Foundry project, set `AIAGENTS_LIVE_FOUNDRY_ENDPOINT` and run `LiveFoundrySmokeTests`.

Not yet supported: a human approval step before a tool runs, and streaming responses.
