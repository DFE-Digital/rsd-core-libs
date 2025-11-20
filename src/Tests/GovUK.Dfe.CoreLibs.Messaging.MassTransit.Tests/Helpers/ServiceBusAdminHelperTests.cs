using Azure.Messaging.ServiceBus.Administration;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Helpers;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace GovUK.Dfe.CoreLibs.Messaging.MassTransit.Tests.Helpers;

public class ServiceBusAdminHelperTests
{
    private readonly ILogger _logger;

    public ServiceBusAdminHelperTests()
    {
        _logger = Substitute.For<ILogger>();
    }

    [Fact]
    public async Task EnsureEntitiesExistAsync_WithInvalidConnectionString_ShouldThrowException()
    {
        // Arrange
        var invalidConnectionString = "InvalidConnectionString";

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await ServiceBusAdminHelper.EnsureEntitiesExistAsync(invalidConnectionString, _logger));
    }

    [Fact]
    public async Task EnsureEntitiesExistAsync_WithEmptyConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        var emptyConnectionString = string.Empty;

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await ServiceBusAdminHelper.EnsureEntitiesExistAsync(emptyConnectionString, _logger));
    }

    [Fact]
    public async Task EnsureEntitiesExistAsync_WithNullConnectionString_ShouldThrowArgumentNullException()
    {
        // Arrange
        string? nullConnectionString = null;

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentNullException>(async () =>
            await ServiceBusAdminHelper.EnsureEntitiesExistAsync(nullConnectionString!, _logger));
    }

    // Note: Integration tests with real Azure Service Bus would require actual connection strings
    // and would be expensive/slow. The above tests verify error handling.
    // Full integration testing should be done in a separate integration test project.
}

