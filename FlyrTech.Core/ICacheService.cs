namespace FlyrTech.Core;

/// <summary>
/// Interface for distributed cache operations
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a value from the cache by key
    /// </summary>
    /// <param name="key">The cache key</param>
    /// <returns>The cached value or null if not found</returns>
    Task<string?> GetAsync(string key);

    /// <summary>
    /// Sets a value in the cache with expiration
    /// </summary>
    /// <param name="key">The cache key</param>
    /// <param name="value">The value to cache</param>
    /// <param name="expiration">The expiration time</param>
    Task SetAsync(string key, string value, TimeSpan? expiration = null);

    /// <summary>
    /// Removes a value from the cache
    /// </summary>
    /// <param name="key">The cache key</param>
    /// <returns>True if the key was removed, false otherwise</returns>
    Task<bool> RemoveAsync(string key);

    /// <summary>
    /// Checks if a key exists in the cache
    /// </summary>
    /// <param name="key">The cache key</param>
    /// <returns>True if the key exists, false otherwise</returns>
    Task<bool> ExistsAsync(string key);
}
