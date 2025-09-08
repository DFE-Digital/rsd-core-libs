using System.Globalization;
using System.Text.RegularExpressions;

namespace GovUK.Dfe.CoreLibs.Utilities.Extensions;

/// <summary>
/// The string extensions.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Splits a string up by detecting pascal casing and inserting a space before each capital letter excluding the first.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <returns></returns>
    public static string SplitPascalCase<T>(this T source)
    {
        return source == null
            ? string.Empty
            : Regex.Replace(source.ToString() ?? string.Empty, "[A-Z]", " $0", RegexOptions.None,
                TimeSpan.FromSeconds(1)).Trim();
    }

    /// <summary>
    /// Converts a string to sentence case, ignoring acronyms.
    /// </summary>
    /// <param name="input">The string to convert.</param>
    /// <param name="ignoreAcronyms">Whether or not Acronyms should be detected and ignored. Defaults to true.</param>
    /// <returns>A string</returns>
    public static string ToSentenceCase(this string input, bool ignoreAcronyms = true)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        string[] words = input.Split(' ');
        bool firstNonAcronymCapitalized = false;

        for (int i = 0; i < words.Length; i++)
        {
            // Not an acronym
            if (ignoreAcronyms is false || IsAcronym(words[i]) is false)
            {
                words[i] = words[i].ToLowerInvariant();

                if (firstNonAcronymCapitalized is false)
                {
                    words[i] = char.ToUpperInvariant(words[i][0]) + words[i][1..];
                    firstNonAcronymCapitalized = true;
                }
            }
        }

        return string.Join(' ', words);
    }

    /// <summary>
    /// Extension method that converts "Yes" and "No" strings to bool values.
    /// "Yes" is converted to true and "No" is converted to false.
    /// The comparison is case-insensitive.
    /// If the input string does not match "Yes" or "No", an ArgumentException will be thrown.
    /// </summary>
    public static bool ToBool(this string str)
    {
        return str.ToLower() switch
        {
            "yes" => true,
            "no" => false,
            _ => throw new ArgumentException("The string must be either 'Yes' or 'No'.")
        };
    }

    /// <summary>
    /// Returns true/false if the word is detected as being an acronym
    /// </summary>
    /// <param name="word"></param>
    /// <returns></returns>
    public static bool IsAcronym(string word)
    {
        return !string.IsNullOrEmpty(word) && word.Length >= 2
                                           && char.IsUpper(word[0]) 
                                           && char.IsUpper(word[^1]);
    }

    /// <summary>
    ///  Checks a string to see if it contains exclusively capital letters
    /// </summary>
    /// <param name="word">The string to check.</param>
    /// <returns>A string</returns>
    public static bool IsAllCaps(string word)
    {
        return !string.IsNullOrEmpty(word) && word.All(char.IsUpper);
    }

    /// <summary>
    /// Applies title casing to a string
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string ToTitleCase(this string str)
    {
        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(str.ToLower());
    }

    /// <summary>
    /// Returns true if IsNullOrWhiteSpace == true
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static bool IsEmpty(this string input)
    {
        return string.IsNullOrWhiteSpace(input);
    }

    /// <summary>
    /// Returns true if IsEmpty == false.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static bool IsPresent(this string input)
    {
        return input.IsEmpty() is false;
    }

    /// <summary>
    /// Removes spaces throughout a string AND returns ToLowerInvariant on the string
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string SquishToLower(this string input)
    {
        return input.Replace(" ", "").ToLowerInvariant();
    }


    /// <summary>
    /// Converts the first character of the string to uppercase, and returns the whole string
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>A string.</returns>
    public static string ToFirstUpper(this string input)
    {
        string lowered = input.ToLower();
        return $"{char.ToUpper(lowered[0])}{lowered[1..]}";
    }

    /// <summary>
    /// Hyphenates a string.
    /// </summary>
    /// <param name="str">The str.</param>
    /// <returns>A string.</returns>
    public static string ToHyphenated(this string str)
    {
        var whitespaceRegex = new Regex(@"\s+", RegexOptions.None, TimeSpan.FromSeconds(1));
        return whitespaceRegex.Replace(str, "-");
    }

    /// <summary>
    /// Removes non alphanumeric and white space characters from a string.
    /// </summary>
    /// <param name="str">The str.</param>
    /// <returns>A string.</returns>
    public static string RemoveNonAlphanumericOrWhiteSpace(this string str)
    {
        var notAlphanumericWhiteSpaceOrHyphen = new Regex(@"[^\w\s-]", RegexOptions.None, TimeSpan.FromSeconds(1));
        return notAlphanumericWhiteSpaceOrHyphen.Replace(str, string.Empty);
    }
}
