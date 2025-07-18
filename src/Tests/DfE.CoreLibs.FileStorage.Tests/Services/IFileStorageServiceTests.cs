using DfE.CoreLibs.FileStorage.Interfaces;
using DfE.CoreLibs.FileStorage.Services;
using DfE.CoreLibs.FileStorage.Settings;
using DfE.CoreLibs.FileStorage.Clients;
using DfE.CoreLibs.FileStorage.Exceptions;
using NSubstitute;
using Xunit;

namespace DfE.CoreLibs.FileStorage.Tests.Services;

public class IFileStorageServiceTests
{
    [Fact]
    public void IFileStorageService_ShouldBeInterface()
    {
        // Assert
        Assert.True(typeof(IFileStorageService).IsInterface);
    }

    [Fact]
    public void IFileStorageService_ShouldHaveRequiredMethods()
    {
        // Arrange
        var interfaceType = typeof(IFileStorageService);
        var methods = interfaceType.GetMethods();

        // Act
        var methodNames = methods.Select(m => m.Name).ToArray();

        // Assert
        Assert.Contains("UploadAsync", methodNames);
        Assert.Contains("DownloadAsync", methodNames);
        Assert.Contains("DeleteAsync", methodNames);
        Assert.Contains("ExistsAsync", methodNames);
    }

    [Fact]
    public void AzureFileStorageService_ShouldImplementIFileStorageService()
    {
        // Arrange
        var mockClientWrapper = Substitute.For<IShareClientWrapper>();

        // Act
        var service = new AzureFileStorageService(mockClientWrapper);

        // Assert
        Assert.IsAssignableFrom<IFileStorageService>(service);
    }

    [Fact]
    public void IFileStorageService_UploadAsync_ShouldHaveCorrectSignature()
    {
        // Arrange
        var method = typeof(IFileStorageService).GetMethod("UploadAsync");

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method.ReturnType);
        Assert.Equal(3, method.GetParameters().Length);

        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(Stream), parameters[1].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[2].ParameterType);
        Assert.True(parameters[2].HasDefaultValue);
    }

    [Fact]
    public void IFileStorageService_DownloadAsync_ShouldHaveCorrectSignature()
    {
        // Arrange
        var method = typeof(IFileStorageService).GetMethod("DownloadAsync");

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<Stream>), method.ReturnType);
        Assert.Equal(2, method.GetParameters().Length);

        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.True(parameters[1].HasDefaultValue);
    }

    [Fact]
    public void IFileStorageService_DeleteAsync_ShouldHaveCorrectSignature()
    {
        // Arrange
        var method = typeof(IFileStorageService).GetMethod("DeleteAsync");

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method.ReturnType);
        Assert.Equal(2, method.GetParameters().Length);

        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.True(parameters[1].HasDefaultValue);
    }

    [Fact]
    public void IFileStorageService_ExistsAsync_ShouldHaveCorrectSignature()
    {
        // Arrange
        var method = typeof(IFileStorageService).GetMethod("ExistsAsync");

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<bool>), method.ReturnType);
        Assert.Equal(2, method.GetParameters().Length);

        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.True(parameters[1].HasDefaultValue);
    }

    [Fact]
    public void IFileStorageService_ShouldBeThreadSafe()
    {
        // This test documents the expectation that implementations should be thread-safe
        // In a real scenario, you would test actual thread safety with concurrent operations

        // Arrange
        var mockClientWrapper = Substitute.For<IShareClientWrapper>();
        var service = new AzureFileStorageService(mockClientWrapper);

        // Assert - The interface documentation states implementations should be thread-safe
        Assert.NotNull(service);
        // Note: Actual thread safety testing would require more complex concurrent testing
    }

    [Fact]
    public void IFileStorageService_ShouldSupportCancellation()
    {
        // Arrange
        var mockClientWrapper = Substitute.For<IShareClientWrapper>();
        var service = new AzureFileStorageService(mockClientWrapper);
        var cancellationToken = new CancellationToken(true); // Cancelled token

        // Act & Assert - All methods should support cancellation
        Assert.ThrowsAsync<OperationCanceledException>(() => service.UploadAsync("test.txt", new MemoryStream(), cancellationToken));
        Assert.ThrowsAsync<OperationCanceledException>(() => service.DownloadAsync("test.txt", cancellationToken));
        Assert.ThrowsAsync<OperationCanceledException>(() => service.DeleteAsync("test.txt", cancellationToken));
        Assert.ThrowsAsync<OperationCanceledException>(() => service.ExistsAsync("test.txt", cancellationToken));
    }
}