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

    /// <summary>
    /// Gets the version number from cache
    /// </summary>
    /// <param name="versionKey">The key for the version</param>
    /// <returns>The version number, or 0 if not found</returns>
    Task<long> GetVersionAsync(string versionKey);

    /// <summary>
    /// Atomically sets JSON data and version if the current version matches the expected version
    /// </summary>
    /// <param name="dataKey">The key for the JSON data</param>
    /// <param name="versionKey">The key for the version</param>
    /// <param name="expectedVersion">The expected current version</param>
    /// <param name="newJson">The new JSON data to set</param>
    /// <param name="newVersion">The new version to set</param>
    /// <returns>True if the operation succeeded, false if version mismatch</returns>
    Task<bool> TrySetJsonIfVersionMatchesAsync(
        string dataKey,
        string versionKey,
        long expectedVersion,
        string newJson,
        long newVersion);
}
