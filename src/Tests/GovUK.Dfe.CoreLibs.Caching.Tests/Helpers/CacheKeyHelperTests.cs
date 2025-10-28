using System.Text;
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

        [Fact]
        public void ComputeSha256_ShouldThrowArgumentNullException_WhenStreamIsNull()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => CacheKeyHelper.ComputeSha256(null!));
        }

        [Fact]
        public void ComputeSha256_ShouldThrowArgumentException_WhenStreamIsNotReadable()
        {
            // Arrange
            using var stream = new NonReadableStream();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => CacheKeyHelper.ComputeSha256(stream));
            Assert.Contains("readable", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ComputeSha256_ShouldReturnUppercaseHexString_WhenGivenValidStream()
        {
            // Arrange
            var content = "test-content"u8.ToArray();
            using var stream = new MemoryStream(content);

            // Act
            var result = CacheKeyHelper.ComputeSha256(stream);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(64, result.Length); // SHA-256 produces 32 bytes = 64 hex characters
            Assert.Matches("^[A-F0-9]+$", result); // Should be uppercase hexadecimal
        }

        [Fact]
        public void ComputeSha256_ShouldReturnConsistentHash_WhenGivenSameContent()
        {
            // Arrange
            var content = "test-content"u8.ToArray();
            using var stream1 = new MemoryStream(content);
            using var stream2 = new MemoryStream(content);

            // Act
            var result1 = CacheKeyHelper.ComputeSha256(stream1);
            var result2 = CacheKeyHelper.ComputeSha256(stream2);

            // Assert
            Assert.Equal(result1, result2);
        }

        [Fact]
        public void ComputeSha256_ShouldReturnDifferentHashes_ForDifferentContent()
        {
            // Arrange
            var content1 = "content-1"u8.ToArray();
            var content2 = "content-2"u8.ToArray();
            using var stream1 = new MemoryStream(content1);
            using var stream2 = new MemoryStream(content2);

            // Act
            var result1 = CacheKeyHelper.ComputeSha256(stream1);
            var result2 = CacheKeyHelper.ComputeSha256(stream2);

            // Assert
            Assert.NotEqual(result1, result2);
        }

        [Fact]
        public void ComputeSha256_ShouldResetStreamPosition_WhenStreamIsSeekable()
        {
            // Arrange
            var content = "test-content"u8.ToArray();
            using var stream = new MemoryStream(content);
            stream.Position = 5; // Set position to middle of stream
            var originalPosition = stream.Position;

            // Act
            var result = CacheKeyHelper.ComputeSha256(stream);

            // Assert
            Assert.Equal(originalPosition, stream.Position); // Position should be restored
            Assert.NotNull(result);
        }

        [Fact]
        public void ComputeSha256_ShouldHandleEmptyStream()
        {
            // Arrange
            using var stream = new MemoryStream();

            // Act
            var result = CacheKeyHelper.ComputeSha256(stream);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(64, result.Length);
            // SHA-256 of empty input is a known value
            Assert.Equal("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855", result);
        }

        [Fact]
        public void ComputeSha256_ShouldHandleLargeStream()
        {
            // Arrange
            var largeContent = new byte[1024 * 1024]; // 1 MB
            for (int i = 0; i < largeContent.Length; i++)
            {
                largeContent[i] = (byte)(i % 256);
            }
            using var stream = new MemoryStream(largeContent);

            // Act
            var result = CacheKeyHelper.ComputeSha256(stream);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(64, result.Length);
            Assert.Matches("^[A-F0-9]+$", result);
        }

        [Fact]
        public void ComputeSha256_ShouldComputeHashFromStreamStart_WhenStreamPositionIsNotAtBeginning()
        {
            // Arrange
            var content = "test-content"u8.ToArray();
            using var stream = new MemoryStream(content);
            stream.Position = 5; // Position in the middle

            // Act
            var result = CacheKeyHelper.ComputeSha256(stream);

            // Assert - should compute hash of entire content, not from position 5
            using var fullStream = new MemoryStream(content);
            var expectedHash = CacheKeyHelper.ComputeSha256(fullStream);
            Assert.Equal(expectedHash, result);
        }

        [Fact]
        public void ComputeSha256_ShouldHandleNonSeekableStream()
        {
            // Arrange
            var content = "test-content"u8.ToArray();
            using var readOnlyStream = new NonSeekableStream(content);

            // Act
            var result = CacheKeyHelper.ComputeSha256(readOnlyStream);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(64, result.Length);
            Assert.Matches("^[A-F0-9]+$", result);
        }

        [Fact]
        public void ComputeSha256_ShouldProduceKnownHashForKnownInput()
        {
            // Arrange
            var content = Encoding.UTF8.GetBytes("hello world");
            using var stream = new MemoryStream(content);

            // Act
            var result = CacheKeyHelper.ComputeSha256(stream);

            // Assert
            // Known SHA-256 hash of "hello world"
            Assert.Equal("B94D27B9934D3E08A52E52D7DA7DABFAC484EFE37A5380EE9088F7ACE2EFCDE9", result);
        }

        // Helper class to simulate a non-readable stream
        private class NonReadableStream : Stream
        {
            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position 
            { 
                get => throw new NotSupportedException(); 
                set => throw new NotSupportedException(); 
            }
            public override void Flush() => throw new NotSupportedException();
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

        // Helper class to simulate a non-seekable but readable stream
        private class NonSeekableStream : Stream
        {
            private readonly byte[] _data;
            private int _position;

            public NonSeekableStream(byte[] data)
            {
                _data = data;
                _position = 0;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => _data.Length;
            public override long Position 
            { 
                get => _position; 
                set => throw new NotSupportedException(); 
            }
            public override void Flush() { }
            
            public override int Read(byte[] buffer, int offset, int count)
            {
                int bytesToRead = Math.Min(count, _data.Length - _position);
                if (bytesToRead <= 0) return 0;
                
                Array.Copy(_data, _position, buffer, offset, bytesToRead);
                _position += bytesToRead;
                return bytesToRead;
            }
            
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
