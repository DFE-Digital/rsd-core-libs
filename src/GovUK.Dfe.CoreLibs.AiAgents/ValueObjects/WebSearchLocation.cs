namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>
/// An approximate location used to bias a web search agent's results (e.g. towards local news or
/// region-specific sources). All parts are optional.
/// </summary>
public sealed record WebSearchLocation(string? Country = null, string? Region = null, string? City = null)
{
    /// <summary>The library's default web search location bias: United Kingdom, England, London.</summary>
    public static readonly WebSearchLocation UnitedKingdom = new(Country: "GB", Region: "England", City: "London");
}
