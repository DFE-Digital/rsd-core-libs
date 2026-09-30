# GovUK.Dfe.CoreLibs.AiAgents

Run Azure AI Foundry agents from .NET: in parallel or in sequence, with tools from your own MCP servers,
evidence from Azure AI Search, checked answers, and token usage for every run.

## Features

| To… | Do this | See |
| --- | --- | --- |
| Run one agent, or several in parallel or in sequence | Inject `IAgentService` | [Run](#5-run) |
| Give an agent tools from your MCP server | List them in `AllowedToolNames` and the agent's `AllowedTools` | [Tools](#tools) |
| Ground answers in search results | Pass `search.GetContextAsync(...)` as `evidence` | [Search](#search-and-prompts) |
| Get a typed answer | Set `OutputSchema`, read with `ReadOutputAs<T>()` | [Agents](#agents) |
| Reject bad answers | Citations are checked by default; add `Validate` for your own rules | [Check every answer](#check-every-answer) |
| Score answers with an AI judge | `agents.AddQualityEvaluation(judgeModel: "myconnection/gpt-5.1")` | [Judge](#score-answers-with-a-judge) |
| Block a worse agent before release | Run test cases with `IAgentTestRunner` in CI | [Gate releases](#gate-releases) |
| Run tested versions in production | `VersionPins`, or `ExternallyManagedAgents` for a central job | [Versions](#environments-and-versions) |
| Stay within your Foundry quota | `MaxConcurrency`, `GlobalConcurrency` | [Scaling out](#scaling-out) |
| Cap what one run can cost | `MaxOutputTokensPerRun` (default 32,000) | [Scaling out](#scaling-out) |
| Track tokens and cost | Subscribe to the metrics (step 3) | [Telemetry](#telemetry) |
| Use a managed identity or other identities per service | `UseCredential`, or `Authentication` blocks | [Credentials](#credentials) |

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
      "DefaultModel": "myconnection/gpt-5.1"
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

- One Entra ID service principal signs in to everything ([or one per service](#credentials)). Supply
  `Authentication:ClientSecret` from Key Vault, never appsettings.json.
- Roles: **Azure AI User** (Foundry), **Search Index Data Reader** (Search), access to each MCP server's API,
  and **Storage Blob Data Contributor** on the run-slot container if you use `GlobalConcurrency` ([details](#scaling-out)).
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

That's the only registration. MCP tools are bound to each agent from its `AllowedTools`. Missing settings fail
startup in one error.

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

`evidence` is fenced as data the model mustn't obey. Put search results and other agents' output there, and your
instructions in the prompt. Evidence over `MaxEvidenceCharacters` (100,000) is cut, keeping the start.

## `IAgentService`

| Method | Use for | On failure |
| --- | --- | --- |
| `RunAsync(definition, prompt, evidence)` | One agent | Throws |
| `RunParallelAsync(definitions, resolvePrompt, context, resolveEvidence: ...)` | Independent specialists | Fallback result; others carry on |
| `RunSequentialAsync(definitions, resolvePrompt, initialInput, context)` | A chain, e.g. draft then review | Fallback result; next agent gets the last good output |
| `ProvisionAsync(definitions)` | A [provisioning job](#centrally-managed-agents) | Throws |

- Results come back one per definition, in order. Total them with `results.ToTokenUsageSummary()`.
- `shouldSuppress: ex => false` throws instead of returning fallbacks, and cancels the other agents.

## Agents

| `AgentDefinition` | |
| --- | --- |
| `Name` | Its Foundry name; unique per app if the project is shared |
| `SystemPromptType` | Its key under `PromptFiles:SystemPrompts` |
| `IsManagedAgent` | `true` (default): kept and reused. `false`: created and deleted per run (two extra Foundry calls); only if its instructions or tools change every run |
| `AllowedTools` | The only tools it may call; empty (default) means none |
| `OutputSchema` | JSON schema for its answer; read with `result.ReadOutputAs<T>()` |
| `Validate`, `RequireCitations` (default `true`) | [Answer checks](#check-every-answer) |

A managed agent gets a new version only when its prompt, tools or schema change. For another model, subclass
`ManagedAgentProviderBase` and add it with `agents.AddAgentProvider<T>()`.

## Tools

MCP tools need no code: list them in the server's `AllowedToolNames` and the agent's `AllowedTools`. Other tools:
`agents.AddTools("news-agent", new WebSearchToolProvider())`.

- **MCP tools run in your app.** Foundry sees only each tool's name, description and schema. The library gets each
  server's token from your credential for its `Scope`, and caches and refreshes it.
- A tool must be in both allow-lists, checked on every call.
- Tool output is cut at `MaxToolOutputCharacters` (20,000) and fenced like evidence (`FenceToolOutput`).
- Malformed arguments and tool errors go back to the model. Calls are never retried; a run allows 10 tool rounds.
- A missing allowed tool fails startup; an unreachable server only warns.

## Search and prompts

```csharp
var evidence = await search.GetContextAsync("ofsted_index", query, size: 10,
    filter: SearchFilter.Create($"urn eq {urn}"), cancellationToken: ct);   // Create escapes values
```

- Set `ContentFields` per index, or every string field (ids, URLs) is sent.
- Results below half the top score are dropped (`Search:MinimumRelevanceFilter`).
- Always filter to one school: relevance alone brings in similarly named ones.

Fill user prompt templates (`PromptFiles:UserPrompts`) with `IPromptTemplateBuilder`:

```csharp
var prompt = promptBuilder.Build("Synthesis", new Dictionary<string, string> { ["Urn"] = urn });   // {{Urn}}
```

Values aren't fenced: put user-typed or retrieved text in `evidence`. Missing prompt files throw.

## Environments and versions

- **One Foundry project per environment (simplest):** set no versions. Each environment builds its agents from its
  deployed prompts. Needs write access in production and the same model version in every project.
- **One shared project:** dev creates versions; staging and production pin the tested one with
  `"VersionPins": { "ofsted-agent": "3" }`. A pinned agent never creates or prunes. `"EnableDriftDetection": true`
  warns when a pin is stale.
- **Either way, pin the model:** a version fixes the prompt and tools, not the model behind the deployment. Use a
  fixed model version in production; each result's `Model` shows which answered.

**Pruning.** `"KeepLatestVersions": 3` deletes older versions on each new one (of 1–5, keeps 5, 4, 3). Minimum 2,
so a rolling deploy's previous version survives. Pins and `ProtectedVersions` (e.g. `{ "ofsted-agent": [ "1" ] }`,
production's version) are never deleted. Unset: never prunes.

### Centrally managed agents

1. A **provisioning job** with the same setup calls `await agents.ProvisionAsync(BriefingAgents.All)`, which creates
   or reuses each version and returns them.
2. **Each app** lists the agents with their version, and needs no prompt files for them:

   ```json
   "ExternallyManagedAgents": { "establishment-agent": "1", "ofsted-agent": "4", "trust-agent": "2" }
   ```

   In another Foundry project, add its `Endpoint` and optionally `Authentication` (unset: this app's Foundry
   credential). They then run there; all must be in that one project:

   ```json
   "ExternallyManagedAgents": {
     "Endpoint": "https://<central>.services.ai.azure.com/api/projects/<project>",
     "Authentication": { "TenantId": "<tenant>", "ClientId": "<client id>" },
     "ofsted-agent": "4"
   }
   ```

3. Apps keep their MCP servers and `AllowedTools`, because they run the tools. At startup each app checks it can
   run every tool the version calls (`ValidateAgentToolsAtStartup`).

To add an agent, add its line here and its `AgentDefinition` (same name) to `AddAgents`. `"latest"` follows the
newest version (dev only). `VersionPins` is for agents the app builds; an agent can't be in both.

## Answer quality

### Check every answer

By default (`RequireCitations`), an agent given numbered evidence (e.g. search results) must cite it as
`[Evidence n]`, and only evidence that exists. Unnumbered evidence isn't checked. Set `RequireCitations = false` for an
answer that can't carry citations, e.g. a schema with no text fields. `Validate` adds your own check: it returns why an
answer is wrong, or null. A failed check is sent back once in the same conversation; if it fails again, the run fails.

```csharp
public static readonly AgentDefinition Ofsted = new("ofsted-agent", "Ofsted")
{
    OutputSchema = AgentOutputSchema.For<OfstedFindings>("ofsted_findings"),   // Strengths carry the citations
    Validate = result => result.ReadOutputAs<OfstedFindings>().Rating is "Outstanding" or "Good" or "Requires improvement" or "Inadequate"
        ? null : "Rating must be an Ofsted grade.",
};
```

### Score answers with a judge

**Purpose:** checks can't tell a well-cited answer that's still wrong or off-topic. A judge model scores each answer
1–5 for **groundedness** (supported by the evidence) and **relevance** (answers the prompt), so you can see quality
drop after a prompt or model change.

1. Register a judge: any model in your Foundry project.

   ```csharp
   agents.AddQualityEvaluation(judgeModel: "myconnection/gpt-5.1", sampleRate: 0.05);
   ```

2. Live runs: 5% are scored in the background, without slowing them, as `aiagents.quality.score` by agent, version
   and metric. Use `sampleRate: 0` to score only release-gate tests.
3. Release gate: the same judge scores each test case (next section).

- Cost: one judge call per scored answer; the sample rate sets how many.
- Reasoning models work: the judge sends only the model and messages.
- A metric the judge couldn't score is logged with the reason.
- For other metrics pass your own `IEvaluator` as `evaluator`, or your own judge through the
  `Func<IServiceProvider, IAgentRunEvaluator>` overload. Don't add Microsoft.Extensions.AI.OpenAI: its versions
  conflict with this library's.

### Gate releases

**Purpose:** stop a worse agent reaching production. Before a new version is published, CI runs fixed test cases
against it and fails the build if facts are wrong, scores are too low, or scores fell since the last release.

Keep test cases per agent as JSON (`prompt`, `evidence`, `mustMention`, `mustNotMention`) and run them in the
provisioning job. They test an ephemeral copy, so a failed gate publishes nothing (`AgentTestTarget.Deployed` tests
the running version). Save a passing report as the next `baseline.json`.

```csharp
var cases = await AgentTestCase.LoadAsync("tests/ofsted-agent");
var report = await tests.RunAsync(BriefingAgents.Ofsted, cases);              // IAgentTestRunner
var baseline = JsonSerializer.Deserialize<AgentEvaluationReport>(await File.ReadAllTextAsync("baseline.json"))!;

// Name the expected metrics, so a judge that scored nothing fails the gate.
if (!report.Passed || report.BelowMinimum(3.5, "Groundedness", "Relevance").Any() || report.RegressionsFrom(baseline, tolerance: 0.2).Any())
{
    throw new InvalidOperationException("ofsted-agent got worse; not publishing it.");
}
```

## Telemetry

**Purpose:** know what each app and agent costs, how fast it is, how often it fails, and whether answers are getting
worse. Token usage is billed, so it's always recorded: startup fails unless the metrics are subscribed (step 3). Set
`"RequireTokenUsageTelemetry": false` only for tests and local dev.

| Recorded | What |
| --- | --- |
| `aiagents.tokens` | Input and output tokens per run, including tool rounds and failed runs |
| `aiagents.run.duration` | Seconds per run, by outcome |
| `aiagents.orchestration.tokens` | Total tokens per parallel or sequential run, e.g. one briefing |
| `aiagents.run.slot_wait` | Seconds waited for a slot; rising means the limits are too low |
| `aiagents.quality.score` | Scores of sampled answers, by agent, version and metric |
| `orchestrate_agents`, `invoke_agent` | Spans for the briefing and each run |

All are tagged with `ApplicationName`, agent and model (`gen_ai.response.model`). Tokens per app and agent per day:

```kusto
customMetrics
| where name == "aiagents.tokens"
| summarize tokens = sum(valueSum) by cloud_RoleName, agent = tostring(customDimensions["gen_ai.agent.name"]), bin(timestamp, 1d)
```

## Scaling out

- **Concurrency.** Each run holds a slot: `MaxConcurrency` per instance, `GlobalConcurrency` across instances (blob
  leases; a dead instance's slots free within a minute). A run waits up to `MaxWaitForRunSlot`, then throws
  `TimeoutException`. Apps sharing a container share the limit; for Redis, implement `IRunSlotStore`.

  ```json
  "GlobalConcurrency": { "MaxConcurrentRuns": 20, "BlobContainerUri": "https://<account>.blob.core.windows.net/aiagents-run-slots" }
  ```

  - **You supply** the storage account and the container URI. Use a container only for run slots.
  - **Role:** the identity in `Authentication` (or `GlobalConcurrency:Authentication`) needs **Storage Blob Data
    Contributor**: on the account if the library should create the container, or, for least privilege, on the
    container only, once you've created it.
  - **At startup** the library creates the container if it's missing (private, no public access), then checks it can
    write and lease a blob. A missing role fails startup and names the role; an unreachable account only warns.
  - **Secure access:** HTTPS and Entra ID only. A URI with `http` or a SAS token fails startup; no keys or connection
    strings are used.

- **Cost per run.** `MaxOutputTokensPerRun` (32,000) caps the output tokens one run may use, across tool rounds and
  the retry; reasoning tokens count. Each response is capped at what's left, and a run that uses it all fails.
- **Caching.** A resolved version is reused for `AgentCacheDuration` (30 seconds; `0` turns it off).
- **Orphans.** Ephemeral agents left by a crash stay until deleted. Schedule
  `IAgentRuntime.DeleteOrphanedEphemeralAgentsAsync(ct)`, or add `agents.AddEphemeralAgentSweep()` (every 30 minutes).
  Only this app's agents older than any run are deleted; safe on every instance. Keep `ApplicationName` unique.
- Instances share no state; racing deletes and duplicate versions are handled.

## Production checklist

- [ ] `ApplicationName` set, and the same name in `AddService(...)`.
- [ ] Secrets from Key Vault (or managed identities), with the roles above.
- [ ] Telemetry arriving in Application Insights.
- [ ] Shared project: agents pinned in staging and production, and those pins in `ProtectedVersions`.
- [ ] `KeepLatestVersions` wherever versions are created.
- [ ] Both tool allow-lists; `ContentFields` and a school `filter` on every search.
- [ ] `RunTimeout`, `MaxOutputTokensPerRun`, `MaxConcurrency` and `GlobalConcurrency` sized to your Foundry quota.
- [ ] A fixed model version, and a release gate for important agents.
- [ ] Ephemeral agents only where the definition changes per run, with orphan clean-up scheduled.

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
| `MaxEvidenceCharacters` | 100000 | Longer evidence is cut |
| `MaxToolOutputCharacters` | 20000 | Longer tool output is cut |
| `MaxOutputTokensPerRun` | 32000 | Output tokens one run may use (min 16); then it fails |
| `FenceToolOutput` | `true` | Fences tool output as data |
| `DeleteConversationsAfterRun` | `true` | Keeps prompts and evidence out of Foundry |
| `RequireTokenUsageTelemetry` | `true` | Startup fails without token metrics |
| `EnableDriftDetection` | `false` | Warns about stale pins |
| `ValidateAgentToolsAtStartup` | `true` | Fails if a pinned or external agent's tools can't run here |
| `KeepLatestVersions` | None | Versions kept on each new one (min 2); unset = never prune |
| `VersionPins` / `ProtectedVersions` | None | Versions this environment runs / other environments need kept |
| `ExternallyManagedAgents` | None | Agents from a provisioning job, with their version (or `"latest"`); optional `Endpoint` and `Authentication` for another project |
| `ResponseFormatKey` / `ResponseFormatExemptPromptTypes` | None | Prompt appended to every agent / types it isn't appended to |
| `MaxRetries` | 3 | Foundry client retries; a retried call may be billed twice |

Change settings in code with `agents.Configure(o => ...)`.

## Credentials

`Authentication` is the default. `Foundry`, `Search`, each MCP server and `GlobalConcurrency` can have their own
`Authentication` block; so can [`ExternallyManagedAgents`](#centrally-managed-agents) in another project. For example,
an MCP server in another tenant:

```json
"McpServers": {
  "school-performance": {
    "ServerUri": "...", "Scope": "api://school-performance/.default", "AllowedToolNames": [ "..." ],
    "Authentication": { "TenantId": "<partner tenant>", "ClientId": "<mcp client id>" }
  }
}
```

Supply each secret from Key Vault, e.g. `AiAgents__McpServers__school-performance__Authentication__ClientSecret`.
For managed identities, set credentials in code:

```csharp
agents.UseCredential(new ManagedIdentityCredential())                     // default
      .UseCredentialFor(AiAgentsService.Search, searchCredential)         // one service
      .UseMcpCredential("school-performance", partnerCredential)          // one MCP server
      .UseExternallyManagedAgentsCredential(centralCredential);           // externally managed agents' project
```

A service uses its code credential, then its own block, then the default. The default is required only if a service
falls back to it. Give each identity only its own service's role.

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
- Against real Azure, set `AIAGENTS_LIVE_FOUNDRY_ENDPOINT` and `AIAGENTS_LIVE_SLOT_CONTAINER` (a container for nothing else)
  and run the `Live*` tests before relying on a new environment.

Not yet supported: human approval before a tool runs, streaming, built-in health checks.
