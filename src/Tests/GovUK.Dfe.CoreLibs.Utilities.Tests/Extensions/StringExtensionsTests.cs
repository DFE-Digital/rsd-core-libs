using GovUK.Dfe.CoreLibs.Utilities.Extensions;
using FluentAssertions;

namespace GovUK.Dfe.CoreLibs.Utilities.Tests.Extensions;

/// <summary>
/// The string extensions tests.
/// </summary>

public class StringExtensionsTests
{
    private const string AcronymSentence = "DAO is CaptuRed CoRrectLy with MAT in DfE";
    private const string LowerCase = "this is lower case";

    [Fact]
    public void ToSentenceCase_WithAcronyms_ReturnsCorrectly()
    {
        AcronymSentence.ToSentenceCase().Should().Be("DAO Is captured correctly with MAT in DfE");
    }

    [Fact]
    public void ToSentenceCase_WithLowerCase_ReturnsCorrectly()
    {
        LowerCase.ToSentenceCase().Should().Be("This is lower case");
    }

    [Fact]
    public void ToSentenceCase_WithAcronyms_When_IgnoreAcronyms_IsTrue_Returns_SentenceCase()
    {
        AcronymSentence.ToSentenceCase(ignoreAcronyms:true).Should().Be("DAO Is captured correctly with MAT in DfE");
    }

    [Fact]
    public void ToSentenceCase_WithAcronyms_When_IgnoreAcronyms_IsFalse_Returns_SentenceCase()
    {
        AcronymSentence.ToSentenceCase(ignoreAcronyms: false).Should().Be("Dao is captured correctly with mat in dfe");
    }

    [Theory]
    [InlineData("")]
    [InlineData(default)]
    [InlineData(" ")]
    public void ToSentenceCase_When_String_IsNullOrWhitespace_Returns_EmptyString(string input)
    {
        input.ToSentenceCase().Should().Be(input);
    }

    [Fact]
    public void All_Acronyms_When_IgnoreAcronyms_True_Returns_Original_String()
    {
        "DAO WTF LOL ROFL BRB".ToSentenceCase().Should().Be("DAO WTF LOL ROFL BRB");
    }

    [Fact]
    public void All_Acronyms_When_IgnoreAcronyms_False_SentenceCases_The_Original_String()
    {
        "DAO WTF LOL ROFL BRB".ToSentenceCase(false)
            .Should().Be("Dao wtf lol rofl brb");
    }

    [Theory]
    [InlineData("MAT", true)]
    [InlineData("TEAM", true)]
    [InlineData("DAO-", false)]
    [InlineData("DfE", true)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData(null, false)]
    public void IsAcronym_ShouldReturnExpectedResult(string word, bool expected)
    {
        // Act
        var result = StringExtensions.IsAcronym(word);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("MAT", true)]
    [InlineData("TEAM", true)]
    [InlineData("Mat", false)]
    [InlineData("Hello", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsAllCaps_ShouldReturnExpectedResult(string word, bool expected)
    {
        // Act
        var result = StringExtensions.IsAllCaps(word);

        // Assert
        result.Should().Be(expected);
    }
    [Fact]
    public void ToBool_Should_ReturnTrue_When_StringIsYes()
    {
        // Arrange
        string yes = "Yes";

        // Act
        bool result = yes.ToBool();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ToBool_Should_ReturnFalse_When_StringIsNo()
    {
        // Arrange
        string no = "No";

        // Act
        bool result = no.ToBool();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ToBool_Should_ThrowArgumentException_When_StringIsNotYesOrNo()
    {
        // Arrange
        string notYesOrNo = "Maybe";

        // Act
        Action act = () => notYesOrNo.ToBool();

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("The string must be either 'Yes' or 'No'.");
    }

    [Fact]
    public void ToFirstUpper_Uppercases_First_Character_And_Returns_Whole_String()
    {
        string input = "this_is_a_test";

        string result = input.ToFirstUpper();
        _ = result.Should().Be("This_is_a_test");
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("All Title Case", "All Title Case")]
    [InlineData("all lower case", "All Lower Case")]
    [InlineData("ALL UPPER CASE", "All Upper Case")]
    [InlineData("a title", "A Title")]
    public void ToTitleCase_GivenString_ShouldConvert(string givenString, string expectedStringAsTitleCase)
    {
        var result = givenString?.ToTitleCase();
        Assert.Equal(expectedStringAsTitleCase, result);
    }

    [Theory]
    [InlineData("some text", "some-text")]
    [InlineData("some    text", "some-text")]
    [InlineData("some\ttext", "some-text")]
    public void ToHyphenated_GivenString_ShouldConvert(string input, string expectedOutput)
    {
        var result = input.ToHyphenated();
        Assert.Equal(expectedOutput, result);
    }

    [Fact]
    public void RemoveNonAlphanumericOrWhiteSpace_Strips_UnwantedCharacters()
    {
        const string text = "some text-with-punctuation_and'numbers99][()";

        var result = text.RemoveNonAlphanumericOrWhiteSpace();

        Assert.Equal("some text-with-punctuation_andnumbers99", result);
    }

    [Theory]
    [InlineData("GoverningBodyResolutiondde880c3-09bb-4940-83a2-e5591bf9a6bb", "Governing Body Resolutiondde880c3-09bb-4940-83a2-e5591bf9a6bb")]
    [InlineData("Consultation4adf7b8f-aaea-41db-aacd-846396989519", "Consultation4adf7b8f-aaea-41db-aacd-846396989519")]
    public void SplitPascalCase_Converts_String(string input, string expectation)
    {
        input.SplitPascalCase().Should().Be(expectation);
    }

    [Theory]
    [InlineData("Sponsored conversion", "sponsoredconversion")]
    [InlineData(" Voluntary conver sion ", "voluntaryconversion")]
    [InlineData("Form a MAT", "formamat")]
    public void SquishToLower_Removes_Spaces_And_Lowercases(string input, string expectation)
    {
        input.SquishToLower().Should().Be(expectation);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData(default, true)]
    [InlineData(" ", true)]
    [InlineData("-", false)]
    public void IsEmpty_Detects_IsNullOrWhitespace(string str, bool expectation)
    {
        str.IsEmpty().Should().Be(expectation);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(default, false)]
    [InlineData(" ", false)]
    [InlineData("-", true)]
    public void IsPresent_Returns_Opposite_Of_IsEmpty(string str, bool expectation)
    {
        str.IsPresent().Should().Be(expectation);
    }
}
