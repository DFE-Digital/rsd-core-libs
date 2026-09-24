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
    public const string McpStartupValidationFailed = "MCP tool configuration validation failed during startup for server '{0}'.";
    public const string McpConnectionFailed = "Failed to connect to MCP server '{0}' at {1}.";
    public const string AgentRunFailed = "Agent '{0}' failed.";
    public const string AgentGetOrCreateFailed = "Failed to get or create Foundry agent '{0}'.";
    public const string AgentCreateFailed = "Failed to create Foundry agent '{0}'.";
    public const string AgentDeleteFailed = "Failed to delete Foundry agent '{0}'.";
    public const string AgentPruneVersionsFailed = "Failed to prune versions for Foundry agent '{0}'.";
    public const string AzureSearchQueryFailed = "Azure Search query against '{0}' failed.";
}
