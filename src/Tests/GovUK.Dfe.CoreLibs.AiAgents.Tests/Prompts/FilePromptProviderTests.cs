using GovUK.Dfe.CoreLibs.AiAgents.Prompts;
using GovUK.Dfe.CoreLibs.AiAgents.Prompts.Interfaces;
using NSubstitute;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Prompts;

public sealed class FilePromptProviderTests
{
    private readonly IPromptTemplateStore _systemPrompts = Substitute.For<IPromptTemplateStore>();
    private readonly IPromptTemplateStore _userPrompts = Substitute.For<IPromptTemplateStore>();

    [Fact]
    public void GetSystemPrompt_ReturnsTheTemplate_WhenNoResponseFormatKeyConfigured()
    {
        _systemPrompts.GetTemplate("Ofsted").Returns("Ofsted instructions.");
        var sut = new FilePromptProvider(_systemPrompts, _userPrompts);

        var result = sut.GetSystemPrompt("Ofsted");

        Assert.Equal("Ofsted instructions.", result);
    }

    [Fact]
    public void GetSystemPrompt_AppendsTheResponseFormat_WhenConfiguredAndNotExempt()
    {
        _systemPrompts.GetTemplate("Ofsted").Returns("Ofsted instructions.");
        _systemPrompts.GetTemplate("ResponseFormat").Returns("Standard format.");
        var sut = new FilePromptProvider(_systemPrompts, _userPrompts, responseFormatKey: "ResponseFormat");

        var result = sut.GetSystemPrompt("Ofsted");

        Assert.Equal($"Ofsted instructions.{Environment.NewLine}{Environment.NewLine}Standard format.", result);
    }

    [Fact]
    public void GetSystemPrompt_DoesNotAppendTheResponseFormat_ForAnExemptPromptType()
    {
        _systemPrompts.GetTemplate("Synthesis").Returns("Synthesis instructions.");
        var sut = new FilePromptProvider(_systemPrompts, _userPrompts, responseFormatKey: "ResponseFormat",
            responseFormatExemptPromptTypes: new HashSet<string> { "Synthesis" });

        var result = sut.GetSystemPrompt("Synthesis");

        Assert.Equal("Synthesis instructions.", result);
        _systemPrompts.DidNotReceive().GetTemplate("ResponseFormat");
    }

    [Fact]
    public void GetUserPrompt_DelegatesToTheUserPromptStore()
    {
        _userPrompts.GetTemplate("Synthesis").Returns("Synthesis user prompt.");
        var sut = new FilePromptProvider(_systemPrompts, _userPrompts);

        var result = sut.GetUserPrompt("Synthesis");

        Assert.Equal("Synthesis user prompt.", result);
    }
}
