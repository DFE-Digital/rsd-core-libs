using GovUK.Dfe.CoreLibs.FileStorage.Clients;
using GovUK.Dfe.CoreLibs.FileStorage.Settings;
using Xunit;

namespace GovUK.Dfe.CoreLibs.FileStorage.Tests.Clients;

public class AzureShareClientWrapperTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldNotThrow()
    {
        // Arrange
        var connectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test;EndpointSuffix=core.windows.net";
        var shareName = "testshare";

        // Act & Assert
        var wrapper = new AzureShareClientWrapper(connectionString, shareName);
        Assert.NotNull(wrapper);
    }

    [Fact]
    public void Constructor_WithNullConnectionString_ShouldThrowArgumentNullException()
    {
        // Arrange
        var shareName = "testshare";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AzureShareClientWrapper(null!, shareName));
    }

    [Fact]
    public void Constructor_WithNullShareName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test;EndpointSuffix=core.windows.net";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AzureShareClientWrapper(connectionString, null!));
    }

    [Fact]
    public async Task GetFileClientAsync_WithNullPath_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test;EndpointSuffix=core.windows.net";
        var shareName = "testshare";
        var wrapper = new AzureShareClientWrapper(connectionString, shareName);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => wrapper.GetFileClientAsync(null!));
    }

    [Fact]
    public void Constructor_WithInvalidConnectionStringFormat_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidConnectionString = "invalid-format-connection-string";
        var shareName = "testshare";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AzureShareClientWrapper(invalidConnectionString, shareName));
    }

    [Fact]
    public void Constructor_WithInvalidShareNameFormat_ShouldThrowArgumentException()
    {
        // Arrange
        var connectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test;EndpointSuffix=core.windows.net";
        var invalidShareName = "invalid-share-name-with-special-chars!@#";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AzureShareClientWrapper(connectionString, invalidShareName));
    }

    [Fact]
    public void Constructor_WithEmptyConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        var emptyConnectionString = "";
        var shareName = "testshare";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AzureShareClientWrapper(emptyConnectionString, shareName));
    }

    [Fact]
    public void Constructor_WithEmptyShareName_ShouldThrowArgumentException()
    {
        // Arrange
        var connectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test;EndpointSuffix=core.windows.net";
        var emptyShareName = "";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AzureShareClientWrapper(connectionString, emptyShareName));
    }

    [Fact]
    public void Constructor_WithWhitespaceConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        var whitespaceConnectionString = "   ";
        var shareName = "testshare";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AzureShareClientWrapper(whitespaceConnectionString, shareName));
    }

    [Fact]
    public void Constructor_WithWhitespaceShareName_ShouldThrowArgumentException()
    {
        // Arrange
        var connectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test;EndpointSuffix=core.windows.net";
        var whitespaceShareName = "   ";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AzureShareClientWrapper(connectionString, whitespaceShareName));
    }
}
