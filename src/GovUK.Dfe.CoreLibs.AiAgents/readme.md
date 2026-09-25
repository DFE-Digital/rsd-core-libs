# GovUK.Dfe.CoreLibs.AiAgents

Reusable .NET building blocks for Azure AI Foundry agents: creation/versioning, run execution,
multi-agent orchestration, MCP tool discovery, and Azure AI Search (RAG) context. 

Built on Foundry's **Prompt Agent / Conversations+Responses** model (`Azure.AI.Projects`,
`Azure.AI.Projects.Agents`, `Azure.AI.Extensions.OpenAI`). This is
required for Admin-connected model gateway connections (`<connection-name>/<model-name>` as the
model string).

## To install the GovUK.Dfe.CoreLibs.AiAgents Library, use the following command in your .NET project:

```sh
dotnet add package GovUK.Dfe.CoreLibs.AiAgents
```

## Setup

Two calls cover almost everything:

```csharp
services.AddAgentExecution(configuration,
    endpoint: sp => new Uri(foundryEndpoint),
    credential: sp => new ClientSecretCredential(tenantId, clientId, clientSecret),
    optionsFactory: sp => new FoundryAgentFactoryOptions(DefaultModel: "my-connection/gpt-4o"));
    // AgentExecutionOptions defaults: EnableVersionPinning = true, EnableDriftDetection = false
    // - pass options: new AgentExecutionOptions { EnableDriftDetection = true, ... } to change any of them

services.AddAgentContextAndTools(configuration,
    enableAzureSearch: true,                                                                // omit/false to skip RAG
    mcpServers: [new McpServerRegistration("school-performance-mcp", sp => mcpOptions)]);    // omit/[] to skip MCP

services.AddSingleton<IAgentDefinitionProvider, YourAgentDefinitionProvider>(); // your own type - not the library's
```

`IAgentDefinitionProvider` and any `AgentToolBinding`/`IManagedAgentProvider` are always yours to
register — the library has no idea what your agents are called.

Already have an `AIProjectClient`? Call `AddFoundryAgents(optionsFactory)` directly. Need finer
control (custom config section names, `maxRetries`, per-server MCP options) than the two combined
calls give you? Call what they wrap instead: `AddFoundryAgents`, `AddFilePrompts`,
`AddAgentVersionPinning`, `AddPinnedAgentVersionDriftDetection`, `AddAzureSearchContextRetriever`,
`AddMcpClientServices`.

## Agents: managed vs ephemeral vs externally-managed

**Managed** agents persist across calls. `ISpecialistAgentRunner` (below) creates/resolves them
automatically from just an `AgentDefinition` — no provider class needed. Only write an
`IManagedAgentProvider` (subclass `ManagedAgentProviderBase`) when an agent needs a spec the default
can't build — attached tools, a non-default model:

```csharp
public sealed class OfstedAgentProvider(IAgentFactory factory, IAgentRuntime runtime, IPromptProvider prompts)
    : ManagedAgentProviderBase(factory, runtime)
{
    public override string AgentName => "ofsted-agent";
    protected override AgentSpec BuildSpec() => new() { Name = AgentName, Instructions = prompts.GetSystemPrompt("Ofsted") };
}
```

**Ephemeral** agents are created, run once, and deleted — for a one-off task (web search, a
synthesis step). No provider class:

```csharp
var result = await agentRuntime.RunEphemeralAsync(
    new AgentSpec { Name = "briefing-synthesis-agent", Instructions = synthesisInstructions }, prompt, cancellationToken: ct);
```

**Externally-managed** agents are created elsewhere (a separate provisioning repo/pipeline) — this
process only resolves and runs them, never creates:

```csharp
services.AddSingleton<IManagedAgentProvider>(sp =>
    new ExternallyManagedAgentProvider("ofsted-agent", sp.GetRequiredService<IAgentFactory>(), sp.GetRequiredService<IAgentRuntime>()));
```

Only works for managed agents — ephemeral ones are created fresh every call, so their instructions
must always be available locally.

## Version pinning and drift detection

```json
{ "Agents": { "VersionPins": { "ofsted-agent": "3" } } }
```

```csharp
services.AddAgentVersionPinning(configuration);   // unpinned agents float to whatever GetOrCreateAsync matches/creates
services.AddPinnedAgentVersionDriftDetection();   // optional: warns at startup if a pin is stale
```

Drift detection checks two things per pinned agent, only ever logging a warning (never failing
startup): the pinned version's content no longer matches what the code would build today, or a
newer version already exists that the pin hasn't caught up to.

## Orchestration: `ISpecialistAgentRunner`

Runs N agents — managed or ephemeral, mixed freely — from a list of `AgentDefinition`s, with
per-agent failure isolation:

```csharp
var definitions = new[]
{
    new AgentDefinition("ofsted-agent", "Ofsted"),                              // managed (default)
    new AgentDefinition("web-search-agent", "WebSearch", IsManagedAgent: false), // ephemeral
};

var results = await specialistAgentRunner.RunParallelAsync(definitions,
    resolvePrompt: (definition, ct) => BuildPromptAsync(definition, ct),
    context: new AgentContext(), cancellationToken: ct);
// IReadOnlyList<AgentResult> - a failed agent's entry holds a fallback message, not an exception.
```

`RunSequentialAsync(definitions, resolvePrompt, initialInput, context, ...)` runs the same set one
at a time, feeding each output into the next prompt — for a review/refine chain rather than
independent analysis. Pass `shouldSuppress: _ => false` to either call to propagate a specific
failure instead of suppressing it. Need the lower-level API (already-resolved `AgentReference`s,
no auto create/resolve)? Use `IAgentRuntime.Orchestrator` (`AgentOrchestrator`) directly.

## Tools: `AgentToolBinding`

Attach a Foundry tool to an agent by binding a provider to its name — works for managed or
ephemeral, no change to how you call `RunParallelAsync`:

```csharp
services.AddSingleton(new AgentToolBinding("web-search-agent", new WebSearchToolProvider())); // defaults to the UK
```

Multiple bindings for the same name combine their tools. MCP tools work the same way —
`IMcpToolClient` is itself an `IAgentToolProvider`:

```csharp
services.AddSingleton(sp => new AgentToolBinding("ofsted-agent", sp.GetRequiredKeyedService<IMcpToolClient>("school-performance-mcp")));
```

To give different agents different subsets of the same server's tools (instead of everyone sharing
the connection's own `AllowedToolNames`), wrap it in `McpAllowedToolsProvider`:

```csharp
services.AddSingleton(sp => new AgentToolBinding("ofsted-agent",
    new McpAllowedToolsProvider(sp.GetRequiredKeyedService<IMcpToolClient>("school-performance-mcp"), ["get_performance_data"])));
```

`AddMcpClientServices(serverKey, optionsFactory)` can be called once per server; a misconfigured
server fails fast at startup, and secrets are redacted from `ToString()`. `RequireApproval: true`
isn't resolved automatically yet — leave it `false` until it is.

## Azure AI Search (RAG)

```csharp
services.AddAzureSearchContextRetriever(configuration); // binds "AzureSearch", validates required fields on start
```

```csharp
var context = await contextRetriever.GetContextAsync("establishment_index", query, size: 10, ct);
// context.Text: formatted, relevance-filtered evidence · context.HasEvidence: false when nothing matched
```

The scope passed to `GetContextAsync` must be one of the configured `Indexes` names.

## Prompts

```csharp
services.AddFilePrompts(configuration);
```

```json
{ "PromptFiles": { "SystemPrompts": { "Ofsted": "Prompts/Systems/Ofsted.md" }, "UserPrompts": { "Synthesis": "Prompts/Synthesis.md" } } }
```

```csharp
var instructions = promptProvider.GetSystemPrompt("Ofsted");
var prompt = promptBuilder.Build("Synthesis", new Dictionary<string, string> { ["SchoolOrTrustName"] = name });
```

Paths resolve relative to `AppContext.BaseDirectory` — mark them `CopyToOutputDirectory`. A prompt
type with no configured path throws immediately, naming the missing type; a configured path that
can't be read (missing/empty file, permissions) propagates as the underlying I/O exception — neither
case is silently substituted with different prompt content, since that would change an agent's
behaviour with no visible error.

## Notes

- Keys (`SystemPromptType`, `EvidenceCategory`, etc.) are plain `string`, not a closed enum.
- Testing: `AgentAdministrationClient` / `IFoundryConversationClient` / `IMcpToolClient` are all
  directly substitutable with NSubstitute; the underlying `McpClient` SDK type isn't (no public
  constructor) — substitute `IMcpToolClient` instead.
- Not yet supported: MCP tool-call *approval* submission, MCP tool-call *execution* proxying
  (Foundry brokers `tools/call` itself), a router/hand-off orchestration pattern.
