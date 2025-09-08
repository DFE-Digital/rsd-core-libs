using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace GovUK.Dfe.CoreLibs.Utilities.Helpers
{
    public static class HashStringHelper
    {
        /// <summary>
        /// Generates a hashed string for any given input string.
        /// </summary>
        /// <param name="input">The input string to be hashed.</param>
        /// <returns>A hashed string.</returns>
        public static string GenerateHashedString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentException("Input cannot be null or empty", nameof(input));
            }

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));

            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        /// <summary>
        /// Generates a hashed string for a collection of strings by concatenating them.
        /// </summary>
        /// <param name="inputs">A collection of strings to be concatenated and hashed.</param>
        /// <returns>A hashed string.</returns>
        public static string GenerateHashedString(IEnumerable<string> inputs)
        {
            if (inputs == null || !inputs.Any())
            {
                throw new ArgumentException("Input collection cannot be null or empty", nameof(inputs));
            }

            var concatenatedInput = string.Join(",", inputs);

            return GenerateHashedString(concatenatedInput);
        }
    }
}
