using GovUK.Dfe.CoreLibs.Caching.Helpers;

namespace GovUK.Dfe.CoreLibs.Caching.Tests.Helpers
{
    public class CacheKeyHelperTests
    {
        [Fact]
        public void GenerateHashedCacheKey_ShouldThrowArgumentException_WhenInputIsNullOrEmpty()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => CacheKeyHelper.GenerateHashedCacheKey(string.Empty));
            Assert.Throws<ArgumentException>(() => CacheKeyHelper.GenerateHashedCacheKey((string)null!));
        }

        [Fact]
        public void GenerateHashedCacheKey_ShouldReturnConsistentHash_WhenGivenSameInput()
        {
            // Arrange
            var input = "test-input";

            // Act
            var result1 = CacheKeyHelper.GenerateHashedCacheKey(input);
            var result2 = CacheKeyHelper.GenerateHashedCacheKey(input);

            // Assert
            Assert.Equal(result1, result2);
        }

        [Fact]
        public void GenerateHashedCacheKey_ShouldThrowArgumentException_WhenInputCollectionIsNullOrEmpty()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => CacheKeyHelper.GenerateHashedCacheKey((IEnumerable<string>)null!));
            Assert.Throws<ArgumentException>(() => CacheKeyHelper.GenerateHashedCacheKey(new List<string>()));
        }

        [Fact]
        public void GenerateHashedCacheKey_ShouldReturnDifferentHashes_ForDifferentInputs()
        {
            // Arrange
            var input1 = "input-1";
            var input2 = "input-2";

            // Act
            var result1 = CacheKeyHelper.GenerateHashedCacheKey(input1);
            var result2 = CacheKeyHelper.GenerateHashedCacheKey(input2);

            // Assert
            Assert.NotEqual(result1, result2);
        }

        [Fact]
        public void GenerateHashedCacheKey_ForCollection_ShouldReturnConsistentHash_WhenGivenSameInputs()
        {
            // Arrange
            var inputs = new List<string> { "input-1", "input-2", "input-3" };

            // Act
            var result1 = CacheKeyHelper.GenerateHashedCacheKey(inputs);
            var result2 = CacheKeyHelper.GenerateHashedCacheKey(inputs);

            // Assert
            Assert.Equal(result1, result2);
        }
    }
}
