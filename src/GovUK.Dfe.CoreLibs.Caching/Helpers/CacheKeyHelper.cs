using System.Security.Cryptography;
using System.Text;

namespace GovUK.Dfe.CoreLibs.Caching.Helpers
{
    public static class CacheKeyHelper
    {
        /// <summary>
        /// Generates a hashed cache key for any given input string.
        /// </summary>
        /// <param name="input">The input string to be hashed.</param>
        /// <returns>A hashed string that can be used as a cache key.</returns>
        public static string GenerateHashedCacheKey(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentException("Input cannot be null or empty", nameof(input));
            }

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));

            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        /// <summary>
        /// Generates a hashed cache key for a collection of strings by concatenating them.
        /// </summary>
        /// <param name="inputs">A collection of strings to be concatenated and hashed.</param>
        /// <returns>A hashed string that can be used as a cache key.</returns>
        public static string GenerateHashedCacheKey(IEnumerable<string> inputs)
        {
            if (inputs == null || !inputs.Any())
            {
                throw new ArgumentException("Input collection cannot be null or empty", nameof(inputs));
            }

            var concatenatedInput = string.Join(",", inputs);

            return GenerateHashedCacheKey(concatenatedInput);
        }

        /// <summary>
        /// Computes the SHA-256 hash of the provided stream and returns it as an uppercase hexadecimal string.
        /// </summary>
        /// <param name="fileStream">
        /// The stream to compute the hash for. The stream must be readable and positioned
        /// at the beginning of the data to hash.
        /// </param>
        /// <returns>
        /// A 64-character uppercase hexadecimal string representing the SHA-256 hash of the stream’s contents.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="fileStream"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if the stream cannot be read.</exception>
        public static string ComputeSha256(Stream fileStream)
        {
            if (fileStream == null)
                throw new ArgumentNullException(nameof(fileStream), "File stream cannot be null.");

            if (!fileStream.CanRead)
                throw new ArgumentException("File stream must be readable.", nameof(fileStream));

            // Remember the original position if the stream supports seeking.
            long originalPosition = 0;
            if (fileStream.CanSeek)
            {
                originalPosition = fileStream.Position;
                fileStream.Position = 0;
            }

            try
            {
                using var sha = SHA256.Create();
                var hashBytes = sha.ComputeHash(fileStream);

                // Convert the hash bytes to an uppercase hexadecimal string.
                return Convert.ToHexString(hashBytes);
            }
            finally
            {
                // Restore the stream position so callers can continue using it if needed.
                if (fileStream.CanSeek)
                {
                    fileStream.Position = originalPosition;
                }
            }
        }
    }

}
