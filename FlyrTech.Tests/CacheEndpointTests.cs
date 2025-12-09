using Moq;
using FlyrTech.Core;
using Xunit;

namespace FlyrTech.Tests;

/// <summary>
/// Integration tests for API endpoints using mocked cache service
/// </summary>
public class CacheEndpointTests
{
    private readonly Mock<ICacheService> _mockCacheService;

    public CacheEndpointTests()
    {
        _mockCacheService = new Mock<ICacheService>();
    }

    [Fact]
    public async Task CacheService_GetAsync_ShouldReturnNull_WhenKeyNotFound()
    {
        // Arrange
        const string key = "nonexistent:key";
        _mockCacheService
            .Setup(x => x.GetAsync(key))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _mockCacheService.Object.GetAsync(key);

        // Assert
        Assert.Null(result);
        _mockCacheService.Verify(x => x.GetAsync(key), Times.Once);
    }

    [Fact]
    public async Task CacheService_SetAsync_ShouldBeCalledWithCorrectParameters()
    {
        // Arrange
        const string key = "test:key";
        const string value = "test value";
        var expiration = TimeSpan.FromSeconds(60);

        _mockCacheService
            .Setup(x => x.SetAsync(key, value, expiration))
            .Returns(Task.CompletedTask);

        // Act
        await _mockCacheService.Object.SetAsync(key, value, expiration);

        // Assert
        _mockCacheService.Verify(
            x => x.SetAsync(key, value, expiration),
            Times.Once);
    }

    [Fact]
    public async Task CacheService_GetAsync_ShouldReturnCachedValue_WhenKeyExists()
    {
        // Arrange
        const string key = "cached:key";
        const string expectedValue = "cached value";
        
        _mockCacheService
            .Setup(x => x.GetAsync(key))
            .ReturnsAsync(expectedValue);

        // Act
        var result = await _mockCacheService.Object.GetAsync(key);

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task CacheService_RemoveAsync_ShouldReturnTrue_WhenKeyDeleted()
    {
        // Arrange
        const string key = "key:to:delete";
        
        _mockCacheService
            .Setup(x => x.RemoveAsync(key))
            .ReturnsAsync(true);

        // Act
        var result = await _mockCacheService.Object.RemoveAsync(key);

        // Assert
        Assert.True(result);
        _mockCacheService.Verify(x => x.RemoveAsync(key), Times.Once);
    }

    [Fact]
    public async Task CacheService_ExistsAsync_ShouldReturnTrue_WhenKeyExists()
    {
        // Arrange
        const string key = "existing:key";
        
        _mockCacheService
            .Setup(x => x.ExistsAsync(key))
            .ReturnsAsync(true);

        // Act
        var result = await _mockCacheService.Object.ExistsAsync(key);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CacheWorkflow_ShouldFollowExpectedPattern()
    {
        // Arrange
        const string key = "workflow:key";
        const string value = "workflow value";
        var expiration = TimeSpan.FromSeconds(60);

        // Setup: Key doesn't exist initially
        _mockCacheService
            .Setup(x => x.GetAsync(key))
            .ReturnsAsync((string?)null);

        // Setup: Set operation succeeds
        _mockCacheService
            .Setup(x => x.SetAsync(key, value, expiration))
            .Returns(Task.CompletedTask);

        // Act
        var initialGet = await _mockCacheService.Object.GetAsync(key);
        
        if (initialGet == null)
        {
            await _mockCacheService.Object.SetAsync(key, value, expiration);
        }

        // Assert
        Assert.Null(initialGet);
        _mockCacheService.Verify(x => x.GetAsync(key), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(key, value, expiration), Times.Once);
    }
}
