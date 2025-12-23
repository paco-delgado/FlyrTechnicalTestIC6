using FlyrTech.Core;
using StackExchange.Redis;

namespace FlyrTech.Infrastructure;

/// <summary>
/// Redis implementation of the cache service using StackExchange.Redis
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;

    /// <summary>
    /// Constructor with Redis connection multiplexer injection
    /// </summary>
    /// <param name="connectionMultiplexer">The Redis connection multiplexer</param>
    public RedisCacheService(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _database = _connectionMultiplexer.GetDatabase();
    }

    /// <inheritdoc/>
    public async Task<string?> GetAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        var value = await _database.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    /// <inheritdoc/>
    public async Task SetAsync(string key, string value, TimeSpan? expiration = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        if (expiration.HasValue)
        {
            await _database.StringSetAsync(key, value, expiration.Value);
        }
        else
        {
            await _database.StringSetAsync(key, value);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        return await _database.KeyDeleteAsync(key);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        return await _database.KeyExistsAsync(key);
    }

    /// <inheritdoc/>
    public async Task<long> GetVersionAsync(string versionKey)
    {
        if (string.IsNullOrWhiteSpace(versionKey))
            throw new ArgumentException("Version key cannot be null or empty", nameof(versionKey));

        var value = await _database.StringGetAsync(versionKey);
        
        if (!value.HasValue)
            return 0;

        return long.TryParse(value.ToString(), out var version) ? version : 0;
    }

    /// <inheritdoc/>
    public async Task<bool> TrySetJsonIfVersionMatchesAsync(
        string dataKey,
        string versionKey,
        long expectedVersion,
        string newJson,
        long newVersion)
    {
        if (string.IsNullOrWhiteSpace(dataKey))
            throw new ArgumentException("Data key cannot be null or empty", nameof(dataKey));

        if (string.IsNullOrWhiteSpace(versionKey))
            throw new ArgumentException("Version key cannot be null or empty", nameof(versionKey));

        if (newJson == null)
            throw new ArgumentNullException(nameof(newJson));

        if (expectedVersion < 0)
            throw new ArgumentOutOfRangeException(nameof(expectedVersion));

        if (newVersion != expectedVersion + 1)
            throw new ArgumentException("newVersion must be expectedVersion + 1", nameof(newVersion));

        var tran = _database.CreateTransaction();

        // Condition: versionKey must equal expectedVersion (stored as string)
        tran.AddCondition(Condition.StringEqual(versionKey, expectedVersion.ToString()));

        _ = tran.StringSetAsync(dataKey, newJson);
        _ = tran.StringSetAsync(versionKey, newVersion.ToString());

        return await tran.ExecuteAsync();
    }
}
