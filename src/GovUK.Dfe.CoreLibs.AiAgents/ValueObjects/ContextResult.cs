namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>Evidence found for a prompt.</summary>
/// <param name="Text">The evidence, formatted for the prompt.</param>
/// <param name="HasEvidence">Whether anything matched.</param>
public sealed record ContextResult(string Text, bool HasEvidence);
