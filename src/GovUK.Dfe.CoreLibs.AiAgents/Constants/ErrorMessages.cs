namespace GovUK.Dfe.CoreLibs.AiAgents.Constants;

internal static class ErrorMessages
{
    public const string NoAzureSearchInformationFound = "No {0} information found."; 
    public const string NoAzureSearchClientConfigured = "No Azure Search client configured for '{0}'.";
    public const string ToolCallsRequiredNoCallback = "Response {0} paused for tool calls but no resolveToolCalls callback was supplied.";
    public const string MissingToolCallOutput = "No tool output was resolved for call '{0}'.";
    public const string ToolCallRoundLimitExceeded = "Agent '{0}' exceeded the maximum of {1} tool-call rounds in a single run.";
    public const string ResponseDidNotComplete = "Agent '{0}' response {1} did not complete (status '{2}'){3}.";
    public const string McpToolsNotFoundOnServer = "MCP server '{0}' does not expose the following configured tool(s): {1}.";
    public const string AgentNotFound = "Agent '{0}'{1} was not found.";
    public const string AgentVersionNotFound = " version '{0}'";
    public const string PromptFileNotFound = "Prompt file not found";
    public const string PromptFileEmpty = "Prompt file is empty: {0}";
    public const string NoPromptFileConfigured = "No prompt file configured for '{0}'.";
    public const string UnableToGenerateSection = "This section could not be generated due to an error retrieving or analysing evidence.";
    public const string McpTokenResponseDeserializationFailed = "Failed to deserialize the MCP access token response.";
    public const string McpOptionsInvalid = "MCP server '{0}' configuration is invalid; missing or empty: {1}.";
    public const string TokenUsageTelemetryNotConfigured =
        "Token usage for AI agents isn't being recorded. Subscribe to the library's metrics at startup, e.g. " +
        "builder.Services.AddOpenTelemetry().UseAzureMonitor().WithMetrics(m => m.AddMeter(AgentTelemetry.SourceName)). " +
        "To run without it (local development or tests only), set AiAgents:RequireTokenUsageTelemetry to false.";
    public const string NoToolExecutor = "The model called '{0}', but no tool bound to this agent runs it.";
    public const string EphemeralAgentNeedsSpec = "Agent '{0}' is ephemeral, so it must be built by this app; it can't use an externally managed provider.";
    public const string StructuredOutputUnreadable = "The answer from agent '{0}' couldn't be read as {1}. Check the agent succeeded and has a matching OutputSchema.";
    public const string AllowedToolNotOffered = "Agent '{0}' lists AllowedTools ({1}) that none of its tool bindings offers. Bind the provider for them, or remove them from AllowedTools.";
    public const string ToolNotAllowedForAgent = "The model called '{0}', which isn't in this agent's AllowedTools, so it wasn't run.";
    public const string McpToolNotAllowed = "Tool(s) '{0}' aren't in AllowedToolNames for MCP server '{1}', so they can't be given to an agent or run.";
    public const string McpToolCallFailed = "Call to tool '{0}' on MCP server '{1}' failed.";
    public const string McpToolNamesClash = "MCP server '{0}' has tools ({1}) that all become the function name '{2}'. Rename one on the server.";
    public const string DuplicateToolCallOutput = "More than one tool output was resolved for call '{0}'.";
    public const string AgentRunTimedOut = "Agent '{0}' did not finish within {1}.";
    public const string ProvisioningNeedsFactory = "ProvisionAsync needs an IAgentFactory. Register the library with AddAiAgents.";
    public const string AgentToolsNotRunnable = "Agent '{0}' version {1} calls tools this app can't run: {2}. Configure the MCP server that allows them and list them in the agent's AllowedTools, or re-provision the agent.";
    public const string AiAgentsSettingsMissing = "AI agents can't start: these settings are missing or empty: {0}.";
    public const string AzureSearchIndexesInvalid = "Azure Search needs at least one entry under Indexes, each with a unique Name.";
    public const string AzureSearchCredentialMissing = "Azure Search needs either a credential passed to AddAzureSearchContextRetriever, or TenantId, ClientId and ClientSecret in the 'AzureSearch' configuration section.";
    public const string McpStartupValidationFailed = "MCP tool configuration validation failed during startup for server '{0}'.";
    public const string McpConnectionFailed = "Failed to connect to MCP server '{0}' at {1}.";
    public const string AgentRunFailed = "Agent '{0}' failed.";
    public const string AgentGetOrCreateFailed = "Failed to get or create Foundry agent '{0}'.";
    public const string AgentCreateFailed = "Failed to create Foundry agent '{0}'.";
    public const string AgentDeleteFailed = "Failed to delete Foundry agent '{0}'.";
    public const string AgentPruneVersionsFailed = "Failed to prune versions for Foundry agent '{0}'.";
    public const string AzureSearchQueryFailed = "Azure Search query against '{0}' failed.";
    public const string DuplicateAgentDefinition = "Agent '{0}' is defined more than once.";
    public const string NoRunSlot = "No agent run slot came free within {0}. Raise MaxConcurrency or GlobalConcurrency:MaxConcurrentRuns, or MaxWaitForRunSlot.";
    public const string RunSlotContainerNotFound = "The run slot container {0} doesn't exist. Create it, or check GlobalConcurrency:BlobContainerUri.";
    public const string McpToolArgumentsInvalid = "The arguments for {0} weren't a valid JSON object. Call it again with arguments matching its input schema.";
}
