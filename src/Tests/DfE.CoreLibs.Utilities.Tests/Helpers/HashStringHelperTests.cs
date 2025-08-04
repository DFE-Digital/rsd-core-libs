using DfE.CoreLibs.Utilities.Helpers;

namespace DfE.CoreLibs.Utilities.Tests.Helpers
{
    public class HashStringHelperTests
    {
        [Fact]
        public void GenerateHashedCacheKey_ShouldThrowArgumentException_WhenInputIsNullOrEmpty()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => HashStringHelper.GenerateHashedString(string.Empty));
            Assert.Throws<ArgumentException>(() => HashStringHelper.GenerateHashedString((string)null!));
        }

        [Fact]
        public void GenerateHashedCacheKey_ShouldReturnConsistentHash_WhenGivenSameInput()
        {
            // Arrange
            var input = "test-input";

            // Act
            var result1 = HashStringHelper.GenerateHashedString(input);
            var result2 = HashStringHelper.GenerateHashedString(input);

            // Assert
            Assert.Equal(result1, result2);
        }

        [Fact]
        public void GenerateHashedCacheKey_ShouldThrowArgumentException_WhenInputCollectionIsNullOrEmpty()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => HashStringHelper.GenerateHashedString((IEnumerable<string>)null!));
            Assert.Throws<ArgumentException>(() => HashStringHelper.GenerateHashedString(new List<string>()));
        }

        [Fact]
        public void GenerateHashedCacheKey_ShouldReturnDifferentHashes_ForDifferentInputs()
        {
            // Arrange
            var input1 = "input-1";
            var input2 = "input-2";

            // Act
            var result1 = HashStringHelper.GenerateHashedString(input1);
            var result2 = HashStringHelper.GenerateHashedString(input2);

            // Assert
            Assert.NotEqual(result1, result2);
        }

        [Fact]
        public void GenerateHashedCacheKey_ForCollection_ShouldReturnConsistentHash_WhenGivenSameInputs()
        {
            // Arrange
            var inputs = new List<string> { "input-1", "input-2", "input-3" };

            // Act
            var result1 = HashStringHelper.GenerateHashedString(inputs);
            var result2 = HashStringHelper.GenerateHashedString(inputs);

            // Assert
            Assert.Equal(result1, result2);
        }
    }
}
